using System;

namespace BikeTracker
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Table;

    public class FileProcessor
    {
        private CloudTable rawDataTable;

        private CloudTable segmentsTable;

        private int sequence;

        public const double StopPeriodInMinutes = 30;

        public const string RawDataTableName = "bikedata";

        public const string SegmentsTableName = "segments";

        public FileProcessor(string accountName, string accountKey)
        {
            var storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
            var tableClient = storageAccount.CreateCloudTableClient();
            this.rawDataTable = tableClient.GetTableReference(RawDataTableName);
            this.rawDataTable.CreateIfNotExists();
            this.segmentsTable = tableClient.GetTableReference(SegmentsTableName);
            this.segmentsTable.CreateIfNotExists();
        }

        public async Task Process(string fileContents)
        {
            try
            {
                var parsedWithDuplicates = await this.Parse(fileContents);
                var parsed = parsedWithDuplicates.Distinct(new BikeDataComnparer());
                var lines = parsed.OrderBy(p => p.PointTimeStamp).ToList();

                var firstPoint = lines.First();
                var lastPoint = lines.Last();

                var partitionGroups = lines.GroupBy(g => g.PartitionKey);
                foreach (var partition in partitionGroups)
                {
                    try
                    {
                        var total = partition.Count();

                        var partitionList = partition.ToList();
                        const int MaxBatchSize = 100;
                        var batchSize = MaxBatchSize;

                        for (var i = 0; i <= total / MaxBatchSize; i++)
                        {
                            if (i == (total / MaxBatchSize))
                            {
                                batchSize = total - ((total / MaxBatchSize) * MaxBatchSize);
                            }

                            var batch =
                                partitionList.GetRange((i * MaxBatchSize), batchSize);

                            var batchOperation = new TableBatchOperation();
                            foreach (var line in batch)
                            {
                                batchOperation.Insert(line);
                            }

                            await this.rawDataTable.ExecuteBatchAsync(batchOperation);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }

                await this.UpdateSegments(lines, firstPoint, lastPoint);

            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        private IEnumerable<TripSegment> GetSegments(string pointPartitionKey)
        {
            var query =
                new TableQuery<TripSegment>().Where(
                    TableQuery.GenerateFilterCondition(
                        "PartitionKey",
                        QueryComparisons.NotEqual,
                        string.Empty));
            return this.segmentsTable.ExecuteQuery(query).OrderBy(s => s.StartTimeStamp);
        }

        private async Task UpdateSegments(IReadOnlyCollection<BikeData> lines, BikeData firstPoint, BikeData lastPoint)
        {
            var segments = this.GetSegments(firstPoint.PartitionKey).ToList();
            TripSegment segment = null;

            if (!segments.Any())
            {
                segment = await this.AddSegmentAsync(firstPoint);
                segment.EndPartitionAndRow = string.Format("{0},{1}", lastPoint.PartitionKey, lastPoint.RowKey);
                segment.EndTimeStamp = lastPoint.PointTimeStamp;
                segment.EndLatitude = lastPoint.Latitude;
                segment.EndLongitude = lastPoint.Longitude;
                segment.EndLocation = await Geocoder.ReverseLookup(lastPoint.Latitude, lastPoint.Longitude);

            }
            else if (
                segments.Any(
                    s =>
                    s.StartTimeStamp < firstPoint.PointTimeStamp
                    && (firstPoint.PointTimeStamp - s.EndTimeStamp).TotalMinutes < StopPeriodInMinutes))
            {
                segment =
                    segments.FirstOrDefault(
                        s =>
                        s.StartTimeStamp < firstPoint.PointTimeStamp
                        && (firstPoint.PointTimeStamp - s.EndTimeStamp).TotalMinutes < StopPeriodInMinutes);

                if (segment == null)
                {
                    return;
                }
                segment.EndPartitionAndRow = string.Format("{0},{1}", lastPoint.PartitionKey, lastPoint.RowKey);
                segment.EndTimeStamp = lastPoint.PointTimeStamp;
                segment.EndLatitude = lastPoint.Latitude;
                segment.EndLongitude = lastPoint.Longitude;
                segment.EndLocation = await Geocoder.ReverseLookup(lastPoint.Latitude, lastPoint.Longitude);
            }
            else if (segments.Any(s => s.EndTimeStamp > lastPoint.PointTimeStamp
                     && (s.StartTimeStamp - lastPoint.PointTimeStamp).TotalMinutes < StopPeriodInMinutes))
            {
                var existingSegment = await this.DeleteSegment(segment);

                segment = await this.AddSegmentAsync(firstPoint);
                segment.CopyFrom(existingSegment);

            }
            else if (segments.Any(s => (firstPoint.PointTimeStamp - s.EndTimeStamp).TotalMinutes < StopPeriodInMinutes && firstPoint.PointTimeStamp != s.StartTimeStamp)
                       && segments.Any(
                           s => (s.StartTimeStamp - lastPoint.PointTimeStamp).TotalMinutes < StopPeriodInMinutes && lastPoint.PointTimeStamp != s.EndTimeStamp))
            {
                var firstSegment =
                    segments.FirstOrDefault(
                        s => (firstPoint.PointTimeStamp - s.EndTimeStamp).TotalMinutes < StopPeriodInMinutes);
                var secondSegment = segments.FirstOrDefault(
                        s => (s.StartTimeStamp - lastPoint.PointTimeStamp).TotalMinutes < StopPeriodInMinutes);

                if (firstSegment != null && secondSegment != null)
                {
                    var existingFirstSegment = await this.DeleteSegment(firstSegment);
                    var existingSecondSegment = await this.DeleteSegment(secondSegment);
                    existingFirstSegment.UpdateSegment(lines);
                    existingFirstSegment.Merge(existingSecondSegment);
                    segment = await this.AddSegmentAsync(existingFirstSegment);

                    return;
                }
            }
            else
            {
                segment = await this.AddSegmentAsync(firstPoint);
                segment.EndPartitionAndRow = string.Format("{0},{1}", lastPoint.PartitionKey, lastPoint.RowKey);
                segment.EndTimeStamp = lastPoint.PointTimeStamp;
                segment.EndLatitude = lastPoint.Latitude;
                segment.EndLongitude = lastPoint.Longitude;
                segment.EndLocation = await Geocoder.ReverseLookup(lastPoint.Latitude, lastPoint.Longitude);
            }

            if (segment == null)
            {
                throw new ApplicationException("No segments found");
            }

            segment.UpdateSegment(lines);

            var replaceOperation = TableOperation.Replace(segment);
            await this.segmentsTable.ExecuteAsync(replaceOperation);
        }

        private async Task<TripSegment> DeleteSegment(TripSegment segment)
        {
            var retrieveperation = TableOperation.Retrieve<TripSegment>(segment.PartitionKey, segment.RowKey);
            var result = await this.segmentsTable.ExecuteAsync(retrieveperation);
            var existingSegment = (TripSegment)result.Result;

            var deleteOperation = TableOperation.Delete(existingSegment);
            await this.segmentsTable.ExecuteAsync(deleteOperation);
            return existingSegment;
        }

        private async Task<TripSegment> AddSegmentAsync(BikeData firstPoint)
        {
            var newSegment = new TripSegment(firstPoint.PartitionKey, firstPoint.RowKey)
                                 {
                                     StartTimeStamp =
                                         firstPoint.PointTimeStamp,
                                     StartPartitionAndRow = string.Format("{0},{1}", firstPoint.PartitionKey, firstPoint.RowKey),
                                     StartLatitude = firstPoint.Latitude,
                                     StartLongitude = firstPoint.Longitude,
                                     StartLocation = await Geocoder.ReverseLookup(firstPoint.Latitude, firstPoint.Longitude),
                                     StartTimeZone = await TimeZoneService.GetTimeZone(firstPoint.Latitude, firstPoint.Longitude, firstPoint.PointTimeStamp),
                                     EndPartitionAndRow = string.Empty,
                                     EndTimeStamp = DateTimeOffset.UtcNow.AddYears(-30)
                                 };

            var insertOperation = TableOperation.Insert(newSegment);
            await this.segmentsTable.ExecuteAsync(insertOperation);
            return newSegment;
        }

        private async Task<TripSegment> AddSegmentAsync(TripSegment newSegment)
        {
            var insertOperation = TableOperation.Insert(newSegment);
            await this.segmentsTable.ExecuteAsync(insertOperation);
            return newSegment;
        }

        private async Task<IEnumerable<BikeData>> Parse(string fileContents)
        {
            var lines = fileContents.Split('\n');

           BikeData previousPoint = null;
            var result = new List<BikeData>();

            var timeZone = string.Empty;

            foreach (var fields in lines.Where(line => line.Length != 0).Select(line => line.Split(',')).Where(fields => fields.Length == 32))
            {
                DateTimeOffset timeStamp;
                if (!DateTimeOffset.TryParse(fields[0], out timeStamp))
                {
                    continue;
                }

                var point = this.GetBikeData(fields, timeStamp);
                if (previousPoint == null)
                {
                    // First point
                    timeZone = await TimeZoneService.GetTimeZone(point.Latitude, point.Longitude, point.PointTimeStamp);
                }
                point.TimeZone = timeZone;

                if (previousPoint != null && ((point.Latitude - previousPoint.Latitude) > 1
                    || (point.Longitude - previousPoint.Longitude > 1)) || point.MaxSpeed > 150)
                {
                    continue;
                }
                
                previousPoint = point;

                result.Add(point);
            }

            return result;
        }

        private BikeData GetBikeData(IList<string> fields, DateTimeOffset timeStamp)
        {
            var partitionKey = string.Format(
                "{0:D4}{1:D2}{2:D2}{3:D2}",
                timeStamp.UtcDateTime.Year,
                timeStamp.UtcDateTime.Month,
                timeStamp.UtcDateTime.Day,
                timeStamp.UtcDateTime.Hour);
            var rowKey = string.Format(
                "{0:D2}{1:D2}{2:D4}",
                timeStamp.UtcDateTime.Minute,
                timeStamp.UtcDateTime.Second,
                this.sequence++);
            var bikeData = new BikeData(partitionKey, rowKey);

            bikeData.PointTimeStamp = timeStamp;
            bikeData.Latitude = Convert.ToDouble(fields[1]);
            bikeData.Longitude = Convert.ToDouble(fields[2]);
            bikeData.CurrentSpeed = Convert.ToDouble(fields[3]);
            bikeData.MaxSpeed = Convert.ToDouble(fields[4]);
            bikeData.AverageSpeed = Convert.ToDouble(fields[5]);
            bikeData.GpsCourse = Convert.ToDouble(fields[6]);
            bikeData.GpsAltitude = Convert.ToDouble(fields[7]);
            bikeData.Satellites = Convert.ToInt32(fields[8]);
            bikeData.Roll = Convert.ToDouble(fields[9]);
            bikeData.MaxRoll = Convert.ToDouble(fields[10]);
            bikeData.AverageRoll = Convert.ToDouble(fields[11]);
            bikeData.Pitch = Convert.ToDouble(fields[12]);
            bikeData.MaxPitch = Convert.ToDouble(fields[13]);
            bikeData.AveragePitch = Convert.ToDouble(fields[14]);
            bikeData.Heading = Convert.ToDouble(fields[15]);
            bikeData.Altitude = Convert.ToDouble(fields[16]);
            bikeData.Temperature = Convert.ToDouble(fields[17]);
            bikeData.MaxTemperature = Convert.ToDouble(fields[18]);
            bikeData.AverageTemperature = Convert.ToDouble(fields[19]);
            bikeData.Bmp = Convert.ToDouble(fields[20]);
            bikeData.MaxBmp = Convert.ToDouble(fields[21]);
            bikeData.AverageBmp = Convert.ToDouble(fields[22]);
            bikeData.AccelerationX = Convert.ToDouble(fields[23]);
            bikeData.MaxAccelerationX = Convert.ToDouble(fields[24]);
            bikeData.AverageAccelerationX = Convert.ToDouble(fields[25]);
            bikeData.AccelerationY = Convert.ToDouble(fields[26]);
            bikeData.MaxAccelerationY = Convert.ToDouble(fields[27]);
            bikeData.AverageAccelerationY = Convert.ToDouble(fields[28]);
            bikeData.AccelerationZ = Convert.ToDouble(fields[29]);
            bikeData.MaxAccelerationZ = Convert.ToDouble(fields[30]);
            bikeData.AverageAccelerationZ = Convert.ToDouble(fields[31]);
            return bikeData;
        }
    }

    internal class TimeZoneService
    {
        public static async Task<string> GetTimeZone(double latitude, double longitude, DateTimeOffset pointTimeStamp)
        {
            using (var client = new HttpClient())
            {
                // New code:
                client.BaseAddress = new Uri("https://maps.googleapis.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response =
                    await
                    client.GetAsync(
                        string.Format(
                            "maps/api/timezone/json?location={0},{1}&timestamp={2}&key=AIzaSyCclZT9g1Xv6V_9E1yWRBBXB_1Ui_zd8Cg",
                            latitude,
                            longitude,
                            (pointTimeStamp.UtcDateTime.Ticks - new DateTime(1970, 1, 1).Ticks) / TimeSpan.TicksPerSecond));

                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }
                var timeZone = await response.Content.ReadAsAsync<GoogleTimeZoneInfo>();

                return timeZone.timeZoneId;
            }
        }
    }

    internal class GoogleTimeZoneInfo
    {
        public float dstOffset { get; set; }
        public float rawOffset { get; set; }
        public string status { get; set; }
        public string timeZoneId { get; set; }
        public string timeZoneName { get; set; }

    }

    internal class Geocoder
    {
        public static async Task<string> ReverseLookup(double latitude, double longitude)
        {
            using (var client = new HttpClient())
            {
                // New code:
                client.BaseAddress = new Uri("https://maps.googleapis.com/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response =
                    await
                    client.GetAsync(
                        string.Format(
                            "maps/api/geocode/json?latlng={0},{1}&key=AIzaSyCclZT9g1Xv6V_9E1yWRBBXB_1Ui_zd8Cg",
                            latitude,
                            longitude));

                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }
                var location = await response.Content.ReadAsAsync<ReverseGeocodeResults>();

                return location.results[1].formatted_address;
            }
        }
    }

    internal class BikeDataComnparer : IEqualityComparer<BikeData>
    {
        public bool Equals(BikeData x, BikeData y)
        {
            return x.PointTimeStamp == y.PointTimeStamp;
        }

        public int GetHashCode(BikeData obj)
        {
            return obj.PointTimeStamp.Ticks.GetHashCode();
        }
    }
}
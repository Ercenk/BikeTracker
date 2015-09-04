namespace BikeTracker
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.WindowsAzure.Storage.Table;

    public class BikeData : TableEntity
    {
        public BikeData(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }
        public DateTimeOffset PointTimeStamp { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double CurrentSpeed { get; set; }
        public double MaxSpeed { get; set; }
        public double AverageSpeed { get; set; }

        public double GpsCourse { get; set; }

        public double GpsAltitude { get; set; }

        public int Satellites { get; set; }

        public double Roll { get; set; }
        public double MaxRoll { get; set; }
        public double AverageRoll { get; set; }

        public double Pitch { get; set; }
        public double MaxPitch { get; set; }
        public double AveragePitch { get; set; }

        public double Heading { get; set; }
        public double Altitude { get; set; }

        public double Temperature { get; set; }
        public double MaxTemperature{ get; set; }
        public double AverageTemperature{ get; set; }

        public double Bmp { get; set; }
        public double MaxBmp{ get; set; }
        public double AverageBmp { get; set; }

        public double AccelerationX { get; set; }
        public double MaxAccelerationX{ get; set; }
        public double AverageAccelerationX { get; set; }

        
        public double AccelerationY { get; set; }
        public double MaxAccelerationY{ get; set; }
        public double AverageAccelerationY { get; set; }

        
        public double AccelerationZ { get; set; }
        public double MaxAccelerationZ{ get; set; }
        public double AverageAccelerationZ { get; set; }

        public string TimeZone { get; set; }
    }
}
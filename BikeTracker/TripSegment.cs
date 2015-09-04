namespace BikeTracker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.WindowsAzure.Storage.Table;

    public class TripSegment: TableEntity
    {
        public TripSegment(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public TripSegment()
        {
        }

        public string StartPartitionAndRow { get; set; }
        public string EndPartitionAndRow { get; set; }
        public int NumberOfPoints { get; set; }
        public DateTimeOffset StartTimeStamp { get; set; }

        public DateTimeOffset EndTimeStamp { get; set; }

        public double StartLatitude { get; set; }

        public double StartLongitude { get; set; }

        public string StartLocation { get; set; }

        public string EndLocation { get; set; }

        public double EndLatitude { get; set; }

        public double EndLongitude { get; set; }

        public double MaxSpeed { get; set; }
        public double AverageSpeed { get; set; }

        public int MaxSatellites { get; set; }
        public int AverageSatellites { get; set; }

        public double MaxRoll { get; set; }
        public double AverageRoll { get; set; }

        public double MaxPitch { get; set; }
        public double AveragePitch { get; set; }

        public double MaxTemperature{ get; set; }
        public double AverageTemperature{ get; set; }

        public double MaxBmp{ get; set; }
        public double AverageBmp { get; set; }

        public double MaxAccelerationX{ get; set; }
        public double AverageAccelerationX { get; set; }

        
        public double MaxAccelerationY{ get; set; }
        public double AverageAccelerationY { get; set; }

        
        public double MaxAccelerationZ{ get; set; }
        public double AverageAccelerationZ { get; set; }

        public string StartTimeZone { get; set; }

        public double AverageAltitude { get; set; }

        public void CopyFrom(TripSegment existingSegment)
        {
            this.AverageAccelerationX = existingSegment.AverageAccelerationX;
            this.AverageAccelerationY = existingSegment.AverageAccelerationY;
            this.AverageAccelerationZ = existingSegment.AverageAccelerationZ;
            this.AverageBmp = existingSegment.AverageBmp;
            this.AveragePitch = existingSegment.AveragePitch;
            this.AverageRoll = existingSegment.AverageRoll;
            this.AverageSatellites = existingSegment.AverageSatellites;
            this.AverageSpeed = existingSegment.AverageSpeed;
            this.AverageTemperature = existingSegment.AverageTemperature;
            this.EndLatitude = existingSegment.EndLatitude;
            this.EndLongitude = existingSegment.EndLongitude;
            this.EndLocation = existingSegment.EndLocation;
            this.EndPartitionAndRow = existingSegment.EndPartitionAndRow;
            this.EndTimeStamp = existingSegment.EndTimeStamp;
            this.MaxAccelerationX = existingSegment.MaxAccelerationX;
            this.MaxAccelerationY = existingSegment.MaxAccelerationY;
            this.MaxAccelerationZ = existingSegment.MaxAccelerationZ;
            this.MaxBmp = existingSegment.MaxBmp;
            this.MaxPitch = existingSegment.MaxPitch;
            this.MaxRoll = existingSegment.MaxRoll;
            this.MaxSatellites = existingSegment.MaxSatellites;
            this.MaxSpeed = existingSegment.MaxSpeed;
            this.MaxTemperature = existingSegment.MaxTemperature;
            this.NumberOfPoints = existingSegment.NumberOfPoints;
            this.StartLongitude = existingSegment.StartLongitude;
            this.StartLocation = existingSegment.StartLocation;
            this.AverageAltitude = existingSegment.AverageAltitude;
        }

        public void UpdateSegment(IReadOnlyCollection<BikeData> lines)
        {
            var pointsInBatch = lines.Count;

            var previousNumberOfPoints = this.NumberOfPoints;

            this.NumberOfPoints += pointsInBatch;

            this.MaxSpeed = this.MaxSpeed > lines.Max(l => l.MaxSpeed) ? this.MaxSpeed : lines.Max(l => l.MaxSpeed);

            this.MaxSatellites = this.MaxSatellites > lines.Max(l => l.Satellites)
                                        ? this.MaxSatellites
                                        : lines.Max(l => l.Satellites);

            this.MaxRoll = this.MaxRoll > lines.Max(l => l.MaxRoll) ? this.MaxSpeed : lines.Max(l => l.MaxRoll);

            this.MaxPitch = this.MaxSpeed > lines.Max(l => l.MaxPitch) ? this.MaxPitch : lines.Max(l => l.MaxPitch);

            this.MaxTemperature = this.MaxTemperature > lines.Max(l => l.MaxTemperature)
                                         ? this.MaxTemperature
                                         : lines.Max(l => l.MaxTemperature);

            this.MaxBmp = this.MaxBmp > lines.Max(l => l.MaxBmp) ? this.MaxBmp : lines.Max(l => l.MaxBmp);

            this.MaxAccelerationX = this.MaxAccelerationX > lines.Max(l => l.MaxAccelerationX)
                                           ? this.MaxAccelerationX
                                           : lines.Max(l => l.MaxAccelerationX);

            this.MaxAccelerationY = this.MaxAccelerationY > lines.Max(l => l.MaxAccelerationY)
                                           ? this.MaxAccelerationY
                                           : lines.Max(l => l.MaxAccelerationY);

            this.MaxAccelerationZ = this.MaxAccelerationZ > lines.Max(l => l.MaxAccelerationZ)
                                           ? this.MaxAccelerationZ
                                           : lines.Max(l => l.MaxAccelerationZ);

            this.AverageSpeed = ((this.AverageSpeed * previousNumberOfPoints)
                                    + (lines.Average(l => l.AverageSpeed) * pointsInBatch))
                                   / (previousNumberOfPoints + pointsInBatch);

            this.AverageSatellites =
                Convert.ToInt32(
                    ((this.AverageSatellites * previousNumberOfPoints) + (lines.Average(l => l.Satellites) * pointsInBatch))
                    / (previousNumberOfPoints + pointsInBatch));

            this.AverageRoll = ((this.AverageRoll * previousNumberOfPoints)
                                   + (lines.Average(l => l.AverageRoll) * pointsInBatch))
                                  / (previousNumberOfPoints + pointsInBatch);

            this.AveragePitch = ((this.AveragePitch * previousNumberOfPoints)
                                    + (lines.Average(l => l.AveragePitch) * pointsInBatch))
                                   / (previousNumberOfPoints + pointsInBatch);

            this.AverageTemperature = ((this.AverageTemperature * previousNumberOfPoints)
                                          + (lines.Average(l => l.AverageTemperature) * pointsInBatch))
                                         / (previousNumberOfPoints + pointsInBatch);

            this.AverageBmp = ((this.AverageBmp * previousNumberOfPoints)
                                  + (lines.Average(l => l.AverageBmp) * pointsInBatch))
                                 / (previousNumberOfPoints + pointsInBatch);

            this.AverageAccelerationX = ((this.AverageAccelerationX * previousNumberOfPoints)
                                            + (lines.Average(l => l.AverageAccelerationX) * pointsInBatch))
                                           / (previousNumberOfPoints + pointsInBatch);

            this.AverageAccelerationY = ((this.AverageAccelerationY * previousNumberOfPoints)
                                            + (lines.Average(l => l.AverageAccelerationY) * pointsInBatch))
                                           / (previousNumberOfPoints + pointsInBatch);

            this.AverageAccelerationZ = ((this.AverageAccelerationZ * previousNumberOfPoints)
                                            + (lines.Average(l => l.AverageAccelerationZ) * pointsInBatch))
                                           / (previousNumberOfPoints + pointsInBatch);

            this.AverageAltitude = ((this.AverageAltitude * previousNumberOfPoints)
                                            + (lines.Average(l => l.GpsAltitude) * pointsInBatch))
                                           / (previousNumberOfPoints + pointsInBatch);
        }

        public void Merge(TripSegment otherSegment)
        {
            var currentNumberOfPoints = this.NumberOfPoints;
            this.NumberOfPoints += otherSegment.NumberOfPoints;

            this.MaxSpeed = this.MaxSpeed > otherSegment.MaxSpeed ? this.MaxSpeed : otherSegment.MaxSpeed;
            this.MaxSatellites = this.MaxSatellites > otherSegment.MaxSatellites ? this.MaxSatellites : otherSegment.MaxSatellites;
            this.MaxRoll = this.MaxRoll > otherSegment.MaxRoll ? this.MaxRoll : otherSegment.MaxRoll;
            this.MaxPitch = this.MaxPitch > otherSegment.MaxPitch ? this.MaxPitch : otherSegment.MaxPitch;

            this.MaxTemperature = this.MaxTemperature > otherSegment.MaxTemperature ? this.MaxTemperature : otherSegment.MaxTemperature;
            this.MaxBmp = this.MaxBmp > otherSegment.MaxBmp ? this.MaxBmp : otherSegment.MaxBmp;
            this.MaxAccelerationX = this.MaxAccelerationX > otherSegment.MaxAccelerationX ? this.MaxAccelerationX : otherSegment.MaxAccelerationX;
            this.MaxAccelerationY = this.MaxAccelerationY > otherSegment.MaxAccelerationY ? this.MaxAccelerationY : otherSegment.MaxAccelerationY;
            this.MaxAccelerationZ = this.MaxAccelerationZ > otherSegment.MaxAccelerationZ ? this.MaxAccelerationZ : otherSegment.MaxAccelerationZ;


            this.AverageSpeed = ((this.AverageSpeed * currentNumberOfPoints)
                                    + otherSegment.AverageSpeed * otherSegment.NumberOfPoints)
                                   / (currentNumberOfPoints + otherSegment.NumberOfPoints);

            this.AverageSatellites = Convert.ToInt32((this.AverageSatellites * currentNumberOfPoints
                                    + otherSegment.AverageSatellites * otherSegment.NumberOfPoints)
                                   / (currentNumberOfPoints + otherSegment.NumberOfPoints));


            this.AverageRoll = ((this.AverageRoll * currentNumberOfPoints)
                                    + otherSegment.AverageRoll * otherSegment.NumberOfPoints)
                                   / (currentNumberOfPoints + otherSegment.NumberOfPoints);


            this.AveragePitch = ((this.AveragePitch * currentNumberOfPoints)
                                    + otherSegment.AveragePitch * otherSegment.NumberOfPoints)
                                   / (currentNumberOfPoints + otherSegment.NumberOfPoints);

            this.AverageTemperature = ((this.AverageTemperature * currentNumberOfPoints)
                                    + otherSegment.AverageTemperature * otherSegment.NumberOfPoints)
                                   / (currentNumberOfPoints + otherSegment.NumberOfPoints);


            this.AverageBmp = ((this.AverageBmp * currentNumberOfPoints)
                                    + otherSegment.AverageBmp * otherSegment.NumberOfPoints)
                                   / (currentNumberOfPoints + otherSegment.NumberOfPoints);

            this.AverageAccelerationX = ((this.AverageAccelerationX * currentNumberOfPoints)
                                     + otherSegment.AverageAccelerationX * otherSegment.NumberOfPoints)
                                    / (currentNumberOfPoints + otherSegment.NumberOfPoints);

            this.AverageAccelerationY = ((this.AverageAccelerationY * currentNumberOfPoints)
                                     + otherSegment.AverageAccelerationY * otherSegment.NumberOfPoints)
                                    / (currentNumberOfPoints + otherSegment.NumberOfPoints);

            this.AverageAccelerationZ = ((this.AverageAccelerationZ * currentNumberOfPoints)
                                     + otherSegment.AverageAccelerationZ * otherSegment.NumberOfPoints)
                                    / (currentNumberOfPoints + otherSegment.NumberOfPoints);

            this.AverageAltitude = ((this.AverageAltitude * currentNumberOfPoints)
                                    + otherSegment.AverageAltitude * otherSegment.NumberOfPoints)
                                   / (currentNumberOfPoints + otherSegment.NumberOfPoints);

        }
    }
}
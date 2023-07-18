using System;

public class GPSUtils
{
    public static double CalculateDistance(GPSData p1, GPSData p2)
    {
        double dx = p2.latitude - p1.latitude;
        double dy = p2.longitude - p1.longitude;
        double dz = p2.altitude - p1.altitude;

        double squaredDistance = dx * dx + dy * dy + dz * dz;
        double distance = Math.Sqrt(squaredDistance);

        return distance;
    }

    public static double CalculateDirection(GPSData p1, GPSData p2)
    {
        double dx = p2.latitude - p1.latitude;
        double dy = p2.longitude - p1.longitude;

        double radianAngle = Math.Atan2(dy, dx);
        double degreeAngle = radianAngle * (180.0 / Math.PI);

        return degreeAngle;
    }
}
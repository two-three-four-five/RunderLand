using System;
using UnityEngine;

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

    public static Vector3 GPSToUnity(GPSData gpsData, Vector3 referenceUnity)
    {
        Vector3 gpsVector = new Vector3((float)gpsData.longitude, (float)gpsData.latitude, (float)gpsData.altitude);

        // Calculate the offset between reference GPS and reference Unity
        Vector3 offset = gpsVector - gpsVector;

        // Convert latitude and longitude to meters (assuming 1 unit = 1 meter)
        float latitudeMeters = offset.x * 111111f;
        float longitudeMeters = offset.y * 111111f * Mathf.Cos(gpsVector.x * Mathf.Deg2Rad);
        float altitudeMeters = offset.z;

        // Apply the offset to the reference Unity coordinates
        Vector3 unityPosition = referenceUnity + new Vector3(longitudeMeters, altitudeMeters, latitudeMeters);      

        return unityPosition;
    }
}
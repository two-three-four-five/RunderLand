using System;
using UnityEngine;

public class GPSUtils
{
    private static float metersPerLat;
    private static float metersPerLon;

    public static double CalculateDistance(in GPSData p1, in GPSData p2)
    {
        const int R = 6371;     // Earth Radius
        double dLat = (p2.latitude - p1.latitude) * Math.PI / 180;
        double dLon = (p2.longitude - p1.longitude) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(p1.latitude * Math.PI / 180) * Math.Cos(p2.latitude * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = R * c * 1000;

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

    public static Vector3 GPSToUnity(GPSData playerGPS, GPSData avatarGPS, Vector3 referencePosition)
    {     
        Vector3 offset = new Vector3((float)(playerGPS.longitude - avatarGPS.longitude),
                                (float)(playerGPS.latitude - avatarGPS.latitude),
                                (float)(playerGPS.altitude - avatarGPS.altitude));
        FindMetersPerLat((float)avatarGPS.latitude);
        // Convert latitude and longitude to meters (assuming 1 unit = 1 meter)
        float latitudeMeters = offset.x * metersPerLat;
        float longitudeMeters = offset.y * metersPerLon;
        float altitudeMeters = offset.z;

        // Apply the offset to the reference Unity coordinates
        Vector3 unityPosition = referencePosition + new Vector3(longitudeMeters, altitudeMeters, latitudeMeters);      

        return unityPosition;
    }

    private static void FindMetersPerLat(float lat) // Compute lengths of degrees
    {
        float m1 = 111132.92f;    // latitude calculation term 1
        float m2 = -559.82f;        // latitude calculation term 2
        float m3 = 1.175f;      // latitude calculation term 3
        float m4 = -0.0023f;        // latitude calculation term 4
        float p1 = 111412.84f;    // longitude calculation term 1
        float p2 = -93.5f;      // longitude calculation term 2
        float p3 = 0.118f;      // longitude calculation term 3

        lat = lat * Mathf.Deg2Rad;

        // Calculate the length of a degree of latitude and longitude in meters
        metersPerLat = m1 + (m2 * Mathf.Cos(2 * (float)lat)) + (m3 * Mathf.Cos(4 * (float)lat)) + (m4 * Mathf.Cos(6 * (float)lat));
        metersPerLon = (p1 * Mathf.Cos((float)lat)) + (p2 * Mathf.Cos(3 * (float)lat)) + (p3 * Mathf.Cos(5 * (float)lat));
    }
}
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class GPXReader : MonoBehaviour
{
    // File path of the GPX file to read
    public string       gpxFilePath = "log.gpx";
    
    private void Start()
    {
        // Read and parse the GPX file
        List<GPSData> gpsDataList = ReadGPXFile(gpxFilePath);

        // Display the extracted GPS data
        foreach (GPSData gpsData in gpsDataList)
        {
            Debug.Log("Latitude: " + gpsData.latitude);
            Debug.Log("Longitude: " + gpsData.longitude);
            Debug.Log("Altitude: " + gpsData.altitude);
            Debug.Log("----------------------");
        }
    }

    private List<GPSData> ReadGPXFile(string filePath)
    {
        List<GPSData> gpsDataList = new List<GPSData>();

        XmlDocument doc = new XmlDocument();
        doc.Load(filePath);

        XmlNodeList trackPoints = doc.SelectNodes("//trkpt");

        foreach (XmlNode trackPoint in trackPoints)
        {
            double latitude = double.Parse(trackPoint.Attributes["lat"].Value);
            double longitude = double.Parse(trackPoint.Attributes["lon"].Value);
            double altitude = double.Parse(trackPoint.SelectSingleNode("ele").InnerText);

            GPSData gpsData = new GPSData(latitude, longitude, altitude);
            gpsDataList.Add(gpsData);
        }

        return gpsDataList;
    }
}

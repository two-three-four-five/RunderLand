using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public class GPXReader : MonoBehaviour
{
    // File path of the GPX file to read
    public string gpxFilePath = "path/to/your/file.gpx";

    private void Start()
    {
        // Read and parse the GPX file
        List<GPXData> gpxDataList = ReadGPXFile(gpxFilePath);

        // Display the extracted GPS data
        foreach (GPXData gpxData in gpxDataList)
        {
            Debug.Log("Latitude: " + gpxData.latitude);
            Debug.Log("Longitude: " + gpxData.longitude);
            Debug.Log("Altitude: " + gpxData.altitude);
            Debug.Log("----------------------");
        }
    }

    private List<GPXData> ReadGPXFile(string filePath)
    {
        List<GPXData> gpxDataList = new List<GPXData>();

        XmlDocument doc = new XmlDocument();
        doc.Load(filePath);

        XmlNodeList trackPoints = doc.SelectNodes("//trkpt");

        foreach (XmlNode trackPoint in trackPoints)
        {
            double latitude = double.Parse(trackPoint.Attributes["lat"].Value);
            double longitude = double.Parse(trackPoint.Attributes["lon"].Value);
            double altitude = double.Parse(trackPoint.SelectSingleNode("ele").InnerText);

            GPXData gpxData = new GPXData(latitude, longitude, altitude);
            gpxDataList.Add(gpxData);
        }

        return gpxDataList;
    }
}

public class GPXData
{
    public double latitude;
    public double longitude;
    public double altitude;

    public GPXData(double latitude, double longitude, double altitude)
    {
        this.latitude = latitude;
        this.longitude = longitude;
        this.altitude = altitude;
    }
}

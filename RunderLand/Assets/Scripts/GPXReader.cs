using System.Collections.Generic;
using System.Xml;
using System.IO;
using UnityEngine;

public class GPXReader : MonoBehaviour
{        
    public static List<GPSData> ReadGPXFile(string filePath)
    {
        try
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
        }catch (System.Exception ex)
        {
            Debug.LogError("Error loading GPX file : " + ex.Message);
            return (null);
        }       
    }
}

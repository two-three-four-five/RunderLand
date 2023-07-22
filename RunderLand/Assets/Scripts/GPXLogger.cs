using System.Collections;
using UnityEngine;
using System.IO;
using System.Xml;

public class GPXLogger : MonoBehaviour
{
    // File path of the existing GPX file
    //public string gpxFilePath = Path.Combine(Application.persistentDataPath, "log.gpx");
    private string gpxFilePath;

    // Time interval between each GPS update
    public float updateInterval = 1f;

    // GPSModule to get gps information
    public GameObject   GPSModule;

    void Start()
    {
        // Continuously log GPS data
        gpxFilePath = Path.Combine(Application.persistentDataPath, "log.gpx");
        StartCoroutine(WriteDataToFile());
    }

    IEnumerator WriteDataToFile()
    {
        while (true)
        {
            double latitude = GPSModule.GetComponent<LocationModule>().latitude;
            double longitude = GPSModule.GetComponent<LocationModule>().longitude;
            double altitude = GPSModule.GetComponent<LocationModule>().altitude;

            // Append track point to GPX file
            if (!System.IO.File.Exists(gpxFilePath))
            {
                CreateGPXFile(latitude, longitude, altitude);
                Debug.Log("gogogogogogogoggogogogoogogogoggogoogogogogogo");
            }

            else
                AppendTrackPointToGPXFile(latitude, longitude, altitude);
            Debug.Log("lolololollololololol");
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void CreateGPXFile(double latitude, double longitude, double altitude)
    {
        XmlDocument doc = new XmlDocument();
        XmlElement root = doc.CreateElement("gpx");
        doc.AppendChild(root);

        XmlElement trk = doc.CreateElement("trk");
        root.AppendChild(trk);

        //XmlElement trkseg = doc.CreateElement("trkseg");
        //root.AppendChild(trkseg);

        XmlElement trkpt = doc.CreateElement("trkpt");
        trkpt.SetAttribute("lat", latitude.ToString());
        trkpt.SetAttribute("lon", longitude.ToString());
        trk.AppendChild(trkpt);

        XmlElement ele = doc.CreateElement("ele");
        ele.InnerText = altitude.ToString();
        trkpt.AppendChild(ele);

        // Save the GPX file
        doc.Save(gpxFilePath);
    }

    private void AppendTrackPointToGPXFile(double latitude, double longitude, double altitude)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(gpxFilePath);

        XmlNode trackSegment = doc.SelectSingleNode("/gpx/trk");

        XmlElement trackPoint = doc.CreateElement("trkpt");
        trackPoint.SetAttribute("lat", latitude.ToString());
        trackPoint.SetAttribute("lon", longitude.ToString());

        XmlElement elevation = doc.CreateElement("ele");
        elevation.InnerText = altitude.ToString();

        trackPoint.AppendChild(elevation);
        trackSegment.AppendChild(trackPoint);

        doc.Save(gpxFilePath);
    }

    private void OnDestroy()
    {
        // Stop GPS
        Input.location.Stop();
    }
}

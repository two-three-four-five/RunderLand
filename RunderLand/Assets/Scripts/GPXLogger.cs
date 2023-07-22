using System.Collections;
using UnityEngine;
using System.IO;
using System.Xml;

public class GPXLogger : MonoBehaviour
{
    // File path of the existing GPX file
    //public string gpxFilePath = Path.Combine(Application.persistentDataPath, "log.gpx");
    private string gpxFilePath;
    private string fileName = "log1.txt";

    // Time interval between each GPS update
    public float updateInterval = 1f;

    // LocationModule to get gps information
    public LocationModule LocationModule;

    void Start()
    {
        LocationModule = GameObject.Find("Location Module").GetComponent<LocationModule>();
        // Continuously log GPS data
        gpxFilePath = Path.Combine(Application.persistentDataPath, fileName);
        int suffix = 1;
        while (System.IO.File.Exists(gpxFilePath))
        {
            fileName = $"log{suffix}.txt";
            gpxFilePath = Path.Combine(Application.persistentDataPath, fileName);
            suffix++;
        }
        StartCoroutine(WriteDataToFile());
    }

    IEnumerator WriteDataToFile()
    {
        double latitude = LocationModule.GetComponent<LocationModule>().latitude;
        double longitude = LocationModule.GetComponent<LocationModule>().longitude;
        double altitude = LocationModule.GetComponent<LocationModule>().altitude;
        CreateGPXFile(latitude, longitude, altitude);

        while (true)
        {
            latitude = LocationModule.GetComponent<LocationModule>().latitude;
            longitude = LocationModule.GetComponent<LocationModule>().longitude;
            altitude = LocationModule.GetComponent<LocationModule>().altitude;
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
        if (latitude == 0 && longitude == 0 && altitude == 0)
            return;

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

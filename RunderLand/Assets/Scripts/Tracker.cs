public class GPXLogger : MonoBehaviour
{
    List<GPSData>       gpsDataList = new List<GPSData>;
    public GameObject   GPSModule;

    private void Start()
    {
        while (1)
        {
            AddTrackPointPoint();
            sleep(1);
        }
    }

    private void AddTrackPoint()
    {
        double latitude = GPSModule.GetComponent<latitude>;
        double longitude = GPSModule.GetComponent<longitude>;
        double altitude = GPSModule.GetComponent<altitude>;

        GPSData gpsData = new GPS(latitude, longitude, altitude);
        gpsDataList.Add(gpsData);
    }
}
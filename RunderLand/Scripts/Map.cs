public class Map : MonoBehaviour
{
    List<GPSData>       map = new List<GPSData>;
    public GameObject   GPSModule;
    public float        trackingInterval = 1f;

    private IEnumerator Start()
    {
        while (1)
        {
            AddTrackPoint();
            // DrawAvatar();
            yield return new WaitForSeconds(trackingInterval);
        }
    }

    private void AddTrackPoint()
    {
            float latitude = GPSModule.GetComponent<GPS>.latitude;
            float longitude = GPSModule.GetComponent<GPS>.longitude;
            // float altitude = GPSModule.GetComponent<GPS>.altitude;
            float altitude = 0;

        GPSData gpsData = new GPS(latitude, longitude, altitude);
        map.Add(gpsData);
    }
}
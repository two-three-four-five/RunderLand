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
        double latitude = GPSModule.GetComponent<latitude>;
        double longitude = GPSModule.GetComponent<longitude>;
        double altitude = GPSModule.GetComponent<altitude>;

        GPSData gpsData = new GPS(latitude, longitude, altitude);
        map.Add(gpsData);
    }
}
public class GPSData
{
    public double latitude;
    public double longitude;
    public double altitude;

    public GPSData(double latitude, double longitude, double altitude)
    {
        this.latitude = latitude;
        this.longitude = longitude;
        this.altitude = altitude;
    }

    public override string ToString()
    {
        string str = latitude.ToString() + ", " + longitude.ToString() + ", " + altitude.ToString();
        return str;
    }
}
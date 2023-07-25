using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Collections;

public class Player : MonoBehaviour
{
	private double									totalDist = 0;	
	private GPSData									prevGPSData;
	public GPXLogger								GPXLogger;
	public List<Tuple<GPSData, double, Vector3>>	route = new List<Tuple<GPSData, double, Vector3>>();
	public GameObject   							LocationModule;
	public TMP_Text									playertext;
	public int										size;

    public List<Tuple<GPSData, double, Vector3>> getRoute()
	{
		return (route);
	}

	public double getTotalDist()
	{
		return (totalDist);
	}

    public void Start()
    {		
		double latitude = LocationModule.GetComponent<LocationModule>().latitude;
		double longitude = LocationModule.GetComponent<LocationModule>().longitude;
		double altitude = LocationModule.GetComponent<LocationModule>().altitude;

		GPSData GPSData = new GPSData(latitude, longitude, altitude);
		route.Add(Tuple.Create(GPSData, 0d, LocationModule.GetComponent<LocationModule>().directionVector));
		prevGPSData = GPSData;
		StartCoroutine(UpdateLocation());
		size++;
	}

	public IEnumerator UpdateLocation()
    {
		double latitude, longitude, altitude;

		while (true)
		{				
			yield return new WaitForSecondsRealtime(1f);

			latitude = LocationModule.GetComponent<LocationModule>().latitude;
			longitude = LocationModule.GetComponent<LocationModule>().longitude;
			altitude = LocationModule.GetComponent<LocationModule>().altitude;

			GPSData currGPSData = new GPSData(latitude, longitude, altitude);		
	
			double	sectionDist = GPSUtils.CalculateDistance(prevGPSData, currGPSData);
			if (sectionDist >= 5)
            {
				if (size == 1)
					continue;
				else
                {
					latitude = route[size - 1].Item1.latitude + route[size - 1].Item1.latitude - route[size - 2].Item1.latitude;
					longitude = route[size - 1].Item1.longitude + route[size - 1].Item1.longitude - route[size - 2].Item1.longitude;
					altitude = route[size - 1].Item1.altitude + route[size - 1].Item1.altitude - route[size - 2].Item1.altitude;
					currGPSData.latitude = latitude;
					currGPSData.longitude = longitude;
					currGPSData.altitude = altitude;
					sectionDist = route[size - 1].Item2 - route[size - 2].Item2;
				}
            }
			totalDist += sectionDist;
			route.Add(Tuple.Create(currGPSData, totalDist, LocationModule.GetComponent<LocationModule>().directionVector));
			playertext.text = totalDist.ToString();			
			prevGPSData = currGPSData;
			size++;
			GPXLogger.AppendTrackPointToGPXFile(latitude, longitude, altitude);
        }
    }
}
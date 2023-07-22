using UnityEngine;
using System.Collections.Generic;
using System;

public class Player : MonoBehaviour
{
	private List<Tuple<GPSData, double, Vector3>>	route = new List<Tuple<GPSData, double, Vector3>>();
	private double							totalDist = 0;
	private int								size = 0;
	private GameObject   					GPSModule = GameObject.Find("GPSModule");

	public List<Tuple<GPSData, double, Vector3>> getRoute()
	{
		return (route);
	}

	public double getTotalDist()
	{
		return (totalDist);
	}

	public int getSize()
    {
		return (size);
    }

	public void SetPosition()
	{
		double latitude = GPSModule.GetComponent<LocationModule>().latitude;
        double longitude = GPSModule.GetComponent<LocationModule>().longitude;
        double altitude = GPSModule.GetComponent<LocationModule>().altitude;        

        GPSData GPSData = new GPSData(latitude, longitude, altitude);
		route.Add(Tuple.Create(GPSData, 0d, GPSModule.GetComponent<LocationModule>().directionVector));
	}

	public void UpdateLocation()
    {
        double latitude = GPSModule.GetComponent<LocationModule>().latitude;
        double longitude = GPSModule.GetComponent<LocationModule>().longitude;
        double altitude = GPSModule.GetComponent<LocationModule>().altitude;

        GPSData currGPSData = new GPSData(latitude, longitude, altitude);
		GPSData prevGPSData = route[route.Count - 1].Item1;
		double	sectionDist = GPSUtils.CalculateDistance(currGPSData, prevGPSData);

		totalDist += sectionDist;
        route.Add(Tuple.Create(currGPSData, totalDist, GPSModule.GetComponent<LocationModule>().directionVector));

		size++;
    }
}
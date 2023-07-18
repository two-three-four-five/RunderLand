using UnityEngine;
using System.Collections.Generic;
using System;

public class Player : MonoBehaviour
{
	private List<Tuple<GPSData, double>>	route = new List<Tuple<GPSData, double>>();
	private double							totalDist = 0;
	private int								size = 0;
	private GameObject   					GPSModule = GameObject.Find("GPSModule");

	public List<Tuple<GPSData, double>> getRoute()
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
		double latitude = GPSModule.GetComponent<GPS>().latitude;
        double longitude = GPSModule.GetComponent<GPS>().longitude;
        // float altitude = GPSModule.GetComponent<GPS>().altitude;
        float altitude = 0;

        GPSData GPSData = new GPSData(latitude, longitude, altitude);
		route.Add(Tuple.Create(GPSData, 0d));
	}

	public void UpdateLocation()
    {
        double latitude = GPSModule.GetComponent<GPS>().latitude;
        double longitude = GPSModule.GetComponent<GPS>().longitude;
        // float altitude = GPSModule.GetComponent<GPS>().altitude;
        float altitude = 0;

        GPSData currGPSData = new GPSData(latitude, longitude, altitude);
		GPSData prevGPSData = route[route.Count - 1].Item1;
		double	sectionDist = GPSUtils.CalculateDistance(currGPSData, prevGPSData);

		totalDist += sectionDist;
        route.Add(Tuple.Create(currGPSData, totalDist));

		size++;
    }
}
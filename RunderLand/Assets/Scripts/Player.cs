using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
	public List<Tuple<GPSData, double, Vector3>>	route = new List<Tuple<GPSData, double, Vector3>>();
	private double									totalDist = 0;	
	private GPSData									prevGPSData;
	public GameObject   							LocationModule;
	public Text										playertext;

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
	}

	public void UpdateLocation()
    {				
		double latitude = LocationModule.GetComponent<LocationModule>().latitude;
        double longitude = LocationModule.GetComponent<LocationModule>().longitude;
        double altitude = LocationModule.GetComponent<LocationModule>().altitude;

		GPSData currGPSData = new GPSData(latitude, longitude, altitude);		
	
		double	sectionDist = GPSUtils.CalculateDistance(prevGPSData, currGPSData);		
		prevGPSData = currGPSData;		
		totalDist += sectionDist;
        route.Add(Tuple.Create(currGPSData, totalDist, LocationModule.GetComponent<LocationModule>().directionVector));
		playertext.text = totalDist.ToString(); 
    }
}
using System.Collections.Generic;
using System;
using UnityEngine;

public class Avatar : MonoBehaviour
{
    private List<double>                    distanceList;
    private int                             dist_idx = 0;
    private Tuple<GPSData, GPSData>         path;
    private int                             section = 0;
    private int                             drawMode = 0;
    private double                          totalDist = 0;
    private double                          distAfterSec;
    private double                          threshold = 2;
    private double                          movePerFrame;
    private GameObject                      avatar;
    private Camera                          arCamera = GameObject.Find("AR Camera").GetComponent<Camera>();

    public Avatar(string filePath)
    {
        List<GPSData>   gpsDataList = GPXReader.ReadGPXFile(filePath);
        distanceList = new List<double>();
        
        for (int idx = 0; idx < gpsDataList.Count - 1; idx++)
        {
            distanceList.Add(GPSUtils.CalculateDistance(gpsDataList[idx], gpsDataList[idx + 1]));
        }
    }

    public void setAvatarAsset(GameObject avatar)
    {
        this.avatar = avatar;
    }

    public void FindNextPoint(in List<Tuple<GPSData, double>> route, in double playerTotalDist)
    {
        distAfterSec = totalDist + distanceList[dist_idx];
        movePerFrame = distanceList[dist_idx] * 0.5;
        dist_idx++;
        // case 1 : Avatar is behind of the Player
        if (distAfterSec < playerTotalDist)
        {
            drawMode = -1;

            // Find the GPS where avatar is currently in.

            // Find the section where Avatar belongs
            while (distAfterSec > route[section].Item2)
                section++;
            // Find Point by interpolation inside the section
            double weight = (distAfterSec - route[section - 1].Item2) / (route[section].Item2 - route[section - 1].Item2);
            GPSData nextPoint = new GPSData((route[section].Item1.latitude - route[section - 1].Item1.latitude) * weight
                                            , (route[section].Item1.longitude - route[section - 1].Item1.longitude) * weight
                                            , (route[section].Item1.altitude - route[section - 1].Item1.altitude) * weight);
            //path = new Tuple<GPSData, GPSData>(tmpGPS, nextPoint);
        }
        // case 2 : Avatar is near the Player
        else if (distAfterSec - playerTotalDist < threshold)
        {
            drawMode = 0;
            path = new Tuple<GPSData, GPSData>(route[0].Item1, route[0].Item1);
        }
        // case 3 : Avatar is in front of the Player
        else if (totalDist > playerTotalDist || distAfterSec - playerTotalDist > threshold)
        {
            drawMode = 1;
        }
    }
    
    public void moveAvatar(in List<Tuple<GPSData, double>> route, in int size)
    {
        totalDist += movePerFrame;
        //Quaternion deviceRotation = GPSUtils.CalculateDirection(route[size - 1].Item1, route[size - 1].Item1);
        if (drawMode == 1)
        {
            avatar.transform.position = arCamera.transform.position;
            avatar.transform.rotation = arCamera.transform.rotation;
            // Move Object to the front and scale down
        }
        else if (drawMode == 0)
        {
            // Draw near the zone
            avatar.transform.rotation = arCamera.transform.rotation;
            avatar.transform.position = arCamera.transform.position;
        }
        else
        {

        }
    }
}
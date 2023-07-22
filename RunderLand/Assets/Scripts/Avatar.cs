using System.Collections.Generic;
using System;
using UnityEngine;

public class Avatar : MonoBehaviour
{
    private GameObject                      avatar = GameObject.Find("avatar");
    private Camera                          arCamera = GameObject.Find("AR Camera").GetComponent<Camera>();
    private GameObject                      locationModule = GameObject.Find("LocationModule");
    private Vector3                         directionVector;
    private List<double>                    distanceList;
    private int                             dist_idx = 0;
    private int                             section = 0;
    private double                          threshold = 2;
    private double                          movePerFrame;
    private double                          movedDist = 0;
    private double                          avatarTotalDist = 0;

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

    public void moveAvatar(in List<Tuple<GPSData, double, Vector3>> route, in double playerTotalDist)
    {
        directionVector = locationModule.GetComponent<LocationModule>().directionVector;
        if (movedDist >= distanceList[dist_idx])
        {
            movedDist = 0;
            dist_idx++;
        }

        if (movedDist == 0)
        {
            movePerFrame = distanceList[dist_idx] * 0.02;
        }

        avatarTotalDist += movePerFrame;

        if (avatarTotalDist - playerTotalDist > threshold)
        {           
			double distDiff = avatarTotalDist - playerTotalDist;
			avatar.transform.position = arCamera.transform.position + directionVector * (float) distDiff;
            avatar.transform.LookAt(avatar.transform.position + directionVector);

        }
        else if (Math.Abs(avatarTotalDist - playerTotalDist) < threshold)
        {
            avatar.transform.position = arCamera.transform.position + directionVector * (float) (avatarTotalDist - playerTotalDist);           
            avatar.transform.LookAt(avatar.transform.position + directionVector);
        }
        else
        {
			// Find the Section where avatar will be at after 0.02sec
            while (avatarTotalDist > route[section].Item2)
                section++;
			// Calcualte the point where avatar will be located at.
            double weight = (avatarTotalDist - route[section - 1].Item2) / (route[section].Item2 - route[section - 1].Item2);
            GPSData currentPoint = new GPSData((route[section].Item1.latitude - route[section - 1].Item1.latitude) * weight,
                                        (route[section].Item1.longitude - route[section - 1].Item1.longitude) * weight,
                                        (route[section].Item1.altitude - route[section - 1].Item1.altitude) * weight);

            // Change currentPoint to unity coordinate
            avatar.transform.position = GPSUtils.GPSToUnity(currentPoint, arCamera.transform.position);
            avatar.transform.LookAt(avatar.transform.position + route[section - 1].Item3);

        }
    }
}
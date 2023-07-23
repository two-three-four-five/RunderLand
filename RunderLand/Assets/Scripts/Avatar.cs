using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.IO;

public class Avatar : MonoBehaviour
{    
    public Camera           arCamera;
    public GameObject       locationModule;
    public GameObject       playerObj;
    public Text             avatarDistText;
    private Vector3         directionVector;
    private List<double>    distanceList;
    private int             dist_idx = 0;
    private int             section = 0;
    private double          threshold = 2;
    private double          movePerFrame;
    private double          movedDist = 0;
    private double          avatarTotalDist = 0;


    public void Start()
    {
        string          filePath = Path.Combine(Application.persistentDataPath, "log1.txt");
        List<GPSData>   gpsDataList = GPXReader.ReadGPXFile(filePath);

        distanceList = new List<double>();

        if (gpsDataList != null)
        {
            for (int idx = 0; idx < gpsDataList.Count - 1; idx++)
            {
                distanceList.Add(GPSUtils.CalculateDistance(gpsDataList[idx], gpsDataList[idx + 1]));
            }
        }
    }

    public void FixedUpdate()
    {
        List<Tuple<GPSData, double, Vector3>>   route = playerObj.GetComponent<Player>().route;
        double                                  playerTotalDist = playerObj.GetComponent<Player>().getTotalDist();

        directionVector = locationModule.GetComponent<LocationModule>().directionVector;
        if (distanceList.Count == 0 || dist_idx >= distanceList.Count)
            return;
        while (movedDist >= distanceList[dist_idx])
        {
            movedDist = 0;
            dist_idx++;
        }

        if (movedDist == 0)
        {
            movePerFrame = distanceList[dist_idx] * 0.02;
        }

        avatarTotalDist += movePerFrame;
        movedDist += movePerFrame;

        if (avatarTotalDist - playerTotalDist > threshold)
        {           
			double distDiff = avatarTotalDist - playerTotalDist;
			transform.position = arCamera.transform.position + directionVector * (float) distDiff;
            Vector3 pos = transform.position;
            pos.y -= 0.4f;
            transform.position = pos;            
            //avatar.transform.LookAt(avatar.transform.position + directionVector);
        }
        else if (Math.Abs(avatarTotalDist - playerTotalDist) < threshold)
        {
            transform.position = arCamera.transform.position + directionVector * (float) (avatarTotalDist - playerTotalDist);
            Vector3 pos = transform.position;
            pos.y -= 0.4f;
            transform.position = pos;
            //avatar.transform.LookAt(avatar.transform.position + directionVector);
        }
        else
        {			
            while (avatarTotalDist > route[section].Item2)
                section++;			
            double weight = (avatarTotalDist - route[section - 1].Item2) / (route[section].Item2 - route[section - 1].Item2);
            GPSData currentPoint = new GPSData((route[section].Item1.latitude - route[section - 1].Item1.latitude) * weight,
                                        (route[section].Item1.longitude - route[section - 1].Item1.longitude) * weight,
                                        (route[section].Item1.altitude - route[section - 1].Item1.altitude) * weight);            
            transform.position = GPSUtils.GPSToUnity(currentPoint, arCamera.transform.position);
            Vector3 pos = transform.position;
            pos.y -= 0.4f;
            transform.position = pos;
            //avatar.transform.LookAt(avatar.transform.position + route[section - 1].Item3);
        }
        avatarDistText.text = avatarTotalDist.ToString();
    }
}
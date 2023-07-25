using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.IO;
using TMPro;

public class Avatar : MonoBehaviour
{    
    public Camera           arCamera;
    public GameObject       locationModule;
    public GameObject       playerObj;
    public TMP_Text         avatarDistText;
    public TMP_Text         thresholdStateText;
    public TMP_Text         pointerText;

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
        string          filePath = Path.Combine(Application.persistentDataPath, "log1.gpx");
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
        Vector3                                 pos;

        directionVector = locationModule.GetComponent<LocationModule>().directionVector;
        if (distanceList.Count == 0 || dist_idx >= distanceList.Count)
        {
            avatarDistText.text = "Avatar Returned";
            return;
        }

        while (movedDist >= distanceList[dist_idx])
        {
            movedDist = 0;
            // 여기서 다시 위치를 초기화시켜주는 일을 하면 오차가 누적되는 것을 방지 가
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
			pos = arCamera.transform.position + directionVector * (float)threshold;  
            pos.y -= 1.2f;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(directionVector);
            avatarDistText.text = ((float)(avatarTotalDist)).ToString();
            thresholdStateText.text = "Front";
            pointerText.text = "Front";
        }   
        else if (Math.Abs(avatarTotalDist - playerTotalDist) < threshold)
        {            
            pos = arCamera.transform.position + directionVector * (float)(avatarTotalDist - playerTotalDist);            
            pos.y -= 1.2f;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(directionVector);
            avatarDistText.text = ((float)avatarTotalDist).ToString();
            thresholdStateText.text = "Near";
            pointerText.text = "Near";
        }
        else
        {
            while (avatarTotalDist > route[section].Item2)
                section++;
            double weight = (avatarTotalDist - route[section - 1].Item2) / (route[section].Item2 - route[section - 1].Item2);
            GPSData currentPoint = new GPSData((route[section].Item1.latitude - route[section - 1].Item1.latitude) * weight,
                                        (route[section].Item1.longitude - route[section - 1].Item1.longitude) * weight,
                                        (route[section].Item1.altitude - route[section - 1].Item1.altitude) * weight);
            pos = GPSUtils.GPSToUnity(route[route.Count - 1].Item1, currentPoint, arCamera.transform.position);
            pos.y -= 1.2f;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(route[section - 1].Item3);
            avatarDistText.text = ((float)avatarTotalDist).ToString();
            thresholdStateText.text = "Back";
            pointerText.text = "Back";
        }
    }
}
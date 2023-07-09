using System.Collections.Generic;
using UnityEngine;

public claass Avatar : MonoBehaviour
{
    private List<GPSData>   gpsDataList;
    private List<double>    velocityList;
    
    public Avatar(string filePath)
    {
        List<GPSData>   gpsDataList = GPXReader.ReadGPXFile(filePath);
        List<double>    velocityList = getVelocityList();
    }

    public List<double> getVelocityList()
    {
        List<double>    velocityList = new List<double>();
        
        for (int idx = 0; idx < gpsDataList.Count - 1; idx++)
        {
            velocityList.Add(GPSUtils.CalculateDistance(gpsDataList[idx], gpsDataList[idx + 1]));
        }

        return (velocityList);
    }
}
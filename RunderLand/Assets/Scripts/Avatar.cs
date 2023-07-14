using System.Collections.Generic;
using UnityEngine;

public class Avatar : MonoBehaviour
{
    private List<double>                    distanceList;
    private int                             dist_idx = 0;
    private double                          distAfterSec;
    private double                          totalDist = 0;
    private int                             section = 0;
    private Tuple<GPSData, GPSData>         path;
    private double                          threshold = 2;
    private int                             drawMode = 0;
    private double                          movePerFrame;

    public Avatar(string filePath)
    {
        List<GPSData>   gpsDataList = GPXReader.ReadGPXFile(filePath);
        distanceList = new List<double>();
        
        for (int idx = 0; idx < gpsDataList.Count - 1; idx++)
        {
            distanceList.Add(GPSUtils.CalculateDistance(gpsDataList[idx], gpsDataList[idx + 1]));
        }
    }

    public void SetPosition(in List<Tuple<GPSData, double>> route)
    {
        path.item1 = SetStartingPosition();
        path.item2 = route[0].item1;
    }

    public GPSData SetStartingPosition()
    {
        // Direction please
    }

    public void FindNextPoint(in List<Tuple<GPSData, double>> route, in double playerTotalDist)
    {
        distAfterSec = totalDist + distanceList[dist_idx];
        movePerFrame = distanceList[dist_idx] / 50;
        dist_idx++;
        // case 1 : Avatar is behind of the Player
        if (distAfterSec < playerTotalDist)
        {
            drawMode = -1;
            path.item1 = path.item2;
            // Find which Section should Avatar belongs to
            while (distAfterSec > route[section])
                section++;
            // Find Point by interpolation inside the section
            double weight = (distAfterSec - route[section - 1].item2) / (route[section].item2 - route[section - 1].item2);
            GPSData nextPoint = new GPSData((route[section].item1.latitude - route[section - 1].item1.latitude) * weight
                                            , (route[section].item1.longitude - route[section - 1].item1.longitude) * weight
                                            , (route[section].item1.altitude - route[section - 1].item1.altitude) * weight);
            path.item2 = newPoint;
        }
        // case 2 : Avatar is near the Player
        else if (distAfterSec - playerTotalDist < threshold)
        {
            drawMode = 0;
            path.item2 = route.item1;
        }
        // case 3 : Avatar is in front of the Player
        else if (totalDist > playerTotalDist || distAfterSec - playerTotalDist > threshold)
        {
            drawMode = 1;
        }
    }
    
    public void moveAvatar(in GameObject avatarObj)
    {
        totalDist += movePerFrame;
        if (drawMode == 1)
        {
            // Move Object to the front and scale down

        }
        else if (drawMode == 0)
        {
            // draw near the zone
        }
        else
        {

        }
    }
}
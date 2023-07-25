using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class ControlTower : MonoBehaviour
{
    public GameObject   player;
    public GameObject   avatar;
    public GameObject   LocationModule;
    public GameObject   GPXLogger;
    public TMP_Text     Stat;
    private bool        isLocationModuleReady = false;

    IEnumerator Start()
    {     
        while (true)
        {
            isLocationModuleReady = LocationModule.GetComponent<LocationModule>().isLocationModuleReady;
            Stat.text = "Warming Up~";
            if (isLocationModuleReady)
            {
                //Stat.text = "oh yes";
                player.GetComponent<Player>().enabled = true;
                avatar.GetComponent<Avatar>().enabled = true;
                GPXLogger.GetComponent<GPXLogger>().enabled = true;
                break;
            }
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }
}
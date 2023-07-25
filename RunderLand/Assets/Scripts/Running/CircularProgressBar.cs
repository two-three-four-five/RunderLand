using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CircularProgressBar : MonoBehaviour
{
	public TMP_Text distanceText;
	public TMP_Text unitText;
	public Image LoadingBar;
	float runningTime;
	float distance;

    // Start is called before the first frame update
    void Start()
    {
			unitText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
		// Get Ready
		if (Time.time < 4)
		{
			distanceText.text = (Time.time < 3) ? (4 - Time.time).ToString("0") : "Start!";
			LoadingBar.fillAmount = (Time.time < 3) ? (Time.time - (int)Time.time) : 0;
		}
		// While Running
		else
		{
			runningTime = Time.time - 4;
			distance = (runningTime) / 10;
			distanceText.text = (distance).ToString("0.00");
			LoadingBar.fillAmount = distance - (int)distance;
			unitText.text = "kilometer";
		}
    }
}

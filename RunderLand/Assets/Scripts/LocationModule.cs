using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class LocationModule : MonoBehaviour
{
    public double latitude;
    public double longitude;
    public double altitude;
    
    public Text statusText;
    public Text latitudeText;
    public Text longitudeText;
    public Text altitudeText;
    // public Text directionText;
    // public Text direction2Text;
    public Vector3 directionVector;
    public bool isLocationModuleReady = false;

    private Vector3 prevPosition;
    private Vector3 currPosition;
    private Camera arCamera;

    void Start()
    {
        arCamera = GameObject.Find("AR Camera").GetComponent<Camera>();

        // 위치 서비스 초기화
        Input.location.Start(0.1f, 0.1f);
        altitudeText.text = NativeToolkit.StartLocation() ? "true" : "false";
        directionVector = new Vector3(0, 0, 0);

        // 위치 서비스 활성화 확인
        if (Input.location.isEnabledByUser)
        {
            statusText.text = "Waiting for GPS Init";
            // 위치 서비스 초기화까지 대기
            StartCoroutine(InitializeGPS());
        }
        else
        {
            statusText.text = "GPS not available";
            Debug.Log("GPS not available");
        }
    }

    IEnumerator InitializeGPS()
    {
        // 위치 서비스 초기화 중일 때까지 대기
        while (Input.location.status == LocationServiceStatus.Initializing)
        {
            statusText.text = "Waiting for Initializing";
            yield return new WaitForSeconds(1);
        }

        // 위치 서비스 초기화가 성공한 경우
        if (Input.location.status == LocationServiceStatus.Running)
        {
            // GPS 데이터 갱신 시작
            StartCoroutine(UpdateGPSData());
        }
        else
        {
            statusText.text = "Failed to initailize GPS";
            Debug.Log("Failed to initialize GPS");
        }
    }

    IEnumerator UpdateGPSData()
    {
        int gps_connect = 0;

        yield return new WaitForSecondsRealtime(5);
        while (true)
        {
            // GPS 데이터 업데이트 대기
            yield return new WaitForSeconds(1);

            currPosition = arCamera.transform.position;

            // 현재 GPS 데이터 가져오기
            LocationInfo currentGPSPosition = Input.location.lastData;

            // 위도와 경도 텍스트 업데이트
            gps_connect++;

            latitude = Math.Round(NativeToolkit.GetLatitude(), 6);
            longitude = Math.Round(NativeToolkit.GetLongitude(), 6);
            altitude = currentGPSPosition.altitude;

            latitudeText.text = latitude.ToString();
            longitudeText.text = longitude.ToString();
            altitudeText.text = altitude.ToString();
            statusText.text = (Input.location.status == LocationServiceStatus.Running ? "run" : "not run") + gps_connect.ToString();

            // direction update
            float dx = currPosition.x - prevPosition.x;
            float dy = currPosition.y - prevPosition.y;
            float dz = currPosition.z - prevPosition.z;

            if (Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2) + Math.Pow(dz, 2)) > 1 && !(dx==0 && dy==0 && dz==0))
                directionVector = Vector3.Normalize(new Vector3(dx, currPosition.y, dz));
            // directionText.text = directionVector.ToString();
            prevPosition = currPosition;
            isLocationModuleReady = true;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointAvatar : MonoBehaviour
{
    public GameObject   avatarObj;
    public Camera       arCamera;

    // Update is called once per frame
    void Update()
    {
        Vector3 avatarPointer = avatarObj.GetComponent<Transform>().position - transform.position;
        Vector3.Normalize(avatarPointer);
        transform.rotation = Quaternion.LookRotation(avatarPointer);
        //transform.position = arCamera.GetComponent<Transform>().position;
    }
}

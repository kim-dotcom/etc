//use if VR headset camera needs to be adjusted
//for HTC Vive external tracking, if misaligned (Unity camera position = Vive ground position)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class RepositionCamera : MonoBehaviour
{
    public GameObject headCamera;
    public Vector3 repositionCoordinates;
    public KeyCode repOffsetPlus = KeyCode.KeypadPlus;
    public KeyCode repOffsetMinus = KeyCode.KeypadMinus;
    public bool repositionOnlyVR;

    private Vector3 repOffset = new Vector3(0f, 0.05f, 0f);

    void Start()
    {
        //to verify if XR device active (?)
        //https://docs.unity3d.com/ScriptReference/XR.XRSettings.html
        if (!repositionOnlyVR || (repositionOnlyVR && XRSettings.enabled))
        {
            headCamera.transform.position += repositionCoordinates;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(repOffsetPlus))
        {
            headCamera.transform.position += repOffset;
        }
        else if (Input.GetKeyDown(repOffsetMinus))
        {
            headCamera.transform.position -= repOffset;
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// Dummy Car Eyetracker, version 2022-10-24
// --------------------------------------------------------------------------------------------------------------------
// This is the dumb implementation of ICarEyetracker.
// Function: ET corrdinates are simply assumed to be in the middle of the screen.
// (a real 3D/VR eye-tracker has varying X/Y coordinates seen thru screenSpace, which are then projected to worldSpace)
// --------------------------------------------------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEyetrackerDummy : MonoBehaviour, ICarEyetracker
{
    [SerializeField] public LayerMask carLayerMask;    
    private Vector3 userPosition;
    private Vector3 userRotation;
    private Vector3 lookedAtCoordiante;
    private GameObject LookedAtObject;

    private Ray ray;
    private RaycastHit hit;
    private Vector3 centerVector = new Vector3(0.5f, 0.5f, 0);

    public bool displayRaycast;
    private float displayRaycastSize;
    public Camera DisplayCamera;
    private GameObject RaycastTarget;
    private bool raycastValid;
    private Vector3 nullVector = new Vector3(0,0,0);

    void Start ()
    {
        if (DisplayCamera == null)
        {
            DisplayCamera = Camera.main;
        }
        if (displayRaycast)
        {
            RaycastTarget = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            RaycastTarget.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            RaycastTarget.GetComponent<Collider>().enabled = false;
            RaycastTarget.SetActive(false);
        }        
    }

    void FixedUpdate()
    {
        raycastValid = false;
        //raycast to the middle of the camera...
        ray = DisplayCamera.ViewportPointToRay(centerVector);
        if (Physics.Raycast(ray, out hit, 5000, carLayerMask))
        {
            //display the raycasty
            if (displayRaycast)
            {
                RaycastTarget.SetActive(true);
                RaycastTarget.transform.position = hit.point;
                displayRaycastSize = (DisplayCamera.transform.position - hit.point).magnitude * 0.015f;
                RaycastTarget.transform.localScale = new Vector3(displayRaycastSize,
                                                                 displayRaycastSize,
                                                                 displayRaycastSize);
            }
            //evaluate raycastHit
            if (hit.collider.gameObject != null)
            {
                raycastValid = true;
                userPosition = DisplayCamera.transform.position;
                userRotation = DisplayCamera.transform.rotation.eulerAngles;
                lookedAtCoordiante = hit.transform.position;
                LookedAtObject = hit.collider.gameObject;
            }
        }
    }

    public GameObject getLookedAtObject()
    {
        return raycastValid ? LookedAtObject : null;
    }

    public Vector3 getLookedFromPosition()
    {
        return raycastValid ? userPosition : nullVector;
    }

    public Vector3 getLookedFromDirection()
    {
        return raycastValid ? userRotation : nullVector;
    }

    public Vector3 getLookedAtCoordinate()
    {
        return raycastValid ? lookedAtCoordiante : nullVector;
    }
}

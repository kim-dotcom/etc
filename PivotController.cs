// --------------------------------------------------------------------------------------------------------------------
// Unity Pivot user controller
// version 2022-02-28
//
// A user controller with limited degrees of freedom (rotate around pivot on pitch/roll axes, zoom in/out)
//
// To set up the controller, do the following:
//   - attach this script as a component to an empty GameObject (the pivot user camera will rotate around)
//   - add a child GameObject with Camera component to this (keep same position/rotation relative to the parent)
//   - link the child GameObject to the PivotCamera variable in the inspector
//
// To set other customization variables in the inspector:
//   - pivot rotation speed (degree distance traveled in seconds; default 20 (zoom is hardcoded as 1/10 of this))
//   - pivot roll min/max (the minimum/maximum roll rotation angle allowed; 0-90, default is 10-80)
//   - pivot zoom min/max (the minimum/maximum distance allowed; default is 0.1-4)
//         This is Vector3.distance(camera, pivot)
//   - move KeyCodes for yaw rotation (left/right), roll rotation (up/down), and zoom in/out
//   - restricted roll movement (per segments): if enabled, number of such segments
//         Each roll degree step is determined as (roll max- roll min) / number os steps
//
// Notes on implementation:
//   - Default camera position and roll segments (if enabled) are determined on Awake()
//   - To prevent Gimbal lock, yaw/roll rotation is solved differently
//         yaw is transform.RotateAround (Quaternion), pitch is transform.Rotate
//   - Update() runtime evaluation is as follows: 1) perform rotations, 2) correct them if needed, 3) logging
//         For logging user position/rotation, get pivotCamera.transform.position & .rotation 
//
// Possible bugs and issues:
//   - For close/far zoom, make sure sure Camera clipping planes (and LoD/details) are set accordingly
//   - If zoom in allowed to 0, zoom may bug out by inverting the vector (current var range settings prevent this)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PivotController : MonoBehaviour
{
    //pivot settings variables
    [SerializeField] private GameObject pivotCamera;
    [Range(0, 45)]
    [SerializeField] private float pivotRollMin = 10f;
    [Range(45, 90)]
    [SerializeField] private float pivotRollMax = 60f;
    [Range(0.1f, 5)]
    [SerializeField] private float pivotZoomMin = 0.2f;
    [Range(0.2f, 20)]
    [SerializeField] private float pivotZoomMax = 3f;
    [Space(10)]

    //movement controls
    [SerializeField] private KeyCode MoveUp = KeyCode.W;
    [SerializeField] private KeyCode MoveDown = KeyCode.S;
    [SerializeField] private KeyCode MoveLeft = KeyCode.A;
    [SerializeField] private KeyCode MoveRight = KeyCode.D;
    [SerializeField] private KeyCode MoveZoomIn = KeyCode.Q;
    [SerializeField] private KeyCode MoveZoomOut = KeyCode.E;
    [Space(10)]

    //movement speed
    [Range(1, 100)]
    [SerializeField] private float pivotRotationSpeed = 25;
    [Range(1, 100)]
    [SerializeField] private float slowRotationSpeed = 4;
    [SerializeField] private KeyCode MoveSlow = KeyCode.LeftShift;
    [Space(10)]

    //movement settings
    [SerializeField] private bool restrictedRollMovement = false;
    [Range(1, 10)]
    [SerializeField] private int restrictedRollSegments = 3;

    //default settings variables (determined on Awake)
    private float pivotZoomDefault;
    private Dictionary<int, float> restrictedRollAngles;

    //auxiliary variables
    private float defaultRotationSpeed;
    private Vector3 currentRotation;
    private float currentZoomDistance;
    private float correctionZoomPosition;
    private int currentRollSegment;
    private float rotationDirection; //per Mathf.Sign (1 = positive, -1 = negative)
    private float rollAnglePerSegment;
    private float timeInit;
    private float timeCurrent;

    private void Awake()
    {
        //correct wrong controller settings, if existent
        if (pivotRollMin > pivotRollMax)
        {
            pivotRollMin = pivotRollMax / 2;
            Debug.LogWarning(this.name + ": invalid pivotRollMin variable, corrected to " + pivotRollMin);
        }
        if (pivotZoomMin > pivotZoomMax)
        {
            pivotZoomMin = pivotZoomMax / 2;
            Debug.LogWarning(this.name + ": invalid pivotZoomMin variable, corrected to " + pivotZoomMin);
        }

        //init default camera position, rotation, ans settings
        pivotZoomDefault = (pivotZoomMin + pivotZoomMax) / 2;        
        pivotCamera.transform.position = new Vector3(this.transform.position.x,
                                                     this.transform.position.y,
                                                     this.transform.position.z - pivotZoomDefault);
        currentRotation = this.transform.localRotation.eulerAngles;
        currentRotation.x = (pivotRollMin + pivotRollMax) / 2;
        this.transform.localRotation = Quaternion.Euler(currentRotation);
        defaultRotationSpeed = pivotRotationSpeed;
        //init roll segments (if segmented)
        if (restrictedRollMovement)
        {
            rollAnglePerSegment = Mathf.Floor((pivotRollMax - pivotRollMin) / restrictedRollSegments);
            if (restrictedRollSegments == 1)
            {
                currentRollSegment = 1;
            }
            else
            {
                currentRollSegment = Mathf.RoundToInt(restrictedRollSegments / 2);
            }            
            setRollSegment(currentRollSegment); //little fix, because segment roll euler degrees rounding to int
        }
        pivotCamera.transform.LookAt(this.transform);
        //determine camera zoom direction
        rotationDirection = Mathf.Sign(pivotCamera.transform.position.z);
        //init logging variables
        timeInit = Time.time;
    }

    private void Update()
    {
        //check for movement speed
        if (Input.GetKey(MoveSlow))
        {
            pivotRotationSpeed = slowRotationSpeed;
        }
        else if (Input.GetKeyUp(MoveSlow))
        {
            pivotRotationSpeed = defaultRotationSpeed;
        }

        //get movement input (yaw rotation)
        if (Input.GetKey(MoveLeft))
        {
            transform.RotateAround(this.transform.position, Vector3.up, pivotRotationSpeed * Time.deltaTime);
        }
        if (Input.GetKey(MoveRight))
        {
            transform.RotateAround(this.transform.position, Vector3.down, pivotRotationSpeed * Time.deltaTime);
        }
        //get movement input (roll rotation continuous (!restrictedRollMovement))
        if (!restrictedRollMovement && Input.GetKey(MoveUp))
        {
            //Debug.Log(this.transform.rotation.eulerAngles.x + " > " + pivotRollMin );
            if (this.transform.rotation.eulerAngles.x > pivotRollMin)
            {
                this.transform.Rotate(Vector3.left, pivotRotationSpeed * Time.deltaTime);
            }
            //correction
            currentRotation = this.transform.localRotation.eulerAngles;
            if (currentRotation.x > pivotRollMax) {
                currentRotation.x = pivotRollMin; //fix for 0-360 deg. jump
                this.transform.localRotation = Quaternion.Euler(currentRotation);
            }
            else if (currentRotation.x < pivotRollMin)
            {
                currentRotation.x = Mathf.Clamp(currentRotation.x, pivotRollMin, pivotRollMax);
                this.transform.localRotation = Quaternion.Euler(currentRotation);
            }
        }
        if (!restrictedRollMovement && Input.GetKey(MoveDown))
        {
            //Debug.Log(this.transform.rotation.eulerAngles.x + " < " + pivotRollMax);
            if (this.transform.rotation.eulerAngles.x < pivotRollMax)
            {
                this.transform.Rotate(Vector3.right, pivotRotationSpeed * Time.deltaTime);
            }
            //correction
            currentRotation = this.transform.localRotation.eulerAngles;
            if (currentRotation.x > pivotRollMax)
            {
                currentRotation.x = Mathf.Clamp(currentRotation.x, pivotRollMin, pivotRollMax);
                this.transform.localRotation = Quaternion.Euler(currentRotation);
            }
        }
        //get movement input (roll rotation segmented (restrictedRollMovement))
        if (restrictedRollMovement && Input.GetKeyDown(MoveUp))
        {
            setRollSegment(currentRollSegment + 1);
        }
        if (restrictedRollMovement && Input.GetKeyDown(MoveDown))
        {
            setRollSegment(currentRollSegment - 1);
        }
        //get zoom input
        if (Input.GetKey(MoveZoomIn))
        {
            pivotCamera.transform.Translate(Vector3.forward * (pivotRotationSpeed / 10) * Time.deltaTime);
            if (Vector3.Distance(this.transform.position, pivotCamera.transform.position) < pivotZoomMin)
            {
                pivotCamera.transform.position = this.transform.position;
                pivotCamera.transform.Translate(Vector3.back * pivotZoomMin);
            }            
        }
        if (Input.GetKey(MoveZoomOut))
        {
            pivotCamera.transform.Translate(Vector3.back * (pivotRotationSpeed / 10) * Time.deltaTime);
            if (Vector3.Distance(this.transform.position, pivotCamera.transform.position) > pivotZoomMax)
            {
                pivotCamera.transform.position = this.transform.position;
                pivotCamera.transform.Translate(Vector3.back * pivotZoomMax);
            }
        }
        //log output
        //logUserPosition();
    }

    //set segmented roll angle
    private void setRollSegment (int segment)
    {
        if (restrictedRollMovement && segment >= 1 && segment <= restrictedRollSegments)
        {
            currentRotation = this.transform.localRotation.eulerAngles;
            currentRotation.x = pivotRollMin + ((segment-1) * rollAnglePerSegment);
            this.transform.localRotation = Quaternion.Euler(currentRotation);
            currentRollSegment = segment;
        }
    }

    //log the user movement (to be implemented as needed...)
    private void logUserPosition()
    {
        timeCurrent = Time.time;
        Debug.Log(timeCurrent - timeInit +": " + "(" +
                  pivotCamera.transform.position.x + "," +
                  pivotCamera.transform.position.y + "," +
                  pivotCamera.transform.position.z + "), " +
                  "(" + pivotCamera.transform.rotation.eulerAngles + ")" );
        //add GetKey, GetKeyDown logging as desired. etc...
    }
}
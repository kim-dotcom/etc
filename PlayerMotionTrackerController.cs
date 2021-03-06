﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMotionTrackerController : MonoBehaviour
{
    //service variables, for dropdowns
    public enum ControllerTypes {keySimulation, motionTrackerSingle, motionTrackerDouble};
    public enum MotionMovementDirections {vertical, horizontal};
    public enum MotionTypes {continuous, limitedTime};
    private enum MotionTrackerOrder {undetermined, first, second};
    private enum MotionTrackerBeyondThreshold {none, first, second, both};
    private enum MotionStates {idle, accelerating, deccelerating, moving};
    //movement speed and potential acceleration
    [Header("User movement")]
    [Range(0.0f, 20.0f)]
    public float movementSpeed;
    [Range(0.0f, 50.0f)]
    public float gravity = 9.807f;
    public bool accelerateMovement;
    [Range(0f, 2f)]
    public float accelerateTime;

    //controller type (key simulation or real motion tracker)
    [Header("Controller Type")]
    public MotionTypes motionType;
    [Range(0.0f, 2f)]
    public float motionLimitedTime;
    public ControllerTypes controllerType;
    public KeyCode keyMovement;

    //objects to base the movement on
    [Header("Associated Objects")]
    public GameObject cameraObject;
    public GameObject controllerObject;
    public GameObject motionObject1;
    public GameObject motionObject2;

    //motion tracker extra settings
    [Header("Motion Tracker Settings")]
    public MotionMovementDirections motionDirection;
    [Range(0.0f, 0.5f)]
    public float motionThreshold;
    [Range(0.0f, 0.5f)]
    public float motionDelay = 0.15f;

    //auxiliaries
    private bool didInitialize;
    private bool didAccelerrationEnable;
    private bool isIndicatingMovement;
    private bool isFinishedWithCurrentMovement;
    private bool isMotionTrackerMovementLocked;
    private MotionStates isInMotionState = MotionStates.idle;
    private float timeMoving;
    private float timeAccelerating;
    private float timeDeccelerating;
    private float timeTillDelay;
    private float currentAccelerationSpeed;
    //ControllerTypes.motionTrackerDouble -- which leg to go next
    private MotionTrackerOrder nextTrackerOfTwoToMove = MotionTrackerOrder.undetermined;
    private Vector3 CameraForward;
    private Camera playerCamera;
    private CharacterController playerController;
    //get tracker base position (at init)
    private float motionTrackerVerticalBase1;
    private float motionTrackerVerticalBase2;
    private Vector2 motionTrackerHorizontalBase1;
    private Vector2 motionTrackerHorizontalBase2;
    private MotionTrackerBeyondThreshold trackerBeyondThreshold = MotionTrackerBeyondThreshold.none;
    private Vector3 movementDirection;
    

    void Start()
    {
        //ensure the script is set up to run correctly (these are mandatory)
        if (cameraObject == null || cameraObject.GetComponent<Camera>() == null)
        {
            Debug.LogWarning("Motion Tracker - nonexistent camera. Aborting script.");
        }
        else if (controllerObject == null || controllerObject.GetComponent<CharacterController>() == null)
        {
            Debug.LogWarning("Motion Tracker - nonexistent character controller. Aborting script.");
        }
        else if (controllerType == ControllerTypes.keySimulation && keyMovement == KeyCode.None)
        {
            Debug.LogWarning("Motion Tracker - nonexistent controller key. Aborting script.");
        }
        else if (controllerType == ControllerTypes.motionTrackerSingle && motionObject1 == null)
        {
            Debug.LogWarning("Motion Tracker - nonexistent movement tracker object. Aborting script.");
        }
        else if (controllerType == ControllerTypes.motionTrackerDouble
                 && (motionObject1 == null || motionObject2 == null))
        {
            Debug.LogWarning("Motion Tracker - nonexistent movement tracker objects. Aborting script.");
        }
        else if (movementSpeed == 0f || (motionType == MotionTypes.limitedTime && motionLimitedTime == 0))
        {
            Debug.LogWarning("Motion Tracker - motion speed/time set to zero. Aborting script.");
        }
        else if (motionThreshold == 0f && controllerType != ControllerTypes.keySimulation)
        {
            Debug.LogWarning("Motion Tracker - motion tracker threshold set to zero. Aborting script.");
        }
        else
        {
            playerCamera = cameraObject.GetComponent<Camera>();
            playerController = controllerObject.GetComponent<CharacterController>();
            Debug.Log("Motion Tracker setup succesfully verfied. Enabling.");
            didInitialize = true;
        }
        //verify movement acceleration (non-critical, but no sense of processing this is set to zero)
        if (accelerateMovement && accelerateTime == 0f)
        {
            Debug.Log("Motion Tracker - acceleration/decceleration of initial movement enabled but set to zero. "
                      + "Acceleration will be disabled.");
        }
        else if (accelerateMovement && accelerateTime > 0f)
        {
            didAccelerrationEnable = true;
        }
        //initialize base tracker position
        if (controllerType != ControllerTypes.keySimulation)
        {
            string calibrationData = "";
            if (motionObject1 != null)
            {
                motionTrackerVerticalBase1 = motionObject1.transform.position.y;
                motionTrackerHorizontalBase1 = new Vector2 (motionObject1.transform.position.x,
                                                            motionObject1.transform.position.z);
                calibrationData = motionObject1.transform.position.ToString();
            }
            if (motionObject2 != null)
            {
                motionTrackerVerticalBase2 = motionObject2.transform.position.y;
                motionTrackerHorizontalBase2 = new Vector2(motionObject2.transform.position.x,
                                                            motionObject2.transform.position.z);
                calibrationData += ", " + motionObject2.transform.position.ToString();
            }
            Debug.Log("Motion controller callibrated at " + calibrationData);
        }
        else
        {
            Debug.Log("Skipping motion controller callibration (keypress simulation instead).");
        }
    }

    void Update()
    {
        if (didInitialize)
        {
            //get the forward coordinates
            //Debug.Log(CameraForward);
            CameraForward = playerCamera.transform.forward;

            //determine if is indicating movement (keypresses are one-time only)
            if (controllerType == ControllerTypes.keySimulation)
            {
                if (Input.GetKeyDown(keyMovement))
                {
                    isIndicatingMovement = true;
                    isFinishedWithCurrentMovement = false;
                }
                if (Input.GetKeyUp(keyMovement))
                {
                    isIndicatingMovement = false;
                }
            }            
            //determine if is indicating movement (trackers are continuous)
            else if (controllerType != ControllerTypes.keySimulation)
            {
                if (getMotionTrackerActivation() && !isMotionTrackerMovementLocked)
                {
                    timeTillDelay += Time.deltaTime;
                    //execute after the delay
                    if (timeTillDelay >= motionDelay)
                    {
                        if (timeMoving >= motionLimitedTime && motionType == MotionTypes.limitedTime)
                        {
                            isIndicatingMovement = true;
                            isMotionTrackerMovementLocked = true;
                        }
                        else
                        {
                            isIndicatingMovement = true;
                            isFinishedWithCurrentMovement = false;
                        }
                    }
                                                        
                }
                else if (!getMotionTrackerActivation())
                {
                    timeTillDelay = 0f;
                    isIndicatingMovement = false;                    
                    if (isMotionTrackerMovementLocked)
                    {
                        isMotionTrackerMovementLocked = false;
                    }
                }
            }

            //get a move on (if any)
            validateMovement();
            getMovementStage();
            if (isInMotionState != MotionStates.idle && !isFinishedWithCurrentMovement)
            {
                getMoving();
            }
        }
    }

    //see if the motion tracked movement interface is currently beyond the motion threshold
    //only do that - no validations for acceleration/deccleration
    bool getMotionTrackerActivation()
    {
        //if motion tracker movement disabled, nothing
        if (controllerType == ControllerTypes.keySimulation)
        {
            return false;
        }
        //otherwise, verify
        else
        {
            //vertical movement simply comares the Y axis distance
            if (motionDirection == MotionMovementDirections.vertical)
            {
                if (controllerType == ControllerTypes.motionTrackerSingle)
                {
                    if (motionObject1.transform.position.y >= motionTrackerVerticalBase1 + motionThreshold)
                    {
                        trackerBeyondThreshold = MotionTrackerBeyondThreshold.first;
                    }
                    else
                    {
                        trackerBeyondThreshold = MotionTrackerBeyondThreshold.none;
                    }
                }
                else if (controllerType == ControllerTypes.motionTrackerDouble)
                {
                    if (motionObject1.transform.position.y >= motionTrackerVerticalBase1 + motionThreshold)
                    {
                        trackerBeyondThreshold = MotionTrackerBeyondThreshold.first;
                    }
                    if (motionObject2.transform.position.y >= motionTrackerVerticalBase2 + motionThreshold)
                    {
                        if (trackerBeyondThreshold == MotionTrackerBeyondThreshold.first)
                        {
                            trackerBeyondThreshold = MotionTrackerBeyondThreshold.both;
                        }
                        else
                        {
                            trackerBeyondThreshold = MotionTrackerBeyondThreshold.second;
                        }
                    }
                    else
                    {
                        trackerBeyondThreshold = MotionTrackerBeyondThreshold.none;
                    }
                }
            }
            //horizontal movement verifies the X/Z axis pythagorean distance
            else if (motionDirection == MotionMovementDirections.horizontal)
            {
                if (controllerType == ControllerTypes.motionTrackerSingle)
                {
                    if (Vector2.Distance(new Vector2(motionObject1.transform.position.x,
                                                     motionObject1.transform.position.z),
                                         motionTrackerHorizontalBase1) >= motionThreshold)
                    {
                        trackerBeyondThreshold = MotionTrackerBeyondThreshold.first;
                    }
                    else
                    {
                        trackerBeyondThreshold = MotionTrackerBeyondThreshold.none;
                    }
                }
            else if (controllerType == ControllerTypes.motionTrackerDouble)
            {
                    if (Vector2.Distance(new Vector2(motionObject1.transform.position.x,
                                                     motionObject1.transform.position.z),
                                         motionTrackerHorizontalBase1) >= motionThreshold)
                    {
                        trackerBeyondThreshold = MotionTrackerBeyondThreshold.first;
                    }
                    if (Vector2.Distance(new Vector2(motionObject2.transform.position.x,
                                                     motionObject2.transform.position.z),
                                         motionTrackerHorizontalBase2) >= motionThreshold)
                    {
                        if (trackerBeyondThreshold == MotionTrackerBeyondThreshold.first)
                        {
                            trackerBeyondThreshold = MotionTrackerBeyondThreshold.both;
                        }
                        else
                        {
                            trackerBeyondThreshold = MotionTrackerBeyondThreshold.second;
                        }
                    }
                    else
                    {
                        trackerBeyondThreshold = MotionTrackerBeyondThreshold.none;
                    }
                }
            }
            //so if at least one motion tracker activated, return true
            if (trackerBeyondThreshold != MotionTrackerBeyondThreshold.none)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    //valid, non-nonsensical motion tracker movement
    void validateMovement()
    {      
        if (controllerType == ControllerTypes.motionTrackerDouble)
        {            
            if (isInMotionState == MotionStates.idle && isIndicatingMovement)
            {
                //to determine which motion tracker comes next
                if (trackerBeyondThreshold == MotionTrackerBeyondThreshold.first ||
                    trackerBeyondThreshold == MotionTrackerBeyondThreshold.second)
                {
                    //first time determining (always valid, just set order)
                    if (nextTrackerOfTwoToMove == MotionTrackerOrder.undetermined)
                    {
                        if (trackerBeyondThreshold == MotionTrackerBeyondThreshold.first)
                        {
                            nextTrackerOfTwoToMove = MotionTrackerOrder.first;
                        }
                        else if (trackerBeyondThreshold == MotionTrackerBeyondThreshold.second)
                        {
                            nextTrackerOfTwoToMove = MotionTrackerOrder.second;
                        }
                    }
                    //regular left/right tracker variation (has to be varying, otherwise no move)
                    else if (nextTrackerOfTwoToMove == MotionTrackerOrder.first)
                    {
                        if  (trackerBeyondThreshold != MotionTrackerBeyondThreshold.first)
                        {
                            nextTrackerOfTwoToMove = MotionTrackerOrder.second;
                        }
                        else
                        {
                            isIndicatingMovement = false;
                        }                        
                    }
                    else if (nextTrackerOfTwoToMove == MotionTrackerOrder.second)
                    {
                        if (trackerBeyondThreshold != MotionTrackerBeyondThreshold.second)
                        {
                            nextTrackerOfTwoToMove = MotionTrackerOrder.first;
                        }
                        else
                        {
                            isIndicatingMovement = false;
                        }
                    }
                }
            }
        }
        //do single-controller-related validization
        else if (controllerType == ControllerTypes.motionTrackerSingle)
        {
            //...
        }
    }

    //determine the current stage of movement (mostly for acceleration/decceleration smoothing)
    void getMovementStage()
    {
        //to accelerate into moving (with acceleration) or start moving constantly (no acceleration)
        if (isInMotionState == MotionStates.idle && isIndicatingMovement)
        {
            if (didAccelerrationEnable)
            {
                isInMotionState = MotionStates.accelerating;
            }
            else
            {
                isInMotionState = MotionStates.moving;
            }
            //cleanup
            timeAccelerating = 0f;
            timeMoving = 0f;
            timeDeccelerating = 0f;
        }
        //to transform from movement acceleration into to a constant pace
        else if (isInMotionState == MotionStates.accelerating && didAccelerrationEnable)
        {
            timeAccelerating += Time.deltaTime;
            if (timeAccelerating >= accelerateTime)
            {
                isInMotionState = MotionStates.moving;
            }
        }
        //to keep moving at a constant pace
        else if (isInMotionState == MotionStates.moving && isIndicatingMovement)
        {
            timeMoving += Time.deltaTime;
            //in case of limited movement time type, cut it short if movement time out
            if (motionType == MotionTypes.limitedTime && timeMoving >= motionLimitedTime)
            {
                if (didAccelerrationEnable)
                {
                    isInMotionState = MotionStates.deccelerating;
                }
                else
                {
                    isInMotionState = MotionStates.idle;
                    isFinishedWithCurrentMovement = true;
                }             
            }
            else
            {
                //isInMotionState = MotionStates.moving; //keep going...
            }
        }
        //to deccelerate from moving (with accelecration), or to just stop (no acceleration)
        else if (isInMotionState == MotionStates.moving && !isIndicatingMovement)
        {
            if (didAccelerrationEnable)
            {
                isInMotionState = MotionStates.deccelerating;
            }
            else
            {
                isInMotionState = MotionStates.idle;
                isFinishedWithCurrentMovement = true;
            }
        }
        //to stop from decceleration to idle
        else if (isInMotionState == MotionStates.deccelerating && didAccelerrationEnable)
        {
            timeDeccelerating += Time.deltaTime;
            if (timeDeccelerating >= accelerateTime)
            {
                isInMotionState = MotionStates.idle;
                isFinishedWithCurrentMovement = true;
                //cleanup
                timeAccelerating = 0f;
                timeMoving = 0f;
                timeDeccelerating = 0f;
            }
        }
    }

    //move the user per movement stage/time they are in
    void getMoving()
    {
        //acceleration/decceleration speed is linear (for now...)
        if (isInMotionState == MotionStates.accelerating || isInMotionState == MotionStates.deccelerating)
        {
            if (isInMotionState == MotionStates.accelerating)
            {
                currentAccelerationSpeed = movementSpeed * (timeAccelerating / accelerateTime);
            }
            else
            {
                currentAccelerationSpeed = movementSpeed * (1 - (timeDeccelerating / accelerateTime));
            }
            movementDirection = (CameraForward).normalized * currentAccelerationSpeed;
        }
        //movement is at a constant speed...
        else if (isInMotionState == MotionStates.moving)
        {
            movementDirection = (CameraForward).normalized * movementSpeed;
        }
        else
        {
            movementDirection = Vector3.zero;
        }

        //"return" the move speed 
        movementDirection.y -= gravity;
        playerController.Move(movementDirection * Time.deltaTime);
    }
}
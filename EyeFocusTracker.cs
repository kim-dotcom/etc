/*
 * Edited script that was included in the ViveSR package (SRanipal_EyeFocusSample_v2).
 * It is supposed to obtain the data from the Focus function which enables to find the focal point of eyes in VR.
 * It should be paired with logger, that stores the coordinates of the focal point, coordinates of the camera
 * and time of the data capture. Also enables more debugging options as
 * the visualization of the focal point as gazeSphere, visualization of the gaze ray.
 */
//========= Copyright 2018, HTC Corporation. All rights reserved. ===========
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ViveSR.anipal.Eye;
using System.Globalization;
using System.Text;

public class EyeFocusTracker : MonoBehaviour
{
    //SRanipal objects/controls
    private FocusInfo focusInfo;
    private readonly GazeIndex[] gazePriority = new GazeIndex[] {
        GazeIndex.COMBINE,
    //  GazeIndex.LEFT,
    //  GazeIndex.RIGHT
    };

    private static EyeData_v2 EyeData = new EyeData_v2();
    private bool eyeCallbackRegistered = false;
    private GameObject gazeSphere;
    private int ignoreLayerBitMask;

    [SerializeField] private float maxDistance = 100;
    [SerializeField] private int ignoreLayer = 2;

    //visuals
    [Space(10)]
    [SerializeField] private LineRenderer gazeRayRenderer;
    [SerializeField] private bool renderGazeRay;
    [SerializeField] private bool drawGazeSphere;
    //if no ET data or !drawGazeSphere, hide the ET gaze sphere
    private Vector3 gazeSphereHiddenPosition = new Vector3(0f, -10000f, 0f);
    [SerializeField] private KeyCode GazeVisualizationKey;

    //calibration
    [Space(10)]
    [SerializeField] private bool runCalibrationOnStart;
    [SerializeField] private bool runCalibrationOnKeypress;
    [SerializeField] private KeyCode CalibrationKey;
    
    //Logging setup
    [Space(10)]
    public GameObject Logger;
    public string logName = "HtcEtLog";
    private bool loggerInitialized;
    private string customLogVariables;
    private Dictionary<string, string> LoggerFormatDictionary = new Dictionary<string, string>();
    [SerializeField] private bool logEtPosition = true;
    [SerializeField] private bool logFixatedObject = true;
    [SerializeField] private bool logBothEyes;
    [SerializeField] private bool logAccuracy;
    private NumberFormatInfo numberFormat;
    private string logData;
    private StringBuilder logDataSb;

    //GUI fixation feedback
    [Space(10)]
    [SerializeField] private bool logFixationToCanvas;
    [SerializeField] private Text FixationCanvas;
    private string fixationReport;

    private void Start()
    {
        if (!SRanipal_Eye_Framework.Instance.EnableEye)
        {
            enabled = false;
            return;
        }

        if (runCalibrationOnStart)
        {
            SRanipal_Eye_API.LaunchEyeCalibration(System.IntPtr.Zero);
        }

        /*GazeRayParameter param = new GazeRayParameter();
        param.sensitive_factor = 1;
        EyeParameter eyeParam = new EyeParameter();
        eyeParam.gaze_ray_parameter = param;
        SRanipal_Eye_API.SetEyeParameter(eyeParam);*/

        //instantiate the gaze sphere (either to be usede or not)
        gazeSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Collider gazeSphereCollider = gazeSphere.GetComponent<Collider>();
        gazeSphereCollider.enabled = false;        
        gazeSphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        gazeSphere.transform.position = gazeSphereHiddenPosition;
        gazeSphere.SetActive(drawGazeSphere);

        /*
        * ignoreLayer is a bit mask that tells which layers should be subjected to Physics.Raycast function,
        * this way it is inverted, so only the layer with id of ignoreLayer is really ignored.
        */
        ignoreLayerBitMask = 1 << ignoreLayer;
        ignoreLayerBitMask = ~ignoreLayerBitMask;

        //init the logger (PathScript, in this case)
        if (Logger != null && Logger.GetComponent<PathScript>() != null)
        {
            LoggerFormatDictionary = Logger.GetComponent<PathScript>().getLogFormat();
            //set number format, per what is specified in logger
            numberFormat = new NumberFormatInfo();
            numberFormat.NumberDecimalSeparator = LoggerFormatDictionary["decimalFormat"];
            generateCustomLogVariables();
            Logger.GetComponent<PathScript>().generateCustomFileNames(customLogVariables, logName, this.name);
            loggerInitialized = true;
            //init the stringbuilder
            logDataSb = new StringBuilder("", 256);
        }
        else
        {
            Debug.LogWarning("No eyetracking logger found on " + Logger.name +
                             ". Therefore, HTC ET script on " + this.name + " not logging.");
        }

        //init the Canvas GUI fixation logger
        if (FixationCanvas == null && logFixationToCanvas)
        {
        	Debug.LogWarning("No fixation canvas for ET logging. Disabling this functionality");
        	logFixationToCanvas = false;
        }
    }

    private void Update()
    {
        //listen to key control inputs
        if (runCalibrationOnKeypress && Input.GetKeyDown(CalibrationKey))
        {
            SRanipal_Eye_API.LaunchEyeCalibration(System.IntPtr.Zero);
        }
        if (Input.GetKeyDown(GazeVisualizationKey))
        {
            drawGazeSphere = !drawGazeSphere;
            gazeSphere.SetActive(drawGazeSphere);
        }
    }

    private void FixedUpdate()
    {
        if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
            SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;




        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eyeCallbackRegistered == false)
        {
            SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(
                Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eyeCallbackRegistered = true;
        }
        else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eyeCallbackRegistered == true)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(
                Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eyeCallbackRegistered = false;
        }

        foreach (GazeIndex index in gazePriority)
        {
            Ray GazeRay;
            bool eye_focus;

            //eye call back should be enabled so we can get extra EyeData and save them in a logger 
            if (eyeCallbackRegistered)
            {
                eye_focus = SRanipal_Eye_v2.Focus(index, out GazeRay, out focusInfo, 0,
                                                  maxDistance, ignoreLayerBitMask, EyeData);

                //Can be used if we don't want to specify layer to be ignored
                //eye_focus = SRanipal_Eye_v2.Focus(index, out GazeRay, out FocusInfo, 0, MaxDistance, eyeData);
            }
            else
            {
                eye_focus = SRanipal_Eye_v2.Focus(index, out GazeRay, out focusInfo, 0,
                                                  maxDistance, ignoreLayerBitMask);
            }
            //Debug.Log("Time_delta_unity: " + Time.deltaTime*1000 + "Time_unity: " + Time.time*1000 +
            //          " Frame_unity: " + Time.frameCount + " Time_ET: " + EyeData.timestamp +
            //          " Frame_ET: " + EyeData.frame_sequence);

            //ET Logging
            //if(focusInfo.collider != null) Debug.Log("Looking at: " + focusInfo.collider.gameObject.name);
            if (loggerInitialized)
            {
                logDataSb.Clear();
                //for 3D ET projection (collider hit) coordinate
                if (logEtPosition)
                {
                    logDataSb.Append(focusInfo.point.x + LoggerFormatDictionary["separatorFormat"] +
                                     focusInfo.point.y + LoggerFormatDictionary["separatorFormat"] +
                                     focusInfo.point.z);
                }
                //for collider hit name
                if (logFixatedObject)
                {
                    if (!string.IsNullOrEmpty(logData))
                    {
                        logDataSb.Append(LoggerFormatDictionary["separatorFormat"]);
                    }

                    if (focusInfo.collider == null)
                    {
                        if (eyeCallbackRegistered && EyeData.no_user)
                        {
                            fixationReport = "no user";
                        }
                        else
                        {
                            fixationReport = "no data";
                        }
                    }
                    else
                    {
                        fixationReport = focusInfo.collider.gameObject.name;
                    }
                    logDataSb.Append(fixationReport);
                }
                //extra data per left/right eye
                if (logBothEyes)
                {
                    //TODO...
                }
                //extra data per HTC eyetracker service variables
                if (logAccuracy)
                {
                    //TODO...
                }

                //finally, send to logger...
                Logger.GetComponent<PathScript>().logCustomData(logName, logDataSb.ToString(), this.gameObject);
            }

            //canvas logging
            if (logFixationToCanvas)
            {
               	FixationCanvas.text = fixationReport;
            }

            if (eye_focus)
            {
                //renders user-point
                // the line has coordinates of gaze ray previosly calulated by Focus function 
                if (renderGazeRay && gazeRayRenderer != null)
                {
                    Vector3 GazeDirectionCombined_FromFocus =
                        Camera.main.transform.TransformDirection(GazeRay.direction);
                    gazeRayRenderer.SetPosition(0, Camera.main.transform.position);
                    gazeRayRenderer.SetPosition(1, Camera.main.transform.position +
                                                GazeDirectionCombined_FromFocus * maxDistance);
                }

                if (drawGazeSphere)
                {
                    gazeSphere.transform.position = focusInfo.point;
                }
                break;
            }
        }
    }

    private void Release()
    {
        if (eyeCallbackRegistered == true)
        {
            SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(
                Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eyeCallbackRegistered = false;
        }
    }

    private static void EyeCallback(ref EyeData_v2 eye_data)
    {
        EyeData = eye_data;
    }

    public EyeData_v2 GetEyeData()
    {
        return EyeData;
    }

    public FocusInfo GetFocusInfo()
    {
        return focusInfo;
    }

    //returns just the first row for the log file to know what columns is what variable
    public string generateCustomLogVariables()
    {
        //for 3D ET projection (collider hit) coordinate
        if (logEtPosition)
        {
            customLogVariables += "EtPositionX" + LoggerFormatDictionary["separatorFormat"] +
                                  "EtPositionY" + LoggerFormatDictionary["separatorFormat"] +
                                  "EtPositionZ";
        }
        //for collider hit name
        if (logFixatedObject)
        {
            if (!string.IsNullOrEmpty(customLogVariables))
            {
                customLogVariables += LoggerFormatDictionary["separatorFormat"];
            }
            customLogVariables += "FixatedObjectName";
        }
        //extra data per left/right eye
        if (logBothEyes)
        {
            //TODO...
        }
        //extra data per HTC eyetracker service variables
        if (logAccuracy)
        {
            //TODO...
        }
        //this is to be added to the log that is to be created
        return customLogVariables;
    }

}

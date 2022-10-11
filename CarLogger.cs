/* RCC Car Logger, version 2022-10-11
 *
 * Usage: Log using PathScript, set logging interval and logged properties
 * (e.g., create a dummy Unity Hierarchy objects, put both PathScript and this CarLogger there)
 *        Logging interval is set per FixedUpdate (default (1) is 50 frames per second)
 *        PathScript: allow custom logs (true), select Camera (any inscene camera will do)
 * The logger finds the active car/camera in RCC by itself
 * 
 * To be able to log car state data, check RCC_SceneManager.cs - getter for car instance states may be needed
 *                                                               ...or public-izing the variables (dirty solution) 
 */

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class CarLogger : MonoBehaviour
{
    //generic logger object (PathScript) and logging setup
    public GameObject LoggerObject;
    private PathScript LoggerScript;
    public string customLogfileName = "carLog";
    [Range(1, 100)]
    public int logSkipInterval;
    [Space(10)]

    //logger settings
    public bool logUserCamera;
    public bool logCarState;                    //rpm, speed, gear, isParked, ABS,ESP, headlights

    //log data format
    private string dataformatDefault;
    private string dataformatCamera;
    private string dataformatOthers;
    private string dataformatConcatenated;

    //auxiliaries
    private Camera logCamera;
    private Vector3 cameraCoordinates;          //camera positional vector
    private Vector3 cameraDirection;            //camera directional vector
    private bool initValidized;
    private bool initWaited;
    private string separatorDecimal = ".";
    private string separatorItem = ",";
    private NumberFormatInfo numberFormat;
    private int logSkipIntervalCounter;

    void Start()
    {
        //check for script components and logger setup
        if (customLogfileName == "" || customLogfileName == null)
        {
            customLogfileName = "carLog";
            if (Application.isEditor)
            {
                Debug.LogWarning("No log name speified. Default log name was set to :" + customLogfileName);
            }
        }
        if (LoggerObject == null || LoggerObject.GetComponent<PathScript>() == null)
        {
            if (Application.isEditor)
            {
                Debug.LogWarning("Logger failed to initialize. Check Logger GameObject (PathScript).");
            }         
        }
        else
        {
            LoggerScript = LoggerObject.GetComponent<PathScript>();
            initValidized = true;
        }

        //init log file and its header
        if (initValidized)
        {
            //init file format
            initValidized = true;
            Dictionary<string, string> fileFormatDict = new Dictionary<string, string>();
            fileFormatDict = LoggerScript.getLogFormat();
            separatorItem = fileFormatDict["separatorFormat"];
            separatorDecimal = fileFormatDict["decimalFormat"];
            numberFormat = new NumberFormatInfo();
            numberFormat.NumberDecimalSeparator = separatorDecimal;

            logCamera = Camera.main;            //dirty solution, but RealisticCarController can have multiple cameras

            //init logger
            dataformatDefault = "userId" + separatorItem + "logId" + separatorItem + "timestamp" + separatorItem +
                                "hour" + separatorItem + "min" + separatorItem + "sec" + separatorItem + "ms";
            dataformatCamera = "dataType" + separatorItem +
                               "sourceX" + separatorItem + "sourceY" + separatorItem + "sourceZ" + separatorItem +
                               "vectorX" + separatorItem + "vectorY" + separatorItem + "vectorZ";
            dataformatOthers = "CarRpm" + separatorItem + "CarSpeed" + separatorItem + "carGear" + separatorItem +
                               "carParked" + separatorItem + "carAbs" + separatorItem + "carEsp" + separatorItem +
                               "carHeadlights";
            dataformatConcatenated = dataformatDefault;
            if (logUserCamera) dataformatConcatenated += separatorItem + dataformatCamera;
            if (logCarState) dataformatConcatenated += separatorItem + dataformatOthers;
            //dataformatConcatenated += "\r\n";
            LoggerScript.generateCustomFileNames(dataformatConcatenated, customLogfileName, gameObject.name);
            StartCoroutine(RaycastInit(2f));
        }
    }

    //RaycastLogger starts with delay (PathScript needs to initialize write-ready files first)
    public IEnumerator RaycastInit(float waitTime)
    {
        while (!initWaited)
        {
            yield return new WaitForSeconds(waitTime);
            StopCoroutine(RaycastInit(0f));
            initWaited = true;
        }
    }

    void FixedUpdate()
    {
        logSkipIntervalCounter++;
        if (initValidized && initWaited && (logSkipIntervalCounter % logSkipInterval == 0))
        {
            string logData = "";
            //camera data
            string coordinates = logCamera.transform.position.x.ToString(numberFormat) + separatorItem +
                                 logCamera.transform.position.y.ToString(numberFormat) + separatorItem +
                                 logCamera.transform.position.z.ToString(numberFormat);
            string rotationGaze = logCamera.transform.rotation.eulerAngles.x.ToString(numberFormat) + separatorItem +
                                  logCamera.transform.rotation.eulerAngles.y.ToString(numberFormat) + separatorItem +
                                  ((Mathf.Round(logCamera.transform.rotation.eulerAngles.z * 100)) / 100.0).ToString(numberFormat); //dirty fix
            logData = coordinates + separatorItem + rotationGaze + separatorItem;

            //car state data
            string isParked = (RCC_SceneManager.Instance.activePlayerVehicle.handbrakeInput > .1f ? true : false).ToString();
            string isAbs = (RCC_SceneManager.Instance.activePlayerVehicle.ABSAct).ToString();
            string isEsp = (RCC_SceneManager.Instance.activePlayerVehicle.ESPAct).ToString();
            string isHeadlights = (RCC_SceneManager.Instance.activePlayerVehicle.lowBeamHeadLightsOn ||
                                   RCC_SceneManager.Instance.activePlayerVehicle.highBeamHeadLightsOn).ToString();
            logData += RCC_SceneManager.Instance.activePlayerVehicle.engineRPM.ToString() + separatorItem +
                       RCC_SceneManager.Instance.activePlayerVehicle.speed.ToString() + separatorItem +
                       RCC_SceneManager.Instance.activePlayerVehicle.currentGear.ToString() + separatorItem +
                       isParked + separatorItem + isAbs + separatorItem + isEsp + separatorItem + isHeadlights;
            LoggerScript.logCustomData(customLogfileName, logData);
        }
    }
}

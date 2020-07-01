using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class FpsCounter : MonoBehaviour
{
    [Range(0.1f, 5f)]
    public float refreshEverySeconds = 1f;
    [Space(10)]
    public bool logToCanvas;
    public bool logToFile;
    public bool logToConsole;
    [Space(10)]

    public Text logCanvas;
    public string logFileName;
    public PathScript logger;
    
    private bool isValidized = true;
    [HideInInspector]
    public float currentLoggingDelay;
    [HideInInspector]
    public int frameCounter;
    [HideInInspector]
    public float currentFps;

    // Start is called before the first frame update
    void Start()
    {
        currentLoggingDelay = refreshEverySeconds;
        if (logCanvas == null && logToCanvas)
        {
            Debug.LogWarning("FPS Counter Error - No canvas to write to. Exiting...");
            isValidized = false;
        }
        if (logger == null && logToFile)
        {
            Debug.LogWarning("FPS Counter Error - No logger script to write to. Exiting...");
            isValidized = false;
        }
        else if (logToFile)
        {
            //create the logger file
            logger.generateCustomFileNames("fps", logFileName, "FpsCounter " + this.name);
        }
    }

    // Update is called once per frame
    void Update()
    {        
        if (isValidized)
        {
            //Debug.Log(Time.unscaledTime + " / " + currentLoggingDelay);
            if (Time.unscaledTime >= currentLoggingDelay)
            {
                //compute FPS
                //deltaTime, smoothDeltaTime, fixedDeltaTime, unscaledTime (?)
                currentFps = (int)(1f / Time.unscaledDeltaTime);
                currentLoggingDelay = Time.unscaledTime + refreshEverySeconds;                

                //write to a desired output
                if (logToCanvas)
                {
                    logCanvas.text = Mathf.Round(currentFps) + " FPS";
                }
                if (logToFile)
                {
                    logger.logCustomData(logFileName, currentFps.ToString());
                }
                if (logToConsole)
                {
                    Debug.Log("Running @" + currentFps + " FPS. Refresh @" + refreshEverySeconds + "s." );
                }
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screenshotter : MonoBehaviour
{
    public KeyCode screenshotKey;
    [Header("File path/naming")]
    public string saveLocation;
    public string attachedPrefix;
    public bool attachCount;
    [Header("Resolution")]
    [Range(1,4)]
    public int supersample = 1;

    private int fileCounter = 1;
    private string pathNamePrefix;
    private string pathNameSuffix;

    // Start is called before the first frame update
    void Start()
    {
        if (attachedPrefix != null)
        {
            attachedPrefix += "_";
        }
        pathNamePrefix = saveLocation + attachedPrefix;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            string thisFileName = pathNamePrefix + System.DateTime.Now.ToString("_yyyyMMdd_HHmmss");
            if (attachCount)
            {
                thisFileName += "_" + fileCounter;
                fileCounter++;
            }
            thisFileName += ".png";
            ScreenCapture.CaptureScreenshot(thisFileName, supersample);
            Debug.Log("Written " + thisFileName);
        }
    }
}

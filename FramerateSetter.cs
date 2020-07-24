using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FramerateSetter : MonoBehaviour
{
    [Range(5, 200)]
    public int updateFrequency = 60;
    [Range(5, 200)]
    public int fixedUpdateFrequency = 60;
    [Space(20)]

    public bool provideFpsData;
    public bool provideFpsToLog;
    public UnityEngine.UI.Text provideFpsToGui;
    private float[] currentFpsData;
    private int updateCount;
    private int fixedUpdateCount;
    private string fpsData;

    //=========================================================================

    void Start()
    {
        setUpdateFrequency(updateFrequency);
        setFixedUpdateFrequency(fixedUpdateFrequency);

        if (provideFpsData)
        {
            currentFpsData = new float[2];
            currentFpsData[0] = 0;
            currentFpsData[1] = 0;
            StartCoroutine(FpsLoop());
        }
    }

    void Update()
    {
        if (provideFpsData) updateCount += 1;
    }

    void FixedUpdate()
    {
        if (provideFpsData) fixedUpdateCount += 1;
    }

    //=========================================================================

    public void setUpdateFrequency(int frequency)
    {
        Application.targetFrameRate = frequency;
    }

    public void setFixedUpdateFrequency(int frequency)
    {
        Time.fixedDeltaTime = 1 / (float)frequency;
    }

    public float[] getFpsData()
    {
        if (provideFpsData)
        {
            return currentFpsData;
        }
        else
        {            
            return null;
        }
    }

    // Update both CountsPerSecond values every second.
    IEnumerator FpsLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            currentFpsData[0] = updateCount;
            currentFpsData[1] = fixedUpdateCount;
            fpsData = updateCount + " / " + fixedUpdateCount + " FPS";

            if (provideFpsToGui != null) provideFpsToGui.text = fpsData;
            if (provideFpsToLog) Debug.Log(fpsData);

            updateCount = 0;
            fixedUpdateCount = 0;
        }
    }
}

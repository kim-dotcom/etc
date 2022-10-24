// --------------------------------------------------------------------------------------------------------------------
// Driving task Manager, version 2022-10-24
// --------------------------------------------------------------------------------------------------------------------
// To set up driving tasks to eval (all set up in inspector).
// The manager then instantiates a series of DrivingTaskElement scripts to take care of individual task eval.
// Make sure a logger (Component PathScript) and eye-treacking script (implementation of ICarEyetracker) are referenced
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrivingTaskManager : MonoBehaviour
{
    public GameObject EtObject; //has implementation of ICarEyetracker assigned as component
    private ICarEyetracker EtScript;
    public GameObject LoggerObject; //has PathScript assigned as component
    private PathScript LoggerScript;
    //speed if checked through RCC_SceneManager.Instance.activePlayerVehicle.speed

    [System.Serializable]
    public struct DrivingTaskItem
    {
        public string taskLabel;
        public GameObject TriggerStartObject;
        public GameObject TriggerEndObject;
        public bool watchMinSpeed;        
        public bool watchMaxSpeed;        
        public bool watchBounds;        
        public bool watchObjects;        

        [Space(15)]
        [Range(0, 200)]
        public float minSpeed;
        [Range(0, 200)]
        public float maxSpeed;
        public GameObject[] WatchedBounds;
        public GameObject[] WatchedObjects;

        [Space(15)]
        [Range(0, 10)]
        public int pointsMinSpeed;
        [Range(0, 10)]
        public int pointsMaxSpeed;
        [Range(0, 10)]
        public int pointsBounds;
        [Range(0, 10)]
        public int[] pointsObjects;
    }
    public DrivingTaskItem[] DrivingTasks;

    void Awake()
    {
        int itemCounter = 1;
        foreach (DrivingTaskItem item in DrivingTasks)
        {
            GameObject NewDrivingTask = new GameObject();
            NewDrivingTask.AddComponent<DrivingTaskElement>();
            NewDrivingTask.GetComponent<DrivingTaskElement>().setLabel(item.taskLabel, itemCounter);
            NewDrivingTask.GetComponent<DrivingTaskElement>().setStartEndObjects(item.TriggerStartObject, item.TriggerEndObject);
            NewDrivingTask.GetComponent<DrivingTaskElement>().setMinSpeed(item.watchMinSpeed, item.minSpeed, item.pointsMinSpeed);
            NewDrivingTask.GetComponent<DrivingTaskElement>().setMaxSpeed(item.watchMaxSpeed, item.maxSpeed, item.pointsMaxSpeed);
            NewDrivingTask.GetComponent<DrivingTaskElement>().setBounds(item.watchBounds, item.WatchedBounds, item.pointsBounds);
            NewDrivingTask.GetComponent<DrivingTaskElement>().setWatchObjects(item.watchObjects, item.WatchedObjects, item.pointsObjects);
            NewDrivingTask.name = "DrivingTask_" + itemCounter;
            NewDrivingTask.transform.parent = this.transform;

            itemCounter++;
        }
    }

    void Start()
    {
        LoggerScript = LoggerObject.GetComponent<PathScript>();
        LoggerScript.generateCustomFileNames("driving tasks evaluation - freeform text log", "drivingTaskLog", gameObject.name);
        EtScript = EtObject.GetComponent<ICarEyetracker>();
    }

    public void logDrivingTaskResults (string results)
    {
        LoggerScript.logCustomData("drivingTaskLog", results);
    }

    public GameObject getEtObject()
    {
        return EtScript.getLookedAtObject();
    }
}

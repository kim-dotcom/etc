// --------------------------------------------------------------------------------------------------------------------
// Driving task Element, version 2022-10-24
// --------------------------------------------------------------------------------------------------------------------
// These are instantiated into runtime hierarchy view per DrivingTaskManager inspector settings
// From there on, they take care of themselves to evaluate their own logic.
// Once finished, the script sends a string of itrs eval to DrivingTaskManager and disables itself (no longer needed.)
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrivingTaskElement : MonoBehaviour
{
    private string taskLabel;
    private int taskNumber;
    private GameObject TriggerStartObject;
    private GameObject TriggerEndObject;
    private float minSpeed;
    private float maxSpeed;
    private GameObject[] WatchedBounds;
    private GameObject[] WatchedObjects;

    enum State {Preinit, Initialized, Running, Finished};
    private State TaskState = State.Preinit;

    private int pointsMinSpeed;
    private int pointsMaxSpeed;
    private int pointsBounds;
    private int[] pointsWatch;

    private int pointsMinSpeedInitial;
    private int pointsMaxSpeedInitial;
    private int pointsBoundsInitial;
    private int[] pointsWatchInitial;

    private bool watchMinSpeed;
    private bool watchMaxSpeed;
    private bool watchBounds;
    private bool watchObjects;

    private bool initializedStartEndObjects;
    private bool initializedMinSpeed;
    private bool initializedMaxSpeed;
    private bool initializedBounds;
    private bool initializedWatch;
    
    void Start()
    {
        if (validizeInstantiation())
        {
            TaskState = State.Initialized;
        }
    }

    //this is called externally by TriggerStartObject.OnCollisionEnter
    public void startTaskEvaluation()
    {
        if (TaskState == State.Initialized)
        {
            //start coroutine
            TaskState = State.Running;
        }
    }

    void FixedUpdate()
    {
        if (TaskState == State.Running)
        {
            if (watchMinSpeed && pointsMinSpeed != 0)
            {
                if (RCC_SceneManager.Instance.activePlayerVehicle.speed < minSpeed)
                {
                    pointsMinSpeed = 0;
                }
            }
            if (watchMaxSpeed && pointsMaxSpeed != 0)
            {
                if (RCC_SceneManager.Instance.activePlayerVehicle.speed > maxSpeed)
                {
                    pointsMinSpeed = 0;
                }
            }
            if (watchObjects)
            {
                GameObject CurrentlyWatchedObject = this.transform.parent.GetComponent<DrivingTaskManager>().getEtObject();
                for (int i = 0; i < WatchedObjects.Length; i++)
                {
                    if (WatchedObjects[i] == CurrentlyWatchedObject)
                    {
                        pointsWatch[i] = pointsWatchInitial[i];
                        //TODO: may want to implement a Time.deltaTime++ delay here (minimum time that object is observed)
                        break;
                    }
                }
            }
        }
    }

    //this is called externally by of the Objects in Bounds[].OnCollisionEnter
    public void reportBoundsCrossing()
    {
        if (TaskState == State.Running && watchBounds && (pointsBounds != 0))
        {
            pointsBounds = 0;
        }
    }

    //this is called externally by TriggerStopObject.OnCollisionEnter
    public void stopTaskEvaluation()
    {
        if (TaskState == State.Running)
        {
            //stop coroutine            
            TaskState = State.Finished;
            //evaluate output and log
            int sumEtPoints = 0;
            int sumEtPointsAwarded = 0;
            for (int i = 0; i < pointsWatchInitial.Length; i++)
            {
                sumEtPoints += pointsWatchInitial[i];
                sumEtPointsAwarded += pointsWatch[i];
            }
            string taskResults = "Evaluation of driving task no. " + taskNumber + " (" + taskLabel + "):\r\n" +
                                 "--------------------------------------------------------------------------------" +
                                 "minSpeed: " + watchMinSpeed + "; " +
                                 "maxSpeed: " + watchMaxSpeed + "; " +
                                 "bounds: " + watchBounds + "; " +
                                 "ET objects: " + WatchedObjects + "; " +
                                 "--------------------------------------------------------------------------------\r\n";
            string taskEval = "";
            if (watchMinSpeed) { taskEval += "minSpeed: " + pointsMinSpeed + "/" + pointsMinSpeedInitial + "\r\n"; }
            if (watchMaxSpeed) { taskEval += "maxSpeed: " + pointsMaxSpeed + "/" + pointsMaxSpeedInitial + "\r\n"; }
            if (watchBounds) { taskEval += "bounds: " + pointsBounds + "/" + pointsBoundsInitial + "\r\n"; }
            if (watchObjects) {
                taskEval += "ET objects: " + sumEtPointsAwarded + "/" + sumEtPoints + ". Detail overview: \r\n";
                for (int i = 0; i < pointsWatchInitial.Length; i++)
                {
                    taskEval += "-- " + WatchedObjects[i].name +
                                ": " + pointsWatch[i] + "/" + pointsWatchInitial[i] + "\r\n";
                }
            }
            taskResults += taskEval;
            this.transform.parent.GetComponent<DrivingTaskManager>().logDrivingTaskResults(taskResults);
            this.gameObject.SetActive(false);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------

    public void setLabel(string label, int number)
    {
        taskLabel = label;
        taskNumber = number;
    }

    public void setStartEndObjects(GameObject StartObject, GameObject EndObject)
    {
        if (StartObject != null && EndObject != null
            && StartObject.GetComponent<Collider>() != null && EndObject.GetComponent<Collider>() != null)
        {
            this.TriggerStartObject = StartObject;
            this.TriggerEndObject = EndObject;
            TriggerStartObject.AddComponent<DrivingTaskCollisionDetection>();
            TriggerStartObject.GetComponent<DrivingTaskCollisionDetection>()
                .initializeCollider(this.gameObject, DrivingTaskCollisionDetection.DetectionType.start);
            TriggerEndObject.AddComponent<DrivingTaskCollisionDetection>();
            TriggerEndObject.GetComponent<DrivingTaskCollisionDetection>()
                .initializeCollider(this.gameObject, DrivingTaskCollisionDetection.DetectionType.stop);
            initializedStartEndObjects = true;            
        }
        else
        {
            initializedStartEndObjects = false;
        }
    }

    public void setMinSpeed(bool watchSpeed, float speed, int points)
    {
        if (setSpeed(watchSpeed, speed, points, "min"))
        {
            initializedMinSpeed = true;
        }
    }

    public void setMaxSpeed(bool watchSpeed, float speed, int points)
    {
        if (setSpeed(watchSpeed, speed, points, "max"))
        {
            initializedMaxSpeed = true;
        }
    }

    public bool setSpeed(bool watchSpeed, float speed, int points, string type)
    {
        if (watchSpeed == false)
        {
            return true;
        }
        else
        {
            switch (type)
            {
                case "min":
                    minSpeed = speed;                    
                    pointsMinSpeed = points; //speed is assumed valid (full points) until crossed
                    pointsMinSpeedInitial = points;
                    return true;
                case "max":
                    maxSpeed = speed;
                    pointsMaxSpeed = points; //speed is assumed valid (full points) until crossed
                    pointsMaxSpeedInitial = points;
                    return true;
                default:
                    return false;
            }
        }
    }

    public void setBounds(bool watchBounds, GameObject[] Bounds, int points)
    {
        if (!watchBounds)
        {
            initializedBounds = true;
        }
        else
        {
            WatchedBounds = Bounds;
            pointsBounds = points; //bounds are assumed valid (full points) until crossed
            pointsBoundsInitial = points;
            for (int i = 0; i < WatchedBounds.Length; i++)
            {
                WatchedBounds[i].AddComponent<DrivingTaskCollisionDetection>();
                WatchedBounds[i].GetComponent<DrivingTaskCollisionDetection>()
                    .initializeCollider(this.gameObject, DrivingTaskCollisionDetection.DetectionType.bounds);
            }
            initializedBounds = true;
        }
    }

    public void setWatchObjects(bool watchObjects, GameObject[] Objects, int[] points)
    {
        if (!watchObjects)
        {
            initializedWatch = true;
        }
        else
        {
            if (Objects.Length == points.Length)
            {
                WatchedObjects = Objects;
                pointsWatchInitial = points;
                pointsWatch = points;
                for (int i = 0; i < pointsWatch.Length; i++) { pointsWatch[i] = 0; }
                //points[] are not awarded yet (0), as they are considered invalid until looked at
                initializedWatch = true;
                
            }
            else
            {
                initializedWatch = false;
            }
            
        }
    }

    public bool validizeInstantiation()
    {
        if (initializedStartEndObjects && initializedMinSpeed && initializedMaxSpeed
            && initializedBounds && initializedWatch)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

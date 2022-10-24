// --------------------------------------------------------------------------------------------------------------------
// Driving task Collision Detection, version 2022-10-24
// --------------------------------------------------------------------------------------------------------------------
// Dumb script attached to triggerable objects. Reports to assigned Driving Task Element, onCollisionEnter.
// Make sure these objects have colliders!
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrivingTaskCollisionDetection : MonoBehaviour
{
    private GameObject parentTaskObject;
    public enum DetectionType { start, stop, bounds }
    public DetectionType typeOfCollision;
    private bool initialized;

    public void initializeCollider(GameObject parent, DetectionType typeCol)
    {
        parentTaskObject = parent;
        typeOfCollision = typeCol;
        initialized = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (typeOfCollision == DetectionType.start && initialized)
        {
            parentTaskObject.GetComponent<DrivingTaskElement>().startTaskEvaluation();
        }
        else if (typeOfCollision == DetectionType.stop && initialized)
        {
            parentTaskObject.GetComponent<DrivingTaskElement>().stopTaskEvaluation();
        }
        else if (typeOfCollision == DetectionType.bounds && initialized)
        {
            parentTaskObject.GetComponent<DrivingTaskElement>().reportBoundsCrossing();
        }
    }
}

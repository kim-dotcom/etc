// --------------------------------------------------------------------------------------------------------------------
// Car Eyetracker interface, version 2022-10-24
// --------------------------------------------------------------------------------------------------------------------
// These are instatntiated into runtime hierarchy view per DrivingTaskManager inspector settings
// From there on, they take care of themselves to evaluate their own logic.
// Once finished, the script sends a string of itrs eval to DrivingTaskManager and disables itself (no longer needed.)
// --------------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICarEyetracker
{
    GameObject getLookedAtObject();
    Vector3 getLookedFromPosition();
    Vector3 getLookedFromDirection();
    Vector3 getLookedAtCoordinate();

    //TODO: perhaps consider an optional physiological ET output data (dummy eyetrackers can return null...)
}

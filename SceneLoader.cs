using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [System.Serializable]
    public struct SceneList
    {
        public KeyCode sceneSwitchKey;
        public string sceneName;
    }
    //for seamless scene switching, a preset of scenes has to be:
        //included in build settings
        //copied and pasted into all the scenes with the same key/scene mappings
    public SceneList[] sceneSet;

    void Update()
    {
        foreach (SceneList thisScene in sceneSet)
        {
            if (Input.GetKeyDown(thisScene.sceneSwitchKey))
            {
                if (thisScene.sceneName != null) {
                    Debug.Log("Switching to scene " + thisScene.sceneName);
                    SceneManager.LoadScene(thisScene.sceneName);
                }
            }
        }
    }
}

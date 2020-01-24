using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneLoader2 : MonoBehaviour
{
	//unique keycode
    public List<KeyCode> loadKeys;
    //unique scene name
		//has to be specified without the ".unity" suffix
		//has to be also added to the build setting / scene order
    public List<string> loadScenes;

    void Update()
    {
        foreach (KeyCode key in loadKeys)
        {
            if (Input.GetKeyDown(loadKeys[loadKeys.IndexOf(key)]) && loadScenes[loadKeys.IndexOf(key)] != null) {
                Application.LoadLevel(loadScenes[loadKeys.IndexOf(key)]);
            }
        }
    }
}

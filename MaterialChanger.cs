using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialChanger : MonoBehaviour
{
    [System.Serializable]
    public struct MaterialChangerData
    {
        public Material material1;
        public Material material2;
        public KeyCode switchKey;
        public GameObject switchableObject;
        [HideInInspector]
        public MeshRenderer objectRenderer;
        [HideInInspector]
        public bool isSwitched;
    }
    public MaterialChangerData[] changerList;

    //init first material on start
    void Start()
    {
        for (int i = 0; i < changerList.Length; i++)
        {
            changerList[i].isSwitched = false;
            changerList[i].objectRenderer = changerList[i].switchableObject.GetComponent<MeshRenderer>();
            changerList[i].objectRenderer.material = changerList[i].material1;
        }
    }

    //switch materials on keypress
    void Update()
    {
        for (int i = 0; i < changerList.Length; i++)
        {
            if (Input.GetKeyDown(changerList[i].switchKey))
            {
                if (!changerList[i].isSwitched) {
                    changerList[i].objectRenderer.material = changerList[i].material2;
                }
                else
                {
                    changerList[i].objectRenderer.material = changerList[i].material1;
                }
                changerList[i].isSwitched = !changerList[i].isSwitched;
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseRotator : MonoBehaviour
{
    // Handles
    [Header("Handles")]
    [SerializeField]
    private Camera m_Camera;

    // Look sensitivity variable
    [Range(0.0f, 5.0f)]
    public float m_LookSensitivity = 1.0f;

    private float m_MouseX;
    private float m_MouseY;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        Rotate();
    }

    private void Rotate()
    {
        // Receive mouse input and modifies
        m_MouseX += Input.GetAxisRaw("Mouse X") * m_LookSensitivity;
        m_MouseY += Input.GetAxisRaw("Mouse Y") * m_LookSensitivity;

        // Keep mouseY between -90 and +90
        m_MouseY = Mathf.Clamp(m_MouseY, -90.0f, 90.0f);

        // Rotate the player object around the y-axis
        transform.localRotation = Quaternion.Euler(Vector3.up * m_MouseX);
        // Rotate the camera around the x-axis
        m_Camera.transform.localRotation = Quaternion.Euler(Vector3.left * m_MouseY);
    }
}

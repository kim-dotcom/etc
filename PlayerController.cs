using UnityEngine;

public class PlayerController : MonoBehaviour
{
    /* The "m_" in front of a variable is a coding convention that helps you seperate
     * Global and Local variables at a glance. It stands for "member variable" and
     * Unity automatically removes the "m_" in the Editor so that they don't look weird
     * in the Inspector!
     */

    // Handles
    [Header("Handles")]
    [SerializeField]
    private Camera m_Camera;
    [SerializeField]
    private CharacterController m_CharacterController;

    // Movement speeds
    [Header("Speed Variables")]
    [SerializeField]
    private float m_MoveSpeed = 5.0f;
    [SerializeField]
    private float m_JumpForce = 5.0f;
    [SerializeField]
    private float m_GravityForce = 9.807f;

    // Look sensitivity variable
    [Range(0.0f, 5.0f)]
    public float m_LookSensitivity = 1.0f;

    [Header("Debugging Variables")]
    /*Store rotation values from mouse input*/
    [SerializeField]
    private float m_MouseX;
    [SerializeField]
    private float m_MouseY;

    [SerializeField]
    private Vector3 m_MoveDirection;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Rotate();
        Movement();
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

    private void Movement()
    {
        // If the player is touching the ground
        if (m_CharacterController.isGrounded)
        {
            // Receive user input for movement
            Vector3 forwardMovement = transform.forward * Input.GetAxisRaw("Vertical");
            Vector3 strafeMovement = transform.right * Input.GetAxisRaw("Horizontal");
            // Convert Input into a Vector3
            m_MoveDirection = (forwardMovement + strafeMovement).normalized * m_MoveSpeed;

            // If user presses the "jump" button
            if (Input.GetKeyDown(KeyCode.Space))
                m_MoveDirection.y = m_JumpForce; // Jump
        }

        // Calculate gravity and modify movement vector as such
        m_MoveDirection.y -= m_GravityForce * Time.deltaTime;

        // Move the player using the movement vector
        m_CharacterController.Move(m_MoveDirection * Time.deltaTime);
    }
}

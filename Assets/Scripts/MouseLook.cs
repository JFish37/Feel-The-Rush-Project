using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public Transform playerBody;     // Reference to the player 
    public float mouseSensitivity = 100f;

    float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Hides and locks cursor to center
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Prevent flipping over

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // Vertical camera look
        playerBody.Rotate(Vector3.up * mouseX); // Rotate player left/right
    }
}

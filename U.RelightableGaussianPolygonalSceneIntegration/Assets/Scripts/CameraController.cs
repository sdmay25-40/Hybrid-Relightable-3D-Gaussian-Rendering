using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // reference
    [SerializeField] private Camera cam;
    // settings
    [SerializeField] private float movementSpeed = 5.0f;
    [SerializeField] private float mouseSensitivity = 100.0f;
    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // mouse movement
        yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // keyboard movement
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 movementVector = transform.right * horizontal + transform.forward * vertical;
        transform.position += movementVector.normalized * movementSpeed * Time.deltaTime;
    }
}

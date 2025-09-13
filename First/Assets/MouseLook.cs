using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody; 
    float xRotation = 0f; // Pitch
    float yRotation = 0f; // Yaw
    float zRotation = 0f; // Roll

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Nhận thêm input xoay Z (có thể map sang phím Q/E hoặc chuột ngang)
        if (Input.GetKey(KeyCode.Q)) zRotation += 50f * Time.deltaTime;
        if (Input.GetKey(KeyCode.E)) zRotation -= 50f * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;

        // Giới hạn nhìn lên/xuống
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        // Áp dụng xoay (Pitch, Yaw, Roll)
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
    }
}

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 5f;         // Tốc độ đi bộ
    public float runSpeed = 9f;          // Tốc độ chạy
    public float lookSensitivity = 2f;   // Độ nhạy chuột
    public float gravity = -9.81f;       // Trọng lực
    public Transform cameraTransform;    // Camera gắn vào đầu

    private CharacterController controller;
    private float verticalVelocity;      // Tốc độ rơi
    private float cameraPitch = 0f;      // Góc nhìn dọc

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; // Khóa chuột
    }

    void Update()
    {
        Move();
        Look();
    }

    void Move()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S

        // Kiểm tra có đang giữ Shift để chạy không
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        // Di chuyển theo hướng camera
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Áp dụng trọng lực
        if (controller.isGrounded)
        {
            verticalVelocity = -1f; // Giữ dính mặt đất
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;

        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        // Xoay player theo trục Y (trái/phải)
        transform.Rotate(Vector3.up * mouseX);

        // Xoay camera theo trục X (lên/xuống)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }
}
//cap nhat controller nhan vat
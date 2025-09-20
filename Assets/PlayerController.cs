using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;      // Tốc độ đi bộ
    public float runSpeed = 9f;       // Tốc độ chạy
    public float lookSensitivity = 2f;// Độ nhạy chuột
    public float gravity = -9.81f;    // Trọng lực

    [Header("Camera")]
    public Transform cameraTransform; // Camera gắn vào player

    [Header("Stamina Settings")]
    public float maxStamina = 5f;     // Thời gian chạy tối đa (giây)
    public float staminaRegenRate = 1f; // Tốc độ hồi stamina mỗi giây
    private float stamina;            // Giá trị stamina hiện tại

    [Header("Breathing Sounds")]
    public AudioSource breathingAudio; // Gắn audio source vào Player
    public AudioClip heavyBreathing;   // Âm thở gấp khi kiệt sức

    private CharacterController controller;
    private float verticalVelocity;    // Tốc độ rơi
    private float cameraPitch = 0f;    // Góc nhìn dọc
    private bool exhausted = false;    // Có bị kiệt sức không?

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; // Khóa chuột
        stamina = maxStamina; // Khởi tạo stamina đầy
    }

    void Update()
    {
        Move();
        Look();
        HandleBreathing();
    }

    void Move()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S

        bool wantsToRun = Input.GetKey(KeyCode.LeftShift) && (moveX != 0 || moveZ != 0);

        // --- Logic stamina ---
        if (wantsToRun && stamina > 0 && !exhausted)
        {
            stamina -= Time.deltaTime; // Giảm stamina khi chạy
            if (stamina <= 0)
            {
                stamina = 0;
                exhausted = true; // Khi chạm 0 thì kiệt sức
            }
        }
        else
        {
            // Hồi stamina khi không chạy
            stamina += staminaRegenRate * Time.deltaTime;
            if (stamina >= maxStamina)
            {
                stamina = maxStamina;
                exhausted = false; // Đầy lại thì hết kiệt sức
            }
        }

        // Tốc độ tùy theo stamina
        float currentSpeed = (wantsToRun && !exhausted) ? runSpeed : walkSpeed;

        // --- Movement ---
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        if (controller.isGrounded)
            verticalVelocity = -1f; // Giữ player dính đất
        else
            verticalVelocity += gravity * Time.deltaTime;

        move.y = verticalVelocity;

        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        // Xoay player trái/phải
        transform.Rotate(Vector3.up * mouseX);

        // Xoay camera lên/xuống
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    void HandleBreathing()
    {
        if (exhausted && !breathingAudio.isPlaying)
        {
            breathingAudio.clip = heavyBreathing;
            breathingAudio.loop = false;
            breathingAudio.Play();
        }
    }

    // --- Getter cho stamina (dùng nếu sau này muốn UI hiển thị) ---
    public float GetStaminaPercent()
    {
        return stamina / maxStamina;
    }
}

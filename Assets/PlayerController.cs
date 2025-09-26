using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;      
    public float runSpeed = 9f;       
    public float lookSensitivity = 2f;
    public float gravity = -9.81f;    

    [Header("Camera")]
    public Transform cameraTransform; 

    [Header("Stamina Settings")]
    public float maxStamina = 5f;     
    public float staminaRegenRate = 1f; 
    private float stamina;            
    private bool exhausted = false;   

    [Header("Breathing Sounds")]
    public AudioSource breathingAudio;
    public AudioClip heavyBreathing;  

    private CharacterController controller;
    private float verticalVelocity;   
    private float cameraPitch = 0f;   

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; 
        stamina = maxStamina; 
    }

    void Update()
    {
        Move();
        Look();
        HandleBreathing();
    }

    void Move()
    {
        float moveX = Input.GetAxis("Horizontal"); 
        float moveZ = Input.GetAxis("Vertical");   

        bool wantsToRun = Input.GetKey(KeyCode.LeftShift) && (moveX != 0 || moveZ != 0);

        // --- Logic stamina ---
        if (wantsToRun && stamina > 0 && !exhausted)
        {
            stamina -= Time.deltaTime; 
            if (stamina <= 0)
            {
                stamina = 0;
                exhausted = true; 
            }
        }
        else
        {
            stamina += staminaRegenRate * Time.deltaTime;
            if (stamina >= maxStamina)
            {
                stamina = maxStamina;
                exhausted = false; 
            }
        }

        float currentSpeed = (wantsToRun && !exhausted) ? runSpeed : walkSpeed;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        if (controller.isGrounded)
            verticalVelocity = -1f; 
        else
            verticalVelocity += gravity * Time.deltaTime;

        move.y = verticalVelocity;

        // Di chuyển
        controller.Move(move * currentSpeed * Time.deltaTime);

        // ----------------------------
        // 🔊 Gọi NoiseEmitter ở đây
        // ----------------------------
        if (moveX != 0 || moveZ != 0) 
        {
            if (wantsToRun && !exhausted)
            {
                NoiseEmitter.EmitNoise(transform.position, 1.4f); // chạy
            }
            else
            {
                NoiseEmitter.EmitNoise(transform.position, 0.6f); // đi bộ
            }
        }
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        transform.Rotate(Vector3.up * mouseX);

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

            // 🔊 Thở gấp cũng phát noise (quái nghe thấy)
            NoiseEmitter.EmitNoise(transform.position, 0.8f);
        }
    }

    public float GetStaminaPercent()
    {
        return stamina / maxStamina;
    }
}

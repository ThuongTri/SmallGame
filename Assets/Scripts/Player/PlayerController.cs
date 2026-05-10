using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;      
    public float runSpeed = 9f;       
    public float lookSensitivity = 2f;
    public float gravity = -9.81f;    

    [Header("Camera")]
    // TÔI KHÔNG THAY ĐỔI GÌ Ở ĐÂY.
    // HÃY KÉO VCam_PlayerFollow CỦA BẠN VÀO Ô NÀY TRONG INSPECTOR.
    public Transform cameraTransform;  

    [Header("Stamina Settings")]
    public float maxStamina = 5f;     
    public float staminaRegenRate = 1f; 
    private float stamina;            
    private bool exhausted = false;     

    [Header("Breathing Sounds")]
    public AudioSource breathingAudio;
    public AudioClip heavyBreathing;  

    [Header("Directors")]
    public GameDirector gameDirector; // optional: report sprinting to dynamic difficulty

    private CharacterController controller;
    private float verticalVelocity;   
    private float cameraPitch = 0f;   

    // === BIẾN MỚI ĐỂ KHÓA INPUT ===
    private bool isInputLocked = false; 
    private bool isMovementLocked = false;
    private float cutsceneLookScale = 1f;
    // =============================

    // Sprint reporting state
    private bool wasSprinting = false;
    private float sprintAccumulatedSeconds = 0f;
    Coroutine panicRoutine;
    float externalSpeedMultiplier = 1f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false; // Thêm dòng này để ẩn chuột
        stamina = maxStamina; 
    }

    void Update()
    {
        // Chúng ta vẫn chạy HandleBreathing()
        // để stamina có thể hồi khi đang trong cutscene
        Move();
        Look();
        HandleBreathing();
    }

    void Move()
    {
        // === CHECK KHÓA INPUT ===
        if (isInputLocked || isMovementLocked) return; 
        // ========================

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

        // --- Report sprint to GameDirector ---
        bool isSprinting = wantsToRun && !exhausted;
        if (isSprinting)
        {
            // accumulate sprint time while sprinting
            sprintAccumulatedSeconds += Time.deltaTime;
            // call once on sprint start (small nudge)
            if (!wasSprinting && gameDirector != null)
            {
                gameDirector.OnPlayerSprinted(0.2f);
            }
            // if sprinting for at least 0.5s, report chunk and reset accumulator
            if (sprintAccumulatedSeconds >= 0.5f && gameDirector != null)
            {
                gameDirector.OnPlayerSprinted(sprintAccumulatedSeconds);
                sprintAccumulatedSeconds = 0f;
            }
        }
        else
        {
            // reset accumulation when player stops sprinting
            sprintAccumulatedSeconds = 0f;
        }
        wasSprinting = isSprinting;

        float currentSpeed = (wantsToRun && !exhausted) ? runSpeed : walkSpeed;
        currentSpeed *= Mathf.Max(0.2f, externalSpeedMultiplier);

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
        // === CHECK KHÓA INPUT ===
        if (isInputLocked) return; 
        // ========================

        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;
        mouseX *= cutsceneLookScale;
        mouseY *= cutsceneLookScale;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);

        // Code của bạn đã chuẩn, chỉ cần gán đúng VCam_PlayerFollow
        // vào 'cameraTransform' là nó sẽ chạy
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

    public void PanicDrainStamina(float seconds, float drainPerSecond, bool forceBreathingLoop = true)
    {
        if (panicRoutine != null) StopCoroutine(panicRoutine);
        panicRoutine = StartCoroutine(PanicDrainRoutine(seconds, drainPerSecond, forceBreathingLoop));
    }

    IEnumerator PanicDrainRoutine(float seconds, float drainPerSecond, bool forceBreathingLoop)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            stamina -= Mathf.Max(0f, drainPerSecond) * Time.deltaTime;
            if (stamina <= 0f)
            {
                stamina = 0f;
                exhausted = true;
            }

            if (forceBreathingLoop && breathingAudio != null && heavyBreathing != null)
            {
                if (!breathingAudio.isPlaying)
                {
                    breathingAudio.clip = heavyBreathing;
                    breathingAudio.loop = true;
                    breathingAudio.Play();
                }
            }

            yield return null;
        }

        if (breathingAudio != null)
        {
            breathingAudio.loop = false;
            // Let natural exhaustion logic continue if still exhausted
            if (!exhausted && breathingAudio.isPlaying && breathingAudio.clip == heavyBreathing)
                breathingAudio.Stop();
        }

        panicRoutine = null;
    }

    // =======================================================
    // === CÁC HÀM MỚI DÙNG CHO CUTSCENE (SIGNAL RECEIVER) ===
    // =======================================================

    /// <summary>
    /// Hàm này được gọi bởi Signal Receiver trên Timeline để khóa input.
    /// </summary>
    public void LockPlayerInput()
    {
        isInputLocked = true;
        
        // Hiện con trỏ chuột để bấm menu (nếu cutscene có lựa chọn)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Hàm này được gọi bởi Signal Receiver trên Timeline để mở khóa input.
    /// </summary>
    public void UnlockPlayerInput()
    {
        isInputLocked = false;
        
        // Khóa con trỏ chuột lại cho gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void LockMovementOnly()
    {
        isMovementLocked = true;
    }

    public void UnlockMovementOnly()
    {
        isMovementLocked = false;
    }

    public void SetCutsceneLookScale(float scale01)
    {
        cutsceneLookScale = Mathf.Clamp(scale01, 0f, 1f);
    }

    public void SetExternalSpeedMultiplier(float multiplier)
    {
        externalSpeedMultiplier = Mathf.Clamp(multiplier, 0.2f, 2.5f);
    }
}
using UnityEngine;

/// <summary>
/// Simple surface-based footstep sounds.
/// Requires a CharacterController and an AudioSource on the player.
/// Uses raycast down to detect surface via tags/layers and picks clips accordingly.
/// Volume scales up when sprinting.
/// </summary>
public class FootstepAudio : MonoBehaviour
{
    public CharacterController controller;
    public AudioSource audioSource;

    [Header("Footstep timing")]
    public float walkStepInterval = 0.5f;
    public float runStepInterval = 0.32f;

    [Header("Clips by surface tag")]
    public AudioClip[] dirtClips;
    public AudioClip[] grassClips;
    public AudioClip[] woodClips;
    public AudioClip[] stoneClips;

    [Header("Detection")]
    public LayerMask groundMask = ~0;
    public float raycastHeight = 1.2f;

    float stepTimer;

    void Reset()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
    }

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    void Update()
    {
        bool isGrounded = controller != null && controller.isGrounded;
        Vector3 horizontalVel = controller != null ? new Vector3(controller.velocity.x, 0f, controller.velocity.z) : Vector3.zero;
        float speed = horizontalVel.magnitude;
        bool isMoving = speed > 0.1f;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && isMoving;

        if (!isGrounded || !isMoving)
        {
            stepTimer = 0f;
            return;
        }

        stepTimer += Time.deltaTime;
        float interval = isSprinting ? runStepInterval : walkStepInterval;
        if (stepTimer >= interval)
        {
            stepTimer = 0f;
            PlayFootstep(isSprinting);
        }
    }

    void PlayFootstep(bool sprint)
    {
        // Raycast to detect surface
        Vector3 origin = transform.position + Vector3.up * raycastHeight;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastHeight + 0.5f, groundMask))
        {
            AudioClip clip = PickClipByTag(hit.collider.tag);
            if (clip != null)
            {
                float vol = sprint ? 1.0f : 0.6f;
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(clip, vol);
            }
        }
    }

    AudioClip PickClipByTag(string tag)
    {
        if (tag == "Grass") return PickRandom(grassClips);
        if (tag == "Wood") return PickRandom(woodClips);
        if (tag == "Stone") return PickRandom(stoneClips);
        // default -> Dirt
        return PickRandom(dirtClips);
    }

    AudioClip PickRandom(AudioClip[] arr)
    {
        if (arr == null || arr.Length == 0) return null;
        return arr[Random.Range(0, arr.Length)];
    }
}



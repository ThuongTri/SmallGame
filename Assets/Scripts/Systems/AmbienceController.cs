using UnityEngine;

/// <summary>
/// Plays layered ambience: wind (loop), crickets (loop), owl hoot (occasional).
/// Attach to a scene-level GameObject. Provide 3D AudioSources (spatialBlend=1) or 2D for global mix.
/// </summary>
public class AmbienceController : MonoBehaviour
{
    [Header("Loops")]
    public AudioSource windLoop;     // soft wind
    public AudioSource cricketsLoop; // night insects

    [Header("One-shots")]
    public AudioSource owlSource;    // use PlayOneShot for varied hoots
    public AudioClip[] owlClips;

    [Header("Owl timing")]
    public Vector2 owlIntervalRange = new Vector2(12f, 28f);

    float owlTimer;
    float nextOwlDelay;

    void Start()
    {
        if (windLoop != null && !windLoop.isPlaying) windLoop.Play();
        if (cricketsLoop != null && !cricketsLoop.isPlaying) cricketsLoop.Play();
        ScheduleNextOwl();
    }

    void Update()
    {
        owlTimer += Time.deltaTime;
        if (owlTimer >= nextOwlDelay)
        {
            PlayOwl();
            ScheduleNextOwl();
        }
    }

    void ScheduleNextOwl()
    {
        owlTimer = 0f;
        nextOwlDelay = Random.Range(owlIntervalRange.x, owlIntervalRange.y);
    }

    void PlayOwl()
    {
        if (owlSource == null || owlClips == null || owlClips.Length == 0) return;
        var clip = owlClips[Random.Range(0, owlClips.Length)];
        if (clip != null)
        {
            owlSource.PlayOneShot(clip, Random.Range(0.7f, 1f));
        }
    }
}



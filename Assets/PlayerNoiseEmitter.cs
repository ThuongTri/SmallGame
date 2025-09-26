using UnityEngine;
using System;
[RequireComponent(typeof(AudioSource))]
public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("Step detection (distance-based)")]
    public float walkStepDistance = 2.0f;   // quãng đường giữa 2 bước khi đi bộ
    public float runStepDistance  = 1.1f;   // quãng đường giữa 2 bước khi chạy
    public float runSpeedThreshold = 3.5f; // nếu tốc độ > ngưỡng => được coi là chạy

    [Header("Loudness values")]
    public float walkLoudness = 0.6f;
    public float runLoudness  = 1.4f;
    public float collisionLoudness = 2.2f;

    [Header("Footstep audio")]
    public AudioClip[] footstepClips;
    public AudioSource audioSource; // nếu null thì sẽ lấy component

    // internal
    Vector3 lastPosition;
    float distanceCounter = 0f;

    void Awake(){
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        lastPosition = transform.position;
    }

    void Update(){
        // tính quãng đường đi trong frame
        float dist = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
        distanceCounter += dist;

        // ước tính "tốc độ" hiện tại (m/s) để phân biệt chạy/đi
        float speed = dist / Mathf.Max(Time.deltaTime, 0.0001f);

        bool isRunning = speed > runSpeedThreshold;

        // chọn threshold theo trạng thái
        float threshold = isRunning ? runStepDistance : walkStepDistance;
        float loudness = isRunning ? runLoudness : walkLoudness;

        if (distanceCounter >= threshold && threshold > 0f){
            distanceCounter = 0f;
            PlayFootstep();
            NoiseEmitter.EmitNoise(transform.position, loudness);
        }
    }

    void PlayFootstep(){
        if (footstepClips != null && footstepClips.Length > 0 && audioSource != null){
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            audioSource.PlayOneShot(clip);
        }
    }

    // public method để các script khác gọi khi có interaction (mở cửa, nhặt đồ...)
    public void EmitInteractionNoise(float loudness){
        NoiseEmitter.EmitNoise(transform.position, loudness);
        // optional: play a sound if you pass an AudioClip in a real interaction script
    }

    // nếu Player dùng CharacterController, bạn có thể bắt va chạm như sau:
    void OnControllerColliderHit(ControllerColliderHit hit){
        // nếu va chạm vật nặng (tag hoặc layer), phát noise
        // ví dụ: nếu lực va chạm lớn (dựa trên hit.moveDirection) -> loud
        float impact = hit.moveDirection.magnitude;
        if (impact > 0.6f){
            NoiseEmitter.EmitNoise(hit.point, collisionLoudness * impact);
        }
    }

    // nếu Player có Rigidbody (OnCollisionEnter)
    void OnCollisionEnter(Collision collision){
        // ignore collisions with terrain if you want
        float rel = collision.relativeVelocity.magnitude;
        if (rel > 1.0f) {
            Vector3 contactPoint = collision.contacts.Length>0 ? collision.contacts[0].point : transform.position;
            NoiseEmitter.EmitNoise(contactPoint, collisionLoudness * Mathf.Clamp01(rel/6f));
        }
    }
}

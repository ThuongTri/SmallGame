using UnityEngine;
using System.Collections;

public class JumpscareSpawner : MonoBehaviour {
    [Header("Prefabs")]
    public GameObject silhouettePrefab; // 2D fallback
    public GameObject jumpscare3DPrefab; // 3D model with optional Animator

    [Header("Audio")]
    public AudioClip[] scareSounds;

    [Header("Refs")]
    public Transform player;
    public MonsterAI monster;

    [Header("Spawn Settings")]
    public bool use3D = true;                 // prefer 3D jumpscare
    public float minDist = 2f, maxDist = 4f;  // distance in front of player
    public float eyeHeightOffset = 1.5f;       // align to eye height
    public LayerMask groundMask = ~0;          // for ground snap
    public LayerMask obstacleMask = ~0;        // to validate visibility (optional)
    public float clearRadius = 0.4f;           // ensure not inside walls

    [Header("Lifetime")]
    public Vector2 visibleDurationRange = new Vector2(1.2f, 2.4f);
    public string animatorTrigger = "Scare";   // optional trigger on Animator

    public void TrySpawn(){
        float chance = monster != null ? monster.aggression : 0f; // 0..1
        Debug.Log($"JumpscareSpawner: aggression={monster?.aggression}, chance={chance * 0.35f}");
        
        if (Random.value < chance * 0.35f) {
            Debug.Log("JUMPSCARE SPAWNED!");
            StartCoroutine(SpawnRoutine());
        } else {
            Debug.Log("Jumpscare failed - not enough aggression");
        }
    }

    IEnumerator SpawnRoutine(){
        // Compute a position IN FRONT of the player, within [minDist, maxDist]
        Vector3 forward = player.forward;
        Vector3 basePos = player.position + forward * Random.Range(minDist, maxDist);

        // Snap to ground (raycast down from above)
        Vector3 pos = basePos + Vector3.up * 3f;
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit groundHit, 6f, groundMask))
        {
            pos = groundHit.point;
        }

        // Raise to eye level
        pos.y = player.position.y + eyeHeightOffset;

        // Ensure not inside obstacles (simple sphere check)
        if (Physics.CheckSphere(pos, clearRadius, obstacleMask))
        {
            // fallback: step slightly to the side
            pos += Vector3.right * clearRadius * 2f;
        }

        // Face toward player
        Quaternion rot = Quaternion.LookRotation((player.position + Vector3.up * eyeHeightOffset) - pos);

        // Choose prefab (3D preferred)
        GameObject prefab = use3D && jumpscare3DPrefab != null ? jumpscare3DPrefab : silhouettePrefab;
        if (prefab == null)
        {
            Debug.LogWarning("JumpscareSpawner: No prefab assigned.");
            yield break;
        }

        GameObject go = Instantiate(prefab, pos, rot);

        // Optional: fire Animator trigger
        var animator = go.GetComponentInChildren<Animator>();
        if (animator != null && !string.IsNullOrEmpty(animatorTrigger))
        {
            animator.SetTrigger(animatorTrigger);
        }

        // Play one-shot scare sound near the spawn
        if (scareSounds != null && scareSounds.Length > 0)
        {
            AudioSource.PlayClipAtPoint(scareSounds[Random.Range(0, scareSounds.Length)], pos, 0.9f);
        }

        // Remain visible for a short time, then despawn
        float stay = Random.Range(visibleDurationRange.x, visibleDurationRange.y);
        yield return new WaitForSeconds(stay);
        Destroy(go);
    }
}
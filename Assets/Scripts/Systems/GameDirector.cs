using UnityEngine;

/// <summary>
/// GameDirector: centralizes dynamic difficulty (aggression) logic.
/// - Increases aggression when player is near the monster
/// - Increases when player sprints (reported via OnPlayerSprinted)
/// - Increases when picking up lore (major/minor)
/// - Decays when player is far away
/// Pushes deltas to MonsterAI.aggression each frame.
/// 
/// Minimal edits: added debug toggles and time-based log interval to avoid console spam.
/// </summary>
public class GameDirector : MonoBehaviour {
    public MonsterAI monster;
    public Transform player;

    [Range(0f,1f)] public float aggression = 0f; // running smoothed aggression value
    public float decayRate = 0.08f; // per second (when not near)
    public float nearIncrease = 0.7f; // amount per second when near threshold
    public float runIncrease = 0.12f; // per second of sprinting
    public float nearThreshold = 12f; // meters

    [Header("Debug (toggle in Inspector)")]
    public bool debugLogs = false;      // <-- mới: bật tắt log
    public float logInterval = 3f;     // giây giữa các log (mặc định 1s)
    private float lastLogTime = 0f;

    void Update(){
        if (monster == null || player == null) return;

        // Near vs far influence
        float dist = Vector3.Distance(player.position, monster.transform.position);
        if (dist < nearThreshold){
            aggression += nearIncrease * Time.deltaTime;
        } else {
            aggression -= decayRate * Time.deltaTime;
        }

        aggression = Mathf.Clamp01(aggression);

        // Push delta to monster
        monster.AdjustAggression(aggression - monster.aggression);
        
        // Debug aggression (giờ sử dụng time interval và toggle)
        if (debugLogs && Time.time - lastLogTime >= logInterval)
        {
            Debug.Log($"GameDirector: aggression={aggression:F3}, monster.aggression={monster.aggression:F3}, distance={dist:F1}m");
            lastLogTime = Time.time;
        }
    }

    /// <summary>
    /// Call this from the player when sprinting occurs.
    /// Pass the sprint duration in seconds (can be 0.5, 1.0, etc.).
    /// </summary>
    public void OnPlayerSprinted(float secs){
        aggression = Mathf.Clamp01(aggression + runIncrease * Mathf.Max(0f, secs));
        if (debugLogs) Debug.Log($"Player sprinted for {secs}s, aggression now: {aggression:F3}");
    }

    /// <summary>
    /// Call when a lore item is picked up. Major = big spike, minor = small spike.
    /// </summary>
    public void OnLorePicked(bool major){
        aggression = Mathf.Clamp01(aggression + (major ? 0.14f : 0.05f));
        if (debugLogs) Debug.Log($"Lore picked up (major={major}), aggression now: {aggression:F3}");
    }
}

using UnityEngine;

/// <summary>
/// GameDirector: centralizes dynamic difficulty (aggression) logic.
/// - Increases aggression when player is near the monster
/// - Increases when player sprints (reported via OnPlayerSprinted)
/// - Increases when picking up lore (major/minor)
/// - Decays when player is far away
/// Pushes deltas to MonsterAI.aggression each frame.
/// </summary>
public class GameDirector : MonoBehaviour {
    public MonsterAI monster;
    public Transform player;

    [Range(0f,1f)] public float aggression = 0f; // running smoothed aggression value
    public float decayRate = 0.08f; // per second (when not near)
    public float nearIncrease = 0.7f; // amount per second when near threshold
    public float runIncrease = 0.12f; // per second of sprinting
    public float nearThreshold = 12f; // meters

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
    }

    /// <summary>
    /// Call this from the player when sprinting occurs.
    /// Pass the sprint duration in seconds (can be 0.5, 1.0, etc.).
    /// </summary>
    public void OnPlayerSprinted(float secs){
        aggression = Mathf.Clamp01(aggression + runIncrease * Mathf.Max(0f, secs));
    }

    /// <summary>
    /// Call when a lore item is picked up. Major = big spike, minor = small spike.
    /// </summary>
    public void OnLorePicked(bool major){
        aggression = Mathf.Clamp01(aggression + (major ? 0.14f : 0.05f));
    }
}



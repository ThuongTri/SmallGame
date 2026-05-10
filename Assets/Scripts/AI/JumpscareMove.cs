using UnityEngine;
using UnityEngine.AI;

public class JumpscareMove : MonoBehaviour
{
    [Header("Settings")]
    public float runSpeed = 6f;
    public float stopDistance = 2.4f;

    [Header("Animation")]
    public string runTriggerName = "Run";
    public bool despawnOnReach = true;
    public float despawnDelay = 0.2f;

    [Header("Ghost dash")]
    [Tooltip("Colliders become triggers so the figure can pass through scenery like a hallucination.")]
    public bool ghostPassThrough;

    Transform player;
    Animator anim;
    bool reached;
    bool configuredFromSpawner;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        anim = GetComponentInChildren<Animator>();
        if (anim != null)
            anim.SetTrigger(runTriggerName);

        if (ghostPassThrough && !configuredFromSpawner)
            ApplyGhostPhysics();
    }

    /// <summary>Call right after Instantiate for night flash-rush (before first Update).</summary>
    public void ConfigureDash(float speed, float stopDist, bool ghostThrough, float despawnAfterReach)
    {
        runSpeed = speed;
        stopDistance = stopDist;
        ghostPassThrough = ghostThrough;
        despawnDelay = despawnAfterReach;
        configuredFromSpawner = true;

        if (ghostThrough)
            ApplyGhostPhysics();
    }

    void ApplyGhostPhysics()
    {
        foreach (var agent in GetComponentsInChildren<NavMeshAgent>(true))
        {
            if (agent != null && agent.enabled)
                agent.enabled = false;
        }

        foreach (var c in GetComponentsInChildren<Collider>(true))
        {
            if (c == null) continue;
            c.isTrigger = true;
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector3 to = player.position - transform.position;
        to.y = 0f;
        float dist = to.magnitude;

        if (to.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(to);
            float turnSharpness = Mathf.Clamp(runSpeed / 10f, 4f, 28f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSharpness);
        }

        if (dist > stopDistance)
        {
            if (to.sqrMagnitude > 0.0001f)
                transform.position += to.normalized * runSpeed * Time.deltaTime;
        }
        else if (!reached)
        {
            reached = true;
            if (despawnOnReach)
                Destroy(gameObject, despawnDelay);
        }
    }
}

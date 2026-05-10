using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(UnityEngine.AudioSource))]
public class MonsterAI : MonoBehaviour
{
    [Header("UI Connection")]
    public GameOverManager uiManager;

    public enum State { Patrol, Stalk, Chase, Search, Jumpscare }
    public State state = State.Patrol;

    [Header("Core")]
    public NavMeshAgent agent;
    public Transform player;

    [Header("Animation")]
    public Animator animator;

    [Header("Random Patrol")]
    public float patrolRadius = 30f;
    public float waitTimeMin = 2f;
    public float waitTimeMax = 5f;

    [Header("Senses")]
    public float viewAngle = 100f;
    public float viewDistance = 30f;
    public LayerMask visionMask;
    public float hearingBase = 6f;
    public float hearingMultiplier = 1f;

    [Header("Speeds")]
    public float patrolSpeed = 1.4f;
    public float stalkSpeed = 2.2f;
    public float chaseSpeed = 7.2f;

    [Header("Chase Settings (New)")]
    public float maxChaseDuration = 5f;
    private float chaseTimer = 0f;

    [Header("Jumpscare / Attack")]
    public Transform faceTarget;
    public float catchDistance = 2.5f;
    public float jumpscareFaceSpeed = 5f;
    private bool isJumpscaring = false;

    [Header("Stalk settings")]
    public float stalkMinDist = 12f;
    public float stalkMaxDist = 18f;
    public AudioClip[] whisperClips;
    public AudioClip mimicVoiceClip;
    public AudioClip jumpscareScreamClip;
    AudioSource audioSource;

    [Header("Search")]
    public float searchDuration = 8f;
    Vector3 lastSighting;
    Coroutine searchCoroutine;

    [Header("Aggression")]
    [Range(0f, 1f)] public float aggression = 0f;

    [Header("Visual")]
    public Renderer bellyRenderer;
    public float emissionBase = 1.2f;
    public float emissionMax = 6f;
    public float bobSpeed = 2f;
    public float bobAmount = 0.12f;

    [Header("Debug")]
    public bool drawGizmos = true;

    /// <summary>Legacy: kept so older scenes/prefabs that serialized this array do not lose data.</summary>
    [HideInInspector]
    public Transform[] waypoints;

    [Header("Chase tuning (append — safe for old prefabs)")]
    public float chaseAdaptiveMaxExtra = 22f;
    [Tooltip("Seconds with almost no progress before path reset / recovery.")]
    public float stuckRecoveryAfter = 2.2f;
    [Tooltip("Max NavMesh sample radius when trying to reach player.")]
    public float chaseTargetSampleRadius = 14f;
    [Tooltip("How long monster keeps visual contact before reaching full chase ramp.")]
    public float chaseRampSeconds = 5f;
    [Tooltip("Extra speed ratio gained at full chase ramp (0.35 = +35%).")]
    public float chaseRampExtraSpeed = 0.55f;
    [Tooltip("Short burst when acquiring visual contact during chase.")]
    public float chaseBurstSeconds = 1.0f;
    public float chaseBurstSpeedMultiplier = 1.4f;
    [Tooltip("Only burst if player farther than this range.")]
    public float chaseBurstMinDistance = 6f;
    [Tooltip("Pulse intensity when monster is very near while chasing.")]
    public float closePressureShakeIntensity = 0.3f;
    public float closePressureDistance = 6.7f;
    public float closePressureCooldown = 0.75f;
    public AudioClip chaseAcquireClip;

    [Header("Capture cinematic (code-only)")]
    public bool useCinematicCapture = true;
    public SimpleScreenFader captureFader;
    public CameraShakeImpulse captureCameraShake;
    [Tooltip("When faceTarget is null, mouth offset in monster local space.")]
    public Vector3 captureMouthLocalOffset = new Vector3(0f, 1.35f, 0.45f);
    [Tooltip("Kéo player vào đứng trước mặt quái (XZ) trước khi xoay nhìn miệng — đặt 0 để bỏ.")]
    public float capturePullInDuration = 0.42f;
    [Tooltip("Khoảng cách đứng trước thân quái sau pha kéo.")]
    public float captureStandDistance = 2.35f;
    public float captureFaceTurnDuration = 0.55f;
    public float captureLiftHeight = 1.25f;
    public float captureLiftTowardMonster = 0.35f;
    public float captureLiftDuration = 0.65f;
    public float captureSwallowDuration = 0.85f;
    public float captureFadeOutDuration = 0.75f;
    public float captureShakeSeconds = 0.85f;
    public float captureShakeIntensity = 1.25f;
    public AudioClip captureGrabClip;
    public AudioClip captureSwallowClip;

    [Header("Day ghost mode (PrologueDay)")]
    public PrologueFlowManager flow;
    public bool enableDayGhostMode = true;
    [Tooltip("Very fast roaming speed during day so player only catches glimpses.")]
    public float dayGhostMoveSpeed = 13.5f;
    public float dayGhostRepathInterval = 0.45f;
    public float dayGhostTargetRadius = 26f;
    [Tooltip("Keep day ghost away from player so it stalks from far, not orbiting close.")]
    public float dayGhostMinPlayerDistance = 22f;
    [Tooltip("Optional center for day roaming. If null, monster spawn position is used.")]
    public Transform dayGhostRoamCenter;
    public float dayGhostRoamRadius = 55f;
    [Tooltip("During day, monster will avoid entering these safe-zone colliders.")]
    public Collider[] dayGhostBlockedZones;
    [Tooltip("How close/far ghost keeps around player instead of drifting one direction forever.")]
    public Vector2 dayGhostOrbitRange = new Vector2(18f, 34f);
    public bool dayGhostTeleportBlink = true;
    public float dayGhostBlinkCooldown = 3.2f;
    public float dayGhostBlinkMinDistance = 14f;
    public float dayGhostBlinkMaxDistance = 30f;
    [Tooltip("Day blink avoids player front cone; higher = less chance appearing in front.")]
    public float dayGhostAvoidFrontAngle = 75f;
    [Tooltip("If player camera directly catches the monster in day mode, it vanishes instantly.")]
    public bool dayGhostVanishWhenSeen = true;
    public float dayGhostSeenVanishCooldown = 1.25f;
    [Tooltip("If monster is seen near this distance during day, it may play a laugh cue.")]
    public float dayGhostLaughDistance = 32f;
    public float dayGhostLaughCooldown = 8f;
    public AudioClip[] dayGhostLaughClips;
    [Header("Day ghost visuals")]
    [Tooltip("Assign only monster body renderers here (exclude debug capsule mesh).")]
    public Renderer[] dayGhostVisualRenderers;
    [Tooltip("Most of the time invisible; occasionally reveal briefly for a glimpse.")]
    public float dayGhostRevealDuration = 0.14f;
    public float dayGhostRevealInterval = 2.2f;
    public float dayGhostRevealJitter = 1.2f;

    [Header("Night blink (balanced)")]
    public bool enableNightBlink = true;
    public float nightBlinkCooldown = 8.5f;
    [Range(0f, 1f)] public float nightBlinkChance = 0.22f;
    public float nightBlinkMinDistance = 10f;
    public float nightBlinkMaxDistance = 24f;
    public float nightBlinkAvoidFrontAngle = 55f;
    [Header("Night hunt control")]
    public bool forceNightHunt = true;
    public float nightReacquireInterval = 1.2f;
    [Tooltip("Sau khi bị gương (suppress), quái sẽ NHẤT ĐỊNH không lao vào săn ngay lập tức.\nTổng thời gian tạm 'ngắt hunt' = suppressSeconds + this value.")]
    public float nightPostSuppressReacquireDelay = 10f;
    [Tooltip("Tắt mặc định để quái không bị kéo ngược về map 1 khi đuổi qua map 2.")]
    public bool useNightLeash = false;
    public float nightLeashRadius = 85f;
    public Transform nightLeashCenter;
    [Range(1f, 2.5f)] public float nightPressureSpeedMultiplier = 1.58f;
    [Range(0f, 1f)] public float nightPressureExtraBlinkChance = 0.3f;
    [Header("Cross-map recovery (anti stuck between map islands)")]
    public bool enableCrossMapRecovery = true;
    [Tooltip("Nếu path tới player invalid quá lâu, quái sẽ tự dịch sang island của player để tiếp tục săn.")]
    public float crossMapRecoveryAfter = 1.6f;
    public float crossMapRepathCheckInterval = 0.35f;
    public float crossMapWarpMinDistanceToPlayer = 10f;
    public float crossMapWarpMaxDistanceToPlayer = 18f;

    private float stuckTimer = 0f;
    private Vector3 lastChasePos;
    float chaseVisualTimer = 0f;
    float chaseBurstTimer = 0f;
    bool hadVisualThisChase = false;
    float nextClosePressureTime = 0f;
    float nextDayGhostRepathTime = 0f;
    float nextDayGhostLaughTime = 0f;
    float nextDayGhostBlinkTime = 0f;
    float nextDayGhostSeenVanishTime = 0f;
    float nextNightBlinkTime = 0f;
    float nextNightReacquireTime = 0f;
    float suppressedUntilTime = 0f;
    bool wasSuppressedState = false;
    [Header("Suppression visuals")]
    public bool suppressHidesMonster = true;
    public float suppressHideMinRevealSeconds = 0f;
    Renderer[] cachedSuppressionRenderers;
    Vector3 dayGhostOrigin;
    Renderer[] cachedGhostRenderers;
    float dayGhostRevealUntilTime = 0f;
    float nextDayGhostRevealTime = 0f;
    bool wasInDayGhostMode = false;

    float lastHeardLogTime = -10f;
    float patrolWaitTimer = 0f;
    float noPathToPlayerTimer = 0f;
    float nextCrossMapCheckTime = 0f;

    NavMeshPath navPath;

    void Awake()
    {
        navPath = new NavMeshPath();
        if (flow == null) flow = PrologueFlowManager.Instance;
        if (uiManager == null) uiManager = FindObjectOfType<GameOverManager>(true);
        if (captureFader == null) captureFader = FindObjectOfType<SimpleScreenFader>(true);

        if (agent == null) agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (captureCameraShake == null && player != null)
            captureCameraShake = player.GetComponentInChildren<CameraShakeImpulse>(true);
        lastChasePos = transform.position;
        CacheDayGhostRenderers();
        CacheSuppressionRenderers();
        HideRootDebugRenderers();
    }

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        BindPlayerIfMissing();
        EnsureAgentOnNavMesh();
        dayGhostOrigin = transform.position;
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = patrolSpeed;
        }
        SetRandomDestination();
    }

    void BindPlayerIfMissing()
    {
        if (player != null) return;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void EnsureAgentOnNavMesh()
    {
        if (agent == null) return;
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            return;
        }

        const float search = 25f;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, search, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            agent.isStopped = false;
        }
        else
        {
            Debug.LogWarning($"[MonsterAI] {name} không tìm thấy NavMesh trong bán kính {search}m — quái sẽ không di chuyển. Đặt quái lên vùng đã bake hoặc bake lại NavMesh.");
        }
    }

    void Update()
    {
        if (isJumpscaring) return;
        bool inDayGhost = IsDayGhostModeActive();
        if (!inDayGhost && wasInDayGhostMode)
        {
            // Leaving day-ghost mode: always restore full visuals once.
            SetDayGhostRenderersVisible(true);
            dayGhostRevealUntilTime = 0f;
        }
        wasInDayGhostMode = inDayGhost;

        bool suppressedNow = IsSuppressed();
        if (!suppressedNow && wasSuppressedState)
        {
            // Suppression ended: restore visuals unless day-ghost mode is taking over.
            wasSuppressedState = false;
            RestoreAfterSuppression();
        }

        if (suppressedNow)
        {
            wasSuppressedState = true;
            SuppressedUpdate();
            return;
        }
        if (inDayGhost)
        {
            DayGhostUpdate();
            return;
        }

        if (forceNightHunt && IsNightPhaseActive() && !IsSuppressed() && !isJumpscaring)
        {
            if (player == null) BindPlayerIfMissing();
            if (player != null && Time.time >= nextNightReacquireTime && state != State.Chase)
            {
                EnterState(State.Chase);
                nextNightReacquireTime = Time.time + Mathf.Max(0.2f, nightReacquireInterval);
            }
            KeepMonsterInsideNightLeash();
        }

        // Tránh toggle renderer mỗi frame (gây hitch nhẹ) khi không ở day-ghost mode.
        if (!IsDayGhostModeActive())
            HideRootDebugRenderers();

        UpdateEmissionBob();

        if (animator != null) animator.SetFloat("Speed", agent.velocity.magnitude);

        switch (state)
        {
            case State.Patrol: PatrolUpdate(); break;
            case State.Stalk: StalkUpdate(); break;
            case State.Chase: ChaseUpdate(); break;
            case State.Search: break;
        }

        if (state != State.Jumpscare && state != State.Chase)
        {
            if (CanSeePlayer())
            {
                lastSighting = player.position;
                EnterState(State.Chase);
            }
        }
    }

    bool IsDayGhostModeActive()
    {
        if (!enableDayGhostMode) return false;
        if (flow == null) flow = PrologueFlowManager.Instance;
        if (flow == null) return false;
        return flow.currentPhase == PrologueFlowManager.Phase.PrologueDay;
    }

    bool IsSuppressed()
    {
        return Time.time < suppressedUntilTime;
    }

    bool IsNightPhaseActive()
    {
        if (flow == null) flow = PrologueFlowManager.Instance;
        if (flow == null) return false;
        return flow.currentPhase == PrologueFlowManager.Phase.NightmareNight;
    }

    void SuppressedUpdate()
    {
        // During suppression (mirror/doll), hide monster immediately for instant "disappear" feel.
        if (suppressHidesMonster && !IsDayGhostModeActive())
            SetSuppressionRenderersEnabled(false);

        BindPlayerIfMissing();
        EnsureAgentOnNavMesh();
        UpdateEmissionBob();

        if (agent == null || !agent.isOnNavMesh) return;
        agent.isStopped = false;
        agent.speed = Mathf.Lerp(agent.speed, patrolSpeed * 0.85f, Time.deltaTime * 6f);
        if (!agent.hasPath || agent.remainingDistance < 1.2f)
            SetRandomDestination();

        if (state != State.Patrol)
            EnterState(State.Patrol);
    }

    void DayGhostUpdate()
    {
        BindPlayerIfMissing();
        EnsureAgentOnNavMesh();
        UpdateEmissionBob();
        UpdateDayGhostStealthVisibility();

        if (agent == null || !agent.isOnNavMesh) return;

        if (state != State.Patrol)
            EnterState(State.Patrol);

        agent.isStopped = false;
        float targetSpeed = Mathf.Max(dayGhostMoveSpeed, chaseSpeed * 1.6f);
        agent.speed = Mathf.Lerp(agent.speed, targetSpeed, Time.deltaTime * 8f);

        // Day rule: never stay in safe zone.
        if (IsInsideBlockedDayZone(transform.position))
        {
            Vector3 escape = PickDayGhostTargetFarFromPlayer();
            TryDayGhostBlink(escape, true);
            nextDayGhostRepathTime = Time.time + 0.08f;
        }

        if (Time.time >= nextDayGhostRepathTime || !agent.hasPath || agent.remainingDistance < 1.2f)
        {
            bool moved = false;
            if (player != null)
            {
                Vector3 target = PickDayGhostTargetFarFromPlayer();
                moved = TryNavigateTo(target, 24f);
                if (dayGhostTeleportBlink && Time.time >= nextDayGhostBlinkTime)
                    TryDayGhostBlink(target);
            }

            if (!moved)
                SetRandomDestination();

            nextDayGhostRepathTime = Time.time + Mathf.Max(0.08f, dayGhostRepathInterval);
        }

        if (player != null)
        {
            if (dayGhostVanishWhenSeen && Time.time >= nextDayGhostSeenVanishTime && IsCaughtByPlayerCameraNow())
            {
                if (Time.time >= nextDayGhostLaughTime)
                {
                    AudioClip clip = PickDayGhostClip();
                    if (clip != null && audioSource != null)
                        audioSource.PlayOneShot(clip, 0.9f);
                    nextDayGhostLaughTime = Time.time + Mathf.Max(0.2f, dayGhostLaughCooldown);
                }

                Vector3 emergencyTarget = PickDayGhostTargetFarFromPlayer();
                TryDayGhostBlink(emergencyTarget, true);
                nextDayGhostSeenVanishTime = Time.time + Mathf.Max(0.08f, dayGhostSeenVanishCooldown);
                return;
            }
        }
    }

    Vector3 PickDayGhostTargetFarFromPlayer()
    {
        Transform centerT = dayGhostRoamCenter != null ? dayGhostRoamCenter : null;
        Vector3 center = centerT != null ? centerT.position : dayGhostOrigin;
        float roamR = Mathf.Max(6f, dayGhostRoamRadius);
        float avoidCos = Mathf.Cos(Mathf.Deg2Rad * Mathf.Clamp(dayGhostAvoidFrontAngle, 0f, 179f));
        Vector3 playerFwd = GetPlayerForwardOnPlane();

        for (int i = 0; i < 16; i++)
        {
            Vector3 dir = Random.insideUnitSphere;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) continue;
            dir.Normalize();

            Vector3 p = center + dir * Random.Range(roamR * 0.25f, roamR);
            p.y = transform.position.y;

            if (player != null)
            {
                Vector3 toP = p - player.position;
                toP.y = 0f;
                if (toP.sqrMagnitude < dayGhostMinPlayerDistance * dayGhostMinPlayerDistance) continue;
                if (toP.sqrMagnitude > 0.001f && Vector3.Dot(playerFwd, toP.normalized) > avoidCos) continue;
            }

            if (IsInsideBlockedDayZone(p)) continue;
            return p;
        }

        Vector3 fallback = center + Random.onUnitSphere * (roamR * 0.7f);
        fallback.y = transform.position.y;
        return fallback;
    }

    void TryDayGhostBlink(Vector3 targetHint, bool force = false)
    {
        if (player == null || agent == null || !agent.isOnNavMesh) return;
        if (!force && Time.time < nextDayGhostBlinkTime) return;

        float d = Vector3.Distance(transform.position, player.position);
        if (!force && (d < dayGhostBlinkMinDistance || d > dayGhostBlinkMaxDistance)) return;

        Vector3 playerFwd = GetPlayerForwardOnPlane();
        Vector3 toHint = targetHint - player.position;
        toHint.y = 0f;
        float avoidCos = Mathf.Cos(Mathf.Deg2Rad * Mathf.Clamp(dayGhostAvoidFrontAngle, 0f, 179f));
        if (toHint.sqrMagnitude > 0.001f && Vector3.Dot(playerFwd, toHint.normalized) > avoidCos)
        {
            Vector3 side = Vector3.Cross(Vector3.up, playerFwd).normalized * Random.Range(-1f, 1f);
            Vector3 behind = -playerFwd * Random.Range(dayGhostBlinkMinDistance, dayGhostBlinkMaxDistance);
            targetHint = player.position + behind + side * (dayGhostBlinkMinDistance * 0.7f);
        }

        Vector3 blinkTarget = targetHint;
        if (!force && IsInsideBlockedDayZone(blinkTarget)) return;
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(blinkTarget, out hit, 18f, NavMesh.AllAreas))
            return;

        if (!force && IsInsideBlockedDayZone(hit.position)) return;
        agent.Warp(hit.position);
        nextDayGhostBlinkTime = Time.time + Mathf.Max(0.2f, dayGhostBlinkCooldown);
        nextDayGhostRepathTime = Time.time + 0.12f;
        dayGhostRevealUntilTime = 0f;
    }

    Vector3 GetPlayerForwardOnPlane()
    {
        if (player == null) return transform.forward;
        Vector3 f = player.forward;
        Camera c = player.GetComponentInChildren<Camera>(true);
        if (c != null) f = c.transform.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.001f) return transform.forward;
        return f.normalized;
    }

    bool IsCaughtByPlayerCameraNow()
    {
        if (player == null) return false;
        Camera cam = player.GetComponentInChildren<Camera>(true);
        if (cam == null) return false;

        Vector3 eye = cam.transform.position;
        Vector3 target = transform.position + Vector3.up * 1.5f;
        Vector3 to = target - eye;
        float dist = to.magnitude;
        if (dist <= 0.01f) return false;

        Vector3 dir = to / dist;
        Vector3 view = cam.WorldToViewportPoint(target);
        if (view.z <= 0f) return false;
        if (view.x < 0.28f || view.x > 0.72f || view.y < 0.24f || view.y > 0.76f) return false;

        // Less strict: if generally centered on screen, vanish instantly.
        return true;
    }

    void UpdateDayGhostStealthVisibility()
    {
        if (Time.time >= nextDayGhostRevealTime && Time.time >= dayGhostRevealUntilTime)
        {
            dayGhostRevealUntilTime = Time.time + Mathf.Max(0.03f, dayGhostRevealDuration);
            nextDayGhostRevealTime = Time.time + Mathf.Max(0.3f, dayGhostRevealInterval) + Random.Range(0f, Mathf.Max(0f, dayGhostRevealJitter));
        }

        bool visible = Time.time < dayGhostRevealUntilTime;
        SetDayGhostRenderersVisible(visible);
    }

    void SetDayGhostRenderersVisible(bool visible)
    {
        if (cachedGhostRenderers == null || cachedGhostRenderers.Length == 0)
            CacheDayGhostRenderers();
        if (cachedGhostRenderers == null) return;
        for (int i = 0; i < cachedGhostRenderers.Length; i++)
        {
            Renderer r = cachedGhostRenderers[i];
            if (r == null) continue;
            r.enabled = visible;
        }
        HideRootDebugRenderers();
    }

    void CacheDayGhostRenderers()
    {
        cachedGhostRenderers = dayGhostVisualRenderers;
        if (cachedGhostRenderers != null && cachedGhostRenderers.Length > 0) return;

        Renderer[] all = GetComponentsInChildren<Renderer>(true);
        System.Collections.Generic.List<Renderer> picks = new System.Collections.Generic.List<Renderer>(all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            Renderer r = all[i];
            if (r == null) continue;
            // Exclude root debug capsule/placeholder mesh to avoid white capsule pop.
            if (r.transform == transform) continue;
            if (r.GetComponent<CapsuleCollider>() != null) continue;
            if (r.GetComponent<CharacterController>() != null) continue;
            if (r.name.ToLowerInvariant().Contains("capsule")) continue;
            picks.Add(r);
        }
        cachedGhostRenderers = picks.ToArray();
    }

    void HideRootDebugRenderers()
    {
        Renderer[] rootRenderers = GetComponents<Renderer>();
        for (int i = 0; i < rootRenderers.Length; i++)
            if (rootRenderers[i] != null) rootRenderers[i].enabled = false;
    }

    void CacheSuppressionRenderers()
    {
        if (cachedSuppressionRenderers != null && cachedSuppressionRenderers.Length > 0) return;

        Renderer[] all = GetComponentsInChildren<Renderer>(true);
        System.Collections.Generic.List<Renderer> picks = new System.Collections.Generic.List<Renderer>(all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            Renderer r = all[i];
            if (r == null) continue;

            // Skip root debug capsule/placeholder meshes.
            if (r.transform == transform) continue;
            if (r.name.ToLowerInvariant().Contains("capsule")) continue;
            if (r.GetComponent<CapsuleCollider>() != null) continue;
            if (r.GetComponent<CharacterController>() != null) continue;

            picks.Add(r);
        }

        cachedSuppressionRenderers = picks.ToArray();
    }

    void SetSuppressionRenderersEnabled(bool enabled)
    {
        if (!suppressHidesMonster) return;
        if (IsDayGhostModeActive()) return; // Day ghost visuals are controlled by its own system.

        if (cachedSuppressionRenderers == null || cachedSuppressionRenderers.Length == 0)
            CacheSuppressionRenderers();

        if (cachedSuppressionRenderers == null) return;
        for (int i = 0; i < cachedSuppressionRenderers.Length; i++)
        {
            Renderer r = cachedSuppressionRenderers[i];
            if (r == null) continue;
            r.enabled = enabled;
        }
        HideRootDebugRenderers();
    }

    void RestoreAfterSuppression()
    {
        if (!suppressHidesMonster) return;
        if (IsDayGhostModeActive()) return; // Let day-ghost system control visibility.
        SetSuppressionRenderersEnabled(true);
    }

    bool IsInsideBlockedDayZone(Vector3 p)
    {
        if (dayGhostBlockedZones == null) return false;
        for (int i = 0; i < dayGhostBlockedZones.Length; i++)
        {
            Collider c = dayGhostBlockedZones[i];
            if (c == null || !c.enabled) continue;
            Vector3 cp = c.ClosestPoint(p);
            cp.y = p.y;
            if ((cp - p).sqrMagnitude < 0.0004f)
                return true;
        }
        return false;
    }

    AudioClip PickDayGhostClip()
    {
        if (dayGhostLaughClips != null && dayGhostLaughClips.Length > 0)
            return dayGhostLaughClips[Random.Range(0, dayGhostLaughClips.Length)];
        if (whisperClips != null && whisperClips.Length > 0)
            return whisperClips[Random.Range(0, whisperClips.Length)];
        return mimicVoiceClip;
    }

    void TryNightBlinkDuringChase(bool seesPlayer, float flatDist)
    {
        if (!enableNightBlink || !seesPlayer) return;
        if (player == null || agent == null || !agent.isOnNavMesh) return;
        if (Time.time < nextNightBlinkTime) return;
        if (flatDist < nightBlinkMinDistance || flatDist > nightBlinkMaxDistance) return;
        float chance = Mathf.Clamp01(nightBlinkChance + (IsNightPhaseActive() ? nightPressureExtraBlinkChance : 0f));
        if (Random.value > chance) return;

        Vector3 playerFwd = GetPlayerForwardOnPlane();
        float avoidCos = Mathf.Cos(Mathf.Deg2Rad * Mathf.Clamp(nightBlinkAvoidFrontAngle, 0f, 179f));

        Vector3 candidate = transform.position;
        bool found = false;
        for (int i = 0; i < 10; i++)
        {
            Vector3 dir = Random.insideUnitSphere;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) continue;
            dir.Normalize();
            if (Vector3.Dot(playerFwd, dir) > avoidCos) continue;

            float minR = Mathf.Max(catchDistance + 2.5f, nightBlinkMinDistance * 0.55f);
            float maxR = Mathf.Max(minR + 0.1f, nightBlinkMinDistance);
            float r = Random.Range(minR, maxR);
            candidate = player.position + dir * r;
            candidate.y = transform.position.y;

            NavMeshHit nh;
            if (NavMesh.SamplePosition(candidate, out nh, 10f, NavMesh.AllAreas))
            {
                candidate = nh.position;
                found = true;
                break;
            }
        }

        if (!found) return;

        agent.Warp(candidate);
        TryNavigateTo(player.position, chaseTargetSampleRadius);
        nextNightBlinkTime = Time.time + Mathf.Max(0.5f, nightBlinkCooldown);
    }

    void KeepMonsterInsideNightLeash()
    {
        if (!useNightLeash) return;
        if (agent == null || !agent.isOnNavMesh) return;
        Vector3 center = nightLeashCenter != null ? nightLeashCenter.position : dayGhostOrigin;
        Vector3 d = transform.position - center;
        d.y = 0f;
        float leash = Mathf.Max(12f, nightLeashRadius);
        if (d.sqrMagnitude <= leash * leash) return;

        Vector3 edge = center + d.normalized * (leash * 0.7f);
        NavMeshHit hit;
        if (NavMesh.SamplePosition(edge, out hit, 20f, NavMesh.AllAreas))
            agent.Warp(hit.position);
        else
            agent.Warp(center);
    }

    void PatrolUpdate()
    {
        if (forceNightHunt && IsNightPhaseActive() && player != null)
        {
            // Trong lúc vừa bị suppress (gương), không được phép quay lại chase ngay.
            if (Time.time >= nextNightReacquireTime)
            {
                EnterState(State.Chase);
                return;
            }
        }

        agent.speed = Mathf.Lerp(agent.speed, patrolSpeed, Time.deltaTime * 2f);

        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= Random.Range(waitTimeMin, waitTimeMax))
            {
                SetRandomDestination();
                patrolWaitTimer = 0f;
            }
        }
    }

    void SetRandomDestination()
    {
        if (forceNightHunt && IsNightPhaseActive() && player != null && Time.time >= nextNightReacquireTime)
        {
            TryNavigateTo(player.position, chaseTargetSampleRadius);
            return;
        }

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector3 randomPoint = GetRandomNavMeshPoint(transform.position, patrolRadius);
            if (TryNavigateTo(randomPoint, 8f))
                return;
        }
        TryNavigateTo(transform.position, 2f);
    }

    Vector3 GetRandomNavMeshPoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPos = center + Random.insideUnitSphere * radius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 5.0f, NavMesh.AllAreas))
                return hit.position;
        }
        return center;
    }

    float AdaptiveChaseLimit()
    {
        if (player == null) return maxChaseDuration;
        float d = Vector3.Distance(transform.position, player.position);
        float estTravel = d / Mathf.Max(0.01f, chaseSpeed * 0.65f);
        return maxChaseDuration + Mathf.Clamp(estTravel, 0f, chaseAdaptiveMaxExtra);
    }

    bool TryNavigateTo(Vector3 target, float sampleRadius)
    {
        // Patrol không được phụ thuộc player — trước đây check player != null khiến quái đứng im nếu quên gán Player.
        if (agent == null || !agent.isOnNavMesh) return false;

        if (agent.CalculatePath(target, navPath))
        {
            if (navPath.status == NavMeshPathStatus.PathComplete || navPath.status == NavMeshPathStatus.PathPartial)
            {
                if (navPath.corners != null && navPath.corners.Length > 0)
                {
                    agent.SetPath(navPath);
                    return true;
                }
            }
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(target, out hit, sampleRadius, NavMesh.AllAreas))
        {
            if (agent.CalculatePath(hit.position, navPath) && navPath.corners != null && navPath.corners.Length > 0)
            {
                agent.SetPath(navPath);
                return true;
            }
        }

        // Strong fallback for multi-island maps: sample a broader ring around target.
        float broad = Mathf.Max(sampleRadius + 14f, 24f);
        for (int i = 0; i < 14; i++)
        {
            Vector3 c = target + Random.insideUnitSphere * broad;
            c.y = target.y;
            if (!NavMesh.SamplePosition(c, out hit, 10f, NavMesh.AllAreas)) continue;
            if (agent.CalculatePath(hit.position, navPath) && navPath.corners != null && navPath.corners.Length > 0)
            {
                agent.SetPath(navPath);
                return true;
            }
        }

        return false;
    }

    void ChaseUpdate()
    {
        if (player == null)
        {
            BindPlayerIfMissing();
            if (player == null)
            {
                EnterState(State.Search);
                return;
            }
        }

        bool seesPlayer = CanSeePlayer();
        if (seesPlayer)
        {
            lastSighting = player.position;
            chaseVisualTimer += Time.deltaTime;
            if (IsNightPhaseActive())
                aggression = Mathf.Clamp01(aggression + Time.deltaTime * 0.08f);
            if (!hadVisualThisChase)
            {
                hadVisualThisChase = true;
                float d = Vector3.Distance(transform.position, player.position);
                if (d >= chaseBurstMinDistance)
                    chaseBurstTimer = Mathf.Max(chaseBurstTimer, chaseBurstSeconds);
                if (chaseAcquireClip != null)
                    audioSource.PlayOneShot(chaseAcquireClip, 0.9f);
            }
        }
        else
        {
            chaseVisualTimer = Mathf.Max(0f, chaseVisualTimer - Time.deltaTime * 0.55f);
        }

        if (chaseBurstTimer > 0f)
            chaseBurstTimer -= Time.deltaTime;

        float ramp01 = Mathf.Clamp01(chaseVisualTimer / Mathf.Max(0.01f, chaseRampSeconds));
        float baseTargetSpeed = Mathf.Lerp(chaseSpeed * 0.9f, chaseSpeed, aggression);
        float ramped = baseTargetSpeed * (1f + chaseRampExtraSpeed * ramp01);
        if (IsNightPhaseActive())
            ramped *= Mathf.Max(1f, nightPressureSpeedMultiplier);
        float burstMul = chaseBurstTimer > 0f ? chaseBurstSpeedMultiplier : 1f;
        agent.speed = Mathf.Lerp(agent.speed, ramped * burstMul, Time.deltaTime * 4f);

        TryNavigateTo(player.position, chaseTargetSampleRadius);
        TryCrossMapRecovery(seesPlayer);

        chaseTimer += Time.deltaTime;

        float flatDist = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(player.position.x, 0f, player.position.z));

        if (flatDist <= closePressureDistance && Time.time >= nextClosePressureTime)
        {
            nextClosePressureTime = Time.time + Mathf.Max(0.15f, closePressureCooldown);
            if (captureCameraShake != null)
                captureCameraShake.Shake(0.18f, closePressureShakeIntensity);
            else
            {
                CameraShakeImpulse nearShake = player.GetComponentInChildren<CameraShakeImpulse>(true);
                if (nearShake != null)
                    nearShake.Shake(0.18f, closePressureShakeIntensity);
            }
        }

        TryNightBlinkDuringChase(seesPlayer, flatDist);

        if (flatDist <= catchDistance)
        {
            StartCoroutine(TriggerJumpscare());
            return;
        }

        if (chaseTimer >= AdaptiveChaseLimit())
        {
            EnterState(State.Search);
            return;
        }

        // Stuck recovery
        float moved = Vector3.Distance(transform.position, lastChasePos);
        lastChasePos = transform.position;
        if (moved < 0.03f * Time.deltaTime * 60f && agent.velocity.sqrMagnitude < 0.02f && agent.remainingDistance > agent.stoppingDistance + 0.35f)
            stuckTimer += Time.deltaTime;
        else
            stuckTimer = 0f;

        if (stuckTimer >= stuckRecoveryAfter)
        {
            stuckTimer = 0f;
            agent.ResetPath();
            Vector3 jitter = Random.insideUnitSphere * 1.5f;
            jitter.y = 0f;
            TryNavigateTo(player.position + jitter, chaseTargetSampleRadius + 4f);
        }
    }

    void TryCrossMapRecovery(bool seesPlayer)
    {
        if (!enableCrossMapRecovery || player == null || agent == null || !agent.isOnNavMesh) return;
        if (seesPlayer) { noPathToPlayerTimer = 0f; return; }
        if (Time.time < nextCrossMapCheckTime) return;
        nextCrossMapCheckTime = Time.time + Mathf.Max(0.1f, crossMapRepathCheckInterval);

        bool hasValidPath = false;
        if (agent.CalculatePath(player.position, navPath))
        {
            if ((navPath.status == NavMeshPathStatus.PathComplete || navPath.status == NavMeshPathStatus.PathPartial) &&
                navPath.corners != null && navPath.corners.Length > 1)
                hasValidPath = true;
        }

        if (hasValidPath)
        {
            noPathToPlayerTimer = 0f;
            return;
        }

        noPathToPlayerTimer += crossMapRepathCheckInterval;
        if (noPathToPlayerTimer < Mathf.Max(0.5f, crossMapRecoveryAfter)) return;
        noPathToPlayerTimer = 0f;

        Vector3 playerFwd = GetPlayerForwardOnPlane();
        float minD = Mathf.Max(4f, crossMapWarpMinDistanceToPlayer);
        float maxD = Mathf.Max(minD + 1f, crossMapWarpMaxDistanceToPlayer);
        Vector3 best = transform.position;
        bool found = false;

        for (int i = 0; i < 18; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, Random.Range(-95f, 95f), 0f) * (-playerFwd);
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = -transform.forward;
            dir.Normalize();

            Vector3 candidate = player.position + dir * Random.Range(minD, maxD);
            NavMeshHit nh;
            if (!NavMesh.SamplePosition(candidate, out nh, 14f, NavMesh.AllAreas)) continue;
            best = nh.position;
            found = true;
            break;
        }

        if (!found) return;
        agent.Warp(best);
        TryNavigateTo(player.position, chaseTargetSampleRadius + 8f);
    }

    IEnumerator TriggerJumpscare()
    {
        if (player == null)
        {
            isJumpscaring = false;
            yield break;
        }

        isJumpscaring = true;
        state = State.Jumpscare;
        // Never allow invisible capture.
        SetDayGhostRenderersVisible(true);
        dayGhostRevealUntilTime = 0f;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        var playerCtrl = player.GetComponent<PlayerController>();
        if (playerCtrl != null) playerCtrl.LockPlayerInput();

        CharacterController cc = player.GetComponent<CharacterController>();
        bool hadCc = cc != null;
        if (hadCc) cc.enabled = false;

        try
        {
            yield return CapturePullPlayerToStandRing(cc);

            CameraShakeImpulse shake = captureCameraShake;
            if (shake == null)
                shake = player.GetComponentInChildren<CameraShakeImpulse>(true);

            SimpleScreenFader fader = captureFader;
            if (fader == null)
                fader = FindObjectOfType<SimpleScreenFader>(true);

            if (!useCinematicCapture)
            {
                yield return StartCoroutine(SimpleCaptureFallback(playerCtrl, shake));
                yield break;
            }

            if (animator != null)
            {
                animator.SetFloat("Speed", 0);
                animator.SetTrigger("Attack");
            }

            if (jumpscareScreamClip != null) audioSource.PlayOneShot(jumpscareScreamClip);

            Vector3 targetLookPos = (faceTarget != null) ? faceTarget.position : transform.position + Vector3.up * 1.7f;
            Vector3 dirToFace = (targetLookPos - player.position).normalized;
            Quaternion lookRot = Quaternion.LookRotation(dirToFace);
            Quaternion startRot = player.rotation;

            float timer = 0f;
            while (timer < captureFaceTurnDuration)
            {
                timer += Time.deltaTime;
                float u = captureFaceTurnDuration > 0f ? Mathf.Clamp01(timer / captureFaceTurnDuration) : 1f;
                player.rotation = Quaternion.Slerp(startRot, lookRot, u);

                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                dirToPlayer.y = 0f;
                if (dirToPlayer.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToPlayer), Time.deltaTime * 10f);

                yield return null;
            }

            if (shake != null)
                shake.Shake(captureShakeSeconds, captureShakeIntensity);

            if (captureGrabClip != null) audioSource.PlayOneShot(captureGrabClip);

            Vector3 mouthWorld = GetMouthWorldPosition();
            Vector3 startP = player.position;
            Vector3 toMouth = mouthWorld - startP;
            if (toMouth.sqrMagnitude < 0.01f)
                toMouth = transform.forward;
            Vector3 toMouthNorm = toMouth.normalized;

            Vector3 liftPos = Vector3.Lerp(startP, mouthWorld, 0.2f)
                + Vector3.up * captureLiftHeight
                + toMouthNorm * Mathf.Min(captureLiftTowardMonster, 0.85f);

            float lt = 0f;
            while (lt < captureLiftDuration)
            {
                lt += Time.deltaTime;
                float u = captureLiftDuration > 0f ? Mathf.Clamp01(lt / captureLiftDuration) : 1f;
                Vector3 p = Vector3.Lerp(startP, liftPos, u);
                SetPlayerWorldPosition(player, cc, p);
                yield return null;
            }

            if (captureSwallowClip != null) audioSource.PlayOneShot(captureSwallowClip);

            Vector3 swallowTarget = Vector3.Lerp(player.position, mouthWorld, 0.97f);
            Vector3 postLift = player.position;
            float st = 0f;
            while (st < captureSwallowDuration)
            {
                st += Time.deltaTime;
                float u = captureSwallowDuration > 0f ? Mathf.Clamp01(st / captureSwallowDuration) : 1f;
                Vector3 p = Vector3.Lerp(postLift, swallowTarget, u);
                SetPlayerWorldPosition(player, cc, p);
                yield return null;
            }

            if (fader != null && captureFadeOutDuration > 0f)
                yield return StartCoroutine(fader.FadeOut(captureFadeOutDuration));

            if (uiManager != null)
                uiManager.ShowGameOverAfterCapture();
            else
                Debug.LogError("[MonsterAI] Chưa gán GameOverManager.");
        }
        finally
        {
            if (hadCc && cc != null)
                cc.enabled = true;
        }
    }

    IEnumerator CapturePullPlayerToStandRing(CharacterController cc)
    {
        if (player == null || capturePullInDuration <= 0f)
            yield break;

        Vector3 monsterFlat = transform.position;
        monsterFlat.y = 0f;
        Vector3 playerFlat = player.position;
        playerFlat.y = 0f;

        Vector3 outward = playerFlat - monsterFlat;
        float currentDist = outward.magnitude;
        if (currentDist < 0.02f)
        {
            outward = -transform.forward;
            currentDist = 0.02f;
        }
        else
            outward /= currentDist;

        float ringDist = Mathf.Max(captureStandDistance, catchDistance * 0.92f);
        float minGrip = Mathf.Max(0.85f, catchDistance * 0.55f);
        // Luôn kéo VÀO gần quái hơn vị trí hiện tại (không bao giờ đẩy ra xa như “ném”).
        float targetDist = Mathf.Min(ringDist, currentDist * 0.9f);
        targetDist = Mathf.Max(targetDist, Mathf.Min(ringDist, minGrip));

        Vector3 targetFlat = monsterFlat + outward * targetDist;

        float elapsed = 0f;
        Vector3 start = player.position;

        while (elapsed < capturePullInDuration)
        {
            elapsed += Time.deltaTime;
            float u = capturePullInDuration > 0f ? Mathf.Clamp01(elapsed / capturePullInDuration) : 1f;
            u = u * u * (3f - 2f * u);

            Vector3 np = Vector3.Lerp(start, new Vector3(targetFlat.x, start.y, targetFlat.z), u);

            SetPlayerWorldPosition(player, cc, np);

            Vector3 toMonster = transform.position + Vector3.up * 1.55f - player.position;
            toMonster.y = 0f;
            if (toMonster.sqrMagnitude > 0.0001f)
            {
                Quaternion yaw = Quaternion.LookRotation(toMonster.normalized);
                Vector3 e = yaw.eulerAngles;
                player.rotation = Quaternion.Euler(0f, e.y, 0f);
            }

            yield return null;
        }
    }

    IEnumerator SimpleCaptureFallback(PlayerController playerCtrl, CameraShakeImpulse shake)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetTrigger("Attack");
        }

        if (jumpscareScreamClip != null) audioSource.PlayOneShot(jumpscareScreamClip);
        if (shake != null) shake.Shake(captureShakeSeconds, captureShakeIntensity);

        Vector3 targetLookPos = (faceTarget != null) ? faceTarget.position : transform.position + Vector3.up * 1.7f;
        Vector3 dirToFace = (targetLookPos - player.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(dirToFace);
        Quaternion startRot = player.rotation;

        float timer = 0f;
        while (timer < 0.5f)
        {
            player.rotation = Quaternion.Slerp(startRot, lookRot, timer / 0.5f);
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToPlayer), Time.deltaTime * 10f);
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(1.0f);

        if (uiManager != null)
            uiManager.ShowGameOver();
    }

    static void SetPlayerWorldPosition(Transform playerT, CharacterController cc, Vector3 worldPos)
    {
        if (playerT == null || float.IsNaN(worldPos.x)) return;
        if (cc != null && cc.enabled)
        {
            cc.enabled = false;
            playerT.position = worldPos;
            cc.enabled = true;
        }
        else
            playerT.position = worldPos;
    }

    Vector3 GetMouthWorldPosition()
    {
        if (faceTarget != null) return faceTarget.position;
        // Ignore lossy scale when estimating mouth offset (monster prefab may be scaled, causing huge vertical miss).
        return transform.position + transform.rotation * captureMouthLocalOffset;
    }

    void StalkUpdate()
    {
        agent.speed = Mathf.Lerp(agent.speed, stalkSpeed, Time.deltaTime * 2f);
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 target = player.position - dir * ((stalkMinDist + stalkMaxDist) * 0.5f);
        TryNavigateTo(target, 12f);

        if (audioSource != null && !audioSource.isPlaying && Random.value < 0.002f * (1f + aggression * 5f))
            PlayWhisperOrMimic();
    }

    void EnterState(State next)
    {
        if (state == next) return;
        state = next;

        if (state == State.Chase)
        {
            chaseTimer = 0f;
            stuckTimer = 0f;
            lastChasePos = transform.position;
            chaseVisualTimer = 0f;
            chaseBurstTimer = 0f;
            hadVisualThisChase = false;
            nextClosePressureTime = 0f;
        }

        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
            searchCoroutine = null;
        }
        if (state == State.Search)
            searchCoroutine = StartCoroutine(DoSearch());
    }

    IEnumerator DoSearch()
    {
        TryNavigateTo(lastSighting, 16f);
        float t = 0f;
        while (t < searchDuration)
        {
            Vector3 offset = new Vector3(Mathf.Sin(t * 1.2f), 0, Mathf.Cos(t * 1.2f)) * 3f;
            TryNavigateTo(lastSighting + offset, 16f);
            t += Time.deltaTime;
            if (CanSeePlayer())
            {
                EnterState(State.Chase);
                yield break;
            }
            yield return null;
        }
        SetRandomDestination();
        EnterState(State.Patrol);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 eye = transform.position + Vector3.up * 1.6f;
        Vector3 targetPos = player.position + Vector3.up * 1.0f;
        Vector3 dir = targetPos - eye;
        float dist = dir.magnitude;
        if (dist > viewDistance) return false;
        float angle = Vector3.Angle(transform.forward, dir.normalized);
        if (angle > viewAngle * 0.5f) return false;
        if (Physics.Raycast(eye, dir.normalized, out RaycastHit hit, dist, ~visionMask))
        {
            if (hit.transform != player && !hit.transform.IsChildOf(player)) return false;
        }
        return true;
    }

    public void OnHearNoise(Vector3 pos)
    {
        if (IsSuppressed()) return;
        if (state == State.Chase || state == State.Jumpscare) return;

        float dist = Vector3.Distance(transform.position, pos);
        float range = hearingBase * hearingMultiplier;
        if (dist <= range)
        {
            lastSighting = pos;
            EnterState(State.Stalk);
            if (Time.time - lastHeardLogTime > 2f)
            {
                Debug.Log($"Heard noise at {pos}");
                lastHeardLogTime = Time.time;
            }
        }
    }

    void PlayWhisperOrMimic()
    {
        if (mimicVoiceClip != null && aggression > 0.6f && Random.value < 0.6f)
            audioSource.PlayOneShot(mimicVoiceClip);
        else if (whisperClips != null && whisperClips.Length > 0)
            audioSource.PlayOneShot(whisperClips[Random.Range(0, whisperClips.Length)]);
    }

    void UpdateEmissionBob()
    {
        if (bellyRenderer == null) return;
        Material mat = bellyRenderer.material;
        float t = Time.time * bobSpeed;
        float emission = emissionBase + Mathf.Abs(Mathf.Sin(t)) * bobAmount * (1f + aggression * 3f);
        emission = Mathf.Lerp(emission, Mathf.Lerp(emissionBase, emissionMax, aggression), 0.5f);
        Color baseColor = Color.red;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseColor * emission);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, hearingBase);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, patrolRadius);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, catchDistance);
    }

    public void AdjustAggression(float delta)
    {
        aggression = Mathf.Clamp01(aggression + delta);
    }

    public void OnEnterSafeZone()
    {
        aggression = Mathf.Max(0f, aggression - 0.4f);
        EnterState(State.Patrol);
    }

    public void SuppressForSeconds(float seconds)
    {
        if (seconds <= 0f) return;
        suppressedUntilTime = Mathf.Max(suppressedUntilTime, Time.time + seconds);
        // Chặn hunt lại theo mốc thời gian: suppress + delay.
        nextNightReacquireTime = Mathf.Max(nextNightReacquireTime, Time.time + seconds + nightPostSuppressReacquireDelay);

        // Instant disappear on suppression start.
        if (suppressHidesMonster && !IsDayGhostModeActive())
        {
            wasSuppressedState = true;
            SetSuppressionRenderersEnabled(false);
        }

        if (isJumpscaring) return;
        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }
        EnterState(State.Patrol);
    }

    public void RepelFrom(Vector3 sourcePosition, float repelDistance, float suppressSeconds = 0f)
    {
        Vector3 away = transform.position - sourcePosition;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = transform.forward;
        away.Normalize();

        Vector3 target = transform.position + away * Mathf.Max(0.5f, repelDistance);
        bool moved = TryNavigateTo(target, Mathf.Max(8f, repelDistance + 6f));
        if (!moved)
            transform.position = target;

        if (suppressSeconds > 0f)
            SuppressForSeconds(suppressSeconds);
    }

    public void ResetForNewRun()
    {
        isJumpscaring = false;
        wasSuppressedState = false;
        suppressedUntilTime = 0f;
        noPathToPlayerTimer = 0f;
        nextCrossMapCheckTime = 0f;
        chaseTimer = 0f;
        chaseVisualTimer = 0f;
        chaseBurstTimer = 0f;
        hadVisualThisChase = false;
        nextClosePressureTime = 0f;
        state = State.Patrol;

        BindPlayerIfMissing();
        EnsureAgentOnNavMesh();
        SetDayGhostRenderersVisible(true);
        dayGhostRevealUntilTime = 0f;
        nextDayGhostRevealTime = 0f;
        wasInDayGhostMode = false;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = patrolSpeed;
            SetRandomDestination();
        }

        // Restore visuals if we previously suppressed.
        if (suppressHidesMonster && !IsDayGhostModeActive())
            SetSuppressionRenderersEnabled(true);
    }
}

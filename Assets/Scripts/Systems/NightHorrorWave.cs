using System.Collections;
using UnityEngine;

/// <summary>
/// Night-only horror bursts: 360 spawns + audio spam + mild screen pulse + stamina drain + camera shake.
/// Designed to feel chaotic without being a permanent loop.
/// </summary>
public class NightHorrorWave : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Camera playerCamera;
    public PlayerController playerController;
    public SimpleScreenFader screenPulse; // reuse black overlay as a cheap "vision pulse"
    public CameraShakeImpulse cameraShake;

    [Header("Prefabs (mix)")]
    public GameObject[] heavyPrefabs;     // e.g. jumscare ramdom
    public GameObject[] lightPrefabs;     // optional lighter prefabs (leave empty to use heavy only)

    [Range(0f, 1f)] public float heavyPickChance = 0.35f;

    [Header("Activation")]
    public bool onlyInNightmareNight = true;
    public bool autoFindPlayer = true;

    [Header("Wave pacing")]
    public Vector2 waveCooldownRange = new Vector2(28f, 52f);
    [Range(0f, 1f)] public float waveChance = 0.55f;

    [Header("Burst (360)")]
    public int burstCountMin = 16;
    public int burstCountMax = 28;
    public float ringRadius = 10f;
    public float ringRadiusJitter = 2.5f;
    public float heightJitter = 0.35f;
    public float burstDuration = 1.2f; // stagger spawns across this window
    public LayerMask groundMask = ~0;
    public float groundRayUp = 80f;
    public float groundRayDown = 400f;
    public bool snapToNavMesh = false;
    public float navMeshSnapDistance = 3f;
    [Tooltip("Extra height above raycast hit so feet clear grass / NavMesh offset.")]
    public float surfaceFootClearance = 0.12f;
    [Tooltip("Manual extra Y offset for night burst spawns (use if still slightly underground).")]
    public float nightSpawnHeightOffset = 0.25f;

    [Tooltip("NavMesh chỉ được kéo XZ tối đa bấy nhiêu mét so với điểm vòng spawn — tránh snap nhảy về chỗ khác / gần player.")]
    public float burstNavmeshSnapMaxPull = 8f;

    [Tooltip("Spawn luôn cách player ít nhất (XZ) để không chồng capsule — giảm CharacterController nảy lung tung.")]
    public float minHorizontalClearFromPlayer = 6f;

    [Header("Spawn lifetime")]
    public Vector2 monsterLifetimeRange = new Vector2(3.5f, 7.5f);
    [Header("Spawn fail-safe")]
    public bool spawnFailSafeEnabled = true;
    public float spawnFailSafeDistance = 9f;

    [Header("Heavy night dash (Flash-style rush)")]
    public Vector2 nightDashSpeedRange = new Vector2(42f, 68f);
    public Vector2 nightDashStopDistanceRange = new Vector2(0.35f, 0.85f);
    public Vector2 nightDashDespawnDelayRange = new Vector2(0.06f, 0.16f);
    public Vector2 nightDashStartDelayRange = new Vector2(0.9f, 1.15f);
    public bool nightDashGhostColliders = true;

    [Header("Long stare (unpredictability)")]
    [Range(0f, 1f)] public float longStareChance = 0.22f;
    public Vector2 longStareLifetime = new Vector2(6f, 14f);

    [Header("Camera-turn extra spawns")]
    public float yawDeltaThresholdDeg = 55f;
    [Range(0f, 1f)] public float extraSpawnOnTurnChance = 0.65f;
    public int extraSpawnCount = 3;

    [Header("Audio")]
    public AudioClip[] horrorOneShots;
    public Vector2 horrorAudioRate = new Vector2(6f, 14f); // clips per second during burst window
    AudioSource horrorAudio;

    [Header("Player punishment (feel)")]
    public float preBurstPanicSeconds = 1.1f;
    public float panicDrainPerSecond = 6f;
    public float screenPulseAlpha = 0.18f;
    public float screenPulseAttack = 0.12f;
    public float screenPulseHold = 0.35f;
    public float screenPulseRelease = 0.55f;

    public float shakeSeconds = 1.1f;
    public float shakeIntensity = 1f;

    [Header("Debug")]
    public bool debugLogs = true;

    float cooldown;
    float lastYaw;
    Coroutine waveRoutine;
    Coroutine audioRoutine;
    float activeWaveEndTime;
    float waveChanceMultiplier = 1f;
    float cooldownMultiplier = 1f;
    bool disabledByRitual = false;

    void OnEnable()
    {
        TryBindReferences();
        EnsureHorrorAudioSource();

        if (onlyInNightmareNight)
        {
            if (PrologueFlowManager.Instance == null ||
                PrologueFlowManager.Instance.currentPhase != PrologueFlowManager.Phase.NightmareNight)
            {
                enabled = false;
                return;
            }
        }

        if (playerCamera != null)
            lastYaw = playerCamera.transform.eulerAngles.y;

        cooldown = Random.Range(waveCooldownRange.x, waveCooldownRange.y) * Mathf.Max(0.2f, cooldownMultiplier);

        if (debugLogs)
            Debug.Log($"[NightHorrorWave] Enabled. player={(player!=null)} cam={(playerCamera!=null)} heavy={(heavyPrefabs!=null && heavyPrefabs.Length>0)} light={(lightPrefabs!=null && lightPrefabs.Length>0)}");
    }

    void TryBindReferences()
    {
        if (autoFindPlayer)
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
            if (playerCamera == null && player != null)
                playerCamera = player.GetComponentInChildren<Camera>();
            if (playerController == null && player != null)
                playerController = player.GetComponent<PlayerController>();
        }

        if (screenPulse == null)
            screenPulse = FindObjectOfType<SimpleScreenFader>(true);

        if (cameraShake == null && playerCamera != null)
            cameraShake = playerCamera.GetComponent<CameraShakeImpulse>() ??
                          playerCamera.GetComponentInParent<CameraShakeImpulse>();
    }

    void EnsureHorrorAudioSource()
    {
        if (horrorAudio != null) return;
        horrorAudio = gameObject.GetComponent<AudioSource>();
        if (horrorAudio == null) horrorAudio = gameObject.AddComponent<AudioSource>();
        horrorAudio.playOnAwake = false;
        horrorAudio.spatialBlend = 1f;
        horrorAudio.dopplerLevel = 0f;
        horrorAudio.rolloffMode = AudioRolloffMode.Linear;
        horrorAudio.minDistance = 1f;
        horrorAudio.maxDistance = 80f;
    }

    void Update()
    {
        if (disabledByRitual) return;
        if (onlyInNightmareNight)
        {
            if (PrologueFlowManager.Instance == null ||
                PrologueFlowManager.Instance.currentPhase != PrologueFlowManager.Phase.NightmareNight)
            {
                StopAllCoroutinesSafe();
                enabled = false;
                return;
            }
        }

        if (player == null || playerCamera == null)
        {
            TryBindReferences();
            if (player == null || playerCamera == null) return;
        }

        // Extra spawns when player turns camera a lot (during an active wave)
        if (waveRoutine != null)
        {
            float y = playerCamera.transform.eulerAngles.y;
            float dy = Mathf.Abs(Mathf.DeltaAngle(y, lastYaw));
            lastYaw = y;

            if (dy >= yawDeltaThresholdDeg && Random.value < extraSpawnOnTurnChance)
            {
                ExtendHorrorAudioWindow(burstDuration * 0.35f + 0.35f);
                StartCoroutine(SpawnRingSegment(extraSpawnCount, 0f));
            }
        }

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;
        cooldown = Random.Range(waveCooldownRange.x, waveCooldownRange.y) * Mathf.Max(0.2f, cooldownMultiplier);

        float effectiveWaveChance = Mathf.Clamp01(waveChance * Mathf.Max(0f, waveChanceMultiplier));
        if (Random.value < effectiveWaveChance && waveRoutine == null)
            waveRoutine = StartCoroutine(WaveRoutine());
    }

    IEnumerator WaveRoutine()
    {
        // Pre-warning: stamina dump + breathing starts ramping via PlayerController helper
        if (playerController != null)
            playerController.PanicDrainStamina(preBurstPanicSeconds, panicDrainPerSecond, true);

        if (screenPulse != null)
            StartCoroutine(screenPulse.PulseAlpha(screenPulseAlpha, screenPulseAttack, screenPulseHold, screenPulseRelease));

        if (cameraShake != null)
            cameraShake.Shake(shakeSeconds, shakeIntensity);

        bool longStare = Random.value < longStareChance;
        if (longStare)
        {
            int n = 1;
            float stare = Random.Range(longStareLifetime.x, longStareLifetime.y);
            activeWaveEndTime = Time.time + stare + burstDuration + 0.75f;
            StartHorrorAudio();
            yield return SpawnRingSegment(n, 0f);
            yield return new WaitForSeconds(stare);
        }
        else
        {
            int count = Random.Range(burstCountMin, burstCountMax + 1);
            activeWaveEndTime = Time.time + burstDuration + 0.75f;
            StartHorrorAudio();
            // Spawn the whole ring immediately (no stagger).
            yield return SpawnRingSegment(count, 0f);
        }

        // Turn-spawn segments are started from Update and are not awaited here; give them a moment to finish.
        yield return new WaitForSeconds(burstDuration * 0.45f + 0.2f);

        StopAudioSpam();
        waveRoutine = null;
    }

    IEnumerator SpawnRingSegment(int count, float duration)
    {
        float startAngle = Random.Range(0f, 360f);
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            float t = (count <= 1) ? 1f : (i / (float)(count - 1));
            float delay = duration * t;
            if (delay > 0f) yield return new WaitForSeconds(delay);

            float ang = startAngle + (360f * i) / Mathf.Max(1, count) + Random.Range(-4f, 4f);
            if (SpawnOneOnRing(ang)) spawned++;
        }

        if (spawned <= 0 && spawnFailSafeEnabled)
        {
            float fallbackYaw = Random.Range(0f, 360f);
            Vector3 fallbackPos = player != null
                ? player.position + (Quaternion.Euler(0f, fallbackYaw, 0f) * Vector3.forward) * Mathf.Max(3f, spawnFailSafeDistance)
                : transform.position;
            SpawnOneAtFallback(fallbackPos);
        }
    }

    bool SpawnOneOnRing(float yawDeg)
    {
        if (player == null) return false;

        Quaternion rotYaw = Quaternion.Euler(0f, yawDeg, 0f);
        Vector3 dir = rotYaw * Vector3.forward;
        float r = ringRadius + Random.Range(-ringRadiusJitter, ringRadiusJitter);

        Vector3 pos = player.position + dir * r + Vector3.up * Random.Range(-heightJitter, heightJitter);

        pos = SpawnSurfaceAlign.Resolve(
            pos,
            player,
            groundMask,
            groundRayUp,
            groundRayDown,
            snapToNavMesh,
            navMeshSnapDistance,
            surfaceFootClearance,
            QueryTriggerInteraction.Ignore,
            burstNavmeshSnapMaxPull);
        pos.y += nightSpawnHeightOffset;

        pos = EnforceMinRingHorizontalDistance(pos, yawDeg);

        GameObject prefab = PickPrefab();
        if (prefab == null) return false;

        Quaternion face = Quaternion.LookRotation(player.position - pos);
        face = Quaternion.Euler(0f, face.eulerAngles.y, 0f);

        GameObject go = Instantiate(prefab, pos, face);
        HorrorSpawnPhysics.MakeSpawnNonSolid(go);

        // Night silhouettes: mostly still, but allow a tiny "charge flash" if heavy prefab has JumpscareMove
        var mover = go.GetComponentInChildren<JumpscareMove>(true);
        if (mover != null)
        {
            bool heavy = IsHeavyPrefab(prefab);
            if (!heavy)
            {
                mover.enabled = false;
            }
            else
            {
                mover.enabled = false;
                mover.despawnOnReach = true;
                float dashSpeed = Random.Range(nightDashSpeedRange.x, nightDashSpeedRange.y);
                float dashStop = Random.Range(nightDashStopDistanceRange.x, nightDashStopDistanceRange.y);
                float dashDespawn = Random.Range(nightDashDespawnDelayRange.x, nightDashDespawnDelayRange.y);
                float dashStartDelay = Random.Range(nightDashStartDelayRange.x, nightDashStartDelayRange.y);
                StartCoroutine(ArmDashAfter(go, mover, dashStartDelay, dashSpeed, dashStop, dashDespawn));
            }
        }

        // Freeze animator sway for light prefabs
        Animator[] anims = go.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < anims.Length; i++)
        {
            anims[i].applyRootMotion = false;
            if (!IsHeavyPrefab(prefab))
                anims[i].speed = 0f;
            else
                anims[i].speed = Mathf.Max(1f, anims[i].speed);
        }

        // Auto cleanup (longer so you can actually see it)
        float life = Random.Range(monsterLifetimeRange.x, monsterLifetimeRange.y);
        Destroy(go, life);

        if (debugLogs)
            Debug.Log($"[NightHorrorWave] Spawned {prefab.name} at {pos} life={life:0.00}s");
        return true;
    }

    void SpawnOneAtFallback(Vector3 worldPos)
    {
        if (player == null) return;
        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        Vector3 pos = SpawnSurfaceAlign.Resolve(
            worldPos,
            player,
            groundMask,
            groundRayUp,
            groundRayDown,
            false,
            navMeshSnapDistance,
            surfaceFootClearance);
        pos.y += nightSpawnHeightOffset;
        Quaternion face = Quaternion.LookRotation(player.position - pos);
        face = Quaternion.Euler(0f, face.eulerAngles.y, 0f);

        GameObject go = Instantiate(prefab, pos, face);
        HorrorSpawnPhysics.MakeSpawnNonSolid(go);
        float life = Random.Range(monsterLifetimeRange.x, monsterLifetimeRange.y);
        Destroy(go, life);
        if (debugLogs)
            Debug.Log($"[NightHorrorWave] Fallback spawn at {pos}");
    }

    Vector3 EnforceMinRingHorizontalDistance(Vector3 pos, float yawDeg)
    {
        if (player == null || minHorizontalClearFromPlayer <= 0f)
            return pos;

        Vector3 pp = player.position;
        Vector3 d = pos - pp;
        d.y = 0f;
        if (d.sqrMagnitude >= minHorizontalClearFromPlayer * minHorizontalClearFromPlayer)
            return pos;

        Vector3 outward = Quaternion.Euler(0f, yawDeg, 0f) * Vector3.forward;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.0001f)
            outward = Vector3.forward;
        outward.Normalize();

        pos.x = pp.x + outward.x * minHorizontalClearFromPlayer;
        pos.z = pp.z + outward.z * minHorizontalClearFromPlayer;
        return pos;
    }

    IEnumerator ArmDashAfter(GameObject go, JumpscareMove mover, float wait, float speed, float stopDist, float despawnDelay)
    {
        if (go == null || mover == null) yield break;
        mover.enabled = false;
        yield return new WaitForSeconds(Mathf.Max(0f, wait));
        if (go == null || mover == null) yield break;
        mover.enabled = true;
        mover.ConfigureDash(speed, stopDist, nightDashGhostColliders, despawnDelay);
    }

    GameObject PickPrefab()
    {
        bool wantHeavy = heavyPrefabs != null && heavyPrefabs.Length > 0 && Random.value < heavyPickChance;
        if (wantHeavy)
            return heavyPrefabs[Random.Range(0, heavyPrefabs.Length)];

        if (lightPrefabs != null && lightPrefabs.Length > 0)
            return lightPrefabs[Random.Range(0, lightPrefabs.Length)];

        if (heavyPrefabs != null && heavyPrefabs.Length > 0)
            return heavyPrefabs[Random.Range(0, heavyPrefabs.Length)];

        return null;
    }

    bool IsHeavyPrefab(GameObject prefab)
    {
        if (heavyPrefabs == null) return false;
        for (int i = 0; i < heavyPrefabs.Length; i++)
            if (heavyPrefabs[i] == prefab) return true;
        return false;
    }

    void StartHorrorAudio()
    {
        if (horrorOneShots == null || horrorOneShots.Length == 0) return;
        if (audioRoutine != null) return;
        EnsureHorrorAudioSource();
        audioRoutine = StartCoroutine(HorrorAudioSpam());
    }

    void ExtendHorrorAudioWindow(float extraSeconds)
    {
        if (horrorOneShots == null || horrorOneShots.Length == 0) return;
        activeWaveEndTime = Mathf.Max(activeWaveEndTime, Time.time + Mathf.Max(0f, extraSeconds));
        if (audioRoutine == null && waveRoutine != null)
            StartHorrorAudio();
    }

    IEnumerator HorrorAudioSpam()
    {
        while (Time.time < activeWaveEndTime)
        {
            var clip = horrorOneShots[Random.Range(0, horrorOneShots.Length)];
            if (clip != null && player != null && horrorAudio != null)
            {
                Vector3 p = player.position + Random.onUnitSphere * 2.2f;
                horrorAudio.transform.position = p;
                horrorAudio.PlayOneShot(clip, Random.Range(0.55f, 1f));
            }

            float rate = Random.Range(horrorAudioRate.x, horrorAudioRate.y);
            float wait = 1f / Mathf.Max(1f, rate);
            yield return new WaitForSeconds(wait);
        }
        audioRoutine = null;
    }

    void StopAudioSpam()
    {
        if (audioRoutine != null)
        {
            StopCoroutine(audioRoutine);
            audioRoutine = null;
        }
    }

    void StopAllCoroutinesSafe()
    {
        StopAudioSpam();
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
    }

    public void SetRelicModifiers(float chanceMultiplier, float cooldownStretch)
    {
        waveChanceMultiplier = Mathf.Clamp(chanceMultiplier, 0.05f, 2f);
        cooldownMultiplier = Mathf.Clamp(cooldownStretch, 0.2f, 4f);
    }

    public void DisableAfterRitual()
    {
        disabledByRitual = true;
        StopAllCoroutinesSafe();
        enabled = false;
    }
}

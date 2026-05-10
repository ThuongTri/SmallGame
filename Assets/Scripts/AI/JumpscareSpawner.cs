using System.Collections;
using UnityEngine;

public class JumpscareSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject monsterPrefab;

    [Header("Spawn placement")]
    public float minDist = 18f;          // xa hon de giong 'bong xa'
    public float maxDist = 38f;
    public float spawnHeightOffset = 0f;
    public LayerMask groundMask = ~0;
    public float groundRayUp = 50f;
    public float groundRayDown = 220f;
    public bool snapToNavMesh = true;
    public float navMeshSnapDistance = 4f;
    [Tooltip("Extra height above raycast hit (NavMesh Y is often below visible ground).")]
    public float surfaceFootClearance = 0.12f;

    [Header("Visibility modes (ban ngay)")]
    [Range(0f, 1f)] public float peripheralSpawnWeight = 0.55f; // ty le 'ngoai bien'
    public float peripheralYawMin = 35f;   // do lech khoi huong nhin (tuyet doi)
    public float peripheralYawMax = 85f;

    public float centerYawMax = 18f;         // 'nhin thang' = trong cone hep

    [Header("Timing / pacing")]
    public Vector2 intervalRange = new Vector2(8f, 14f);
    public AnimationCurve chanceByAggression = AnimationCurve.Linear(0, 0.55f, 1, 0.85f);

    [Header("Lifetime")]
    public Vector2 autoDespawnRange = new Vector2(2.8f, 5.2f); // neu player khong lai gan

    [Header("Vanish when approached")]
    public float vanishDistance = 14f;
    [Range(0f, 1f)] public float instantVanishChance = 0.5f;
    public float fadeDuration = 0.3f;

    [Header("Ban ngay: chi khi camera nhin thay")]
    public Vector2 dayDespawnAfterSeenRange = new Vector2(1f, 2f);
    public float dayRevealMaxDistance = 55f;
    [Range(0f, 0.2f)] public float dayViewportEdgeReject = 0.04f;

    [Header("Audio (giu nguyen y tuong cu)")]
    public AudioClip[] scareSounds;
    [Range(0f, 1f)] public float scareSoundChance = 0.65f; // chi dem: random phat luc spawn (dem)
    AudioSource scareAudio;

    [Header("References")]
    public Transform player;
    public MonsterAI monster;
    public Camera playerCamera;

    [Header("Creepy stance")]
    public bool keepFacingPlayer = true;
    public float faceTurnSpeed = 6f;
    public bool freezeAnimatorRootMotionInDay = true;

    [Header("Night chase behaviour")]
    public float nightRunSpeed = 48f;
    public float nightStopDistance = 0.5f;
    public float nightDespawnDelay = 0.12f;
    public bool nightGhostPassThrough = true;

    [Header("Phase gating")]
    [Tooltip("If true, spawner pauses during TransitionSleep. Keep OFF to run continuously day+night.")]
    public bool gateByProloguePhase = false;

    float cooldown;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (playerCamera == null && player != null)
            playerCamera = player.GetComponentInChildren<Camera>();

        EnsureScareAudioSource();

        cooldown = Random.Range(intervalRange.x, intervalRange.y);
    }

    void EnsureScareAudioSource()
    {
        if (scareAudio != null) return;
        scareAudio = gameObject.GetComponent<AudioSource>();
        if (scareAudio == null) scareAudio = gameObject.AddComponent<AudioSource>();
        scareAudio.playOnAwake = false;
        scareAudio.spatialBlend = 1f;
        scareAudio.dopplerLevel = 0f;
        scareAudio.rolloffMode = AudioRolloffMode.Linear;
        scareAudio.minDistance = 2f;
        scareAudio.maxDistance = 90f;
    }

    void Update()
    {
        if (monster == null || player == null) return;
        if (gateByProloguePhase && PrologueFlowManager.Instance != null)
        {
            if (PrologueFlowManager.Instance.currentPhase == PrologueFlowManager.Phase.TransitionSleep)
                return;
        }

        cooldown -= Time.deltaTime;
        if (cooldown > 0f) return;

        float agg = monster != null ? monster.aggression : 0f;
        float chance = chanceByAggression.Evaluate(Mathf.Clamp01(agg));

        if (Random.value < chance)
            StartCoroutine(SpawnRoutine());

        cooldown = Random.Range(intervalRange.x, intervalRange.y);
    }

    IEnumerator SpawnRoutine()
    {
        if (monsterPrefab == null || playerCamera == null) yield break;

        bool peripheral = Random.value < peripheralSpawnWeight;

        float yawOffsetDeg = peripheral
            ? Random.Range(peripheralYawMin, peripheralYawMax) * (Random.value < 0.5f ? -1f : 1f)
            : Random.Range(-centerYawMax, centerYawMax);

        Vector3 flatForward = playerCamera.transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.0001f) yield break;
        flatForward.Normalize();

        Quaternion yawRot = Quaternion.Euler(0f, yawOffsetDeg, 0f);
        Vector3 dir = yawRot * flatForward;
        dir.Normalize();

        float dist = Random.Range(minDist, maxDist);
        Vector3 pos = player.position + dir * dist;

        pos = ProjectSpawnPosition(pos);

        Quaternion rot = Quaternion.LookRotation(player.position - pos);
        rot = Quaternion.Euler(0f, rot.eulerAngles.y, 0f);

        GameObject go = Instantiate(monsterPrefab, pos, rot);
        HorrorSpawnPhysics.MakeSpawnNonSolid(go);

        bool isNight = PrologueFlowManager.Instance != null &&
                       PrologueFlowManager.Instance.currentPhase == PrologueFlowManager.Phase.NightmareNight;

        // Day: dung yen nhin player | Night: ruot toi nguong hu roi bien mat
        var mover = go.GetComponentInChildren<JumpscareMove>(true);
        if (mover != null)
        {
            if (isNight)
            {
                mover.enabled = true;
                mover.despawnOnReach = true;
                mover.ConfigureDash(nightRunSpeed, nightStopDistance, nightGhostPassThrough, nightDespawnDelay);
            }
            else
            {
                mover.enabled = false;
            }
        }

        if (!isNight && freezeAnimatorRootMotionInDay)
        {
            Animator[] anims = go.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i].applyRootMotion = false;
                anims[i].speed = 0f;
            }
        }

        // Dem: am thanh khi spawn (random). Ban ngay: am thanh chi khi camera thay (trong SpawnedFigureVanishDriver).
        if (isNight && scareSounds != null && scareSounds.Length > 0 && Random.value < scareSoundChance)
        {
            EnsureScareAudioSource();
            var clip = scareSounds[Random.Range(0, scareSounds.Length)];
            if (clip != null)
            {
                scareAudio.transform.position = pos;
                scareAudio.PlayOneShot(clip, 1f);
            }
        }

        if (isNight)
        {
            Animator[] anims = go.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < anims.Length; i++)
            {
                anims[i].applyRootMotion = false;
                anims[i].speed = Mathf.Max(1.25f, anims[i].speed);
            }
        }

        float life = Random.Range(autoDespawnRange.x, autoDespawnRange.y);

        if (!isNight)
            EnsureScareAudioSource();

        var runner = go.AddComponent<SpawnedFigureVanishDriver>();
        bool dayReveal = !isNight && playerCamera != null;
        runner.Init(
            player,
            vanishDistance,
            instantVanishChance,
            fadeDuration,
            !isNight && keepFacingPlayer,
            faceTurnSpeed,
            dayReveal,
            dayReveal ? playerCamera : null,
            dayReveal ? scareAudio : null,
            dayReveal ? scareSounds : null,
            dayDespawnAfterSeenRange,
            dayRevealMaxDistance,
            dayViewportEdgeReject,
            life);

        float t = 0f;
        while (t < life && go != null)
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (go != null)
            runner.ForceDespawnPreferFade();
    }

    Vector3 ProjectSpawnPosition(Vector3 pos)
    {
        return SpawnSurfaceAlign.Resolve(
            pos,
            player,
            groundMask,
            groundRayUp,
            groundRayDown,
            snapToNavMesh,
            navMeshSnapDistance,
            surfaceFootClearance + spawnHeightOffset);
    }

    // Mini helper component (tao tren instance luc spawn)
    class SpawnedFigureVanishDriver : MonoBehaviour
    {
        Transform player;
        float vanishDist;
        float vanishSqr;
        float instantChance;
        float fadeDur;
        bool despawned;
        bool facePlayer;
        float turnSpeed;

        Renderer[] rends;
        Color[] startColors;
        Material[] mats; // instance materials

        bool dayCameraReveal;
        Camera dayCam;
        AudioSource dayScareAudio;
        AudioClip[] dayScareClips;
        Vector2 dayAfterSeenDespawn;
        float dayRevealMaxDist;
        float dayViewportEdge;
        float maxUnseenLifetime;
        float spawnTime;
        bool seenByCamera;
        Coroutine seenDespawnRoutine;

        public void Init(
            Transform player,
            float vanishDistance,
            float instantChance,
            float fadeDur,
            bool facePlayer,
            float turnSpeed,
            bool dayCameraReveal,
            Camera dayRevealCamera,
            AudioSource scareAudioHost,
            AudioClip[] scareWhenSeenClips,
            Vector2 despawnAfterSeenRange,
            float revealMaxDistance,
            float viewportEdgeReject,
            float maxUnseenLife)
        {
            this.player = player;
            this.vanishDist = vanishDistance;
            this.vanishSqr = vanishDistance * vanishDistance;
            this.instantChance = instantChance;
            this.fadeDur = Mathf.Max(0.01f, fadeDur);
            this.facePlayer = facePlayer;
            this.turnSpeed = Mathf.Max(0.1f, turnSpeed);

            this.dayCameraReveal = dayCameraReveal && dayRevealCamera != null;
            this.dayCam = dayRevealCamera;
            this.dayScareAudio = scareAudioHost;
            this.dayScareClips = scareWhenSeenClips;
            this.dayAfterSeenDespawn = despawnAfterSeenRange;
            this.dayRevealMaxDist = revealMaxDistance;
            this.dayViewportEdge = Mathf.Clamp01(viewportEdgeReject);
            this.maxUnseenLifetime = maxUnseenLife;
            this.spawnTime = Time.time;

            rends = GetComponentsInChildren<Renderer>();
            startColors = new Color[rends.Length];
            mats = new Material[rends.Length];

            for (int i = 0; i < rends.Length; i++)
            {
                mats[i] = rends[i].material; // tao material instance
                startColors[i] = mats[i].HasProperty("_Color") ? mats[i].color : Color.white;
            }
        }

        bool IsFigureVisibleFromCamera()
        {
            if (dayCam == null) return false;
            var planes = GeometryUtility.CalculateFrustumPlanes(dayCam);
            float m = dayViewportEdge;

            if (rends != null && rends.Length > 0)
            {
                for (int i = 0; i < rends.Length; i++)
                {
                    var r = rends[i];
                    if (r == null || !r.enabled) continue;
                    if (!GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;

                    Vector3 c = r.bounds.center;
                    if (Vector3.Distance(dayCam.transform.position, c) > dayRevealMaxDist) continue;

                    Vector3 vp = dayCam.WorldToViewportPoint(c);
                    if (vp.z < 0.15f) continue;
                    if (vp.x < m || vp.x > 1f - m || vp.y < m || vp.y > 1f - m) continue;
                    return true;
                }
            }

            Vector3 p = transform.position + Vector3.up * 1.2f;
            if (!GeometryUtility.TestPlanesAABB(planes, new Bounds(p, Vector3.one * 0.5f))) return false;
            Vector3 vpp = dayCam.WorldToViewportPoint(p);
            if (vpp.z < 0.15f) return false;
            if (vpp.x < m || vpp.x > 1f - m || vpp.y < m || vpp.y > 1f - m) return false;
            return Vector3.Distance(dayCam.transform.position, p) <= dayRevealMaxDist;
        }

        void PlaySeenScareSound()
        {
            if (dayScareAudio == null || dayScareClips == null || dayScareClips.Length == 0) return;
            var clip = dayScareClips[Random.Range(0, dayScareClips.Length)];
            if (clip == null) return;
            dayScareAudio.transform.position = transform.position;
            dayScareAudio.PlayOneShot(clip, 1f);
        }

        void Update()
        {
            if (despawned || player == null) return;

            if (facePlayer)
            {
                Vector3 toPlayer = player.position - transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.0001f)
                {
                    Quaternion target = Quaternion.LookRotation(toPlayer.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * turnSpeed);
                }
            }

            if (dayCameraReveal)
            {
                if (!seenByCamera && Time.time - spawnTime >= maxUnseenLifetime)
                {
                    if (!despawned)
                    {
                        despawned = true;
                        Destroy(gameObject);
                    }
                    return;
                }

                if (!seenByCamera && IsFigureVisibleFromCamera())
                {
                    seenByCamera = true;
                    PlaySeenScareSound();
                    if (seenDespawnRoutine == null)
                        seenDespawnRoutine = StartCoroutine(DespawnAfterSeenRoutine());
                }

                return;
            }
        }

        IEnumerator DespawnAfterSeenRoutine()
        {
            float wait = Random.Range(dayAfterSeenDespawn.x, dayAfterSeenDespawn.y);
            yield return new WaitForSeconds(Mathf.Max(0.05f, wait));
            ForceDespawnPreferFade();
        }

        IEnumerator FadeOutDestroy()
        {
            float t = 0f;
            while (t < fadeDur)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / fadeDur);
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    if (!mats[i].HasProperty("_Color")) continue;
                    Color c = startColors[i];
                    c.a = Mathf.Lerp(c.a, 0f, u);
                    mats[i].color = c;
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        public void ForceDespawnPreferFade()
        {
            if (despawned) return;
            despawned = true;
            if (Random.value < instantChance) Destroy(gameObject);
            else StartCoroutine(FadeOutDestroy());
        }
    }
}
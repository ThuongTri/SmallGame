using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MonsterAI : MonoBehaviour {
    public enum State { Patrol, Stalk, Chase, Search }
    public State state = State.Patrol;

    [Header("Core")]
    public NavMeshAgent agent;
    public Transform player;
    public Transform[] waypoints;
    int wpIndex = 0;

    [Header("Senses")]
    public float viewAngle = 100f;
    public float viewDistance = 30f;
    public LayerMask visionMask; // layers that block view (trees, rocks)
    public float hearingBase = 6f; // base hearing radius
    public float hearingMultiplier = 1f;

    [Header("Speeds")]
    public float patrolSpeed = 1.4f;
    public float stalkSpeed = 2.2f;
    public float chaseSpeed = 5.0f;

    [Header("Stalk settings")]
    public float stalkMinDist = 12f;
    public float stalkMaxDist = 18f;
    public AudioClip[] whisperClips;
    public AudioClip mimicVoiceClip;
    AudioSource audioSource;

    [Header("Search")]
    public float searchDuration = 8f;
    Vector3 lastSighting;
    Coroutine searchCoroutine;

    [Header("Aggression")]
    [Range(0f,1f)] public float aggression = 0f; // 0–1 from GameDirector

    [Header("Visual")]
    public Renderer bellyRenderer; // renderer for the glowing 'ruột' material
    public float emissionBase = 1.2f;
    public float emissionMax = 6f;
    public float bobSpeed = 2f;
    public float bobAmount = 0.12f;

    [Header("Debug")]
    public bool drawGizmos = true;

    void Awake(){
        if (agent==null) agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start(){
        agent.speed = patrolSpeed;
        GoNextWaypoint();
      //  NoiseEmitter.OnNoise += OnNoiseHeard;
    }

  //  void OnDestroy(){
   //     NoiseEmitter.OnNoise -= OnNoiseHeard;
  //  }

    void Update(){
        UpdateEmissionBob(); // visual bob
        switch(state){
            case State.Patrol: PatrolUpdate(); break;
            case State.Stalk: StalkUpdate(); break;
            case State.Chase: ChaseUpdate(); break;
            case State.Search: /*Search handled in coroutine*/ break;
        }

        if (CanSeePlayer()){
            lastSighting = player.position;
            EnterState(State.Chase);
        }
    }

    void PatrolUpdate(){
        agent.speed = Mathf.Lerp(agent.speed, patrolSpeed, Time.deltaTime*2f);
        if (!agent.pathPending && agent.remainingDistance < 1f) GoNextWaypoint();
    }

    void StalkUpdate(){
        agent.speed = Mathf.Lerp(agent.speed, stalkSpeed, Time.deltaTime*2f);
        // maintain distance behind / to the side
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 target = player.position - dir * ((stalkMinDist + stalkMaxDist) * 0.5f);
        agent.SetDestination(target);

        // occasionally mimic voice or whisper
        if (!audioSource.isPlaying && Random.value < 0.002f * (1f + aggression*5f)) {
            PlayWhisperOrMimic();
        }
    }

    void ChaseUpdate(){
        agent.speed = Mathf.Lerp(agent.speed, Mathf.Lerp(chaseSpeed*0.9f, chaseSpeed, aggression), Time.deltaTime*2f);
        agent.SetDestination(player.position);
    }

    void GoNextWaypoint(){
        if (waypoints == null || waypoints.Length==0) return;
        agent.SetDestination(waypoints[wpIndex].position);
        wpIndex = (wpIndex + 1) % waypoints.Length;
    }

    bool CanSeePlayer(){
        if (player == null) return false;
        Vector3 eye = transform.position + Vector3.up * 1.6f;
        Vector3 targetPos = player.position + Vector3.up * 1.0f; // player's torso/head
        Vector3 dir = targetPos - eye;
        float dist = dir.magnitude;
        if (dist > viewDistance) return false;
        float angle = Vector3.Angle(transform.forward, dir.normalized);
        if (angle > viewAngle * 0.5f) return false;
        if (Physics.Raycast(eye, dir.normalized, out RaycastHit hit, dist, ~visionMask)) {
            // if ray hits something not player, can't see
            if (hit.transform != player && !hit.transform.IsChildOf(player)) return false;
        } else {
            // no hit? assume visible (rare)
        }
        return true;
    }

    void OnNoiseHeard(Vector3 pos, float loudness){
        // loudness in arbitrary units. hearing = base + loudness * multiplier
        float hearingRange = hearingBase + loudness * hearingMultiplier;
        if (Vector3.Distance(transform.position, pos) <= hearingRange){
            lastSighting = pos;
            EnterState(State.Stalk);
        }
    }

    void EnterState(State next){
        if (state == next) return;
        state = next;
        // cancel search coroutine if any
        if (searchCoroutine != null){ StopCoroutine(searchCoroutine); searchCoroutine = null; }
        if (state == State.Search){
            searchCoroutine = StartCoroutine(DoSearch());
        }
    }

    IEnumerator DoSearch(){
        agent.SetDestination(lastSighting);
        float t = 0f;
        while(t < searchDuration){
            // circle around lastSighting a bit
            Vector3 offset = new Vector3(Mathf.Sin(t*1.2f),0, Mathf.Cos(t*1.2f)) * 3f;
            agent.SetDestination(lastSighting + offset);
            t += Time.deltaTime;
            // if sees player break
            if (CanSeePlayer()){
                EnterState(State.Chase);
                yield break;
            }
            yield return null;
        }
        // give up -> patrol
        EnterState(State.Patrol);
    }

    void PlayWhisperOrMimic(){
        if (mimicVoiceClip != null && aggression > 0.6f && Random.value < 0.6f) {
            audioSource.PlayOneShot(mimicVoiceClip);
        } else if (whisperClips != null && whisperClips.Length > 0) {
            audioSource.PlayOneShot(whisperClips[Random.Range(0, whisperClips.Length)]);
        }
    }

    // Visual: emission bobbing
    void UpdateEmissionBob(){
        if (bellyRenderer==null) return;
        Material mat = bellyRenderer.material;
        float t = Time.time * bobSpeed;
        float emission = emissionBase + Mathf.Abs(Mathf.Sin(t)) * bobAmount * (1f + aggression*3f);
        emission = Mathf.Lerp(emission, Mathf.Lerp(emissionBase, emissionMax, aggression), 0.5f);
        Color baseColor = Color.red; // tune in inspector by making material parametric
        if (mat.HasProperty("_EmissionColor")) {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseColor * emission);
        }
    }

    // SafeZone hook called by SafeZone script
    public void OnEnterSafeZone(){
        // calm down
        aggression = Mathf.Max(0f, aggression - 0.4f);
        // retreat a bit
        EnterState(State.Patrol);
    }

    public void AdjustAggression(float delta){
        aggression = Mathf.Clamp01(aggression + delta);
    }

    // Debug gizmos
    void OnDrawGizmosSelected(){
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingBase);
        Gizmos.color = Color.red;
        Vector3 eye = transform.position + Vector3.up * 1.6f;
        Gizmos.DrawWireSphere(eye, viewDistance);
        // draw FOV lines
        Vector3 left = Quaternion.Euler(0, -viewAngle*0.5f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle*0.5f, 0) * transform.forward;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(eye, eye + left * viewDistance);
        Gizmos.DrawLine(eye, eye + right * viewDistance);
    }
}

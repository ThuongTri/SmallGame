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
    public float chaseSpeed = 6.0f;

    [Header("Chase Settings (New)")]
    public float maxChaseDuration = 5f; 
    private float chaseTimer = 0f;      

    [Header("Jumpscare / Attack")]
    public Transform faceTarget; // ✅ Kéo xương ĐẦU của quái vào đây
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

    float lastHeardLogTime = -10f;
    float patrolWaitTimer = 0f; 

    void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        agent.speed = patrolSpeed;
        SetRandomDestination();
    }

    void Update()
    {
        if (isJumpscaring) return; 

        UpdateEmissionBob();

        if (animator != null) animator.SetFloat("Speed", agent.velocity.magnitude);

        switch (state)
        {
            case State.Patrol: PatrolUpdate(); break;
            case State.Stalk: StalkUpdate(); break;
            case State.Chase: ChaseUpdate(); break;
            case State.Search: break;
        }

        // Ưu tiên: Nếu đang Chase thì không cần check nhìn lại liên tục (trừ khi mất dấu)
        if (state != State.Jumpscare && state != State.Chase)
        {
            if (CanSeePlayer())
            {
                lastSighting = player.position;
                EnterState(State.Chase);
            }
        }
    }

    // =====================================================
    // LOGIC ĐI TUẦN NGẪU NHIÊN
    // =====================================================
    void PatrolUpdate()
    {
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
        Vector3 randomPoint = GetRandomNavMeshPoint(transform.position, patrolRadius);
        agent.SetDestination(randomPoint);
    }

    Vector3 GetRandomNavMeshPoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomPos = center + Random.insideUnitSphere * radius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 5.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return center;
    }

    // =====================================================
    // LOGIC ĐUỔI BẮT (CÓ GIỚI HẠN THỜI GIAN)
    // =====================================================
    void ChaseUpdate()
    {
        agent.speed = Mathf.Lerp(agent.speed, Mathf.Lerp(chaseSpeed * 0.9f, chaseSpeed, aggression), Time.deltaTime * 2f);
        agent.SetDestination(player.position);

        // Logic thời gian đuổi
        chaseTimer += Time.deltaTime;

        if (CanSeePlayer())
        {
            lastSighting = player.position;
            // chaseTimer = 0f; // Nếu muốn reset timer khi nhìn thấy thì bỏ comment
        }

        if (chaseTimer >= maxChaseDuration)
        {
            EnterState(State.Search); 
            return;
        }

        // Kiểm tra bắt người chơi (Bỏ qua trục Y để tính chính xác hơn)
        Vector3 monsterPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 playerPosFlat = new Vector3(player.position.x, 0, player.position.z);
        float distToPlayer = Vector3.Distance(monsterPosFlat, playerPosFlat);
        
        if (distToPlayer <= catchDistance)
        {
            StartCoroutine(TriggerJumpscare());
        }
    }

    IEnumerator TriggerJumpscare()
    {
        isJumpscaring = true;
        state = State.Jumpscare;
        
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        var playerCtrl = player.GetComponent<PlayerController>();
        if (playerCtrl != null) playerCtrl.LockPlayerInput();

        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetTrigger("Attack");
        }

        if (jumpscareScreamClip != null) audioSource.PlayOneShot(jumpscareScreamClip);

        float timer = 0f;
        Quaternion startRot = player.rotation;

        // ✅ LOGIC MỚI: Nhìn thẳng vào cái đầu (faceTarget)
        // Nếu quên gán faceTarget thì nhìn cao lên 1.7m (tạm)
        Vector3 targetLookPos = (faceTarget != null) ? faceTarget.position : transform.position + Vector3.up * 1.7f;
        Vector3 dirToFace = (targetLookPos - player.position).normalized;
        
        // Cho phép xoay cả lên/xuống (bỏ việc khóa trục Y)
        Quaternion lookRot = Quaternion.LookRotation(dirToFace);

        while (timer < 0.5f)
        {
            // Xoay Player từ từ hướng về mặt quái
            player.rotation = Quaternion.Slerp(startRot, lookRot, timer / 0.5f);
            
            // Quái cũng xoay mặt về phía player
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            dirToPlayer.y = 0; // Quái thì chỉ cần xoay ngang thôi
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToPlayer), Time.deltaTime * 10f);
            
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(1.0f); // Chờ animation đánh xong 1 chút

        // Gọi UI
        if (uiManager != null)
        {
            uiManager.ShowGameOver();
        }
        else
        {
            Debug.LogError("Chưa gán UIManager vào MonsterAI!");
        }
    }

    void StalkUpdate()
    {
        agent.speed = Mathf.Lerp(agent.speed, stalkSpeed, Time.deltaTime * 2f);
        Vector3 dir = (player.position - transform.position).normalized;
        Vector3 target = player.position - dir * ((stalkMinDist + stalkMaxDist) * 0.5f);
        agent.SetDestination(target);

        if (audioSource != null && !audioSource.isPlaying && UnityEngine.Random.value < 0.002f * (1f + aggression * 5f))
        {
            PlayWhisperOrMimic();
        }
    }

    void EnterState(State next)
    {
        if (state == next) return;
        state = next;

        // Reset timer khi bắt đầu Chase
        if (state == State.Chase)
        {
            chaseTimer = 0f;
        }

        if (searchCoroutine != null)
        {
            StopCoroutine(searchCoroutine);
            searchCoroutine = null;
        }
        if (state == State.Search)
        {
            searchCoroutine = StartCoroutine(DoSearch());
        }
    }

    IEnumerator DoSearch()
    {
        agent.SetDestination(lastSighting);
        float t = 0f;
        while (t < searchDuration)
        {
            Vector3 offset = new Vector3(Mathf.Sin(t * 1.2f), 0, Mathf.Cos(t * 1.2f)) * 3f;
            agent.SetDestination(lastSighting + offset);
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
        // KHÔNG chuyển sang Stalk nếu đang Chase (để tránh ngắt quãng việc đuổi)
        if (state == State.Chase || state == State.Jumpscare) return;

        float dist = Vector3.Distance(transform.position, pos);
        float range = hearingBase * hearingMultiplier; 
        if (dist <= range)
        {
            lastSighting = pos;
            EnterState(State.Stalk);
             if (Time.time - lastHeardLogTime > 2f) { Debug.Log($"Heard noise at {pos}"); lastHeardLogTime = Time.time; }
        }
    }

    void PlayWhisperOrMimic()
    {
        if (mimicVoiceClip != null && aggression > 0.6f && UnityEngine.Random.value < 0.6f)
            audioSource.PlayOneShot(mimicVoiceClip);
        else if (whisperClips != null && whisperClips.Length > 0)
            audioSource.PlayOneShot(whisperClips[UnityEngine.Random.Range(0, whisperClips.Length)]);
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
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, catchDistance); // Vẽ vòng tròn bắt
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
}
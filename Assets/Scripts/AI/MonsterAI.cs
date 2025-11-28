using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(UnityEngine.AudioSource))]
public class MonsterAI : MonoBehaviour
{
    public enum State { Patrol, Stalk, Chase, Search, Jumpscare } // Thêm state Jumpscare
    public State state = State.Patrol;

    [Header("Core")]
    public NavMeshAgent agent;
    public Transform player;
    // ❌ ĐÃ XÓA: public Transform[] waypoints; 

    [Header("Animation")]
    public Animator animator;

    [Header("Random Patrol")]
    public float patrolRadius = 30f; // Bán kính đi tuần ngẫu nhiên
    public float waitTimeMin = 2f;   // Đứng chơi xíu rồi đi tiếp
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
    public float chaseSpeed = 6.0f; // Tăng tốc độ đuổi lên chút cho ghê

    [Header("Jumpscare / Attack")]
    public float catchDistance = 1.5f; // Khoảng cách bị bắt (gần sát mặt)
    public float jumpscareFaceSpeed = 5f; // Tốc độ quái xoay mặt vào player
    private bool isJumpscaring = false;

    [Header("Stalk settings")]
    public float stalkMinDist = 12f;
    public float stalkMaxDist = 18f;
    public AudioClip[] whisperClips;
    public AudioClip mimicVoiceClip;
    public AudioClip jumpscareScreamClip; // Âm thanh khi bắt được
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
    float patrolWaitTimer = 0f; // Biến đếm thời gian đứng nghỉ

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
        // Bắt đầu bằng việc tìm điểm ngẫu nhiên
        SetRandomDestination();
    }

    void Update()
    {
        if (isJumpscaring) return; // Nếu đang hù thì không làm gì cả (để animation chạy)

        UpdateEmissionBob();

        // Cập nhật Animator Speed
        if (animator != null) animator.SetFloat("Speed", agent.velocity.magnitude);

        switch (state)
        {
            case State.Patrol: PatrolUpdate(); break;
            case State.Stalk: StalkUpdate(); break;
            case State.Chase: ChaseUpdate(); break;
            case State.Search: break;
        }

        // Chỉ check nhìn thấy khi KHÔNG phải đang đuổi (để tối ưu) hoặc muốn update vị trí liên tục
        if (state != State.Jumpscare && CanSeePlayer())
        {
            lastSighting = player.position;
            EnterState(State.Chase);
        }
    }

    // =====================================================
    // LOGIC ĐI TUẦN NGẪU NHIÊN (MỚI)
    // =====================================================
    void PatrolUpdate()
    {
        agent.speed = Mathf.Lerp(agent.speed, patrolSpeed, Time.deltaTime * 2f);

        // Nếu đã đến đích (hoặc gần đến)
        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            // Đứng nghỉ một chút cho tự nhiên
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

    // Hàm tìm điểm ngẫu nhiên trên NavMesh
    Vector3 GetRandomNavMeshPoint(Vector3 center, float radius)
    {
        for (int i = 0; i < 10; i++) // Thử 10 lần
        {
            Vector3 randomPos = center + Random.insideUnitSphere * radius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPos, out hit, 5.0f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }
        return center; // Nếu không tìm được thì đứng yên
    }

    // =====================================================
    // LOGIC ĐUỔI BẮT & JUMPSCARE (MỚI)
    // =====================================================
    void ChaseUpdate()
    {
        agent.speed = Mathf.Lerp(agent.speed, Mathf.Lerp(chaseSpeed * 0.9f, chaseSpeed, aggression), Time.deltaTime * 2f);
        agent.SetDestination(player.position);

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Nếu quái đến quá gần -> BẮT LUÔN!
        if (distToPlayer <= catchDistance)
        {
            StartCoroutine(TriggerJumpscare());
        }
    }

    IEnumerator TriggerJumpscare()
    {
        isJumpscaring = true;
        state = State.Jumpscare;
        
        // 1. Dừng quái lại ngay lập tức
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 2. Khóa Player (Gọi hàm Lock từ script PlayerController của bạn)
        // Giả sử script PlayerController nằm trên object Player
        var playerCtrl = player.GetComponent<PlayerController>();
        if (playerCtrl != null) playerCtrl.LockPlayerInput();

        // 3. Animation Tấn công
        if (animator != null)
        {
            animator.SetFloat("Speed", 0);
            animator.SetTrigger("Attack");
        }

        // 4. Âm thanh Jumpscare
        if (jumpscareScreamClip != null) audioSource.PlayOneShot(jumpscareScreamClip);

        // 5. Xoay Player nhìn vào mặt quái & Quái nhìn vào Player
        float timer = 0f;
        Quaternion startRot = player.rotation;
        // Tính hướng nhìn vào quái (chỉ xoay trục Y)
        Vector3 dirToMonster = (transform.position - player.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(new Vector3(dirToMonster.x, 0, dirToMonster.z));

        // Hiệu ứng xoay camera (hoặc xoay player) trong 0.5 giây
        while (timer < 0.5f)
        {
            player.rotation = Quaternion.Slerp(startRot, lookRot, timer / 0.5f);
            
            // Quái cũng xoay mặt về phía player cho chuẩn
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(dirToPlayer.x, 0, dirToPlayer.z)), Time.deltaTime * 10f);
            
            timer += Time.deltaTime;
            yield return null;
        }

        // 6. Chờ animation đánh xong (ví dụ 1.5 giây)
        yield return new WaitForSeconds(1.5f);

        // 7. GAME OVER
        Debug.Log("<color=red>YOU DIED! - RELOAD SCENE HERE</color>");
        // Ở đây bạn có thể gọi SceneManager.LoadScene() hoặc hiện UI
        // Time.timeScale = 0; // Tạm dừng game
    }

    // ... (Các hàm Stalk, Search, Vision giữ nguyên như cũ) ...
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
        // Tìm không thấy thì đi tuần tiếp
        SetRandomDestination(); 
        EnterState(State.Patrol);
    }

    // ... (Phần còn lại giữ nguyên: CanSeePlayer, OnHearNoise...)
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
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, patrolRadius); // Vẽ bán kính đi tuần
    }
    // =====================================================
    // CÁC HÀM HỖ TRỢ GAME DIRECTOR (BỊ THIẾU)
    // =====================================================

    // Hàm này để GameDirector gọi khi Player nhặt đồ hoặc nhìn vào quái
    public void AdjustAggression(float delta)
    {
        aggression = Mathf.Clamp01(aggression + delta);
    }

    // Hàm này để SafeZone gọi khi Player chạy vào vùng an toàn
    public void OnEnterSafeZone()
    {
        aggression = Mathf.Max(0f, aggression - 0.4f);
        EnterState(State.Patrol);
    }
}
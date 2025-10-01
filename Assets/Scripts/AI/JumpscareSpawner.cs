using UnityEngine;
using System.Collections;

/// <summary>
/// JumpscareSpawner: Kết hợp cả timer và spawner
/// - Jumpscare ngẫu nhiên: xuất hiện xung quanh player
/// - Jumpscare trước mặt: xuất hiện trước mặt khi gần quái/chạy nhiều
/// - Hỗ trợ cả 2D (Silhouette) và 3D (Model với Animator)
/// </summary>
public class JumpscareSpawner : MonoBehaviour {
    
    [Header("Prefabs")]
    public GameObject silhouettePrefab;     // 2D fallback (Quad với texture)
    public GameObject jumpscare3DPrefab;    // 3D model với Animator (khuyến nghị)
    
    [Header("Audio")]
    public AudioClip[] scareSounds;
    
    [Header("References")]
    public Transform player;
    public MonsterAI monster;
    
    [Header("Spawn Settings")]
    public bool use3D = true;                    // Ưu tiên 3D jumpscare
    public float minDist = 2f, maxDist = 4f;     // Khoảng cách trước mặt
    public float eyeHeightOffset = 1.5f;         // Độ cao ngang tầm mắt
    public LayerMask groundMask = ~0;            // Layer mặt đất
    public LayerMask obstacleMask = ~0;           // Layer vật cản
    public float clearRadius = 0.4f;              // Bán kính tránh vật cản
    
    [Header("Jumpscare Types")]
    [Tooltip("Random jumpscare: 3D model xuất hiện gần player (2-4m)")]
    public bool enableRandomJumpscare = true;
    public float randomMinDist = 2f, randomMaxDist = 4f;  // Gần hơn nhiều
    public float randomChance = 0.35f;           // Xác suất base cho random
    
    [Tooltip("360 jumpscare: 2D Silhouette xuất hiện 360 độ xung quanh player")]
    public bool enable360Jumpscare = true;
    public float frontChance = 0.6f;             // Xác suất cao hơn cho 360
    public float frontTriggerDistance = 8f;      // Khoảng cách trigger 360 jumpscare
    public float frontMinDist = 1.5f, frontMaxDist = 3f;  // Khoảng cách gần hơn cho 360
    
    [Header("Timing")]
    public float randomInterval = 4f;             // Thời gian giữa các random jumpscare
    public float frontInterval = 2f;             // Thời gian giữa các front jumpscare
    public Vector2 visibleDurationRange = new Vector2(3f, 5f);  // Thời gian hiển thị lâu hơn
    
    [Header("Animation")]
    public string animatorTrigger = "Scare";     // Trigger cho Animator
    
    // Private variables
    private float randomTimer;
    private float frontTimer;
    
    void Start()
    {
        Debug.Log("JumpscareSpawner started with 2 types: Random + Front");
        randomTimer = randomInterval;
        frontTimer = frontInterval;
    }
    
    void Update()
    {
        if (monster == null || player == null) return;
        
        // Random jumpscare timer
        if (enableRandomJumpscare)
        {
            randomTimer -= Time.deltaTime;
            if (randomTimer <= 0f)
            {
                TryRandomJumpscare();
                randomTimer = randomInterval;
            }
        }
        
        // 360 jumpscare timer (chỉ khi gần quái hoặc aggression cao)
        if (enable360Jumpscare)
        {
            float distanceToMonster = Vector3.Distance(player.position, monster.transform.position);
            bool isNearMonster = distanceToMonster < frontTriggerDistance;
            bool hasHighAggression = monster.aggression > 0.1f;
            
            if (isNearMonster || hasHighAggression)
            {
                frontTimer -= Time.deltaTime;
                if (frontTimer <= 0f)
                {
                    Try360Jumpscare();
                    frontTimer = frontInterval;
                }
            }
        }
    }
    
    /// <summary>
    /// Random jumpscare: xuất hiện xung quanh player
    /// </summary>
    void TryRandomJumpscare()
    {
        float chance = monster.aggression * randomChance;
        if (Random.value < chance)
        {
            Debug.Log("Random jumpscare triggered!");
            StartCoroutine(SpawnRandomJumpscare());
        }
    }
    
    /// <summary>
    /// 360 jumpscare: xuất hiện xung quanh player 360 độ
    /// </summary>
    void Try360Jumpscare()
    {
        float chance = monster.aggression * frontChance;
        if (Random.value < chance)
        {
            Debug.Log("360 jumpscare triggered!");
            StartCoroutine(Spawn360Jumpscare());
        }
    }
    
    /// <summary>
    /// Spawn random jumpscare xung quanh player
    /// </summary>
    IEnumerator SpawnRandomJumpscare()
    {
        // Random vị trí xung quanh player
        Vector3 dir = Random.onUnitSphere;
        dir.y = 0;
        dir.Normalize();
        Vector3 pos = player.position + dir * Random.Range(randomMinDist, randomMaxDist);
        
        // Raycast xuống đất
        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundMask))
        {
            pos = hit.point + Vector3.up * 0.5f;
        }
        
        // Tạo jumpscare (Random: dùng 3D model)
        GameObject go = CreateJumpscare(pos, Quaternion.LookRotation(player.position - pos), false);
        if (go != null)
        {
            yield return new WaitForSeconds(Random.Range(visibleDurationRange.x, visibleDurationRange.y));
            Destroy(go);
        }
    }
    
    /// <summary>
    /// Spawn 360 jumpscare xung quanh player
    /// </summary>
    IEnumerator Spawn360Jumpscare()
    {
        // Random góc 360 độ xung quanh player
        float randomAngle = Random.Range(0f, 360f);
        Vector3 direction = new Vector3(Mathf.Cos(randomAngle * Mathf.Deg2Rad), 0, Mathf.Sin(randomAngle * Mathf.Deg2Rad));
        Vector3 basePos = player.position + direction * Random.Range(frontMinDist, frontMaxDist);
        
        // Snap xuống đất
        Vector3 pos = basePos + Vector3.up * 3f;
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit groundHit, 6f, groundMask))
        {
            pos = groundHit.point;
        }
        
        // Độ cao ngang tầm mắt
        pos.y = player.position.y + eyeHeightOffset;
        
        // Tránh vật cản
        if (Physics.CheckSphere(pos, clearRadius, obstacleMask))
        {
            // Thử vị trí khác nếu bị cản
            pos += direction * clearRadius * 2f;
        }
        
        // Quay mặt về player
        Quaternion rot = Quaternion.LookRotation((player.position + Vector3.up * eyeHeightOffset) - pos);
        
        // Tạo jumpscare (360: dùng Silhouette 2D)
        GameObject go = CreateJumpscare(pos, rot, true);
        if (go != null)
        {
            yield return new WaitForSeconds(Random.Range(visibleDurationRange.x, visibleDurationRange.y));
            Destroy(go);
        }
    }
    
    /// <summary>
    /// Tạo jumpscare GameObject
    /// </summary>
    GameObject CreateJumpscare(Vector3 position, Quaternion rotation, bool useSilhouette = false)
    {
        // Chọn prefab dựa trên loại jumpscare
        GameObject prefab;
        if (useSilhouette)
        {
            // 360 jumpscare: dùng Silhouette (2D)
            prefab = silhouettePrefab;
        }
        else
        {
            // Random jumpscare: dùng 3D model
            prefab = use3D && jumpscare3DPrefab != null ? jumpscare3DPrefab : silhouettePrefab;
        }
        
        if (prefab == null)
        {
            Debug.LogWarning("JumpscareSpawner: No prefab assigned!");
            return null;
        }
        
        // Instantiate
        GameObject go = Instantiate(prefab, position, rotation);
        
        // Trigger animation nếu có
        Animator animator = go.GetComponentInChildren<Animator>();
        if (animator != null && !string.IsNullOrEmpty(animatorTrigger))
        {
            animator.SetTrigger(animatorTrigger);
        }
        
        // Phát âm thanh
        if (scareSounds != null && scareSounds.Length > 0)
        {
            AudioSource.PlayClipAtPoint(scareSounds[Random.Range(0, scareSounds.Length)], position, 0.9f);
        }
        
        Debug.Log($"Jumpscare spawned: {go.name} at {position} (useSilhouette: {useSilhouette})");
        return go;
    }
    
    /// <summary>
    /// Manual test jumpscare (cho testing)
    /// </summary>
    public void TestRandomJumpscare()
    {
        StartCoroutine(SpawnRandomJumpscare());
    }
    
    /// <summary>
    /// Manual test 360 jumpscare (cho testing)
    /// </summary>
    public void Test360Jumpscare()
    {
        StartCoroutine(Spawn360Jumpscare());
    }
}
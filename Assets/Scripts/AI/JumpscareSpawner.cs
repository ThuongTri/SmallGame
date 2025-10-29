using UnityEngine;
using System.Collections;

/// <summary>
/// JumpscareSpawner: Sinh hiệu ứng jumpscare (ngẫu nhiên hoặc trước mặt)
/// - Giữ nguyên gameplay, chỉ tối ưu log.
/// </summary>
public class JumpscareSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject silhouettePrefab;
    public GameObject jumpscare3DPrefab;

    [Header("Audio")]
    public AudioClip[] scareSounds;

    [Header("References")]
    public Transform player;
    public MonsterAI monster;

    [Header("Spawn Settings")]
    public bool use3D = true;
    public float minDist = 2f, maxDist = 4f;
    public float eyeHeightOffset = 1.5f;
    public LayerMask groundMask = ~0;
    public LayerMask obstacleMask = ~0;
    public float clearRadius = 0.4f;

    [Header("Jumpscare Types")]
    public bool enableRandomJumpscare = true;
    public float randomMinDist = 2f, randomMaxDist = 4f;
    public float randomChance = 0.35f;

    public bool enable360Jumpscare = true;
    public float frontChance = 0.6f;
    public float frontTriggerDistance = 8f;
    public float frontMinDist = 1.5f, frontMaxDist = 3f;

    [Header("Timing")]
    public float randomInterval = 4f;
    public float frontInterval = 2f;
    public Vector2 visibleDurationRange = new Vector2(3f, 5f);

    [Header("Animation")]
    public string animatorTrigger = "Scare";

    [Header("Debug")]
    public bool debugMode = false; // <--- Thêm cái này

    // Private
    private float randomTimer;
    private float frontTimer;

    void Start()
    {
        if (debugMode)
            Debug.Log("[JumpscareSpawner] Initialized (Random + Front)");
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

        // 360 jumpscare timer
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

    void TryRandomJumpscare()
    {
        float chance = monster.aggression * randomChance;
        if (Random.value < chance)
        {
            if (debugMode)
                Debug.Log("[JumpscareSpawner] Random jumpscare triggered.");
            StartCoroutine(SpawnRandomJumpscare());
        }
    }

    void Try360Jumpscare()
    {
        float chance = monster.aggression * frontChance;
        if (Random.value < chance)
        {
            if (debugMode)
                Debug.Log("[JumpscareSpawner] 360 jumpscare triggered.");
            StartCoroutine(Spawn360Jumpscare());
        }
    }

    IEnumerator SpawnRandomJumpscare()
    {
        Vector3 dir = Random.onUnitSphere;
        dir.y = 0;
        dir.Normalize();
        Vector3 pos = player.position + dir * Random.Range(randomMinDist, randomMaxDist);

        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundMask))
            pos = hit.point + Vector3.up * 0.5f;

        GameObject go = CreateJumpscare(pos, Quaternion.LookRotation(player.position - pos), false);
        if (go != null)
        {
            yield return new WaitForSeconds(Random.Range(visibleDurationRange.x, visibleDurationRange.y));
            Destroy(go);
        }
    }

    IEnumerator Spawn360Jumpscare()
    {
        float randomAngle = Random.Range(0f, 360f);
        Vector3 direction = new Vector3(Mathf.Cos(randomAngle * Mathf.Deg2Rad), 0, Mathf.Sin(randomAngle * Mathf.Deg2Rad));
        Vector3 basePos = player.position + direction * Random.Range(frontMinDist, frontMaxDist);

        Vector3 pos = basePos + Vector3.up * 3f;
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit groundHit, 6f, groundMask))
            pos = groundHit.point;

        pos.y = player.position.y + eyeHeightOffset;

        if (Physics.CheckSphere(pos, clearRadius, obstacleMask))
            pos += direction * clearRadius * 2f;

        Quaternion rot = Quaternion.LookRotation((player.position + Vector3.up * eyeHeightOffset) - pos);

        GameObject go = CreateJumpscare(pos, rot, true);
        if (go != null)
        {
            yield return new WaitForSeconds(Random.Range(visibleDurationRange.x, visibleDurationRange.y));
            Destroy(go);
        }
    }

    GameObject CreateJumpscare(Vector3 position, Quaternion rotation, bool useSilhouette = false)
    {
        GameObject prefab = useSilhouette
            ? silhouettePrefab
            : (use3D && jumpscare3DPrefab != null ? jumpscare3DPrefab : silhouettePrefab);

        if (prefab == null)
        {
            if (debugMode)
                Debug.LogWarning("[JumpscareSpawner] No prefab assigned!");
            return null;
        }

        GameObject go = Instantiate(prefab, position, rotation);

        Animator animator = go.GetComponentInChildren<Animator>();
        if (animator != null && !string.IsNullOrEmpty(animatorTrigger))
            animator.SetTrigger(animatorTrigger);

        if (scareSounds != null && scareSounds.Length > 0)
            AudioSource.PlayClipAtPoint(scareSounds[Random.Range(0, scareSounds.Length)], position, 0.9f);

        if (debugMode)
            Debug.Log($"[JumpscareSpawner] Spawned: {go.name} at {position}");
        return go;
    }

    public void TestRandomJumpscare() => StartCoroutine(SpawnRandomJumpscare());
    public void Test360Jumpscare() => StartCoroutine(Spawn360Jumpscare());
}

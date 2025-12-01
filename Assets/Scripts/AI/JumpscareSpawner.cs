using UnityEngine;
using System.Collections;

public class JumpscareSpawner : MonoBehaviour
{
    [Header("Prefab Quái")]
    public GameObject monsterPrefab; // Kéo con jumscare ramdom vào đây

    [Header("Cài đặt Spawn")]
    public float minDist = 5f;
    public float maxDist = 12f;
    public float spawnHeightOffset = 0f; // ✅ CHỈNH CÁI NÀY ĐỂ KÉO QUÁI LÊN KHỎI ĐẤT
    public LayerMask groundMask = ~0;

    [Header("Thời gian")]
    public float randomInterval = 10f; // Bao lâu hù 1 lần
    public float randomChance = 0.5f; // Tỉ lệ xuất hiện

    [Header("Audio")]
    public AudioClip[] scareSounds;

    [Header("References")]
    public Transform player;
    public MonsterAI monster;

    private float randomTimer;

    void Start()
    {
        randomTimer = randomInterval;
        if (player == null && GameObject.FindGameObjectWithTag("Player") != null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (monster == null || player == null) return;

        randomTimer -= Time.deltaTime;
        if (randomTimer <= 0f)
        {
            TryRandomJumpscare();
            randomTimer = randomInterval;
        }
    }

    void TryRandomJumpscare()
    {
        float chance = (monster != null) ? monster.aggression * randomChance : 0.3f;
        
        if (Random.value < chance)
        {
            StartCoroutine(SpawnRandomJumpscare());
        }
    }

    IEnumerator SpawnRandomJumpscare()
    {
        // 1. Tìm vị trí ngẫu nhiên
        Vector3 dir = Random.onUnitSphere;
        dir.y = 0;
        dir.Normalize();
        Vector3 pos = player.position + dir * Random.Range(minDist, maxDist);

        // 2. Chiếu tia xuống đất để tìm mặt sàn
        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundMask))
        {
            // ✅ CỘNG THÊM OFFSET ĐỂ KHÔNG BỊ CHÌM
            pos = hit.point + Vector3.up * spawnHeightOffset;
        }

        // 3. Xoay mặt về phía Player
        Quaternion rot = Quaternion.LookRotation(player.position - pos);
        // Giữ thẳng đứng (không nghiêng theo dốc)
        rot = Quaternion.Euler(0, rot.eulerAngles.y, 0); 

        // 4. Sinh ra quái
        if (monsterPrefab != null)
        {
            GameObject go = Instantiate(monsterPrefab, pos, rot);

            // Phát âm thanh
            if (scareSounds != null && scareSounds.Length > 0)
                AudioSource.PlayClipAtPoint(scareSounds[Random.Range(0, scareSounds.Length)], pos, 1f);

            // Tồn tại 4 giây rồi biến mất
            yield return new WaitForSeconds(4f);
            if (go != null) Destroy(go);
        }
    }
}
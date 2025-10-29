using System.Collections.Generic;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [Header("Mục tiêu & Vật phẩm đã nhặt")]
    private HashSet<string> collectedItems = new HashSet<string>();

    [Header("Tham chiếu trong game")]
    public GameObject bunkerWaypoint;   // vật chỉ dẫn/ánh sáng tới bunker
    public GameObject bunkerDoor;       // cửa hầm có animator
    public AudioSource hintAudio;       // tiếng gợi ý khi mở đường

    private bool bunkerRevealed = false;
    private bool bunkerUnlocked = false;

    void Awake()
    {
        Instance = this;
        if (bunkerWaypoint) bunkerWaypoint.SetActive(false); // ẩn ban đầu
    }

    public void OnItemCollected(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return;
        collectedItems.Add(itemID);

        Debug.Log($"[Objective] Collected item: {itemID}");
        CheckProgress();
    }

    private void CheckProgress()
    {
        // Kiểm tra 3 mảnh bản đồ
        int mapPieces = 0;
        foreach (var id in collectedItems)
            if (id.StartsWith("map_")) mapPieces++;

        if (mapPieces >= 3 && !bunkerRevealed)
        {
            bunkerRevealed = true;
            if (bunkerWaypoint) bunkerWaypoint.SetActive(true);
            if (hintAudio) hintAudio.Play();
            Debug.Log("✅ Đã tìm đủ bản đồ! Đường đến Bunker được hé lộ!");
        }

        // Khi có chìa rỉ, cho phép mở bunker
        if (collectedItems.Contains("rusty_key") && bunkerRevealed && !bunkerUnlocked)
        {
            bunkerUnlocked = true;
            if (bunkerDoor)
            {
                var anim = bunkerDoor.GetComponent<Animator>();
                if (anim) anim.SetTrigger("Unlock");
            }
            Debug.Log("🔓 Đã có chìa rỉ — có thể mở Bunker!");
        }
    }

    public bool HasItem(string id)
    {
        return collectedItems.Contains(id);
    }
}

using UnityEngine;
using UnityEngine.Playables;

public class Collectible : MonoBehaviour, IInteractable
{
    [Header("Thong tin vat pham")]
    public string itemID;                 // Vi du: wood_01, map_piece_1, rusty_key...
    public string itemTitle;              // Ten hien thi
    [TextArea(3, 6)]
    public string loreText;               // Mo ta codex/lore
    public Sprite itemIcon;
    public AudioClip pickupSound;

    [Header("Tuong tac")]
    public string interactionPrompt = "Nhan E de nhat";
    public bool requireItemID = false;    // Bat buoc itemID hay khong

    [Header("Tuy chon")]
    public bool oneTimeCollect = true;
    private bool collected = false;

    [Header("Cutscene (Tuy chon)")]
    public PlayableDirector cutsceneToPlay;
    public bool destroyAfterCollect = true;
    public float destroyDelay = 0f;       // Tang nhe (0.2~0.5) neu muon an toan khi play timeline

    public void OnInteract()
    {
        if (oneTimeCollect && collected) return;

        if (requireItemID && string.IsNullOrWhiteSpace(itemID))
        {
            Debug.LogWarning($"[Collectible] {name} chua co itemID, bo qua tuong tac.");
            return;
        }

        collected = true;

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        Debug.Log($"[Nhat do] {itemID}: {itemTitle}");

        // 1) Lore/Codex
        if (LoreManager.Instance != null)
            LoreManager.Instance.AddLore(itemID, itemTitle, loreText, itemIcon);

        // 2) Objective hien tai
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnItemCollected(itemID);

        // 3) Hook cho Prologue task (nhat cui)
        if (PrologueFlowManager.Instance != null &&
            !string.IsNullOrWhiteSpace(itemID) &&
            itemID.StartsWith("wood_"))
        {
            PrologueFlowManager.Instance.AddWood(1);

            int cur = PrologueFlowManager.Instance.woodCollected;
            int req = PrologueFlowManager.Instance.requiredWood;

            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage($"Da nhat cui: {cur}/{req}");
        }

        // 4) UI thong bao chung
        if (UIMessageManager.Instance != null)
        {
            string displayName = string.IsNullOrWhiteSpace(itemTitle) ? "vat pham" : itemTitle;
            UIMessageManager.Instance.ShowMessage("Ban nhat duoc " + displayName);
        }

        // 5) Cutscene (neu co)
        if (cutsceneToPlay != null && cutsceneToPlay.state != PlayState.Playing)
        {
            cutsceneToPlay.Play();
        }

        // 6) Xoa item
        if (destroyAfterCollect)
        {
            if (destroyDelay > 0f) Destroy(gameObject, destroyDelay);
            else Destroy(gameObject);
        }
    }

    public string GetInteractionPrompt()
    {
        return string.IsNullOrWhiteSpace(interactionPrompt) ? "Nhan E de nhat" : interactionPrompt;
    }
}
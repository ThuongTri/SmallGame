using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

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
    bool interactionConsumed = false;

    [Header("Cutscene (Tuy chon)")]
    public PlayableDirector cutsceneToPlay;
    [Tooltip("Cho item này chạy cutscene kiểu khóa di chuyển, vẫn cho rê camera nhẹ.")]
    public bool useSoftLockDuringCutscene = false;
    [Tooltip("Với doll_sence: mặc định khóa full input để Timeline điều khiển camera ổn định.")]
    public bool forceFullLockForPastDoll = true;
    [Tooltip("Để OFF mặc định cho ổn định. Bật nếu muốn pause gameplay khi xem cutscene quá khứ.")]
    public bool pauseWorldDuringPastCutscene = false;
    [Tooltip("Fail-safe: quá thời gian này thì tự thoát lock để tránh kẹt cứng.")]
    public float cutsceneTimeoutSeconds = 20f;
    [Range(0f, 1f)] public float softLookScale = 0.25f;
    public bool destroyAfterCollect = true;
    public float destroyDelay = 0f;       // Tang nhe (0.2~0.5) neu muon an toan khi play timeline

    public void OnInteract()
    {
        if (interactionConsumed) return;
        if (oneTimeCollect && collected) return;

        if (requireItemID && string.IsNullOrWhiteSpace(itemID))
        {
            Debug.LogWarning($"[Collectible] {name} chua co itemID, bo qua tuong tac.");
            return;
        }

        collected = true;
        interactionConsumed = true;

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        string resolvedItemId = ResolveItemId();
        Debug.Log($"[Nhat do] {resolvedItemId}: {itemTitle}");

        // 1) Lore/Codex
        if (LoreManager.Instance != null)
            LoreManager.Instance.AddLore(resolvedItemId, itemTitle, loreText, itemIcon);

        // 2) Objective hien tai
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.OnItemCollected(resolvedItemId);

        // 2.5) Relic hooks (mirror / necklace / doll)
        if (PlayerRelicController.Instance != null)
            PlayerRelicController.Instance.OnItemCollected(resolvedItemId);

        // 3) Hook cho Prologue task (nhat cui)
        if (PrologueFlowManager.Instance != null &&
            !string.IsNullOrWhiteSpace(resolvedItemId) &&
            resolvedItemId.StartsWith("wood_"))
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

        // 5) Cutscene + cleanup phải đi cùng 1 coroutine, tránh destroy object quá sớm làm player bị kẹt lock.
        StartCoroutine(CollectFlowRoutine());
    }

    public string GetInteractionPrompt()
    {
        return string.IsNullOrWhiteSpace(interactionPrompt) ? "Nhan E de nhat" : interactionPrompt;
    }

    IEnumerator PlayDirectorWithPlayerLock()
    {
        if (cutsceneToPlay == null) yield break;

        PlayerController pc = null;
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) pc = p.GetComponent<PlayerController>();

        bool isPastDoll = IsPastDollCutsceneItem();
        bool soft = useSoftLockDuringCutscene || (isPastDoll && !forceFullLockForPastDoll);
        var prevMode = cutsceneToPlay.timeUpdateMode;
        float prevTimeScale = Time.timeScale;
        bool pauseWorld = isPastDoll && pauseWorldDuringPastCutscene;
        if (pc != null)
        {
            if (soft)
            {
                pc.LockMovementOnly();
                pc.SetCutsceneLookScale(softLookScale);
            }
            else
            {
                pc.LockPlayerInput();
            }
        }
        try
        {
            if (pauseWorld)
            {
                cutsceneToPlay.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                Time.timeScale = 0f;
            }

            cutsceneToPlay.Play();
            float timeout = Mathf.Max(3f, cutsceneTimeoutSeconds);
            float t = 0f;
            while (cutsceneToPlay != null && cutsceneToPlay.state == PlayState.Playing && t < timeout)
            {
                t += pauseWorld ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
            if (cutsceneToPlay != null && cutsceneToPlay.state == PlayState.Playing)
                cutsceneToPlay.Stop();
        }
        finally
        {
            if (pauseWorld)
            {
                Time.timeScale = prevTimeScale;
                if (cutsceneToPlay != null) cutsceneToPlay.timeUpdateMode = prevMode;
            }

            if (pc != null)
            {
                if (soft)
                {
                    pc.UnlockMovementOnly();
                    pc.SetCutsceneLookScale(1f);
                }
                else
                {
                    pc.UnlockPlayerInput();
                }
            }
        }
    }

    IEnumerator CollectFlowRoutine()
    {
        if (cutsceneToPlay != null && cutsceneToPlay.state != PlayState.Playing)
            yield return PlayDirectorWithPlayerLock();

        if (!destroyAfterCollect) yield break;
        if (destroyDelay > 0f) yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    bool IsPastDollCutsceneItem()
    {
        if (string.IsNullOrWhiteSpace(itemID)) return false;
        string id = itemID.Trim().ToLowerInvariant();
        return id == "doll_sence" || id.Contains("doll") || id.Contains("nom");
    }

    string ResolveItemId()
    {
        if (!string.IsNullOrWhiteSpace(itemID))
            return itemID.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(itemTitle))
        {
            string t = itemTitle.Trim().ToLowerInvariant();
            t = t.Replace(' ', '_');
            if (t.Contains("đ")) t = t.Replace("đ", "d");
            if (t.Contains("è")) t = t.Replace("è", "e");
            return t;
        }

        return name.Trim().ToLowerInvariant();
    }
}
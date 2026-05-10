using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles relic effects:
/// - Mirror: active repel when monster is close (press E).
/// - Necklace: passive anti-horror + slight speed buff.
/// - Doll: can be burned at campfire for a long monster suppression.
/// </summary>
public class PlayerRelicController : MonoBehaviour
{
    public static PlayerRelicController Instance { get; private set; }

    [Header("References")]
    public PlayerController playerController;
    public NightHorrorWave nightHorrorWave;
    public PrologueFlowManager flow;

    [Header("Mirror (active repel)")]
    public bool hasMirror;
    public float mirrorUseDistance = 14f;
    public float mirrorRepelDistance = 24f;
    [Tooltip("Gương làm quái 'disappear / ngắt hunt' trong bao lâu (tính theo seconds).")]
    public float mirrorSuppressSeconds = 20f;
    [Tooltip("Thời gian chờ để dùng gương lại.")]
    public float mirrorCooldown = 30f;
    public KeyCode mirrorKey = KeyCode.E;
    public string mirrorPrompt = "Nhấn E dùng gương đẩy lùi quái";
    public AudioClip mirrorUseClip;

    [Header("Necklace (passive)")]
    public bool hasNecklace;
    public float necklaceMoveSpeedMultiplier = 1.15f;
    [Range(0.05f, 2f)] public float necklaceWaveChanceMultiplier = 0.5f;
    [Range(0.2f, 4f)] public float necklaceWaveCooldownMultiplier = 1.6f;

    [Header("Doll (quest item)")]
    public bool hasDoll;
    public float dollPickupRageBoost = 0.5f;
    public string dollCollectedMessage = "Bạn nhặt được nộm búp bê. Hãy đem về lửa trại để thiêu.";
    public string mirrorCollectedMessage = "Bạn nhặt được gương. Khi quái áp sát, nhấn E để đẩy lùi.";
    public string necklaceCollectedMessage = "Bạn nhặt được dây chuyền. Bạn cảm thấy bình tĩnh hơn.";

    AudioSource oneShotAudio;
    float nextMirrorReadyTime;
    float nextMirrorPromptTime;
    MonsterAI cachedNearestMonster;
    float nextNearestScanTime;
    readonly HashSet<string> collectedItemIds = new HashSet<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(this); return; }

        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (flow == null) flow = PrologueFlowManager.Instance;
        if (nightHorrorWave == null) nightHorrorWave = FindObjectOfType<NightHorrorWave>(true);

        oneShotAudio = GetComponent<AudioSource>();
        if (oneShotAudio == null) oneShotAudio = gameObject.AddComponent<AudioSource>();
        oneShotAudio.playOnAwake = false;
        oneShotAudio.spatialBlend = 0f;

        ApplyPassiveRelics();
        SyncRelicFlagsFromInventory();
    }

    void Update()
    {
        if (!hasMirror) return;
        if (Time.time < nextMirrorReadyTime) return;

        MonsterAI monster = FindNearestMonsterCached();
        if (monster == null) return;
        if (!CanUseMirrorOn(monster)) return;

        if (UIMessageManager.Instance != null && Time.time >= nextMirrorPromptTime)
        {
            UIMessageManager.Instance.ShowMessage(mirrorPrompt);
            nextMirrorPromptTime = Time.time + 0.9f;
        }

        if (Input.GetKeyDown(mirrorKey))
        {
            monster.RepelFrom(transform.position, mirrorRepelDistance, mirrorSuppressSeconds);
            nextMirrorReadyTime = Time.time + Mathf.Max(0.2f, mirrorCooldown);
            if (mirrorUseClip != null && oneShotAudio != null)
                oneShotAudio.PlayOneShot(mirrorUseClip, 1f);
            // Mirror triggers an instant "disappear/cry" cue from the monster.
            if (monster.jumpscareScreamClip != null)
                AudioSource.PlayClipAtPoint(monster.jumpscareScreamClip, monster.transform.position);
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage("Quái bị đẩy lùi bởi ánh gương!");
        }
    }

    MonsterAI FindNearestMonsterCached()
    {
        if (cachedNearestMonster != null && Time.time < nextNearestScanTime)
            return cachedNearestMonster;

        nextNearestScanTime = Time.time + 0.3f;
        MonsterAI[] all = FindObjectsOfType<MonsterAI>(true);
        MonsterAI best = null;
        float bestSq = float.MaxValue;
        Vector3 p = transform.position;

        for (int i = 0; i < all.Length; i++)
        {
            MonsterAI m = all[i];
            if (m == null || !m.enabled || !m.gameObject.activeInHierarchy) continue;
            float sq = (m.transform.position - p).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                best = m;
            }
        }

        cachedNearestMonster = best;
        return cachedNearestMonster;
    }

    bool CanUseMirrorOn(MonsterAI monster)
    {
        if (monster == null) return false;
        float d = Vector3.Distance(transform.position, monster.transform.position);
        return d <= mirrorUseDistance;
    }

    void ApplyPassiveRelics()
    {
        if (playerController != null)
            playerController.SetExternalSpeedMultiplier(hasNecklace ? necklaceMoveSpeedMultiplier : 1f);

        if (nightHorrorWave != null)
        {
            if (hasNecklace) nightHorrorWave.SetRelicModifiers(necklaceWaveChanceMultiplier, necklaceWaveCooldownMultiplier);
            else nightHorrorWave.SetRelicModifiers(1f, 1f);
        }
    }

    public void OnItemCollected(string itemID)
    {
        if (string.IsNullOrWhiteSpace(itemID)) return;
        string id = itemID.Trim().ToLowerInvariant();
        collectedItemIds.Add(id);

        if (id == "mirror_relic" || id == "mirror" || id.Contains("mirror"))
        {
            hasMirror = true;
            if (UIMessageManager.Instance != null) UIMessageManager.Instance.ShowMessage(mirrorCollectedMessage);
            return;
        }

        if (id == "necklace_relic" || id == "necklace" || id.Contains("necklace"))
        {
            hasNecklace = true;
            ApplyPassiveRelics();
            if (UIMessageManager.Instance != null) UIMessageManager.Instance.ShowMessage(necklaceCollectedMessage);
            return;
        }

        if (id == "doll_cursed" || id == "doll" || id.Contains("doll"))
        {
            hasDoll = true;
            MonsterAI m = FindNearestMonsterCached();
            if (m != null)
            {
                m.AdjustAggression(dollPickupRageBoost);
                m.RepelFrom(transform.position, 4f, 0f);
                if (m.jumpscareScreamClip != null)
                    AudioSource.PlayClipAtPoint(m.jumpscareScreamClip, m.transform.position);
            }
            if (UIMessageManager.Instance != null) UIMessageManager.Instance.ShowMessage(dollCollectedMessage);
        }
    }

    public bool ConsumeDollForBurn()
    {
        if (!hasDoll) return false;
        hasDoll = false;
        return true;
    }

    public bool HasCollectedItem(string itemID)
    {
        if (string.IsNullOrWhiteSpace(itemID)) return false;
        return collectedItemIds.Contains(itemID.Trim().ToLowerInvariant());
    }

    void SyncRelicFlagsFromInventory()
    {
        bool foundMirror = HasAnyKnownItem("mirror", "mirror_relic");
        bool foundNecklace = HasAnyKnownItem("necklace", "necklace_relic");
        bool foundDoll = HasAnyKnownItem("doll", "doll_cursed");

        if (foundMirror) hasMirror = true;
        if (foundNecklace) hasNecklace = true;
        if (foundDoll) hasDoll = true;
        ApplyPassiveRelics();
    }

    bool HasAnyKnownItem(params string[] ids)
    {
        if (ids == null) return false;
        for (int i = 0; i < ids.Length; i++)
        {
            string id = ids[i];
            if (string.IsNullOrWhiteSpace(id)) continue;
            string low = id.Trim().ToLowerInvariant();
            if (collectedItemIds.Contains(low)) return true;
            if (ObjectiveManager.Instance != null && ObjectiveManager.Instance.HasItem(low)) return true;
        }
        return false;
    }
}


using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DayFogBoundaryBlocker : MonoBehaviour
{
    [Header("Phase rule")]
    [Tooltip("If true, block only in PrologueDay. If false, block in all phases except NightmareNight.")]
    public bool blockOnlyInPrologueDay = true;
    public PrologueFlowManager flow;

    [Header("Player handling")]
    [Tooltip("Optional fixed return point. If null, blocker uses trigger bounds.")]
    public Transform returnPoint;
    [Tooltip("If true, hard-block player at entry point (no sideways slide).")]
    public bool hardBlockAtEntryPoint = true;
    [Min(0f)] public float pushBackDistance = 0.2f;
    [Min(0f)] public float yLift = 0f;
    [Tooltip("Push out only to nearest trigger edge + this padding.")]
    public bool pushOutOfTriggerBounds = true;
    [Min(0.02f)] public float escapePadding = 0.6f;
    [Tooltip("Optional extra solid colliders that are enabled while blocked phase is active (2nd hard layer).")]
    public Collider[] hardBlockColliders;

    [Header("Dialogue")]
    public string blockedMessage = "Không cần thiết qua đó.";
    [Min(0f)] public float messageCooldown = 1.2f;

    float nextMessageTime;
    Vector3 lastSafePlayerPos;
    bool hasSafePos;
    Transform autoCampReturn;

    void Awake()
    {
        if (flow == null) flow = PrologueFlowManager.Instance;

        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;

        if (returnPoint == null)
        {
            CampFogSafeZone camp = FindObjectOfType<CampFogSafeZone>(true);
            if (camp != null) autoCampReturn = camp.transform;
        }
    }

    void Update()
    {
        bool on = ShouldBlockNow();
        if (hardBlockColliders != null)
        {
            for (int i = 0; i < hardBlockColliders.Length; i++)
            {
                Collider c = hardBlockColliders[i];
                if (c != null) c.enabled = on;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            lastSafePlayerPos = other.transform.position;
            hasSafePos = true;
        }
        TryBlock(other);
    }

    void OnTriggerStay(Collider other)
    {
        TryBlock(other);
    }

    bool ShouldBlockNow()
    {
        if (flow == null) return true;

        if (blockOnlyInPrologueDay)
            return flow.currentPhase == PrologueFlowManager.Phase.PrologueDay;

        return flow.currentPhase != PrologueFlowManager.Phase.NightmareNight;
    }

    void TryBlock(Collider other)
    {
        if (!ShouldBlockNow()) return;
        if (!other.CompareTag("Player")) return;

        Collider boundaryCol = GetComponent<Collider>();

        Transform t = other.transform;
        CharacterController cc = t.GetComponent<CharacterController>();
        Vector3 targetPos;

        Transform targetReturn = returnPoint != null ? returnPoint : autoCampReturn;
        if (targetReturn != null)
        {
            // Simpler and stronger: touching boundary returns player back to camp-side point.
            targetPos = targetReturn.position + Vector3.up * yLift;
        }
        else if (hardBlockAtEntryPoint && boundaryCol != null)
        {
            targetPos = ForceOutsideBoundary(boundaryCol, t.position, Mathf.Max(escapePadding, 0.25f));
            targetPos.y = t.position.y + yLift;
        }
        else if (pushOutOfTriggerBounds && boundaryCol != null)
        {
            Vector3 escaped = EscapeBoundsXZ(boundaryCol.bounds, t.position, escapePadding);
            Vector3 radial = escaped - t.position;
            radial.y = 0f;
            float radialMag = radial.magnitude;
            if (radialMag > 0.0001f && radialMag < Mathf.Max(0f, pushBackDistance))
                escaped += radial.normalized * (pushBackDistance - radialMag);
            else if (radialMag <= 0.0001f)
            {
                Vector3 away = t.position - transform.position;
                away.y = 0f;
                if (away.sqrMagnitude < 0.0001f)
                    away = -t.forward;
                escaped = t.position + away.normalized * Mathf.Max(pushBackDistance, escapePadding);
            }

            escaped.y = t.position.y + yLift;
            targetPos = escaped;
        }
        else
        {
            Vector3 away = t.position - transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = -t.forward;
            away.Normalize();
            targetPos = t.position + away * Mathf.Max(pushBackDistance, escapePadding) + Vector3.up * yLift;
        }

        if (cc != null)
        {
            bool wasEnabled = cc.enabled;
            cc.enabled = false;
            t.position = targetPos;
            cc.enabled = wasEnabled;
        }
        else
        {
            t.position = targetPos;
        }

        if (Time.time >= nextMessageTime && UIMessageManager.Instance != null)
        {
            UIMessageManager.Instance.ShowMessage(blockedMessage);
            nextMessageTime = Time.time + messageCooldown;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        hasSafePos = false;
    }

    /// <summary>Đưa điểm ra ngoài hình chiếu XZ của bounds (giữ Y).</summary>
    static Vector3 EscapeBoundsXZ(Bounds b, Vector3 p, float pad)
    {
        Vector3 min = b.min;
        Vector3 max = b.max;

        bool insideX = p.x >= min.x && p.x <= max.x;
        bool insideZ = p.z >= min.z && p.z <= max.z;
        if (!(insideX && insideZ))
            return p;

        float dxL = p.x - min.x;
        float dxR = max.x - p.x;
        float dzB = p.z - min.z;
        float dzF = max.z - p.z;

        float m = Mathf.Min(dxL, dxR, dzB, dzF);
        Vector3 q = p;
        if (m == dxL) q.x = min.x - pad;
        else if (m == dxR) q.x = max.x + pad;
        else if (m == dzB) q.z = min.z - pad;
        else q.z = max.z + pad;

        return q;
    }

    static Vector3 ForceOutsideBoundary(Collider c, Vector3 fromPos, float pad)
    {
        if (c == null) return fromPos;
        Vector3 cp = c.ClosestPoint(fromPos);
        Vector3 dir = fromPos - cp;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            Vector3 toCenter = fromPos - c.bounds.center;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude < 0.0001f) toCenter = Vector3.forward;
            dir = toCenter.normalized;
        }
        else
        {
            dir.Normalize();
        }

        Vector3 outPos = cp + dir * Mathf.Max(0.02f, pad);
        outPos.y = fromPos.y;
        return outPos;
    }
}

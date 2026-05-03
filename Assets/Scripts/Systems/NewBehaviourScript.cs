using UnityEngine;

public class CampReachTrigger : MonoBehaviour
{
    [Tooltip("Neu Player khong co tag Player thi bo tick va dung layer check")]
    public bool requirePlayerTag = true;

    void OnTriggerEnter(Collider other)
    {
        if (requirePlayerTag && !other.CompareTag("Player")) return;

        if (PrologueFlowManager.Instance != null)
            PrologueFlowManager.Instance.MarkReachedCamp();
    }
}
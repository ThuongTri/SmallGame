using UnityEngine;

public class TentSleepInteraction : MonoBehaviour, IInteractable
{
    public PrologueFlowManager flow;

    public string promptReady = "Nhan E de vao leu ngu";
    public string promptNotReady = "Ban chua san sang de ngu";

    void Awake()
    {
        if (flow == null) flow = PrologueFlowManager.Instance;
    }

    public void OnInteract()
    {
        if (flow == null) return;

        if (!flow.CanSleep())
        {
            if (UIMessageManager.Instance != null)
                UIMessageManager.Instance.ShowMessage("Ban chua hoan thanh trai.");
            return;
        }

        // TODO buoc tiep: fade + chuyen phase (minh se huong dan buoc 6)
        if (UIMessageManager.Instance != null)
            UIMessageManager.Instance.ShowMessage("Ban buon ngu... (TODO: chuyen canh)");

        flow.SetPhase(PrologueFlowManager.Phase.TransitionSleep);
    }

    public string GetInteractionPrompt()
    {
        if (flow != null && flow.CanSleep()) return promptReady;
        return promptNotReady;
    }
}
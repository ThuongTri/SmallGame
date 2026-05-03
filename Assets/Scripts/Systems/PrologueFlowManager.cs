using UnityEngine;

public class PrologueFlowManager : MonoBehaviour
{
    public static PrologueFlowManager Instance;

    public enum Phase
    {
        PrologueDay,
        TransitionSleep,
        NightmareNight
    }

    [Header("State")]
    public Phase currentPhase = Phase.PrologueDay;

    [Header("Day Tasks")]
    public bool reachedCamp = false;
    public int woodCollected = 0;
    public int requiredWood = 3;

    // Da nhom lua trai chua (bat buoc de duoc phep ngu neu ban muon)
    public bool campfireLit = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool CanSleep()
    {
        return reachedCamp && woodCollected >= requiredWood && campfireLit;
    }

    public void MarkReachedCamp()
    {
        reachedCamp = true;
    }

    public void AddWood(int amount = 1)
    {
        woodCollected += Mathf.Max(0, amount);
    }

    public void MarkCampfireLit()
    {
        campfireLit = true;
    }

    public void SetPhase(Phase phase)
    {
        currentPhase = phase;
    }
}
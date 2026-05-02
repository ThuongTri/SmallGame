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
    public bool gotWater = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool CanSleep()
    {
        return reachedCamp && woodCollected >= requiredWood && gotWater;
    }

    public void MarkReachedCamp()
    {
        reachedCamp = true;
    }

    public void AddWood(int amount = 1)
    {
        woodCollected += amount;
    }

    public void MarkGotWater()
    {
        gotWater = true;
    }

    public void SetPhase(Phase phase)
    {
        currentPhase = phase;
    }
}
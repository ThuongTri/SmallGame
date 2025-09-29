using UnityEngine;

public class JumpscareTimer : MonoBehaviour
{
    public JumpscareSpawner spawner;
    public float interval = 4f;
    
    void Start()
    {
        Debug.Log("JumpscareTimer started, will try jumpscare every " + interval + " seconds");
        InvokeRepeating("TryJumpscare", interval, interval);
    }
    
    void TryJumpscare()
    {
        Debug.Log("JumpscareTimer: Attempting jumpscare...");
        if (spawner != null)
        {
            spawner.TrySpawn();
        }
        else
        {
            Debug.LogError("JumpscareSpawner not assigned to JumpscareTimer!");
        }
    }
}
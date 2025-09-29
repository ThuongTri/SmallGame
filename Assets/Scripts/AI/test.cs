using UnityEngine;

public class TestJumpscare : MonoBehaviour
{
    public JumpscareSpawner spawner;
    
    void Update()
    {
        // Nhấn J để test manual jumpscare
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("Manual jumpscare test - J pressed");
            if (spawner != null)
            {
                spawner.TrySpawn();
            }
            else
            {
                Debug.LogError("JumpscareSpawner not assigned!");
            }
        }
        
        // Nhấn K để test với aggression cao
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("Setting high aggression for test");
            if (spawner.monster != null)
            {
                spawner.monster.AdjustAggression(1f);
                Debug.Log($"Monster aggression set to: {spawner.monster.aggression}");
            }
        }
    }
}
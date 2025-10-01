using UnityEngine;

/// <summary>
/// TestJumpscare: Script để test jumpscare manually
/// - Nhấn J: Test random jumpscare
/// - Nhấn K: Test front jumpscare  
/// - Nhấn L: Set aggression cao
/// </summary>
public class TestJumpscare : MonoBehaviour
{
    public JumpscareSpawner spawner;
    
    void Update()
    {
        // Test random jumpscare
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("Testing random jumpscare - J pressed");
            if (spawner != null)
            {
                spawner.TestRandomJumpscare();
            }
            else
            {
                Debug.LogError("JumpscareSpawner not assigned!");
            }
        }
        
        // Test 360 jumpscare
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("Testing 360 jumpscare - K pressed");
            if (spawner != null)
            {
                spawner.Test360Jumpscare();
            }
            else
            {
                Debug.LogError("JumpscareSpawner not assigned!");
            }
        }
        
        // Set high aggression
        if (Input.GetKeyDown(KeyCode.L))
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

using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
    public Light flashlight; // <- Cái này bị thiếu assign trong Inspector

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class DisableFog : MonoBehaviour
{
    void OnPreRender()
    {
        // Tắt sương mù trước khi camera này render
        RenderSettings.fog = false;
    }

    void OnPostRender()
    {
        // Bật lại sương mù sau khi render xong (để Main Camera dùng)
        RenderSettings.fog = true;
    }
}
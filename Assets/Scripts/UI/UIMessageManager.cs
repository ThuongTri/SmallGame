using UnityEngine;
using TMPro;

public class UIMessageManager : MonoBehaviour
{
    public static UIMessageManager Instance;
    public TextMeshProUGUI messageText;
    public float displayTime = 2f;
    private float timer;

    void Awake()
    {
        Instance = this;
        messageText.text = "";
    }

    void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
                messageText.text = "";
        }
    }

    public void ShowMessage(string msg)
    {
        if (messageText == null) return;
        messageText.text = msg;
        timer = displayTime;
    }
}

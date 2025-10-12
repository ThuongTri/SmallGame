using UnityEngine;

public class Collectible : MonoBehaviour
{
    public string itemID;
    public AudioClip pickupSound;
    private bool isCollected = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))   // ✅ nhớ đóng ngoặc
        {
            Collect();
        }
    }

    void Collect()
    {
        AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        isCollected = true;
        gameObject.SetActive(false);
    }
}

using UnityEngine;
using System.Collections;

public class JumpscareSpawner : MonoBehaviour {
    public GameObject silhouettePrefab; // cheap quad billboard with sprite
    public AudioClip[] scareSounds;
    public Transform player;
    public float minDist = 2f, maxDist = 8f; // Giảm khoảng cách để gần hơn
    public MonsterAI monster;

    public void TrySpawn(){
        float chance = monster != null ? monster.aggression : 0f; // 0..1
        Debug.Log($"JumpscareSpawner: aggression={monster?.aggression}, chance={chance * 0.35f}");
        
        if (Random.value < chance * 0.35f) {
            Debug.Log("JUMPSCARE SPAWNED!");
            StartCoroutine(SpawnRoutine());
        } else {
            Debug.Log("Jumpscare failed - not enough aggression");
        }
    }

    IEnumerator SpawnRoutine(){
        Vector3 dir = Random.onUnitSphere; dir.y = 0; dir.Normalize();
        Vector3 pos = player.position + dir * Random.Range(minDist, maxDist);
        
        // Debug vị trí spawn
        Debug.Log($"Spawning at position: {pos}, distance from player: {Vector3.Distance(player.position, pos)}");
        
        // place slightly above ground - perform a raycast down to ground
        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f)) {
            pos = hit.point + Vector3.up * 0.5f;
            Debug.Log($"Raycast hit ground at: {pos}");
        }
        
        GameObject go = Instantiate(silhouettePrefab, pos, Quaternion.LookRotation(player.position - pos));
        
        // Tăng scale để dễ thấy
        go.transform.localScale = Vector3.one * 3f; // Gấp 3 lần size
        
        // Debug GameObject
        Debug.Log($"Silhouette spawned: {go.name} at {go.transform.position}");
        Debug.Log($"Silhouette scale: {go.transform.localScale}");
        Debug.Log($"Silhouette active: {go.activeInHierarchy}");
        
        // random sound
        if (scareSounds != null && scareSounds.Length>0) {
            AudioSource.PlayClipAtPoint(scareSounds[Random.Range(0, scareSounds.Length)], pos, 0.8f);
            Debug.Log("Scare sound played");
        }
        
        // fade out
        yield return new WaitForSeconds(0.65f + Random.value*0.6f);
        Debug.Log("Destroying silhouette");
        Destroy(go);
    }
}
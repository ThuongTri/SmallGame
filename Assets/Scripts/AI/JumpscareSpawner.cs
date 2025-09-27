using UnityEngine;
using System.Collections;

public class JumpscareSpawner : MonoBehaviour {
    public GameObject silhouettePrefab; // cheap quad billboard with sprite
    public AudioClip[] scareSounds;
    public Transform player;
    public float minDist = 6f, maxDist = 14f;
    public MonsterAI monster;

    public void TrySpawn(){
        float chance = monster != null ? monster.aggression : 0f; // 0..1
        if (Random.value < chance * 0.35f) {
            StartCoroutine(SpawnRoutine());
        }
    }

    IEnumerator SpawnRoutine(){
        Vector3 dir = Random.onUnitSphere; dir.y = 0; dir.Normalize();
        Vector3 pos = player.position + dir * Random.Range(minDist, maxDist);
        // place slightly above ground - perform a raycast down to ground
        if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f)) pos = hit.point + Vector3.up * 0.5f;
        GameObject go = Instantiate(silhouettePrefab, pos, Quaternion.LookRotation(player.position - pos));
        // random sound
        if (scareSounds != null && scareSounds.Length>0) AudioSource.PlayClipAtPoint(scareSounds[Random.Range(0, scareSounds.Length)], pos, 0.8f);
        // fade out
        yield return new WaitForSeconds(0.65f + Random.value*0.6f);
        Destroy(go);
    }
}

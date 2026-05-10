using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    public float noiseRadius = 5f;
    public float noiseInterval = 2f;
    public LayerMask monsterLayer;
    public bool debugNoiseLogs = false;

    private float noiseTimer;

    void Update()
    {
        noiseTimer += Time.deltaTime;

        if (noiseTimer >= noiseInterval)
        {
            EmitNoise();
            noiseTimer = 0f;
        }
    }

    void EmitNoise()
    {
        // Random vị trí trong bán kính noiseRadius
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * noiseRadius;
        Vector3 noisePosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);

        // Debug vị trí phát tiếng ồn
        Debug.DrawLine(transform.position, noisePosition, Color.yellow, 1f);

        // Gửi tín hiệu đến quái vật trong vùng nghe thấy
        Collider[] monsters = Physics.OverlapSphere(noisePosition, noiseRadius, monsterLayer);
        foreach (Collider monster in monsters)
        {
            MonsterAI monsterAI = monster.GetComponent<MonsterAI>();
            if (monsterAI != null)
            {
                monsterAI.OnHearNoise(noisePosition);
            }
        }

        if (debugNoiseLogs)
            Debug.Log("Noise emitted at " + noisePosition);
    }
}

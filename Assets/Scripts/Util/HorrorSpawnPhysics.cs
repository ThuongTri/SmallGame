using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawn jumpscare figures không đẩy CharacterController của player (tránh cảm giác "bay về spawn").
/// </summary>
public static class HorrorSpawnPhysics
{
    public static void MakeSpawnNonSolid(GameObject root)
    {
        if (root == null) return;

        foreach (var c in root.GetComponentsInChildren<Collider>(true))
        {
            if (c != null)
                c.enabled = false;
        }

        foreach (var agent in root.GetComponentsInChildren<NavMeshAgent>(true))
        {
            if (agent != null)
                agent.enabled = false;
        }

        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
        {
            if (rb == null) continue;
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        // Hide accidental debug capsule meshes that sometimes pop before the real model.
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            string n = r.name.ToLowerInvariant();
            if (n.Contains("capsule") || r.GetComponent<CapsuleCollider>() != null)
                r.enabled = false;
        }
    }
}

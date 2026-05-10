using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMesh sample height is often slightly below the rendered terrain mesh.
/// Use NavMesh only to correct XZ when desired, then always derive Y from a downward raycast.
/// </summary>
public static class SpawnSurfaceAlign
{
    public static Vector3 Resolve(
        Vector3 approximateWorld,
        Transform fallbackForRayHeight,
        LayerMask groundMask,
        float rayUp,
        float rayDown,
        bool snapNavMeshXZ,
        float navSnapDistance,
        float surfaceClearance,
        QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore,
        float maxNavmeshXZPull = -1f)
    {
        Vector3 p = approximateWorld;

        if (snapNavMeshXZ && NavMesh.SamplePosition(p, out NavMeshHit nm, navSnapDistance, NavMesh.AllAreas))
        {
            float dx = nm.position.x - p.x;
            float dz = nm.position.z - p.z;
            bool accept = maxNavmeshXZPull <= 0f || dx * dx + dz * dz <= maxNavmeshXZPull * maxNavmeshXZPull;
            if (accept)
            {
                p.x = nm.position.x;
                p.z = nm.position.z;
            }
        }

        float probeY = (fallbackForRayHeight != null ? fallbackForRayHeight.position.y : p.y) + rayUp;
        Vector3 origin = new Vector3(p.x, probeY, p.z);
        float maxDist = rayUp + rayDown;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDist, groundMask, triggers))
        {
            p.y = hit.point.y + surfaceClearance;
            return p;
        }

        if (fallbackForRayHeight != null)
            p.y = fallbackForRayHeight.position.y + surfaceClearance;
        return p;
    }
}

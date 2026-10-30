using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Correct only two mesh-verified floating natural trees in Kriegspire North.
// No authored terrain, water, building, or imported mesh is modified.
public static class MMKriegspireFloatingPineGrounding
{
    public static int GroundedCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Apply()
    {
        if (SceneManager.GetActiveScene().name != "Enroth")
            return;

        Terrain terrain = Terrain.activeTerrains
            .FirstOrDefault(t => t && t.name == "Terrain_Kriegspire_North");
        if (!terrain) return;

        Transform[] all = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();

        GroundedCount = 0;
        Ground(all, terrain, "AuthoredPine_888", new Vector3(-322.46f, 36.77f, 1118.46f));
        Ground(all, terrain, "AuthoredPine_636", new Vector3(-359.64f, 23.52f, 931.66f));
    }

    static void Ground(Transform[] all, Terrain terrain, string name, Vector3 anchor)
    {
        Transform tree = all.FirstOrDefault(t =>
            t.name == name && Vector3.Distance(t.position, anchor) < 0.25f);
        if (!tree) return;

        MeshFilter mesh = tree.GetComponentsInChildren<MeshFilter>(true)
            .FirstOrDefault(m => m.name == "M_LOD0" && m.sharedMesh);
        if (!mesh) return;

        // Inspect actual component vertices near the lower trunk instead of
        // aggregate parent bounds, which can produce false floating reports.
        Vector3[] vertices = mesh.sharedMesh.vertices;
        float lowest = float.PositiveInfinity;
        foreach (Vector3 local in vertices)
            lowest = Mathf.Min(lowest, mesh.transform.TransformPoint(local).y);

        float minimumGap = float.PositiveInfinity;
        foreach (Vector3 local in vertices)
        {
            Vector3 world = mesh.transform.TransformPoint(local);
            if (world.y > lowest + 1.5f) continue;
            float terrainY = terrain.SampleHeight(world) + terrain.transform.position.y;
            minimumGap = Mathf.Min(minimumGap, world.y - terrainY);
        }

        // If later manual terrain work already grounded these pines or moved
        // them substantially, preserve the new authoring rather than repeat.
        if (minimumGap < 0.70f || minimumGap > 3.0f)
            return;

        Vector3 corrected = tree.position;
        corrected.y -= minimumGap + 0.15f;
        tree.position = corrected;
        GroundedCount++;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// A sparse, explicitly composed treeline on the far side of the accepted
// Frozen Highlands forest-to-clearing route. Runtime visuals only: no source
// tree, terrain, water, building, scene or collider changes.
public static class MMFrozenRidgeTreeLine
{
    const string RootName = "MM_FrozenRidgeTreeLine";
    const string TerrainName = "Terrain_FrozenHighlands_North";
    public static GameObject RuntimeRoot { get; private set; }
    public static int AddedPines { get; private set; }

    // Fixed art-reviewed sites. Forest remains clustered left of the protected
    // corridor, with only two sparse sentinels on the far right shoulder.
    static readonly Vector2[] Sites =
    {
        new Vector2(-136f, 1149f), new Vector2(-127f, 1162f),
        new Vector2(-118f, 1148f), new Vector2(-109f, 1167f),
        new Vector2(-103f, 1147f), new Vector2(-96f, 1182f),
        new Vector2(-93f, 1159f), new Vector2(-86f, 1178f),
        new Vector2(-79f, 1164f), new Vector2(-143f, 1175f),
        new Vector2(-90f, 1138f), new Vector2(-110f, 1183f),
        new Vector2(2f, 1148f), new Vector2(7f, 1166f)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        RuntimeRoot = null;
        AddedPines = 0;
        if (SceneManager.GetActiveScene().name != "Enroth" || GameObject.Find(RootName))
            return;

        Terrain t = Terrain.activeTerrains.FirstOrDefault(x =>
            x && x.name == TerrainName && x.terrainData);
        if (!t) return;

        Transform[] all = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Exclude);
        Transform small = all.FirstOrDefault(x => x && x.name == "AuthoredPine_620");
        Transform large = all.FirstOrDefault(x => x && x.name == "AuthoredPine_631");
        if (!small || !large || !small.GetComponentsInChildren<Renderer>(true).Any() ||
            !large.GetComponentsInChildren<Renderer>(true).Any()) return;

        Vector2[] original = all
            .Where(x => x && x.name.StartsWith("AuthoredPine_", StringComparison.Ordinal))
            .Select(x => new Vector2(x.position.x, x.position.z)).ToArray();

        var placed = new List<Vector2>();
        // A normal Play Mode root is destroyed automatically on leaving Play.
        // DontSave would let cosmetic trees leak into the editor afterward.
        var root = new GameObject(RootName);
        RuntimeRoot = root;

        for (int i = 0; i < Sites.Length; i++)
        {
            float x = Sites[i].x, z = Sites[i].y;
            // No vegetation on the accepted forest-to-clearing traversal lane.
            if (x > -76f && x < -24f && z > 1108f) continue;

            var site = new Vector3(x, 0f, z);
            float height = t.SampleHeight(site) + t.transform.position.y;
            float u = (x - t.transform.position.x) / t.terrainData.size.x;
            float v = (z - t.transform.position.z) / t.terrainData.size.z;
            if (u <= 0f || u >= 1f || v <= 0f || v >= 1f ||
                height < 27f || t.terrainData.GetSteepness(u, v) > 26f)
                continue;

            Vector2 candidate = new Vector2(x, z);
            if (original.Any(p => Vector2.Distance(p, candidate) < 8f) ||
                placed.Any(p => Vector2.Distance(p, candidate) < 9f))
                continue;

            bool tall = i % 4 == 0 || i % 5 == 2;
            Transform source = tall ? large : small;
            var tree = UnityEngine.Object.Instantiate(source.gameObject, root.transform);
            tree.name = "AuthoredPine_FrozenRidge_" + i;
            tree.transform.rotation = Quaternion.Euler(0f, (21 + i * 83) % 360, 0f);
            float scale = tall ? .67f + .11f * (i % 3) : .38f + .13f * (i % 3);
            tree.transform.localScale = source.lossyScale * scale;
            tree.transform.position = new Vector3(x, height, z);

            Renderer[] lod0 = tree.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name.Contains("LOD0")).ToArray();
            if (lod0.Length == 0)
            {
                UnityEngine.Object.Destroy(tree);
                continue;
            }

            // Ground by the rendered base of the LOD0 model, with shallow
            // snow embed, rather than by the prefab's often displaced pivot.
            float baseY = lod0.Min(r => r.bounds.min.y);
            tree.transform.position += Vector3.up * (height - baseY - .10f);

            foreach (Collider collider in tree.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.Destroy(collider);

            placed.Add(candidate);
            AddedPines++;
        }

        if (AddedPines == 0)
        {
            RuntimeRoot = null;
            UnityEngine.Object.Destroy(root);
        }
    }
}

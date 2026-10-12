using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Add a young age layer to selected northern forest margins, preserving
// existing authored pine placements, clearing, shoreline and terrain.
public static class MMFrozenGroveAgeLayer
{
    const string RootName = "MM_FrozenGroveAgeLayer";
    const string TerrainName = "Terrain_FrozenHighlands_North";
    public static GameObject RuntimeRoot { get; private set; }
    public static int AddedPines { get; private set; }

    static readonly Vector2[] ClusterCenters =
    {
        new Vector2(-121f, 1109f),
        new Vector2(-113f, 1126f),
        new Vector2(-14f, 1101f),
        new Vector2(12f, 1114f),
        new Vector2(-145f, 1093f)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        RuntimeRoot = null;
        AddedPines = 0;
        if (SceneManager.GetActiveScene().name != "Enroth" ||
            GameObject.Find(RootName))
            return;

        Terrain terrain = Terrain.activeTerrains.FirstOrDefault(t =>
            t && t.name == TerrainName && t.terrainData);
        if (!terrain) return;

        Transform[] active = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Exclude);
        Transform youngSource = active.FirstOrDefault(x =>
            x && x.name == "AuthoredPine_620");
        Transform adultSource = active.FirstOrDefault(x =>
            x && x.name == "AuthoredPine_631");
        if (!youngSource || !adultSource ||
            !youngSource.GetComponentsInChildren<Renderer>(true).Any() ||
            !adultSource.GetComponentsInChildren<Renderer>(true).Any())
            return;

        Vector2[] oldPines = active
            .Where(x => x && x.name.StartsWith("AuthoredPine_", StringComparison.Ordinal) &&
                x.position.x > -180f && x.position.x < 65f &&
                x.position.z > 1055f && x.position.z < 1160f)
            .Select(x => new Vector2(x.position.x, x.position.z))
            .ToArray();

        var groupRoot = new GameObject(RootName);
        // Keep a normal Play Mode root: DontSave can leave stale grove clones in Edit Mode.
        RuntimeRoot = groupRoot;
        var placed = new List<Vector2>();
        var random = new System.Random(17767);

        for (int group = 0; group < ClusterCenters.Length; group++)
        {
            for (int slot = 0; slot < 9; slot++)
            {
                bool wider = (group + slot) % 3 == 0;
                float angle = (float)random.NextDouble() * 6.283185f;
                float radius = 2f + (float)random.NextDouble() * 13f;
                float x = ClusterCenters[group].x + Mathf.Cos(angle) * radius;
                float z = ClusterCenters[group].y + Mathf.Sin(angle) * radius;
                var candidate = new Vector2(x, z);

                // Retain the navigable forest-to-clearing lane rather
                // than turning every snow opening into new vegetation.
                if (x > -76f && x < -24f && z > 1108f)
                    continue;

                Vector3 sample = new Vector3(x, 0f, z);
                float worldY = terrain.SampleHeight(sample) +
                    terrain.transform.position.y;
                float u = (x - terrain.transform.position.x) /
                    terrain.terrainData.size.x;
                float v = (z - terrain.transform.position.z) /
                    terrain.terrainData.size.z;
                float slope = terrain.terrainData.GetSteepness(u, v);
                if (worldY < 27f || slope > 32f)
                    continue;

                float separation = wider ? 5.2f : 3.8f;
                if (oldPines.Any(p => Vector2.Distance(p, candidate) < separation) ||
                    placed.Any(p => Vector2.Distance(p, candidate) < 3f))
                    continue;

                Transform source = wider ? adultSource : youngSource;
                GameObject instance = UnityEngine.Object.Instantiate(
                    source.gameObject, groupRoot.transform);

                // These names remain eligible for existing camera foliage
                // occlusion handling; no camera or original model changes.
                instance.name = "AuthoredPine_GroveJuvenile_" + group + "_" + slot;
                float scale = wider
                    ? .27f + (float)random.NextDouble() * .22f
                    : .32f + (float)random.NextDouble() * .26f;
                instance.transform.localScale = source.lossyScale * scale;
                instance.transform.rotation = Quaternion.Euler(
                    0f, (float)random.NextDouble() * 360f, 0f);
                instance.transform.position = new Vector3(x, worldY, z);

                Renderer[] lowestDetail = instance.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r.name.Contains("LOD0")).ToArray();
                if (lowestDetail.Length == 0)
                {
                    UnityEngine.Object.Destroy(instance);
                    continue;
                }

                // Ground the actual mesh, not the source prefab pivot.
                float baseY = lowestDetail.Min(r => r.bounds.min.y);
                instance.transform.position += Vector3.up *
                    (worldY - baseY - .10f);
                placed.Add(candidate);
                AddedPines++;
            }
        }
    }
}

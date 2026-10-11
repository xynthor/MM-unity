using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Localized winter-bank dressing for the already-authored Frozen Highlands north forest.
// Uses existing production meshes and materials; never moves an original instance,
// modifies TerrainData, or adds geometry to the serialized Enroth scene.
public static class MMNorthWinterBankDressing
{
    const string RootName = "MM_NorthForestBankDressing";
    const string TerrainName = "Terrain_FrozenHighlands_North";
    public static GameObject RuntimeRoot { get; private set; }
    public static int PlacedBrush { get; private set; }
    public static int PlacedRocks { get; private set; }

    static readonly Vector2[] Clusters =
    {
        new Vector2(-128f, 1110f),
        new Vector2(-102f, 1118f),
        new Vector2(-62f, 1120f),
        new Vector2(-20f, 1108f),
        new Vector2(4f, 1112f),
        new Vector2(-142f, 1100f)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (SceneManager.GetActiveScene().name != "Enroth" ||
            GameObject.Find(RootName))
            return;

        Terrain terrain = Terrain.activeTerrains.FirstOrDefault(t =>
            t && t.name == TerrainName && t.terrainData);
        if (!terrain)
            return;

        // Only existing approved natural source objects are eligible.
        Transform[] sceneTransforms = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Exclude);
        Transform sourceBrush = sceneTransforms
            .Where(t => t && t.name == "Shrub_0")
            .OrderBy(t => Vector2.Distance(
                new Vector2(t.position.x, t.position.z), new Vector2(-62f, 844f)))
            .FirstOrDefault();
        Transform sourceRock = sceneTransforms.FirstOrDefault(t =>
            t && t.name == "AuthoredBoulder_11" &&
            t.position.x > -256f && t.position.x < 256f &&
            t.position.z > 768f && t.position.z < 1280f);

        if (!sourceBrush || !sourceRock ||
            sourceBrush.GetComponentsInChildren<Renderer>(true).Length == 0 ||
            sourceRock.GetComponentsInChildren<Renderer>(true).Length == 0)
            return;

        Vector2[] existingPines = sceneTransforms
            .Where(t => t && t.name.StartsWith("AuthoredPine_", StringComparison.Ordinal) &&
                t.position.x > -165f && t.position.x < 40f &&
                t.position.z > 1060f && t.position.z < 1150f)
            .Select(t => new Vector2(t.position.x, t.position.z))
            .ToArray();

        RuntimeRoot = new GameObject(RootName);
        PlacedBrush = 0;
        PlacedRocks = 0;

        // Site-specific naturalization, not a global biome regeneration:
        // protected open movement between clusters; low, muted underbrush
        // and occasional modest stone exposures near existing pines.
        var random = new System.Random(91073);
        for (int group = 0; group < Clusters.Length; group++)
        {
            for (int index = 0; index < 12; index++)
            {
                bool rock = index == 0;
                float angle = (float)(random.NextDouble() * Math.PI * 2.0);
                float radius = rock
                    ? 3f + (float)random.NextDouble() * 11f
                    : 2f + (float)random.NextDouble() * 13f;
                float x = Clusters[group].x + Mathf.Cos(angle) * radius;
                float z = Clusters[group].y + Mathf.Sin(angle) * radius;
                var surface = new Vector3(x, 0f, z);

                float height = terrain.SampleHeight(surface) + terrain.transform.position.y;
                float steepness = terrain.terrainData.GetSteepness(
                    (x - terrain.transform.position.x) / terrain.terrainData.size.x,
                    (z - terrain.transform.position.z) / terrain.terrainData.size.z);
                if (height < 24.5f || steepness > 32f)
                    continue;

                Vector2 candidate = new Vector2(x, z);
                if (existingPines.Any(p => Vector2.Distance(p, candidate) <
                    (rock ? 6f : 3.2f)))
                    continue;

                Transform source = rock ? sourceRock : sourceBrush;
                GameObject instance = UnityEngine.Object.Instantiate(
                    source.gameObject, RuntimeRoot.transform);
                instance.name = (rock ? "WinterRock" : "WinterBrush") +
                    "_" + group + "_" + index;
                instance.transform.position = new Vector3(x, height, z);
                instance.transform.rotation = Quaternion.Euler(
                    0f, (float)random.NextDouble() * 360f, 0f);
                float size = rock
                    ? 0.55f + (float)random.NextDouble() * 0.40f
                    : 1.6f + (float)random.NextDouble() * 1.25f;
                instance.transform.localScale = source.localScale * size;

                Renderer[] components = instance.GetComponentsInChildren<Renderer>(true);
                if (components.Length == 0)
                {
                    UnityEngine.Object.Destroy(instance);
                    continue;
                }

                // Align ACTUAL individual rendered-mesh bounds to terrain,
                // rather than trusting potentially misleading parent bounds.
                float bottom = components.Min(r => r.bounds.min.y);
                instance.transform.position += Vector3.up *
                    (height - bottom - (rock ? 0.27f : 0.10f));
                if (rock)
                {
                    PlacedRocks++;
                }
                else
                {
                    // Muted dormant-winter twigs, never recolor the source shared material.
                    foreach (Renderer renderer in components)
                    {
                        var properties = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(properties);
                        properties.SetColor("_Color", new Color(0.74f, 0.75f, 0.73f, 1f));
                        renderer.SetPropertyBlock(properties);
                    }
                    PlacedBrush++;
                }
            }
        }
    }
}

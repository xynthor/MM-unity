using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// A local snow cap for one visibly tan Frozen Highlands coastal headland.
// Alter only the already cloned Play Mode TerrainData's surface weights.
// Original terrain, heightmaps, ocean, alpine overlay, ice and colliders stay untouched.
[DisallowMultipleComponent]
public sealed class MMFrozenCoastSnowCapPresentation : MonoBehaviour
{
    const string RootName = "MM_FrozenCoastSnowCapPresentation";
    const string TerrainName = "Terrain_FrozenHighlands_North";
    const int VolcanicLayer = 3;
    const int OriginalSnowLayer = 18;
    const int ExpectedOriginalLayers = 19;
    const int ExpectedRuntimeLayers = 20;

    public static MMFrozenCoastSnowCapPresentation Instance { get; private set; }
    public bool Applied { get; private set; }
    public int ChangedAlphamapCells { get; private set; }
    public float ConvertedRockWeight { get; private set; }

    Terrain terrain;
    TerrainData runtimeData;
    float[,,] originalWeights;
    int x0, z0, nx, nz;
    bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (SceneManager.GetActiveScene().name != "Enroth" ||
            GameObject.Find(RootName))
            return;

        Terrain site = Terrain.activeTerrains.FirstOrDefault(t =>
            t && t.name == TerrainName && t.terrainData);
        if (!site) return;

        // A normal GameObject disappears when exiting Play Mode.
        GameObject root = new GameObject(RootName);
        root.AddComponent<MMFrozenCoastSnowCapPresentation>().terrain = site;
    }

    IEnumerator Start()
    {
        // The accepted packed-snow layer clones TerrainData in the same
        // AfterSceneLoad phase; wait until that clone exists.
        for (int frame = 0; frame < 20; frame++)
        {
            MMFrozenPackedSnowPresentation packed =
                MMFrozenPackedSnowPresentation.Instance;
            if (packed && packed.IsActive &&
                packed.OriginalTerrainData &&
                packed.OriginalTerrainData.terrainLayers.Length == ExpectedOriginalLayers &&
                terrain && terrain.terrainData != packed.OriginalTerrainData &&
                terrain.terrainData.alphamapLayers == ExpectedRuntimeLayers)
            {
                runtimeData = terrain.terrainData;
                initialized = true;
                Apply();
                yield break;
            }
            yield return null;
        }

        Debug.LogWarning("Frozen coast snow cap skipped: no supported runtime snow clone.");
        Destroy(gameObject);
    }

    void OnEnable()
    {
        if (initialized && !Applied) Apply();
    }

    void OnDisable() { Restore(); }
    void OnDestroy()
    {
        Restore();
        if (Instance == this) Instance = null;
    }

    void Apply()
    {
        if (!initialized || !terrain || !runtimeData ||
            terrain.terrainData != runtimeData ||
            runtimeData.alphamapLayers != ExpectedRuntimeLayers)
            return;

        int aw = runtimeData.alphamapWidth;
        int ah = runtimeData.alphamapHeight;
        TerrainData data = runtimeData;
        Vector3 origin = terrain.transform.position;
        x0 = Mathf.Clamp(Mathf.FloorToInt(
            (-163f - origin.x) / data.size.x * (aw - 1)), 0, aw - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(
            (-32f - origin.x) / data.size.x * (aw - 1)), 0, aw - 1);
        z0 = Mathf.Clamp(Mathf.FloorToInt(
            (1205f - origin.z) / data.size.z * (ah - 1)), 0, ah - 1);
        int z1 = Mathf.Clamp(Mathf.CeilToInt(
            (1245f - origin.z) / data.size.z * (ah - 1)), 0, ah - 1);
        nx = x1 - x0 + 1;
        nz = z1 - z0 + 1;
        if (nx < 2 || nz < 2) return;

        if (originalWeights == null)
            originalWeights = data.GetAlphamaps(x0, z0, nx, nz);
        float[,,] weights = (float[,,])originalWeights.Clone();

        int changed = 0;
        float total = 0f;
        for (int iz = 0; iz < nz; iz++)
        for (int ix = 0; ix < nx; ix++)
        {
            float wx = origin.x + (ix + x0) / (float)(aw - 1) * data.size.x;
            float wz = origin.z + (iz + z0) / (float)(ah - 1) * data.size.z;
            float u = (wx - origin.x) / data.size.x;
            float v = (wz - origin.z) / data.size.z;
            float altitude = terrain.SampleHeight(new Vector3(wx, 0f, wz)) + origin.y;
            float slope = data.GetSteepness(u, v);
            float rock = weights[iz, ix, VolcanicLayer];

            // Do not snow-cover submerged shore, exposed steep cliff faces,
            // pre-existing snowy ground or outside this single headland.
            if (altitude < 29f || slope >= 36f || rock < .12f)
                continue;

            float edgeX = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(
                Mathf.Min(wx + 159f, -37f - wx) / 15f));
            float edgeZ = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(
                Mathf.Min(wz - 1208f, 1240f - wz) / 10f));
            float cap = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(
                (altitude - 29f) / 8f)) *
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((36f - slope) / 16f));
            float grain = .81f + .19f * Mathf.PerlinNoise(
                wx * .075f + 6f, wz * .069f + 16f);
            float shift = rock * .90f * edgeX * edgeZ * cap * grain;
            if (shift < .015f) continue;

            weights[iz, ix, VolcanicLayer] -= shift;
            weights[iz, ix, OriginalSnowLayer] += shift;
            total += shift;
            changed++;
        }

        // Fail closed if this terrain or its intended patch has changed.
        if (changed < 150 || changed > 800)
        {
            Debug.LogWarning("Frozen coast snow cap skipped: unexpected patch " + changed);
            return;
        }

        data.SetAlphamaps(x0, z0, weights);
        ChangedAlphamapCells = changed;
        ConvertedRockWeight = total;
        Applied = true;
        Instance = this;
    }

    void Restore()
    {
        if (Applied && terrain && runtimeData && originalWeights != null &&
            terrain.terrainData == runtimeData)
            runtimeData.SetAlphamaps(x0, z0, originalWeights);

        Applied = false;
        if (Instance == this) Instance = null;
    }
}

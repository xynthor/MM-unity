using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Specific reversible Play Mode presentation repair for the verified
// artificial Frozen Highlands step-cliff at roughly (-146, 1142).
// Original TerrainData, coast mesh asset, colliders and Enroth scene are never saved.
[DisallowMultipleComponent]
public sealed class MMFrozenCliffSeamPresentation : MonoBehaviour
{
    const string RootName = "MM_FrozenCliffSeamPresentation";
    const string TerrainName = "Terrain_FrozenHighlands_North";
    const string OverlayName = "North Alpine Rock Surface 2 20261007";
    const string BoulderName = "AuthoredBoulder_11";
    const int HeightX = 86, HeightZ = 359, Width = 58, Height = 37;

    Terrain terrain;
    TerrainCollider terrainCollider;
    TerrainData sourceData, runtimeData;
    MeshFilter overlayFilter;
    UnityEngine.Mesh runtimeOverlay;
    MeshRenderer perchedBoulder;
    bool boulderWasEnabled, ready;
    float[,] sourceHeights;
    Vector3[] overlayOriginalVertices;
    Vector3[] overlayOriginalNormals;

    public static MMFrozenCliffSeamPresentation Instance { get; private set; }
    public bool Applied { get; private set; }
    public int ChangedHeightSamples { get; private set; }
    public int ShiftedOverlayVertices { get; private set; }
    public float MaximumHeightAdjustment { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (SceneManager.GetActiveScene().name != "Enroth" ||
            GameObject.Find(RootName))
            return;
        var terrain = Terrain.activeTerrains.FirstOrDefault(t =>
            t && t.name == TerrainName && t.terrainData);
        if (!terrain) return;

        var root = new GameObject(RootName);
        root.AddComponent<MMFrozenCliffSeamPresentation>().terrain = terrain;
    }

    IEnumerator Start()
    {
        for (int frame = 0; frame < 60; frame++)
        {
            var snow = MMFrozenPackedSnowPresentation.Instance;
            var coast = MMFrozenCoastOverlayContactFix.Instance;
            if (snow && snow.IsActive && coast && coast.Applied)
            {
                sourceData = snow.OriginalTerrainData;
                runtimeData = terrain.terrainData;
                try
                {
                    Initialize();
                    ready = true;
                    Apply();
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Frozen cliff seam skipped: " + e.Message);
                    Restore();
                    Destroy(gameObject);
                }
                yield break;
            }
            yield return null;
        }
        Debug.LogWarning("Frozen cliff seam skipped: runtime snow/overlay not ready.");
        Destroy(gameObject);
    }

    void Initialize()
    {
        if (!terrain || !sourceData || !runtimeData ||
            sourceData == runtimeData ||
            terrain.transform.position != new Vector3(-256f, -24f, 768f) ||
            runtimeData.heightmapResolution != 513 ||
            runtimeData.size != new Vector3(512f, 320f, 512f) ||
            sourceData.terrainLayers.Length != 19 ||
            runtimeData.terrainLayers.Length != 20)
            throw new InvalidOperationException("Frozen terrain baseline changed.");

        terrainCollider = terrain.GetComponent<TerrainCollider>();
        if (!terrainCollider || terrainCollider.terrainData != sourceData)
            throw new InvalidOperationException("Original terrain collider changed.");

        var renderer = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
            FindObjectsInactive.Exclude).FirstOrDefault(r =>
            r && r.name == OverlayName && r.enabled);
        if (!renderer) throw new InvalidOperationException("Alpine overlay missing.");

        overlayFilter = renderer.GetComponent<MeshFilter>();
        runtimeOverlay = overlayFilter ? overlayFilter.sharedMesh : null;
        if (!runtimeOverlay || !runtimeOverlay.isReadable ||
            runtimeOverlay.name != "Frozen coastal alpine contact (runtime)" ||
            runtimeOverlay.vertexCount != 263169 ||
            runtimeOverlay.subMeshCount != 1 ||
            runtimeOverlay.triangles.Length != (524288 - 408) * 3)
            throw new InvalidOperationException("Accepted coast overlay changed.");

        var boulder = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Exclude).FirstOrDefault(x => x.name == BoulderName);
        if (!boulder ||
            Vector3.Distance(boulder.position, new Vector3(-151.18f, 44.18f, 1142.52f)) > .20f ||
            boulder.GetComponentsInChildren<Collider>(true).Length != 1)
            throw new InvalidOperationException("Exact perched boulder changed.");
        perchedBoulder = boulder.GetComponentInChildren<MeshRenderer>();
        if (!perchedBoulder) throw new InvalidOperationException("Boulder renderer missing.");
        boulderWasEnabled = perchedBoulder.enabled;

        sourceHeights = sourceData.GetHeights(HeightX, HeightZ, Width, Height);
        var current = runtimeData.GetHeights(HeightX, HeightZ, Width, Height);
        for (int z = 0; z < Height; z++)
        for (int x = 0; x < Width; x++)
            if (Mathf.Abs(current[z, x] - sourceHeights[z, x]) > .000001f)
                throw new InvalidOperationException("Runtime cliff already differs from source.");
        overlayOriginalVertices = runtimeOverlay.vertices;
        overlayOriginalNormals = runtimeOverlay.normals;
        if (overlayOriginalNormals.Length != overlayOriginalVertices.Length)
            throw new InvalidOperationException("Overlay normals unavailable.");
    }

    void Apply()
    {
        if (!ready || Applied || !terrain || !runtimeData || !runtimeOverlay ||
            terrain.terrainData != runtimeData || !terrainCollider ||
            terrainCollider.terrainData != sourceData ||
            overlayFilter.sharedMesh != runtimeOverlay)
            return;

        bool heightChanged = false, overlayChanged = false;
        try
        {
            float[,] after = (float[,])sourceHeights.Clone();
            int changed = 0;
            float max = 0f;
            const int rowA = 1131 - 768 - HeightZ;
            const int rowB = 1156 - 768 - HeightZ;
            for (int z = 0; z < Height; z++)
            for (int x = 0; x < Width; x++)
            {
                float wx = -256f + HeightX + x;
                float wz = 768f + HeightZ + z;
                if (wx <= -169f || wx >= -116f ||
                    wz <= 1132f || wz >= 1156f)
                    continue;
                float expected = Mathf.Lerp(sourceHeights[rowA, x],
                    sourceHeights[rowB, x], Mathf.Clamp01((wz - 1131f) / 25f));
                float difference = (expected - sourceHeights[z, x]) * 320f;
                if (Mathf.Abs(difference) < .12f) continue;
                float edgeX = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(
                    Mathf.Min(wx + 168f, -117f - wx) / 7f));
                float edgeZ = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(
                    Mathf.Min(wz - 1132f, 1156f - wz) / 5f));
                float delta = Mathf.Clamp(difference, -3.2f, 3.2f) *
                              edgeX * edgeZ * .96f;
                if (Mathf.Abs(delta) < .02f) continue;
                after[z, x] = sourceHeights[z, x] + delta / 320f;
                max = Mathf.Max(max, Mathf.Abs(delta));
                changed++;
            }
            if (changed != 906 || max < 2.4f || max > 2.6f)
                throw new InvalidOperationException("Unexpected cliff height patch.");

            runtimeData.SetHeights(HeightX, HeightZ, after);
            heightChanged = true;

            Vector3[] vertices = (Vector3[])overlayOriginalVertices.Clone();
            Vector3[] normals = (Vector3[])overlayOriginalNormals.Clone();
            Matrix4x4 toWorld = overlayFilter.transform.localToWorldMatrix;
            Matrix4x4 toLocal = toWorld.inverse;
            int warped = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 world = toWorld.MultiplyPoint3x4(vertices[i]);
                if (world.x < -171f || world.x > -116f ||
                    world.z < 1130f || world.z > 1158f) continue;

                float u = (world.x + 256f) / 512f;
                float v = (world.z - 768f) / 512f;
                float change = runtimeData.GetInterpolatedHeight(u, v) -
                               sourceData.GetInterpolatedHeight(u, v);
                if (Mathf.Abs(change) < .025f) continue;
                vertices[i] = toLocal.MultiplyPoint3x4(
                    world + Vector3.up * change);
                normals[i] = overlayFilter.transform.InverseTransformDirection(
                    runtimeData.GetInterpolatedNormal(u, v)).normalized;
                warped++;
            }
            if (warped != 903)
                throw new InvalidOperationException("Unexpected alpine vertex patch: " + warped);

            runtimeOverlay.vertices = vertices;
            runtimeOverlay.normals = normals;
            runtimeOverlay.RecalculateBounds();
            overlayChanged = true;

            // On the rest of the world this cloned heightfield is unchanged.
            // The collider must match the localized visual changes here.
            terrainCollider.terrainData = runtimeData;
            if (terrainCollider.terrainData != runtimeData)
                throw new InvalidOperationException("Runtime terrain/collider mismatch.");

            // This boulder floats more visibly after removing its artificial shelf.
            // Do not change its authored transform or its collider.
            perchedBoulder.enabled = false;
            ChangedHeightSamples = changed;
            ShiftedOverlayVertices = warped;
            MaximumHeightAdjustment = max;
            Applied = true;
            Instance = this;
        }
        catch
        {
            if (terrainCollider && terrainCollider.terrainData == runtimeData)
                terrainCollider.terrainData = sourceData;
            if (perchedBoulder) perchedBoulder.enabled = boulderWasEnabled;
            if (overlayChanged && runtimeOverlay)
            {
                runtimeOverlay.vertices = overlayOriginalVertices;
                runtimeOverlay.normals = overlayOriginalNormals;
                runtimeOverlay.RecalculateBounds();
            }
            if (heightChanged && terrain && terrain.terrainData == runtimeData)
                runtimeData.SetHeights(HeightX, HeightZ, sourceHeights);
            throw;
        }
    }

    void OnEnable()
    {
        if (ready && !Applied) Apply();
    }

    void OnDisable() { Restore(); }
    void OnDestroy()
    {
        Restore();
        if (Instance == this) Instance = null;
    }

    void Restore()
    {
        if (terrainCollider && sourceData &&
            terrainCollider.terrainData == runtimeData)
            terrainCollider.terrainData = sourceData;
        if (perchedBoulder)
            perchedBoulder.enabled = boulderWasEnabled;
        // This is the accepted coast fix's *own* runtime cloned mesh.
        // Its existing OnDestroy still restores the original authored mesh.
        if (runtimeOverlay && overlayFilter &&
            overlayFilter.sharedMesh == runtimeOverlay &&
            overlayOriginalVertices != null)
        {
            runtimeOverlay.vertices = overlayOriginalVertices;
            runtimeOverlay.normals = overlayOriginalNormals;
            runtimeOverlay.RecalculateBounds();
        }
        if (runtimeData && terrain && terrain.terrainData == runtimeData &&
            sourceHeights != null)
            runtimeData.SetHeights(HeightX, HeightZ, sourceHeights);

        Applied = false;
        if (Instance == this) Instance = null;
    }
}

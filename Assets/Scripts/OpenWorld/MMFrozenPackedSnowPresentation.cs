using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Site-specific, non-destructive snow-surface variation for the approved
// Frozen Highlands forest-to-clearing visual pilot. Original TerrainData,
// TerrainLayers, textures, height, colliders and world transforms are untouched.
[DisallowMultipleComponent]
public sealed class MMFrozenPackedSnowPresentation : MonoBehaviour
{
    const string TerrainName = "Terrain_FrozenHighlands_North";
    const string RootName = "MM_FrozenPackedSnowPresentation";
    const int TextureResolution = 1024;
    const int ExistingSnowIndex = 18;
    const int VariantSnowIndex = 17;

    public static MMFrozenPackedSnowPresentation Instance { get; private set; }
    public static string LastInitializationError { get; private set; }
    public int ChangedAlphamapCells { get; private set; }
    public TerrainData OriginalTerrainData => sourceData;
    public bool IsActive => terrain && runtimeData && terrain.terrainData == runtimeData;

    Terrain terrain;
    TerrainData sourceData, runtimeData;
    TerrainLayer packedSnow;
    Texture2D packedAlbedo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (SceneManager.GetActiveScene().name != "Enroth")
            return;

        // Unity editor can retain DontSave roots across Play transitions.
        // Do not let a stale inactive root block a new session.
        var stale = GameObject.Find(RootName);
        if (stale)
        {
            var oldComponent = stale.GetComponent<MMFrozenPackedSnowPresentation>();
            if (oldComponent && oldComponent.IsActive)
                return;
            DestroyImmediate(stale);
        }

        var target = Terrain.activeTerrains.FirstOrDefault(t =>
            t && t.name == TerrainName && t.terrainData);
        if (!target || target.terrainData.terrainLayers.Length <= ExistingSnowIndex)
            return;

        var root = new GameObject(RootName);
        // A regular runtime root is destroyed on leaving Play Mode; its
        // generated TerrainData, TerrainLayer and texture are DontSave.
        var component = root.AddComponent<MMFrozenPackedSnowPresentation>();
        component.Initialize(target);
    }

    void Initialize(Terrain target)
    {
        terrain = target;
        LastInitializationError = null;
        sourceData = terrain.terrainData;
        try
        {
            runtimeData = Instantiate(sourceData);
            runtimeData.name = sourceData.name + " (runtime packed snow)";
            runtimeData.hideFlags = HideFlags.DontSave;

            TerrainLayer[] layers = runtimeData.terrainLayers;
            packedSnow = Instantiate(layers[VariantSnowIndex]);
            packedSnow.name = "Frozen wind-packed snow (runtime)";
            packedSnow.hideFlags = HideFlags.DontSave;
            packedSnow.diffuseTexture = BuildPackedSnowAlbedo(layers[ExistingSnowIndex].diffuseTexture);
            packedSnow.normalMapTexture = layers[ExistingSnowIndex].normalMapTexture;
            packedSnow.tileSize = new Vector2(8f, 8f);
            packedSnow.smoothness = .045f;
            layers[VariantSnowIndex] = packedSnow;
            runtimeData.terrainLayers = layers;

            ApplyLocalVariation();
            // Heightmaps and collision profiles are exactly the same; the
            // borrowed TerrainData is presentation-only for this live tile.
            terrain.terrainData = runtimeData;
            Instance = this;
        }
        catch (Exception error)
        {
            LastInitializationError = error.ToString();
            Debug.LogWarning("Frozen packed-snow presentation skipped: " + error.Message);
            RestoreAndClean();
            Destroy(gameObject);
        }
    }

    Texture2D BuildPackedSnowAlbedo(Texture source)
    {
        if (!source)
            throw new InvalidOperationException("Existing snow texture is missing");

        var result = new Texture2D(TextureResolution, TextureResolution,
            TextureFormat.RGBA32, true, false)
        {
            name = "Frozen snow: original-grain-derived packed variant (runtime)",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4,
            hideFlags = HideFlags.DontSave
        };

        var previous = RenderTexture.active;
        var temporary = RenderTexture.GetTemporary(TextureResolution, TextureResolution,
            0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        try
        {
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;
            result.ReadPixels(
                new Rect(0, 0, TextureResolution, TextureResolution), 0, 0, false);
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
        }

        Color[] colors = result.GetPixels();
        for (int y = 0; y < TextureResolution; y++)
        {
            for (int x = 0; x < TextureResolution; x++)
            {
                int index = y * TextureResolution + x;
                float u = x / (float)TextureResolution;
                float v = y / (float)TextureResolution;
                float grain = Mathf.PerlinNoise(u * 4.9f + 13.1f, v * 10.9f + 23.7f);
                float darkness = Mathf.Lerp(.75f, .90f, grain);
                Color baseColor = colors[index];
                baseColor.r *= darkness * .96f;
                baseColor.g *= darkness * .986f;
                baseColor.b *= darkness;
                // Keep the original snow texture's alpha (smoothness data).
                colors[index] = baseColor;
            }
        }

        result.SetPixels(colors);
        result.Apply(true, false);
        packedAlbedo = result;
        return result;
    }

    void ApplyLocalVariation()
    {
        int width = runtimeData.alphamapWidth;
        int height = runtimeData.alphamapHeight;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(
            (-175f - terrain.transform.position.x) / sourceData.size.x * (width - 1)), 0, width - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt(
            (40f - terrain.transform.position.x) / sourceData.size.x * (width - 1)), 0, width - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(
            (1060f - terrain.transform.position.z) / sourceData.size.z * (height - 1)), 0, height - 1);
        int z1 = Mathf.Clamp(Mathf.CeilToInt(
            (1180f - terrain.transform.position.z) / sourceData.size.z * (height - 1)), 0, height - 1);

        if (x1 < x0 || z1 < z0)
            throw new InvalidOperationException("Packed snow pilot lies outside Frozen tile");

        float[,,] weights = runtimeData.GetAlphamaps(x0, z0, x1 - x0 + 1, z1 - z0 + 1);
        int changed = 0;
        for (int z = 0; z <= z1 - z0; z++)
        {
            for (int x = 0; x <= x1 - x0; x++)
            {
                float worldX = terrain.transform.position.x +
                    (x + x0) / (float)(width - 1) * sourceData.size.x;
                float worldZ = terrain.transform.position.z +
                    (z + z0) / (float)(height - 1) * sourceData.size.z;
                float margin = Mathf.Min(
                    Mathf.Min(worldX + 174f, 39f - worldX),
                    Mathf.Min(worldZ - 1062f, 1179f - worldZ));
                if (margin <= 0f)
                    continue;

                float fade = Mathf.SmoothStep(0, 1, Mathf.Clamp01(margin / 12f));
                float nx = (worldX - terrain.transform.position.x) / sourceData.size.x;
                float nz = (worldZ - terrain.transform.position.z) / sourceData.size.z;
                float slope = sourceData.GetSteepness(nx, nz);
                if (slope > 40f || weights[z, x, ExistingSnowIndex] < .95f)
                    continue;

                float pattern = Mathf.PerlinNoise(
                    worldX * .045f + 11f, worldZ * .050f + 7f);
                float amount = fade *
                    (.34f + .34f * Mathf.SmoothStep(0, 1, pattern)) *
                    Mathf.Lerp(.80f, 1f, Mathf.Clamp01(slope / 23f));
                float transferred = Mathf.Min(weights[z, x, ExistingSnowIndex], amount);
                weights[z, x, ExistingSnowIndex] -= transferred;
                weights[z, x, VariantSnowIndex] += transferred;
                changed++;
            }
        }

        runtimeData.SetAlphamaps(x0, z0, weights);
        ChangedAlphamapCells = changed;
    }

    void OnDestroy()
    {
        RestoreAndClean();
    }

    static void ReleaseRuntime(UnityEngine.Object obj)
    {
        if (!obj) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    void RestoreAndClean()
    {
        if (terrain && sourceData && terrain.terrainData == runtimeData)
            terrain.terrainData = sourceData;

        ReleaseRuntime(runtimeData);
        ReleaseRuntime(packedSnow);
        ReleaseRuntime(packedAlbedo);

        runtimeData = null;
        packedSnow = null;
        packedAlbedo = null;

        if (Instance == this)
            Instance = null;
    }
}

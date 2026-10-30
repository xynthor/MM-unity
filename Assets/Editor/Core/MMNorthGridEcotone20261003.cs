using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MMNorthGridEcotone20261003
{
    const int Band = 64;
    const float LandThreshold = 0.55f;
    const string ReportDir = "Validation/GridAudit20261003";

    static readonly (string a, string b)[] Pairs =
    {
        ("DragonIsleNorthTerrain", "SweetWater_NorthTerrain"),
        ("SweetWater_NorthTerrain", "SweetWater_LinkedNorthProfile"),
        ("Kriegspire_NorthTerrain", "Kriegspire_LinkedNorthProfile"),
        ("FrozenHighlands_NorthTerrain", "FrozenHighlands_LinkedNorthProfile"),
        ("SilverCove_NorthTerrain", "SilverCove_LinkedNorthProfile"),
        ("SweetWater_LinkedNorthProfile", "Kriegspire_LinkedNorthProfile"),
        ("Kriegspire_LinkedNorthProfile", "FrozenHighlands_LinkedNorthProfile"),
        ("FrozenHighlands_LinkedNorthProfile", "SilverCove_LinkedNorthProfile")
    };

    static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    static string Category(TerrainLayer layer)
    {
        if (!layer) return "NULL";

        string name = (layer.name ?? "").ToLowerInvariant();
        string texturePath = AssetDatabase.GetAssetPath(layer.diffuseTexture).ToLowerInvariant();

        if (name.Contains("snow") || texturePath.Contains("snow")) return "SNOW";
        if (name.Contains("lava") || name.Contains("volcan") ||
            texturePath.Contains("ash_") || texturePath.Contains("rocky_terrain")) return "VOLC";
        if (name.Contains("lightgreen")) return "LIGHTGREEN";
        if (name.Contains("green") || texturePath.Contains("grass") || texturePath.Contains("moss")) return "GREEN";
        if (name.Contains("desert") || texturePath.Contains("sand")) return "DESERT";
        if (name.Contains("arid") || texturePath.Contains("soil") ||
            texturePath.Contains("dirt") || texturePath.Contains("mud")) return "ARID";
        if (name.Contains("road") || texturePath.Contains("road") || texturePath.Contains("path")) return "ROAD";

        return "OTHER";
    }

    static Dictionary<string, float> Pack(TerrainData data, float[,,] alpha, int z, int x)
    {
        var result = new Dictionary<string, float>();
        for (int k = 0; k < data.alphamapLayers; k++)
        {
            string category = Category(data.terrainLayers[k]);
            float value = alpha[z, x, k];
            result[category] = result.TryGetValue(category, out float old) ? old + value : value;
        }
        return result;
    }

    static Dictionary<string, float> Mix(
        Dictionary<string, float> a,
        Dictionary<string, float> b,
        float t)
    {
        var result = new Dictionary<string, float>();
        var keys = new HashSet<string>(a.Keys);
        keys.UnionWith(b.Keys);

        foreach (string key in keys)
        {
            a.TryGetValue(key, out float av);
            b.TryGetValue(key, out float bv);
            result[key] = Mathf.Lerp(av, bv, t);
        }

        return result;
    }

    static void Apply(
        TerrainData data,
        float[,,] alpha,
        int z,
        int x,
        Dictionary<string, float> target,
        float blend)
    {
        Dictionary<string, float> current = Pack(data, alpha, z, x);
        Dictionary<string, float> mixed = Mix(current, target, blend);

        var groups = new Dictionary<string, List<int>>();
        for (int k = 0; k < data.alphamapLayers; k++)
        {
            string category = Category(data.terrainLayers[k]);
            if (!groups.TryGetValue(category, out List<int> list))
            {
                list = new List<int>();
                groups[category] = list;
            }
            list.Add(k);
        }

        float represented = 0f;
        foreach (var group in groups)
            if (mixed.TryGetValue(group.Key, out float value))
                represented += value;

        if (represented < 0.000001f) return;

        foreach (var group in groups)
        {
            float total = mixed.TryGetValue(group.Key, out float value)
                ? value / represented
                : 0f;

            float oldSum = 0f;
            foreach (int k in group.Value)
                oldSum += alpha[z, x, k];

            if (oldSum > 0.000001f)
            {
                foreach (int k in group.Value)
                    alpha[z, x, k] = total * (alpha[z, x, k] / oldSum);
            }
            else
            {
                for (int j = 0; j < group.Value.Count; j++)
                    alpha[z, x, group.Value[j]] = j == 0 ? total : 0f;
            }
        }
    }

    static Terrain FindTerrain(string dataName)
    {
        var hits = UnityEngine.Object.FindObjectsByType<Terrain>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None)
            .Where(t => t.terrainData && t.terrainData.name == dataName)
            .ToArray();

        if (hits.Length != 1)
            throw new InvalidOperationException(
                "Expected one live terrain named " + dataName + ", found " + hits.Length);

        return hits[0];
    }

    static bool IsLand(Terrain terrain, float nx, float nz)
    {
        float y = terrain.transform.position.y +
                  terrain.terrainData.GetInterpolatedHeight(
                      Mathf.Clamp01(nx),
                      Mathf.Clamp01(nz));
        return y > LandThreshold;
    }

    static int BlendPair(Terrain a, Terrain b)
    {
        var ap = a.transform.position;
        var bp = b.transform.position;
        var ad = a.terrainData;
        var bd = b.terrainData;

        bool aWestOfB = Mathf.Abs(ap.x + ad.size.x - bp.x) < 0.1f;
        bool bWestOfA = Mathf.Abs(bp.x + bd.size.x - ap.x) < 0.1f;
        bool aSouthOfB = Mathf.Abs(ap.z + ad.size.z - bp.z) < 0.1f;
        bool bSouthOfA = Mathf.Abs(bp.z + bd.size.z - ap.z) < 0.1f;

        bool ew = (aWestOfB || bWestOfA) &&
                  Mathf.Max(ap.z, bp.z) < Mathf.Min(ap.z + ad.size.z, bp.z + bd.size.z) - 0.1f;
        bool ns = (aSouthOfB || bSouthOfA) &&
                  Mathf.Max(ap.x, bp.x) < Mathf.Min(ap.x + ad.size.x, bp.x + bd.size.x) - 0.1f;

        if (!ew && !ns)
            throw new InvalidOperationException(
                "Terrains are not adjacent: " + ad.name + " / " + bd.name);

        Terrain first = a;
        Terrain second = b;
        if (ew && bWestOfA) { first = b; second = a; }
        if (ns && bSouthOfA) { first = b; second = a; }

        TerrainData fd = first.terrainData;
        TerrainData sd = second.terrainData;
        float[,,] fa = fd.GetAlphamaps(0, 0, fd.alphamapWidth, fd.alphamapHeight);
        float[,,] sa = sd.GetAlphamaps(0, 0, sd.alphamapWidth, sd.alphamapHeight);

        int changed = 0;

        if (ew)
        {
            int samples = Mathf.Max(fd.alphamapHeight, sd.alphamapHeight);
            int fBand = Mathf.Min(Band, fd.alphamapWidth);
            int sBand = Mathf.Min(Band, sd.alphamapWidth);

            for (int n = 0; n < samples; n++)
            {
                float u = samples <= 1 ? 0f : n / (float)(samples - 1);
                int fz = Mathf.RoundToInt(u * (fd.alphamapHeight - 1));
                int sz = Mathf.RoundToInt(u * (sd.alphamapHeight - 1));

                if (!IsLand(first, 1f, u) || !IsLand(second, 0f, u))
                    continue;

                var target = Mix(
                    Pack(fd, fa, fz, fd.alphamapWidth - 1),
                    Pack(sd, sa, sz, 0),
                    0.5f);

                for (int d = 0; d < fBand; d++)
                {
                    float blend = 1f - Smooth(d / (float)Mathf.Max(1, fBand - 1));
                    Apply(fd, fa, fz, fd.alphamapWidth - 1 - d, target, blend);
                }

                for (int d = 0; d < sBand; d++)
                {
                    float blend = 1f - Smooth(d / (float)Mathf.Max(1, sBand - 1));
                    Apply(sd, sa, sz, d, target, blend);
                }

                changed++;
            }
        }
        else
        {
            int samples = Mathf.Max(fd.alphamapWidth, sd.alphamapWidth);
            int fBand = Mathf.Min(Band, fd.alphamapHeight);
            int sBand = Mathf.Min(Band, sd.alphamapHeight);

            for (int n = 0; n < samples; n++)
            {
                float u = samples <= 1 ? 0f : n / (float)(samples - 1);
                int fx = Mathf.RoundToInt(u * (fd.alphamapWidth - 1));
                int sx = Mathf.RoundToInt(u * (sd.alphamapWidth - 1));

                if (!IsLand(first, u, 1f) || !IsLand(second, u, 0f))
                    continue;

                var target = Mix(
                    Pack(fd, fa, fd.alphamapHeight - 1, fx),
                    Pack(sd, sa, 0, sx),
                    0.5f);

                for (int d = 0; d < fBand; d++)
                {
                    float blend = 1f - Smooth(d / (float)Mathf.Max(1, fBand - 1));
                    Apply(fd, fa, fd.alphamapHeight - 1 - d, fx, target, blend);
                }

                for (int d = 0; d < sBand; d++)
                {
                    float blend = 1f - Smooth(d / (float)Mathf.Max(1, sBand - 1));
                    Apply(sd, sa, d, sx, target, blend);
                }

                changed++;
            }
        }

        fd.SetAlphamaps(0, 0, fa);
        sd.SetAlphamaps(0, 0, sa);
        fd.SetBaseMapDirty();
        sd.SetBaseMapDirty();
        EditorUtility.SetDirty(fd);
        EditorUtility.SetDirty(sd);
        return changed;
    }

    [MenuItem("MMUnity/World/North/Apply Grid Ecotones 20261003")]
    public static void Run()
    {
        Directory.CreateDirectory(ReportDir);
        var lines = new List<string>();

        foreach (var pair in Pairs)
        {
            Terrain a = FindTerrain(pair.a);
            Terrain b = FindTerrain(pair.b);
            int samples = BlendPair(a, b);
            lines.Add(pair.a + " <-> " + pair.b + " landSamples=" + samples);
        }

        AssetDatabase.SaveAssets();
        lines.Insert(0,
            "PASS band=" + Band +
            " landOnly=true heightsUntouched=true terrainLayersUntouched=true pairs=" + Pairs.Length);
        File.WriteAllLines(ReportDir + "/north_grid_ecotone_apply.txt", lines);
        Debug.Log("MM_NORTH_GRID_ECOTONE_20261003_DONE " + string.Join(" | ", lines));
    }
}

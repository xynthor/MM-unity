using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MMNorthMaterialEcotone20261002
{
    const string GeneratedRoot = "Assets/World/WorldExtensions/Generated";
    const int SouthBand = 64;
    const int LateralBand = 48;

    static readonly string[] Regions =
    {
        "SweetWater",
        "Kriegspire",
        "FrozenHighlands",
        "SilverCove"
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
        {
            if (mixed.TryGetValue(group.Key, out float value))
                represented += value;
        }

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

    static TerrainData LoadNorth(string region)
    {
        return AssetDatabase.LoadAssetAtPath<TerrainData>(
            GeneratedRoot + "/" + region + "_NorthTerrain.asset");
    }

    static TerrainData LoadSouth(string region)
    {
        return AssetDatabase.LoadAssetAtPath<TerrainData>(
            GeneratedRoot + "/LinkedSourceTransitions/" + region + "_LinkedNorthProfile.asset");
    }

    [MenuItem("MM/World/North/Apply Material Ecotones 20261002")]
    public static void Run()
    {
        var north = Regions.ToDictionary(region => region, LoadNorth);
        var south = Regions.ToDictionary(region => region, LoadSouth);

        foreach (string region in Regions)
        {
            if (!north[region] || !south[region])
                throw new InvalidOperationException("Missing north terrain data for " + region);
        }

        var maps = Regions.ToDictionary(
            region => region,
            region => north[region].GetAlphamaps(
                0, 0, north[region].alphamapWidth, north[region].alphamapHeight));

        foreach (string region in Regions)
        {
            TerrainData northData = north[region];
            TerrainData southData = south[region];
            float[,,] northAlpha = maps[region];

            float[,,] southEdge = southData.GetAlphamaps(
                0,
                southData.alphamapHeight - 1,
                southData.alphamapWidth,
                1);

            int width = Mathf.Min(northData.alphamapWidth, southData.alphamapWidth);
            int band = Mathf.Min(SouthBand, northData.alphamapHeight);

            for (int z = 0; z < band; z++)
            {
                float blend = 1f - Smooth(z / (float)(band - 1));

                for (int x = 0; x < width; x++)
                {
                    var target = new Dictionary<string, float>();

                    for (int k = 0; k < southData.alphamapLayers; k++)
                    {
                        string category = Category(southData.terrainLayers[k]);
                        float value = southEdge[0, x, k];
                        target[category] = target.TryGetValue(category, out float old)
                            ? old + value
                            : value;
                    }

                    Apply(northData, northAlpha, z, x, target, blend);
                }
            }
        }

        for (int i = 0; i < Regions.Length - 1; i++)
        {
            string leftName = Regions[i];
            string rightName = Regions[i + 1];

            TerrainData left = north[leftName];
            TerrainData right = north[rightName];
            float[,,] leftAlpha = maps[leftName];
            float[,,] rightAlpha = maps[rightName];

            int height = Mathf.Min(left.alphamapHeight, right.alphamapHeight);
            int band = Mathf.Min(
                LateralBand,
                Mathf.Min(left.alphamapWidth, right.alphamapWidth));

            for (int z = 0; z < height; z++)
            {
                Dictionary<string, float> target = Mix(
                    Pack(left, leftAlpha, z, left.alphamapWidth - 1),
                    Pack(right, rightAlpha, z, 0),
                    0.5f);

                for (int distance = 0; distance < band; distance++)
                {
                    float blend = 1f - Smooth(distance / (float)(band - 1));

                    Apply(
                        left,
                        leftAlpha,
                        z,
                        left.alphamapWidth - 1 - distance,
                        target,
                        blend);

                    Apply(
                        right,
                        rightAlpha,
                        z,
                        distance,
                        target,
                        blend);
                }
            }
        }

        foreach (string region in Regions)
        {
            TerrainData data = north[region];
            data.SetAlphamaps(0, 0, maps[region]);
            data.SetBaseMapDirty();
            EditorUtility.SetDirty(data);
        }

        AssetDatabase.SaveAssets();

        Directory.CreateDirectory("Validation/EdgeGrid20260923/NorthEcotone20261002");
        File.WriteAllText(
            "Validation/EdgeGrid20260923/NorthEcotone20261002/source_apply.txt",
            "APPLIED southBand=64 lateralBand=48 heightsUntouched=TRUE terrainLayersUntouched=TRUE\n");

        Debug.Log("NORTH_MATERIAL_ECOTONE_20261002_DONE");
    }
}

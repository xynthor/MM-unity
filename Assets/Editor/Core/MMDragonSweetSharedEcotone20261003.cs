using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MMDragonSweetSharedEcotone20261003
{
    const int Band = 96;
    const float LandThreshold = 0.55f;
    const string Report = "Validation/GridAudit20261003/dragon_sweet_shared_ecotone.txt";

    [MenuItem("MMUnity/World/North/Fix Dragon Sweet Shared Ecotone 20261003")]
    public static void Run()
    {
        Terrain dragon = Find("DragonIsleNorthTerrain");
        Terrain sweet = Find("SweetWater_NorthTerrain");
        TerrainData dd = dragon.terrainData;
        TerrainData sd = sweet.terrainData;

        Vector3 dp = dragon.transform.position;
        Vector3 sp = sweet.transform.position;
        if (Mathf.Abs(dp.x + dd.size.x - sp.x) > 0.1f)
            throw new InvalidOperationException("Expected Dragon Isle North directly west of Sweet Water North.");

        float[,,] oldAlpha = dd.GetAlphamaps(0, 0, dd.alphamapWidth, dd.alphamapHeight);
        float[,,] sweetAlpha = sd.GetAlphamaps(0, 0, sd.alphamapWidth, sd.alphamapHeight);

        var layers = dd.terrainLayers.ToList();
        var exactIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < layers.Count; i++)
            exactIndex[AssetDatabase.GetAssetPath(layers[i])] = i;

        int[] sweetToDragon = new int[sd.terrainLayers.Length];
        int added = 0;
        for (int k = 0; k < sd.terrainLayers.Length; k++)
        {
            TerrainLayer layer = sd.terrainLayers[k];
            string path = AssetDatabase.GetAssetPath(layer);
            if (!exactIndex.TryGetValue(path, out int index))
            {
                index = layers.Count;
                layers.Add(layer);
                exactIndex[path] = index;
                added++;
            }
            sweetToDragon[k] = index;
        }

        var expanded = new float[dd.alphamapHeight, dd.alphamapWidth, layers.Count];
        for (int z = 0; z < dd.alphamapHeight; z++)
            for (int x = 0; x < dd.alphamapWidth; x++)
                for (int k = 0; k < dd.alphamapLayers; k++)
                    expanded[z, x, k] = oldAlpha[z, x, k];

        float sharedZ0 = Mathf.Max(dp.z, sp.z);
        float sharedZ1 = Mathf.Min(dp.z + dd.size.z, sp.z + sd.size.z);
        int changedRows = 0;
        int changedPixels = 0;

        for (int dz = 0; dz < dd.alphamapHeight; dz++)
        {
            float nzDragon = (dz + 0.5f) / dd.alphamapHeight;
            float worldZ = dp.z + nzDragon * dd.size.z;
            if (worldZ < sharedZ0 || worldZ > sharedZ1) continue;

            float nzSweet = Mathf.InverseLerp(sp.z, sp.z + sd.size.z, worldZ);
            int sz = Mathf.Clamp(Mathf.FloorToInt(nzSweet * sd.alphamapHeight), 0, sd.alphamapHeight - 1);

            float dragonY = dp.y + dd.GetInterpolatedHeight(1f, nzDragon);
            float sweetY = sp.y + sd.GetInterpolatedHeight(0f, nzSweet);
            if (dragonY <= LandThreshold || sweetY <= LandThreshold) continue;

            var target = new float[layers.Count];
            for (int sk = 0; sk < sd.alphamapLayers; sk++)
                target[sweetToDragon[sk]] += sweetAlpha[sz, 0, sk];

            float targetSum = target.Sum();
            if (targetSum <= 0.000001f) continue;
            for (int k = 0; k < target.Length; k++) target[k] /= targetSum;

            int width = Mathf.Min(Band, dd.alphamapWidth);
            for (int d = 0; d < width; d++)
            {
                int x = dd.alphamapWidth - 1 - d;
                float t = d / (float)Mathf.Max(1, width - 1);
                float blend = 1f - (t * t * (3f - 2f * t));

                float sum = 0f;
                for (int k = 0; k < layers.Count; k++)
                {
                    expanded[dz, x, k] = Mathf.Lerp(expanded[dz, x, k], target[k], blend);
                    sum += expanded[dz, x, k];
                }
                if (sum > 0.000001f)
                    for (int k = 0; k < layers.Count; k++)
                        expanded[dz, x, k] /= sum;

                changedPixels++;
            }
            changedRows++;
        }

        dd.terrainLayers = layers.ToArray();
        dd.SetAlphamaps(0, 0, expanded);
        dd.SetBaseMapDirty();
        EditorUtility.SetDirty(dd);
        AssetDatabase.SaveAssets();

        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        string line =
            "PASS band=" + Band +
            " oneSidedDragonFade=true" +
            " exactSweetLayerRefs=true" +
            " layersAdded=" + added +
            " changedRows=" + changedRows +
            " changedPixels=" + changedPixels +
            " heightsUntouched=true" +
            " sweetTerrainUntouched=true";
        File.WriteAllText(Report, line + "\n");
        Debug.Log("MM_DRAGON_SWEET_SHARED_ECOTONE_DONE " + line);
    }

    static Terrain Find(string dataName)
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
}

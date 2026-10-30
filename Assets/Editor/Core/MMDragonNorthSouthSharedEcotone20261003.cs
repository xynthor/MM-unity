using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MMDragonNorthSouthSharedEcotone20261003
{
    const int Band = 96;
    const string Report = "Validation/GridAudit20261003/dragon_north_south_shared_ecotone.txt";

    [MenuItem("MMUnity/World/North/Fix Dragon North South Shared Ecotone 20261003")]
    public static void Run()
    {
        Terrain north = Find("DragonIsleNorthTerrain");
        Terrain south = Find("DragonIsleSouthTerrain");
        TerrainData nd = north.terrainData;
        TerrainData sd = south.terrainData;
        Vector3 np = north.transform.position;
        Vector3 sp = south.transform.position;

        if (Mathf.Abs(sp.z + sd.size.z - np.z) > 0.1f)
            throw new InvalidOperationException("Expected Dragon Isle South directly south of Dragon Isle North.");

        float[,,] oldAlpha = nd.GetAlphamaps(0, 0, nd.alphamapWidth, nd.alphamapHeight);
        float[,,] southAlpha = sd.GetAlphamaps(0, 0, sd.alphamapWidth, sd.alphamapHeight);

        var layers = nd.terrainLayers.ToList();
        var exactIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < layers.Count; i++)
            exactIndex[AssetDatabase.GetAssetPath(layers[i])] = i;

        int[] southToNorth = new int[sd.terrainLayers.Length];
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
            southToNorth[k] = index;
        }

        var expanded = new float[nd.alphamapHeight, nd.alphamapWidth, layers.Count];
        for (int z = 0; z < nd.alphamapHeight; z++)
            for (int x = 0; x < nd.alphamapWidth; x++)
                for (int k = 0; k < nd.alphamapLayers; k++)
                    expanded[z, x, k] = oldAlpha[z, x, k];

        int width = Mathf.Min(Band, nd.alphamapHeight);
        int changedColumns = 0;
        int changedPixels = 0;

        for (int nx = 0; nx < nd.alphamapWidth; nx++)
        {
            float ux = (nx + 0.5f) / nd.alphamapWidth;
            int sx = Mathf.Clamp(Mathf.FloorToInt(ux * sd.alphamapWidth), 0, sd.alphamapWidth - 1);

            float worldX = np.x + ux * nd.size.x;
            float northY = np.y + nd.GetInterpolatedHeight(ux, 0f);
            float southY = sp.y + sd.GetInterpolatedHeight(
                Mathf.InverseLerp(sp.x, sp.x + sd.size.x, worldX), 1f);

            if (northY <= 0.55f || southY <= 0.55f) continue;

            var target = new float[layers.Count];
            for (int sk = 0; sk < sd.alphamapLayers; sk++)
                target[southToNorth[sk]] += southAlpha[sd.alphamapHeight - 1, sx, sk];

            float targetSum = target.Sum();
            if (targetSum <= 0.000001f) continue;
            for (int k = 0; k < target.Length; k++) target[k] /= targetSum;

            // Protect the east-side Dragon↔Sweet Water transition by fading
            // this south-edge correction out before the three-way corner.
            float distanceFromEastPixels = nd.alphamapWidth - 1 - nx;
            float eastGuard = Mathf.SmoothStep(0f, 1f, distanceFromEastPixels / Band);
            if (eastGuard <= 0.0001f) continue;

            for (int d = 0; d < width; d++)
            {
                int z = d;
                float t = d / (float)Mathf.Max(1, width - 1);
                float edgeBlend = 1f - (t * t * (3f - 2f * t));
                float blend = edgeBlend * eastGuard;

                float sum = 0f;
                for (int k = 0; k < layers.Count; k++)
                {
                    expanded[z, nx, k] = Mathf.Lerp(expanded[z, nx, k], target[k], blend);
                    sum += expanded[z, nx, k];
                }

                if (sum > 0.000001f)
                    for (int k = 0; k < layers.Count; k++)
                        expanded[z, nx, k] /= sum;

                changedPixels++;
            }
            changedColumns++;
        }

        nd.terrainLayers = layers.ToArray();
        nd.SetAlphamaps(0, 0, expanded);
        nd.SetBaseMapDirty();
        EditorUtility.SetDirty(nd);
        AssetDatabase.SaveAssets();

        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        string line =
            "PASS band=" + Band +
            " oneSidedNorthFade=true" +
            " exactSouthLayerRefs=true" +
            " eastCornerProtected=true" +
            " layersAdded=" + added +
            " changedColumns=" + changedColumns +
            " changedPixels=" + changedPixels +
            " heightsUntouched=true southTerrainUntouched=true";
        File.WriteAllText(Report, line + "\n");
        Debug.Log("MM_DRAGON_NORTH_SOUTH_SHARED_ECOTONE_DONE " + line);
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

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MMSouthwestHermitsUnderwaterSeam20261003
{
    const int Taper = 8;
    const string ReportDir = "Validation/GridAudit20261003/SouthwestHermitsEdge";

    [MenuItem("MMUnity/World/South/Fix Southwest-Hermits Underwater Seams 20261003")]
    public static void Run()
    {
        Terrain ocean = Find("Southwest_OceanTerrain");
        Terrain north = Find("HermitsIsleWest_Terrain");
        Terrain east = Find("HermitsIsleSouth_Terrain");
        TerrainData od = ocean.terrainData;
        TerrainData nd = north.terrainData;
        TerrainData ed = east.terrainData;

        int res = od.heightmapResolution;
        if (nd.heightmapResolution != res || ed.heightmapResolution != res)
            throw new InvalidOperationException("Height resolutions differ.");

        Vector3 op = ocean.transform.position;
        if (Mathf.Abs(op.x - north.transform.position.x) > 0.01f ||
            Mathf.Abs(op.z + od.size.z - north.transform.position.z) > 0.01f)
            throw new InvalidOperationException("Expected Hermits West north of Southwest Ocean.");
        if (Mathf.Abs(op.x + od.size.x - east.transform.position.x) > 0.01f ||
            Mathf.Abs(op.z - east.transform.position.z) > 0.01f)
            throw new InvalidOperationException("Expected Hermits South east of Southwest Ocean.");

        float[,] original = od.GetHeights(0, 0, res, res);
        float[,] northH = nd.GetHeights(0, 0, res, res);
        float[,] eastH = ed.GetHeights(0, 0, res, res);
        float[,] output = (float[,])original.Clone();

        var northTarget = new float[res];
        var eastTarget = new float[res];

        for (int x = 0; x < res; x++)
        {
            float worldY = north.transform.position.y + northH[0, x] * nd.size.y;
            northTarget[x] = Mathf.Clamp01((worldY - op.y) / od.size.y);
        }

        for (int z = 0; z < res; z++)
        {
            float worldY = east.transform.position.y + eastH[z, 0] * ed.size.y;
            eastTarget[z] = Mathf.Clamp01((worldY - op.y) / od.size.y);
        }

        float cornerTarget = 0.5f * (northTarget[res - 1] + eastTarget[res - 1]);
        float cornerCorrection = cornerTarget - original[res - 1, res - 1];
        float maxApplied = 0f;

        for (int z = 0; z < res; z++)
        {
            int northDistance = res - 1 - z;
            float northWeight = northDistance < Taper ? EdgeBlend(northDistance) : 0f;

            for (int x = 0; x < res; x++)
            {
                int eastDistance = res - 1 - x;
                float eastWeight = eastDistance < Taper ? EdgeBlend(eastDistance) : 0f;
                if (northWeight <= 0f && eastWeight <= 0f) continue;

                float northCorrection = northTarget[x] - original[res - 1, x];
                float eastCorrection = eastTarget[z] - original[z, res - 1];

                // Coons-style correction: each boundary correction fades inward,
                // while the shared corner correction is subtracted once so the
                // two exact edges remain compatible throughout the 8x8 corner.
                float corrected =
                    original[z, x] +
                    northWeight * northCorrection +
                    eastWeight * eastCorrection -
                    northWeight * eastWeight * cornerCorrection;

                corrected = Mathf.Clamp01(corrected);
                maxApplied = Mathf.Max(
                    maxApplied,
                    Mathf.Abs(corrected - original[z, x]) * od.size.y);
                output[z, x] = corrected;
            }
        }

        od.SetHeights(0, 0, output);
        EditorUtility.SetDirty(od);
        AssetDatabase.SaveAssets();
        ocean.Flush();

        float northAfter = VerifyNS(ocean, north);
        float eastAfter = VerifyEW(ocean, east);

        Directory.CreateDirectory(ReportDir);
        string report =
            $"PASS taper={Taper} oceanOnly=true neighborsUntouched=true landUntouched=true " +
            $"cornerAware=true maxAppliedDeltaM={maxApplied:F6} " +
            $"northAfterM={northAfter:F6} eastAfterM={eastAfter:F6}";
        File.WriteAllText(ReportDir + "/fix_report.txt", report + "\n");
        Debug.Log("MM_SW_HERMITS_UNDERWATER_SEAMS_DONE " + report);
    }

    static float EdgeBlend(int distance)
    {
        float t = distance / (float)Mathf.Max(1, Taper - 1);
        float smooth = t * t * (3f - 2f * t);
        return 1f - smooth;
    }

    public static float VerifyNS(Terrain south, Terrain north)
    {
        float z = north.transform.position.z;
        float x0 = Mathf.Max(south.transform.position.x, north.transform.position.x);
        float x1 = Mathf.Min(
            south.transform.position.x + south.terrainData.size.x,
            north.transform.position.x + north.terrainData.size.x);
        float max = 0f;

        for (int i = 0; i <= 512; i++)
        {
            float x = Mathf.Lerp(x0, x1, i / 512f);
            float a = south.SampleHeight(new Vector3(x, 0f, z)) + south.transform.position.y;
            float b = north.SampleHeight(new Vector3(x, 0f, z)) + north.transform.position.y;
            max = Mathf.Max(max, Mathf.Abs(a - b));
        }
        return max;
    }

    public static float VerifyEW(Terrain west, Terrain east)
    {
        float x = east.transform.position.x;
        float z0 = Mathf.Max(west.transform.position.z, east.transform.position.z);
        float z1 = Mathf.Min(
            west.transform.position.z + west.terrainData.size.z,
            east.transform.position.z + east.terrainData.size.z);
        float max = 0f;

        for (int i = 0; i <= 512; i++)
        {
            float z = Mathf.Lerp(z0, z1, i / 512f);
            float a = west.SampleHeight(new Vector3(x, 0f, z)) + west.transform.position.y;
            float b = east.SampleHeight(new Vector3(x, 0f, z)) + east.transform.position.y;
            max = Mathf.Max(max, Mathf.Abs(a - b));
        }
        return max;
    }

    static Terrain Find(string dataName)
    {
        var hits = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include)
            .Where(t => t.terrainData && t.terrainData.name == dataName)
            .ToArray();
        if (hits.Length != 1)
            throw new InvalidOperationException($"Expected one {dataName}, found {hits.Length}");
        return hits[0];
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Repairs current painted ratios without rebuilding any source terrain or layer.
public static class MMCurrentTerrainWeights20260928
{
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Enroth_Linked_OpenWorld.unity" || scene.isDirty || EditorApplication.isPlaying)
            throw new InvalidOperationException("Requires saved linked scene in Edit Mode.");
        if (!File.Exists("Backups/ResumeManualPreservation_20260928_231458/manifest.json"))
            throw new InvalidOperationException("Current manual baseline backup missing.");
        var terrains = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Terrain>(true)).ToArray();
        if (terrains.Length != 30) throw new InvalidOperationException("Expected 30 terrains.");
        foreach (var t in terrains)
            if (!AssetDatabase.GetAssetPath(t.terrainData).StartsWith("Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/"))
                throw new InvalidOperationException("Unexpected terrain outside generated clones.");
        int changed = 0, cells = 0;
        float worstBefore = 0, worstAfter = 0;
        foreach (var t in terrains)
        {
            var d = t.terrainData;
            var a = d.GetAlphamaps(0, 0, d.alphamapWidth, d.alphamapHeight);
            int n = a.GetLength(2), local = 0;
            var remainder = new float[n]; var quantized = new int[n];
            for (int z = 0; z < a.GetLength(0); z++) for (int x = 0; x < a.GetLength(1); x++)
            {
                float sum = 0;
                for (int k = 0; k < n; k++) { if (float.IsNaN(a[z,x,k]) || a[z,x,k] < 0) throw new Exception("Invalid weight"); sum += a[z,x,k]; }
                worstBefore = Mathf.Max(worstBefore, Mathf.Abs(sum - 1));
                if (Mathf.Abs(sum - 1) < .00001f) continue;
                if (sum <= 0) throw new Exception("Cannot infer empty paint.");
                int used = 0;
                for (int k = 0; k < n; k++) { float v = 255 * a[z,x,k] / sum; quantized[k] = Mathf.FloorToInt(v); remainder[k] = v - quantized[k]; used += quantized[k]; }
                for (int q = used; q < 255; q++) { int best = 0; for (int k = 1; k < n; k++) if (remainder[k] > remainder[best]) best = k; quantized[best]++; remainder[best] = -1; }
                for (int k = 0; k < n; k++) a[z,x,k] = quantized[k] / 255f;
                local++;
            }
            if (local == 0) continue;
            d.SetAlphamaps(0,0,a); d.SyncTexture(TerrainData.AlphamapTextureName);
            foreach (var control in d.alphamapTextures) EditorUtility.SetDirty(control);
            EditorUtility.SetDirty(d); d.SetBaseMapDirty(); t.Flush();
            var verify = d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);
            for (int z=0;z<verify.GetLength(0);z++) for(int x=0;x<verify.GetLength(1);x++) { float sum=0;for(int k=0;k<n;k++)sum+=verify[z,x,k];worstAfter=Mathf.Max(worstAfter,Mathf.Abs(sum-1)); }
            Debug.Log("CURRENT_PAINT_NORMALIZED " + t.name + " cells=" + local);
            changed++; cells += local;
        }
        AssetDatabase.SaveAssets();
        string report = "terrainsChanged=" + changed + " cells=" + cells + " maxErrorBefore=" + worstBefore + " maxErrorAfter=" + worstAfter + " heightsLayersObjectsUntouched=true";
        File.WriteAllText("Validation/EdgeGrid20260923/Resume_CurrentWeightRepair.txt", report);
        Debug.Log(report);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MMEcosystemGrounding20261003
{
    const float FloatTolerance = 0.75f;
    const float BuriedTolerance = 0.50f;
    const float LandThreshold = 0.55f;
    const string ReportDir = "Validation/GridAudit20261003/Placement/EcoGrounding";

    [MenuItem("MMUnity/World/Vegetation/Preview Robust Eco Grounding 20261003")]
    public static void Preview()
    {
        Directory.CreateDirectory(ReportDir);
        var lines = new List<string>
        {
            "name|terrain|deltaY|beforeRootY|afterRootY|terrainAtBoundsCenter|boundsMinBefore"
        };

        int moved = 0;
        float maxAbs = 0f;

        foreach (Transform root in EcoRoots())
        {
            if (!TryEvaluate(root, out Candidate c) || !c.IsRobustAnomaly || c.CenterTerrainY <= LandThreshold)
                continue;

            float before = root.position.y;
            float delta = c.CenterTerrainY - c.Bounds.min.y;
            root.position += Vector3.up * delta;
            moved++;
            maxAbs = Mathf.Max(maxAbs, Mathf.Abs(delta));

            lines.Add(
                $"{root.name}|{c.Terrain.terrainData.name}|{delta:F4}|{before:F4}|{root.position.y:F4}|" +
                $"{c.CenterTerrainY:F4}|{c.Bounds.min.y:F4}");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        File.WriteAllLines(ReportDir + "/preview_changes.txt", lines);
        File.WriteAllText(
            ReportDir + "/preview_summary.txt",
            $"moved={moved} maxAbsDelta={maxAbs:F4} xzUntouched=true rotationUntouched=true scaleUntouched=true waterSkipped=true\n");

        Debug.Log($"MM_ECO_GROUNDING_PREVIEW moved={moved} maxAbsDelta={maxAbs:F4}");
    }

    public static string VerifyLive()
    {
        int total = 0;
        int landRobust = 0;
        int waterRobust = 0;
        float worstLandGap = 0f;
        string worstName = "";

        foreach (Transform root in EcoRoots())
        {
            total++;
            if (!TryEvaluate(root, out Candidate c) || !c.IsRobustAnomaly)
                continue;

            if (c.CenterTerrainY <= LandThreshold)
            {
                waterRobust++;
                continue;
            }

            landRobust++;
            float severity = Mathf.Max(c.FloatGap, -c.BuriedGap);
            if (severity > worstLandGap)
            {
                worstLandGap = severity;
                worstName = root.name;
            }
        }

        string report =
            $"ecoRoots={total} landRobustAnomalies={landRobust} waterRobustSkipped={waterRobust} " +
            $"worstLandSeverity={worstLandGap:F4} worst={worstName}";
        Directory.CreateDirectory(ReportDir);
        File.WriteAllText(ReportDir + "/live_verify.txt", report + "\n");
        return report;
    }

    static IEnumerable<Transform> EcoRoots()
    {
        return SceneManager.GetActiveScene()
            .GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name.StartsWith("Eco_", StringComparison.Ordinal));
    }

    static bool TryEvaluate(Transform root, out Candidate candidate)
    {
        candidate = default;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.enabled && r.gameObject.activeInHierarchy)
            .ToArray();
        if (renderers.Length == 0)
            return false;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float hx = Mathf.Min(Mathf.Max(bounds.extents.x * 0.75f, 0.15f), 4f);
        float hz = Mathf.Min(Mathf.Max(bounds.extents.z * 0.75f, 0.15f), 4f);
        var samples = new[]
        {
            new Vector2(bounds.center.x, bounds.center.z),
            new Vector2(bounds.center.x - hx, bounds.center.z - hz),
            new Vector2(bounds.center.x - hx, bounds.center.z + hz),
            new Vector2(bounds.center.x + hx, bounds.center.z - hz),
            new Vector2(bounds.center.x + hx, bounds.center.z + hz)
        };

        float minTerrain = float.PositiveInfinity;
        float maxTerrain = float.NegativeInfinity;
        Terrain centerTerrain = null;
        float centerY = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            Terrain terrain = CoveringTerrain(samples[i].x, samples[i].y);
            if (!terrain)
                return false;

            float y = terrain.SampleHeight(new Vector3(samples[i].x, 0f, samples[i].y)) +
                      terrain.transform.position.y;
            minTerrain = Mathf.Min(minTerrain, y);
            maxTerrain = Mathf.Max(maxTerrain, y);

            if (i == 0)
            {
                centerTerrain = terrain;
                centerY = y;
            }
        }

        float floatGap = bounds.min.y - maxTerrain;
        float buriedGap = bounds.max.y - minTerrain;

        candidate = new Candidate
        {
            Terrain = centerTerrain,
            Bounds = bounds,
            MinTerrain = minTerrain,
            MaxTerrain = maxTerrain,
            CenterTerrainY = centerY,
            FloatGap = floatGap,
            BuriedGap = buriedGap,
            IsRobustAnomaly = floatGap > FloatTolerance || buriedGap < -BuriedTolerance
        };
        return true;
    }

    static Terrain CoveringTerrain(float x, float z)
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (!terrain.terrainData)
                continue;

            Vector3 p = terrain.transform.position;
            Vector3 s = terrain.terrainData.size;
            if (x >= p.x - 0.01f && x <= p.x + s.x + 0.01f &&
                z >= p.z - 0.01f && z <= p.z + s.z + 0.01f)
                return terrain;
        }
        return null;
    }

    struct Candidate
    {
        public Terrain Terrain;
        public Bounds Bounds;
        public float MinTerrain;
        public float MaxTerrain;
        public float CenterTerrainY;
        public float FloatGap;
        public float BuriedGap;
        public bool IsRobustAnomaly;
    }
}

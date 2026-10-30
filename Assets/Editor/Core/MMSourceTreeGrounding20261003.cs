using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MMSourceTreeGrounding20261003
{
    public const float Tolerance = 0.75f;
    const string ReportDir = "Validation/GridAudit20261003/Placement/SourceTreeGrounding";

    [MenuItem("MMUnity/World/Vegetation/Preview Source Tree Grounding 20261003")]
    public static void Preview()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("No loaded active scene.");

        Terrain[] terrains = Terrain.activeTerrains;
        Transform[] roots = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Where(t => t.gameObject.activeInHierarchy &&
                        t.name.StartsWith("SourceTree_", StringComparison.Ordinal))
            .ToArray();

        var lines = new List<string>();
        int movedDown = 0;
        int movedUp = 0;
        float maxDown = 0f;
        float maxUp = 0f;

        foreach (Transform root in roots)
        {
            Terrain terrain = TerrainAt(terrains, root.position);
            if (!terrain || !TryBounds(root, out Bounds bounds))
                continue;

            float terrainY =
                terrain.SampleHeight(new Vector3(root.position.x, 0f, root.position.z)) +
                terrain.transform.position.y;

            float gap = bounds.min.y - terrainY;
            if (Mathf.Abs(gap) <= Tolerance)
                continue;

            Vector3 before = root.position;
            root.position += Vector3.up * -gap;

            if (gap > 0f)
            {
                movedDown++;
                maxDown = Mathf.Max(maxDown, gap);
            }
            else
            {
                movedUp++;
                maxUp = Mathf.Max(maxUp, -gap);
            }

            lines.Add(
                terrain.terrainData.name + "|" +
                root.name + "|" +
                "deltaY=" + (-gap).ToString("F3") + "|" +
                "before=" + before.ToString("F3") + "|" +
                "after=" + root.position.ToString("F3"));
        }

        if (movedDown + movedUp > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        Directory.CreateDirectory(ReportDir);
        lines.Insert(0,
            "PREVIEW ONLY sceneUnsaved=true xzUntouched=true rotationsUntouched=true " +
            "movedDown=" + movedDown +
            " movedUp=" + movedUp +
            " maxDown=" + maxDown.ToString("F3") +
            " maxUp=" + maxUp.ToString("F3"));

        File.WriteAllLines(ReportDir + "/preview_changes.txt", lines);

        Debug.Log(
            "MM_SOURCE_TREE_GROUNDING_PREVIEW " +
            "movedDown=" + movedDown +
            " movedUp=" + movedUp +
            " maxDown=" + maxDown.ToString("F3") +
            " maxUp=" + maxUp.ToString("F3") +
            " sceneDirty=" + scene.isDirty);
    }

    public static string VerifyLive()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        Scene scene = SceneManager.GetActiveScene();

        Transform[] roots = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Where(t => t.gameObject.activeInHierarchy &&
                        t.name.StartsWith("SourceTree_", StringComparison.Ordinal))
            .ToArray();

        int overTolerance = 0;
        float worst = 0f;

        foreach (Transform root in roots)
        {
            Terrain terrain = TerrainAt(terrains, root.position);
            if (!terrain || !TryBounds(root, out Bounds bounds))
                continue;

            float terrainY =
                terrain.SampleHeight(new Vector3(root.position.x, 0f, root.position.z)) +
                terrain.transform.position.y;

            float gap = bounds.min.y - terrainY;
            if (Mathf.Abs(gap) > Tolerance)
            {
                overTolerance++;
                worst = Mathf.Max(worst, Mathf.Abs(gap));
            }
        }

        string report =
            "sourceTrees=" + roots.Length +
            " overTolerance=" + overTolerance +
            " worstAbsGap=" + worst.ToString("F4") +
            " sceneDirty=" + scene.isDirty;

        Directory.CreateDirectory(ReportDir);
        File.WriteAllText(ReportDir + "/live_verify.txt", report + "\n");
        return report;
    }

    static Terrain TerrainAt(Terrain[] terrains, Vector3 position)
    {
        foreach (Terrain terrain in terrains)
        {
            if (!terrain || !terrain.terrainData)
                continue;

            Vector3 p = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;

            if (position.x >= p.x && position.x <= p.x + size.x &&
                position.z >= p.z && position.z <= p.z + size.z)
                return terrain;
        }

        return null;
    }

    static bool TryBounds(Transform root, out Bounds bounds)
    {
        Renderer[] renderers = root
            .GetComponentsInChildren<Renderer>(true)
            .Where(r => r.enabled)
            .ToArray();

        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return true;
    }
}

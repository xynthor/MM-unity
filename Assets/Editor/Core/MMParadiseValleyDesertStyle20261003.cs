using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMParadiseValleyDesertStyle20261003
{
    const string ScenePath = "Assets/Scenes/World/Enroth.unity";
    const string ParentName = "Paradise Valley - LINKED REFERENCE";
    const string SourcePath = "Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/0b418e44aca4e28419de75491345a173.asset";
    const string Root = "Assets/World/WorldExtensions/Generated/DesertStyle20261001";
    const string TerrainPath = Root + "/ParadiseValley_Terrain.asset";
    const string LayerPath = Root + "/ParadiseValley_DragonIsleSand.terrainlayer";
    const string TemplatePath = "Assets/World/WorldExtensions/Generated/LinkedWorldTexturePhase20260929/93b2b2aa74ad40546ae5b165f4ff17c0_1.terrainlayer";

    static float Smooth(float a, float b, float x)
    {
        float q = Mathf.Clamp01((x - a) / Mathf.Max(.001f, b - a));
        return q * q * (3f - 2f * q);
    }

    static int FindLayer(TerrainData d, string textureName)
    {
        for (int i = 0; i < d.terrainLayers.Length; i++)
        {
            var l = d.terrainLayers[i];
            if (!l || !l.diffuseTexture) continue;
            if (AssetDatabase.GetAssetPath(l.diffuseTexture)
                .IndexOf(textureName, StringComparison.OrdinalIgnoreCase) >= 0)
                return i;
        }
        return -1;
    }

    static TerrainLayer GetDragonSand(Vector3 terrainPosition)
    {
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerPath);
        if (layer) return layer;

        var template = AssetDatabase.LoadAssetAtPath<TerrainLayer>(TemplatePath);
        if (!template) throw new Exception("Dragon Isle sand template missing");

        layer = UnityEngine.Object.Instantiate(template);
        layer.name = "ParadiseValley_DragonIsleSand";
        layer.tileSize = new Vector2(9f, 9f);
        layer.tileOffset = new Vector2(
            Mathf.Repeat(terrainPosition.x, 9f),
            Mathf.Repeat(terrainPosition.z, 9f));
        AssetDatabase.CreateAsset(layer, LayerPath);
        return layer;
    }

    [MenuItem("MM/World/Paradise Valley/Apply Dragon Isle Desert Style 20261003")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var parent = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == ParentName);
        if (!parent) throw new Exception("Paradise Valley parent missing");

        var terrain = parent.GetComponentInChildren<Terrain>(true);
        if (!terrain) throw new Exception("Paradise Valley terrain missing");

        var src = AssetDatabase.LoadAssetAtPath<TerrainData>(SourcePath);
        if (!src) throw new Exception("Paradise Valley source TerrainData missing");

        var dst = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
        if (!dst)
        {
            dst = UnityEngine.Object.Instantiate(src);
            dst.name = "ParadiseValley_Terrain";
            AssetDatabase.CreateAsset(dst, TerrainPath);
        }

        if (dst.heightmapResolution != src.heightmapResolution ||
            dst.alphamapResolution != src.alphamapResolution ||
            dst.alphamapLayers != src.alphamapLayers)
            throw new Exception("Paradise Valley desert clone incompatible");

        dst.SetHeights(0, 0, src.GetHeights(0, 0,
            src.heightmapResolution, src.heightmapResolution));
        dst.terrainLayers = src.terrainLayers;
        var alpha = src.GetAlphamaps(0, 0, src.alphamapWidth, src.alphamapHeight);

        int sand = FindLayer(dst, "coast_sand_02_diff_1k.jpg");
        int dry = FindLayer(dst, "dry_soil_CH_Opaque.png");
        if (sand < 0 || dry < 0) throw new Exception("Required Paradise Valley layers missing");

        for (int z = 0; z < src.alphamapHeight; z++)
        for (int x = 0; x < src.alphamapWidth; x++)
        {
            float edge = Mathf.Min(
                Mathf.Min(Smooth(0, 64, x), Smooth(0, 64, src.alphamapWidth - 1 - x)),
                Mathf.Min(Smooth(0, 64, z), Smooth(0, 64, src.alphamapHeight - 1 - z)));
            float u = (x + .5f) / src.alphamapWidth;
            float v = (z + .5f) / src.alphamapHeight;
            float worldY = terrain.transform.position.y + dst.GetInterpolatedHeight(u, v);
            if (worldY <= .55f) continue;

            float shift = alpha[z, x, dry] * .47f * edge;
            alpha[z, x, dry] -= shift;
            alpha[z, x, sand] += shift;
        }

        dst.SetAlphamaps(0, 0, alpha);
        var layers = dst.terrainLayers.ToArray();
        layers[sand] = GetDragonSand(terrain.transform.position);
        dst.terrainLayers = layers;
        dst.SetBaseMapDirty();
        EditorUtility.SetDirty(dst);

        terrain.terrainData = dst;
        var collider = terrain.GetComponent<TerrainCollider>();
        if (collider) collider.terrainData = dst;

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Enroth");
        AssetDatabase.SaveAssets();
        Debug.Log("PARADISE_VALLEY_DRAGON_DESERT_DONE");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Play-only visual correction for a verified cyan protrusion from the inherited
// alpine surface at the Frozen Highlands ice coast. Authored geometry, terrain,
// coastline, water, ice floes, colliders and the serialized scene stay unchanged.
[DisallowMultipleComponent]
public sealed class MMFrozenCoastOverlayContactFix : MonoBehaviour
{
    const string RootName = "MM_FrozenCoastOverlayContactFix";
    const string TerrainName = "Terrain_FrozenHighlands_North";
    const string TargetName = "North Alpine Rock Surface 2 20261007";
    const string TargetShader = "MMUnity/NorthAlpineSurface20261007";

    MeshFilter target;
    Mesh originalMesh;
    Mesh displayMesh;

    public int RemovedTriangles { get; private set; }
    public bool Applied { get; private set; }
    public static MMFrozenCoastOverlayContactFix Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (SceneManager.GetActiveScene().name != "Enroth" || GameObject.Find(RootName))
            return;

        Terrain terrain = Terrain.activeTerrains.FirstOrDefault(t =>
            t && t.name == TerrainName && t.terrainData);
        if (!terrain) return;

        MeshRenderer renderer = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
            FindObjectsInactive.Exclude).FirstOrDefault(r =>
            r && r.name == TargetName && r.enabled && r.sharedMaterial &&
            r.sharedMaterial.shader && r.sharedMaterial.shader.name == TargetShader);

        if (!renderer) return;
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        if (!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable) return;

        Mesh source = filter.sharedMesh;
        // Fail closed if a future geometry pass changes the authored overlay.
        if (source.name != "Surface2" || source.vertexCount != 263169 ||
            source.subMeshCount != 1 || source.triangles.Length != 524288 * 3)
            return;

        int[] indices = source.triangles;
        Vector3[] vertices = source.vertices;
        Matrix4x4 matrix = filter.transform.localToWorldMatrix;
        var keep = new List<int>(indices.Length);
        int removed = 0;

        for (int i = 0; i < indices.Length; i += 3)
        {
            int a = indices[i], b = indices[i + 1], c = indices[i + 2];
            Vector3 center = matrix.MultiplyPoint3x4(
                (vertices[a] + vertices[b] + vertices[c]) / 3f);

            // Exact 408 upper-surface triangles causing the conspicuous
            // cyan sea/snow seam, based on five matched route-camera tests.
            // Lower alpine faces and every other region are preserved.
            bool seam = center.x >= -170f && center.x <= -30f &&
                center.z >= 1225f && center.z <= 1264f &&
                center.y > 34f && center.y <= 45f;
            if (seam) { removed++; continue; }

            keep.Add(a);
            keep.Add(b);
            keep.Add(c);
        }

        if (removed != 408) return;

        Mesh visual = Instantiate(source);
        visual.name = "Frozen coastal alpine contact (runtime)";
        visual.hideFlags = HideFlags.DontSave;
        visual.SetTriangles(keep, 0, false);

        var root = new GameObject(RootName);
        // A normal root is automatically destroyed on leaving Play Mode.
        // DontSave is appropriate only for the dynamically cloned mesh.
        var fix = root.AddComponent<MMFrozenCoastOverlayContactFix>();
        fix.target = filter;
        fix.originalMesh = source;
        fix.displayMesh = visual;
        fix.RemovedTriangles = removed;
        filter.sharedMesh = visual;
        fix.Applied = true;
        Instance = fix;
    }

    void OnDestroy()
    {
        if (target && originalMesh && target.sharedMesh == displayMesh)
            target.sharedMesh = originalMesh;

        if (displayMesh)
        {
            if (Application.isPlaying) Destroy(displayMesh);
            else DestroyImmediate(displayMesh);
        }

        if (Instance == this) Instance = null;
        Applied = false;
    }
}

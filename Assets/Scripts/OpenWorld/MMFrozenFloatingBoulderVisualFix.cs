using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runtime-only visual correction for one mesh-verified, fully detached Frozen
// cliff boulder. Leave the authored object, original transform, terrain,
// materials and colliders unchanged; automatically restore visible renderers.
[DisallowMultipleComponent]
public sealed class MMFrozenFloatingBoulderVisualFix : MonoBehaviour
{
    const string RootName = "MM_FrozenFloatingBoulderVisualFix";
    const string BoulderName = "AuthoredBoulder_24";
    const string TerrainName = "Terrain_FrozenHighlands_North";
    static readonly Vector3 ExpectedPosition = new Vector3(-143.31f, 35.88f, 1081.29f);

    Renderer[] renderers;
    bool[] previous;
    public bool Applied { get; private set; }
    public float MinimumClearance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (SceneManager.GetActiveScene().name != "Enroth" ||
            GameObject.Find(RootName))
            return;

        var terrain = Terrain.activeTerrains.FirstOrDefault(t =>
            t && t.name == TerrainName && t.terrainData);
        if (!terrain) return;

        var boulder = FindObjectsByType<Transform>(FindObjectsInactive.Exclude)
            .FirstOrDefault(t => t.name == BoulderName &&
                Vector3.Distance(t.position, ExpectedPosition) < 1f);
        if (!boulder) return;

        var sourceMesh = boulder.GetComponentsInChildren<MeshFilter>(true)
            .FirstOrDefault(f => f.sharedMesh);
        if (!sourceMesh) return;

        // Guard against hiding a boulder that a future author has properly
        // grounded. All 4,179 original LOD0 vertices were above terrain when
        // this defect was measured. A future grounded mesh must stay visible.
        float minGap = float.MaxValue;
        foreach (Vector3 local in sourceMesh.sharedMesh.vertices)
        {
            var world = sourceMesh.transform.TransformPoint(local);
            float ground = terrain.SampleHeight(world) + terrain.transform.position.y;
            minGap = Mathf.Min(minGap, world.y - ground);
            if (minGap <= .30f) return;
        }

        GameObject root = new GameObject(RootName);
        var fix = root.AddComponent<MMFrozenFloatingBoulderVisualFix>();
        fix.MinimumClearance = minGap;
        fix.Apply(boulder);
    }

    void Apply(Transform source)
    {
        renderers = source.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Destroy(gameObject);
            return;
        }

        previous = renderers.Select(r => r && r.enabled).ToArray();
        foreach (Renderer renderer in renderers)
            if (renderer) renderer.enabled = false;

        Applied = true;
    }

    void OnDestroy()
    {
        if (!Applied || renderers == null || previous == null)
            return;

        for (int i = 0; i < renderers.Length && i < previous.Length; i++)
            if (renderers[i]) renderers[i].enabled = previous[i];

        Applied = false;
    }
}

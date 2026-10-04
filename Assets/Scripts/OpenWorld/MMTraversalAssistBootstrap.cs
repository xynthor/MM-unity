using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MMTraversalAssistBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        var scene = SceneManager.GetActiveScene();
        var stair = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.gameObject.activeInHierarchy && t.name == "041_M040_T2stairsS");

        if (!stair)
            return;

        Collider source = stair.GetComponentInChildren<Collider>(true);
        if (!source)
            return;

        var root = new GameObject("MM_TraversalAssists");
        Object.DontDestroyOnLoad(root);
        Bounds b = source.bounds;

        // Replace only this coarse stair collider; the tower/building uses
        // separate collision. Keeping the imported stair MeshCollider active
        // would still block the capsule through the smooth assist ramps.
        source.enabled = false;

        // This imported stair mesh exposes an oversized first riser. The two
        // visual side flights rise toward +Z. World-space ramps begin before
        // the riser, ride just above the stair treads and stop before the
        // blocked central doorway / upper wall.
        float laneOffset = b.extents.x * 0.60f;
        float centerZ = b.center.z - 0.38f;
        float centerY = b.min.y + 0.64f;

        CreateWorldRamp(
            root.transform,
            "MM_TraversalRamp_Left",
            new Vector3(b.center.x - laneOffset, centerY, centerZ),
            new Vector3(1.55f, 0.16f, 3.85f),
            -18.5f);

        CreateWorldRamp(
            root.transform,
            "MM_TraversalRamp_Right",
            new Vector3(b.center.x + laneOffset, centerY, centerZ),
            new Vector3(1.55f, 0.16f, 3.85f),
            -18.5f);

        // Preserve the upper landing surface that belonged to the disabled
        // stair mesh collider, while leaving the doorway/walls to their own
        // existing architecture colliders.
        CreateWorldRamp(
            root.transform,
            "MM_TraversalLanding",
            new Vector3(b.center.x, b.max.y - 0.08f, b.max.z - 0.55f),
            new Vector3(b.size.x * 0.72f, 0.16f, 1.10f),
            0f);
    }

    static void CreateWorldRamp(
        Transform parent,
        string name,
        Vector3 worldPosition,
        Vector3 size,
        float xRotation)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = worldPosition;
        go.transform.rotation = Quaternion.Euler(xRotation, 0f, 0f);

        var box = go.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = size;
    }
}

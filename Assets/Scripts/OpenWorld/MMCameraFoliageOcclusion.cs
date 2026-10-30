using System;
using System.Collections.Generic;
using UnityEngine;

// Cosmetic camera-only foliage clearance. Does not change vegetation transforms,
// colliders, materials, LOD selection, terrain or any scene asset.
[DefaultExecutionOrder(1000)]
public sealed class MMCameraFoliageOcclusion : MonoBehaviour
{
    public Camera playerCamera;

    const float CellSize = 32f;
    const float ExitMargin = 0.5f;
    const int MaxCellsPerRenderer = 64;

    readonly Dictionary<long, List<Renderer>> spatialCells = new Dictionary<long, List<Renderer>>();
    readonly Dictionary<Renderer, bool> originalForceOff = new Dictionary<Renderer, bool>();
    readonly HashSet<Renderer> candidates = new HashSet<Renderer>();
    readonly HashSet<Renderer> wanted = new HashSet<Renderer>();
    readonly List<Renderer> restoreBuffer = new List<Renderer>();

    public int IndexedRendererCount { get; private set; }
    public int HiddenRendererCount => originalForceOff.Count;
    public int IndexedCellCount => spatialCells.Count;

    static long CellKey(int x, int z) => ((long)x << 32) | (uint)z;

    static bool IsCameraClearanceFoliage(Renderer renderer)
    {
        // Restrict to already-existing natural tree families; never hide
        // buildings, bridges, stairs, foundations, ruins or rock geometry.
        int depth = 0;
        for (Transform t = renderer.transform; t && depth++ < 14; t = t.parent)
        {
            string n = t.name;
            if (n.StartsWith("Searsia_", StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("ReferenceTallPine_", StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("SourceTree_", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    void Start() => BuildIndex();

    void BuildIndex()
    {
        spatialCells.Clear();
        IndexedRendererCount = 0;
        if (!playerCamera) return;

        foreach (var renderer in UnityEngine.Object.FindObjectsByType<Renderer>(
                     FindObjectsInactive.Exclude))
        {
            if (!renderer || !renderer.gameObject.activeInHierarchy ||
                !IsCameraClearanceFoliage(renderer))
                continue;

            Bounds b = renderer.bounds;
            if (b.size.x <= 0f || b.size.z <= 0f)
                continue;

            int minX = Mathf.FloorToInt((b.min.x - ExitMargin) / CellSize);
            int maxX = Mathf.FloorToInt((b.max.x + ExitMargin) / CellSize);
            int minZ = Mathf.FloorToInt((b.min.z - ExitMargin) / CellSize);
            int maxZ = Mathf.FloorToInt((b.max.z + ExitMargin) / CellSize);

            long cellCount = (long)(maxX - minX + 1) * (maxZ - minZ + 1);
            if (cellCount <= 0 || cellCount > MaxCellsPerRenderer)
                continue;

            for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                long key = CellKey(x, z);
                if (!spatialCells.TryGetValue(key, out var list))
                    spatialCells[key] = list = new List<Renderer>();
                list.Add(renderer);
            }

            IndexedRendererCount++;
        }
    }

    void LateUpdate()
    {
        if (!playerCamera) return;
        Vector3 cameraPosition = playerCamera.transform.position;
        // Unity domain reload can restore primitive fields without restoring
        // the nonserialized spatial dictionary; rebuild it if necessary.
        if (spatialCells.Count == 0) BuildIndex();

        // Bounds are indexed at Start; the nearby search stays local even in
        // the 30-tile Enroth scene with tens of thousands of renderers.
        int cx = Mathf.FloorToInt(cameraPosition.x / CellSize);
        int cz = Mathf.FloorToInt(cameraPosition.z / CellSize);
        candidates.Clear();
        wanted.Clear();

        for (int x = cx - 1; x <= cx + 1; x++)
        for (int z = cz - 1; z <= cz + 1; z++)
        {
            if (!spatialCells.TryGetValue(CellKey(x, z), out var list))
                continue;
            foreach (var renderer in list)
                if (renderer) candidates.Add(renderer);
        }

        foreach (var renderer in candidates)
        {
            if (!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            Bounds b = renderer.bounds;
            bool nearPreviouslyHidden = originalForceOff.ContainsKey(renderer) &&
                                        b.SqrDistance(cameraPosition) <= ExitMargin * ExitMargin;
            if (!b.Contains(cameraPosition) && !nearPreviouslyHidden)
                continue;

            wanted.Add(renderer);
            if (originalForceOff.ContainsKey(renderer))
                continue;

            originalForceOff.Add(renderer, renderer.forceRenderingOff);
            renderer.forceRenderingOff = true;
        }

        restoreBuffer.Clear();
        foreach (var pair in originalForceOff)
            if (!wanted.Contains(pair.Key)) restoreBuffer.Add(pair.Key);

        foreach (var renderer in restoreBuffer)
        {
            if (renderer) renderer.forceRenderingOff = originalForceOff[renderer];
            originalForceOff.Remove(renderer);
        }
    }

    void OnDisable() => RestoreAll();
    void OnDestroy() => RestoreAll();

    void RestoreAll()
    {
        foreach (var pair in originalForceOff)
            if (pair.Key) pair.Key.forceRenderingOff = pair.Value;
        originalForceOff.Clear();
        candidates.Clear();
        wanted.Clear();
        restoreBuffer.Clear();
    }
}

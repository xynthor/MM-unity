using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MMRegionWorldStreamer : MonoBehaviour
{
    [Serializable]
    public class Region
    {
        public string id;
        public string sceneName;
        public string up;
        public string down;
        public string left;
        public string right;
        public bool mirrorX;
    }

    public Transform player;
    public Region[] regions;
    public string startRegion = "New Sorpigal";
    public float edge = 252f;
    public float inset = 10f;
    public float dryThreshold = 0.35f;

    readonly Dictionary<string,Region> map = new Dictionary<string,Region>(StringComparer.OrdinalIgnoreCase);
    Region current;
    bool busy;

    void Start()
    {
        foreach (var r in regions) if (r != null && !string.IsNullOrEmpty(r.id)) map[r.id] = r;
        StartCoroutine(SwitchRegion(startRegion, null, true));
    }

    void Update()
    {
        if (busy || current == null || !player) return;
        string logical = EdgeDirection(current, player.position);
        if (logical == null) return;
        string target = Neighbor(current, logical);
        if (string.IsNullOrEmpty(target)) { ClampInside(); return; }
        StartCoroutine(SwitchRegion(target, Opposite(logical), false));
    }

    string EdgeDirection(Region r, Vector3 p)
    {
        if (p.x <= -edge) return "left";
        if (p.x >=  edge) return "right";
        if (p.z >=  edge) return "up";
        if (p.z <= -edge) return "down";
        return null;
    }

    string Neighbor(Region r, string d)
    {
        if (d == "up") return r.up;
        if (d == "down") return r.down;
        if (d == "left") return r.left;
        if (d == "right") return r.right;
        return null;
    }

    static string Opposite(string d)
    {
        if (d == "up") return "down";
        if (d == "down") return "up";
        if (d == "left") return "right";
        if (d == "right") return "left";
        return null;
    }

    void ClampInside()
    {
        var p = player.position;
        p.x = Mathf.Clamp(p.x, -edge + 4f, edge - 4f);
        p.z = Mathf.Clamp(p.z, -edge + 4f, edge - 4f);
        player.position = p;
    }

    IEnumerator SwitchRegion(string id, string incomingLogicalEdge, bool initial)
    {
        if (busy || !map.TryGetValue(id, out var next)) yield break;
        busy = true;
        var controller = player ? player.GetComponent<MMThirdPersonController>() : null;
        if (controller) controller.enabled = false;

        Scene nextScene = SceneManager.GetSceneByName(next.sceneName);
        if (!nextScene.IsValid() || !nextScene.isLoaded)
        {
            var load = SceneManager.LoadSceneAsync(next.sceneName, LoadSceneMode.Additive);
            if (load == null) { busy = false; if (controller) controller.enabled = true; yield break; }
            yield return load;
            nextScene = SceneManager.GetSceneByName(next.sceneName);
        }

        GameObject root = PrepareRegion(nextScene);
        if (!root) { busy = false; if (controller) controller.enabled = true; yield break; }

        if (!initial && incomingLogicalEdge != null)
            PlaceAtEntry(next, root, incomingLogicalEdge);

        if (current != null && !string.Equals(current.sceneName, next.sceneName, StringComparison.OrdinalIgnoreCase))
        {
            var old = SceneManager.GetSceneByName(current.sceneName);
            if (old.IsValid() && old.isLoaded)
            {
                var unload = SceneManager.UnloadSceneAsync(old);
                if (unload != null) yield return unload;
            }
        }

        current = next;
        if (controller) controller.enabled = true;
        busy = false;
        Debug.Log($"REGION_WORLD_ACTIVE {current.id} scene={current.sceneName} mirrorX={current.mirrorX}");
    }

    GameObject PrepareRegion(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        var root = roots.FirstOrDefault(g => g.name.IndexOf("Open World", StringComparison.OrdinalIgnoreCase) >= 0) ?? roots.FirstOrDefault();
        if (!root) return null;
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        foreach (var p in root.GetComponentsInChildren<MMThirdPersonController>(true)) p.gameObject.SetActive(false);
        foreach (var c in root.GetComponentsInChildren<Camera>(true)) c.gameObject.SetActive(false);
        foreach (var a in root.GetComponentsInChildren<AudioListener>(true)) a.enabled = false;
        return root;
    }

    void PlaceAtEntry(Region r, GameObject root, string logicalEdge)
    {
        float x = Mathf.Clamp(player.position.x, -edge + 40f, edge - 40f);
        float z = Mathf.Clamp(player.position.z, -edge + 40f, edge - 40f);
        if (logicalEdge == "left")  x = -edge + inset;
        if (logicalEdge == "right") x =  edge - inset;
        if (logicalEdge == "up")    z =  edge - inset;
        if (logicalEdge == "down")  z = -edge + inset;

        var terrain = root.GetComponentInChildren<Terrain>(true);
        if (terrain)
        {
            Vector3 best = FindDryEntry(terrain, new Vector3(x, 0f, z), logicalEdge);
            player.position = best;
        }
        else player.position = new Vector3(x, player.position.y, z);
    }

    Vector3 FindDryEntry(Terrain t, Vector3 basePos, string logicalEdge)
    {
        Vector3 best = basePos;
        float bestY = float.NegativeInfinity;
        for (int i = 0; i < 15; i++)
        {
            int k = i == 0 ? 0 : ((i + 1) / 2) * (i % 2 == 1 ? 1 : -1);
            Vector3 p = basePos;
            if (logicalEdge == "left" || logicalEdge == "right") p.z = Mathf.Clamp(basePos.z + k * 20f, -edge + 28f, edge - 28f);
            else p.x = Mathf.Clamp(basePos.x + k * 20f, -edge + 28f, edge - 28f);
            float y = t.SampleHeight(p) + t.transform.position.y;
            if (y > bestY) { bestY = y; best = p; }
            if (y > dryThreshold) { bestY = y; best = p; break; }
        }
        best.y = bestY + 0.38f;
        return best;
    }
}

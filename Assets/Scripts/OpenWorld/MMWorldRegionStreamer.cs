using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MMWorldRegionStreamer : MonoBehaviour
{
    [System.Serializable]
    public class Region
    {
        public string sceneName;
        public Vector3 offset;
        public bool keepTerrain;
    }

    public Transform player;
    public Region[] regions;
    public float loadDistance = 2700f;
    public float unloadDistance = 3400f;
    public float checkInterval = 0.5f;

    readonly HashSet<string> loaded = new HashSet<string>();
    readonly HashSet<string> loading = new HashSet<string>();

    void Start()
    {
        StartCoroutine(StreamLoop());
    }

    IEnumerator StreamLoop()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (true)
        {
            if (player && regions != null)
            {
                foreach (var r in regions)
                {
                    float d = Vector2.Distance(
                        new Vector2(player.position.x, player.position.z),
                        new Vector2(r.offset.x + 768f, r.offset.z + 768f));
                    if (d <= loadDistance && !loaded.Contains(r.sceneName) && !loading.Contains(r.sceneName))
                        StartCoroutine(LoadRegion(r));
                    else if (d >= unloadDistance && loaded.Contains(r.sceneName))
                        StartCoroutine(UnloadRegion(r));
                }
            }
            yield return wait;
        }
    }

    IEnumerator LoadRegion(Region r)
    {
        loading.Add(r.sceneName);
        var op = SceneManager.LoadSceneAsync(r.sceneName, LoadSceneMode.Additive);
        if (op == null) { loading.Remove(r.sceneName); yield break; }
        yield return op;
        var scene = SceneManager.GetSceneByName(r.sceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            var roots = scene.GetRootGameObjects();
            var root = roots.FirstOrDefault(g => g.name.IndexOf("Open World", System.StringComparison.OrdinalIgnoreCase) >= 0)
                       ?? roots.FirstOrDefault();
            if (root)
            {
                root.transform.position += r.offset;
                foreach (var t in root.GetComponentsInChildren<Terrain>(true))
                {
                    if (!r.keepTerrain)
                    {
                        t.enabled = false;
                        var tc = t.GetComponent<TerrainCollider>();
                        if (tc) tc.enabled = false;
                    }
                    else
                    {
                        t.enabled = true;
                        var tc = t.GetComponent<TerrainCollider>();
                        if (tc) tc.enabled = true;
                    }
                }
                foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                    if (tr.name.StartsWith("Water -", System.StringComparison.OrdinalIgnoreCase)) tr.gameObject.SetActive(false);
                foreach (var c in root.GetComponentsInChildren<MMThirdPersonController>(true)) c.gameObject.SetActive(false);
                foreach (var c in root.GetComponentsInChildren<Camera>(true)) c.gameObject.SetActive(false);
                foreach (var a in root.GetComponentsInChildren<AudioListener>(true)) a.enabled = false;
                foreach (var l in root.GetComponentsInChildren<Light>(true)) l.enabled = false;
            }
            loaded.Add(r.sceneName);
        }
        loading.Remove(r.sceneName);
    }

    IEnumerator UnloadRegion(Region r)
    {
        loaded.Remove(r.sceneName);
        var scene = SceneManager.GetSceneByName(r.sceneName);
        if (scene.IsValid() && scene.isLoaded)
        {
            var op = SceneManager.UnloadSceneAsync(scene);
            if (op != null) yield return op;
        }
    }
}

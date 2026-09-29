using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MMUnity.OpenWorld
{
    /// <summary>
    /// Keeps the public repository runnable when optional/full-fidelity art packs are absent.
    /// Private/original-game assets are intentionally not redistributed with the repository.
    /// </summary>
    public static class MMPublicAssetFallback
    {
        private static Material _fallbackMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            for (int i = 0; i < SceneManager.sceneCount; i++)
                RepairScene(SceneManager.GetSceneAt(i));
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RepairScene(scene);
        }

        private static Material FallbackMaterial
        {
            get
            {
                if (_fallbackMaterial != null)
                    return _fallbackMaterial;

                var shader = Shader.Find("Standard")
                             ?? Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("HDRP/Lit")
                             ?? Shader.Find("Unlit/Color");

                if (shader == null)
                    return null;

                _fallbackMaterial = new Material(shader)
                {
                    name = "MM Public Fallback Material",
                    color = new Color(0.42f, 0.48f, 0.38f, 1f)
                };
                Object.DontDestroyOnLoad(_fallbackMaterial);
                return _fallbackMaterial;
            }
        }

        private static void RepairScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            var roots = scene.GetRootGameObjects();
            bool publicBaseline = !HasPrivateSourceAssets();

            if (publicBaseline && scene.name == "Enroth")
                DisableSourceSceneStreaming(roots);

            int disabledMissingMeshes = 0;
            int repairedMaterialSlots = 0;
            int disabledMissingSkinnedMeshes = 0;
            int disabledBrokenAnimators = 0;

            foreach (var root in roots)
            {
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh != null)
                        continue;

                    var renderer = filter.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.enabled)
                    {
                        renderer.enabled = false;
                        disabledMissingMeshes++;
                    }
                }

                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var skinned = renderer as SkinnedMeshRenderer;
                    if (skinned != null && skinned.sharedMesh == null)
                    {
                        if (skinned.enabled)
                        {
                            skinned.enabled = false;
                            disabledMissingSkinnedMeshes++;
                        }
                        continue;
                    }

                    var materials = renderer.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] != null)
                            continue;

                        materials[i] = FallbackMaterial;
                        changed = true;
                        repairedMaterialSlots++;
                    }

                    if (changed && FallbackMaterial != null)
                        renderer.sharedMaterials = materials;
                }

                foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                {
                    if (animator.runtimeAnimatorController != null)
                        continue;

                    animator.enabled = false;
                    disabledBrokenAnimators++;
                }

                foreach (var controller in root.GetComponentsInChildren<global::MMThirdPersonController>(true))
                    if (controller.gameObject.activeInHierarchy)
                        EnsurePlayerVisual(controller.gameObject);
            }

            if (disabledMissingMeshes + repairedMaterialSlots + disabledMissingSkinnedMeshes + disabledBrokenAnimators > 0)
            {
                Debug.Log(
                    $"MM Public Fallback [{scene.name}]: disabled {disabledMissingMeshes} unavailable mesh renderers, " +
                    $"disabled {disabledMissingSkinnedMeshes} unavailable skinned renderers, repaired {repairedMaterialSlots} material slots, " +
                    $"disabled {disabledBrokenAnimators} animators without controllers.");
            }
        }

        private static bool HasPrivateSourceAssets()
        {
            return Directory.Exists(Path.Combine(Application.dataPath, "MMOriginal"))
                   || Directory.Exists(Path.Combine(Application.dataPath, "EnvironmentAssets"));
        }

        private static void DisableSourceSceneStreaming(GameObject[] roots)
        {
            int disabled = 0;

            foreach (var root in roots)
            {
                foreach (var streamer in root.GetComponentsInChildren<global::MMWorldRegionStreamer>(true))
                {
                    if (!streamer.enabled)
                        continue;
                    streamer.enabled = false;
                    disabled++;
                }

                foreach (var streamer in root.GetComponentsInChildren<global::MMRegionWorldStreamer>(true))
                {
                    if (!streamer.enabled)
                        continue;
                    streamer.enabled = false;
                    disabled++;
                }
            }

            if (disabled > 0)
                Debug.Log($"MM Public Fallback [Enroth]: disabled {disabled} source-scene streamer(s); using integrated world.");
        }

        private static void EnsurePlayerVisual(GameObject player)
        {
            var validRenderer = player.GetComponentsInChildren<Renderer>(true)
                .Any(r =>
                {
                    if (r is SkinnedMeshRenderer skin)
                        return skin.sharedMesh != null;
                    var filter = r.GetComponent<MeshFilter>();
                    return filter == null || filter.sharedMesh != null;
                });

            if (validRenderer || player.transform.Find("PublicFallback_PlayerVisual") != null)
                return;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "PublicFallback_PlayerVisual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);

            var collider = visual.GetComponent<Collider>();
            if (collider != null)
                Object.Destroy(collider);

            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null && FallbackMaterial != null)
                renderer.sharedMaterial = FallbackMaterial;
        }
    }
}

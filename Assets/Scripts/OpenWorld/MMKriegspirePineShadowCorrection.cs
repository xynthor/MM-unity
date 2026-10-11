using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Fix one verified oversized polygonal shadow cast by a particular
// high-detail northern pine, without moving the tree or changing its materials.
// Other pine LODs and every other tree retain their existing shadow settings.
public static class MMKriegspirePineShadowCorrection
{
    public const string SourceTree = "AuthoredPine_831";
    public const string OffendingRenderer = "M_LOD0";
    public static Renderer CorrectedRenderer { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Apply()
    {
        if (SceneManager.GetActiveScene().name != "Enroth")
            return;

        Transform tree = SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(x => x.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(x => x.name == SourceTree &&
                Mathf.Abs(x.position.x + 483.55f) < 0.5f &&
                Mathf.Abs(x.position.z - 1042.25f) < 0.5f);

        if (!tree) return;

        var renderer = tree.GetComponentsInChildren<Renderer>(true)
            .FirstOrDefault(x => x.name == OffendingRenderer);
        if (!renderer) return;

        // The full mesh remains visually present; only its defective
        // close LOD shadow is suppressed. Its remaining LODs are preserved.
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        CorrectedRenderer = renderer;
    }
}

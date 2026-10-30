using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Preview the runtime sea reflection without serializing editor helpers.</summary>
[InitializeOnLoad]
public static class MMWaterReflectionPreview20261007
{
    static MMWaterReflectionPreview20261007()
    {
        EditorApplication.delayCall += Attach;
        EditorSceneManager.sceneOpened += (_, __) => Attach();
        EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Attach; };
    }
    static void Attach()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var ocean = GameObject.Find("Linked connected sea and existing lowland waters 20260929");
        if (ocean && !ocean.GetComponent<MMPlanarWaterReflection>())
            ocean.AddComponent<MMPlanarWaterReflection>().hideFlags = HideFlags.DontSaveInEditor;
    }
}

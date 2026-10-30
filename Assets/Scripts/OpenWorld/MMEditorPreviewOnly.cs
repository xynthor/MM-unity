using UnityEngine;

public class MMEditorPreviewOnly : MonoBehaviour
{
    [Tooltip("Keep the saved reconstruction active in the linked world. Other preview scenes retain their original behavior.")]
    public bool keepActiveInPlayMode;
    void Awake()
    {
        if (Application.isPlaying && !keepActiveInPlayMode)
            gameObject.SetActive(false);
    }
}

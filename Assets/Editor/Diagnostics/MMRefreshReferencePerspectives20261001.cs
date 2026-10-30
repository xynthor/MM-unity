using UnityEditor.SceneManagement;
public static class MMRefreshReferencePerspectives20261001
{
 public static void Run(){
  EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  MMReferenceDetailPerspectiveQA20260925.CaptureAll();
 }
}
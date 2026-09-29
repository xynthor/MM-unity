using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public static class CaptureAuditScreens
{
    static readonly string[] Scenes={
        "NewSorpigal","CastleIronfist","MireOfTheDamned","Dragonsand","HermitsIsle",
        "MistyIslands","BootlegBay","FreeHaven","Blackshire","ParadiseValley",
        "EelInfestedWaters","SilverCove","FrozenHighlands","Kriegspire","SweetWater","Enroth"};
    [MenuItem("MMUnity/Capture Fresh Audit Screens")]
    public static void Run()
    {
        Directory.CreateDirectory("C:/MMUnityPort/Validation/AuditScreens");
        foreach(var sn in Scenes) Capture(sn);
        Debug.Log("AUDIT_SCREENSHOTS_DONE count="+Scenes.Length);
    }
    static void Capture(string sn)
    {
        string path="Assets/Scenes/"+sn+".unity";
        var sc=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        bool linked=sn=="Enroth";
        var go=new GameObject("__AUDIT_CAMERA"); SceneManager.MoveGameObjectToScene(go,sc);
        var cam=go.AddComponent<Camera>(); cam.orthographic=true; cam.orthographicSize=linked?1100f:300f;
        cam.transform.position=linked?new Vector3(-256f,2400f,256f):new Vector3(0,650,0); cam.transform.rotation=Quaternion.Euler(90,0,0);
        cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.08f,.11f,.12f); cam.farClipPlane=linked?4000f:1200f;
        int rw=linked?1400:1024,rh=linked?850:1024;var rt=new RenderTexture(rw,rh,24); cam.targetTexture=rt; cam.Render();
        RenderTexture.active=rt; var tex=new Texture2D(rw,rh,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,rw,rh),0,0); tex.Apply();
        File.WriteAllBytes("C:/MMUnityPort/Validation/AuditScreens/"+sn+".png",tex.EncodeToPNG());
        RenderTexture.active=null; cam.targetTexture=null;
        Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
    }
}

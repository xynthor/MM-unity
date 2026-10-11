using System;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class MMNorthSq01SavedCoastalWaterScreenshot20261011{
 static void Shot(string path,Vector3 pos,Vector3 target,bool ortho,float size){
  var go=new GameObject("TEMP_SAVED_COVE_SHOT");var c=go.AddComponent<Camera>();
  c.transform.position=pos;c.transform.rotation=Quaternion.LookRotation(target-pos,Vector3.up);
  c.orthographic=ortho;if(ortho)c.orthographicSize=size;else c.fieldOfView=size;
  c.nearClipPlane=.1f;c.farClipPlane=3500;c.clearFlags=CameraClearFlags.Skybox;
  var old=RenderTexture.active;var rt=new RenderTexture(1250,850,24);c.targetTexture=rt;c.Render();RenderTexture.active=rt;
  var image=new Texture2D(1250,850,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1250,850),0,0);image.Apply();
  File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=old;c.targetTexture=null;
  UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);
 }
 public static void Run(){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);if(s.isDirty)throw new Exception("Scene already dirty");
  for(int i=0;i<2;i++){
   var go=GameObject.Find("North Alpine Rock Surface "+i+" 20261007");
   if(!go||go.GetComponent<MeshRenderer>().sharedMaterial.shader.name!="MMUnity/NorthAlpineCoveFade20261011")throw new Exception("Saved shader not linked");
  }
  var patch=GameObject.Find("North Cove Seamless Water Extension 20261011");if(!patch||!patch.GetComponent<MeshFilter>()||AssetDatabase.GetAssetPath(patch.GetComponent<MeshFilter>().sharedMesh)!="Assets/World/WorldExtensions/Generated/NorthCoastWater20261011/CoveGapV2.asset")throw new Exception("Saved coast water patch missing or wrong");
  string p="Preview/NorthSq01CoastalWaterSaved20261011";Directory.CreateDirectory(p);
  Shot(p+"/top.png",new Vector3(-771,410,1192),new Vector3(-771,0,1192),true,158);
  Shot(p+"/sea.png",new Vector3(-768,125,1390),new Vector3(-768,9,1194),false,55);
  Shot(p+"/low.png",new Vector3(-750,70,1275),new Vector3(-770,9,1192),false,63);
  Debug.Log("SQ01_COASTAL_WATER_SAVED_RENDER_PASS sceneDirty="+s.isDirty);
 }
}

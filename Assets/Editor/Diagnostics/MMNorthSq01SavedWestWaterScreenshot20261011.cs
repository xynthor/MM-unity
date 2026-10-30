using System;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
public static class MMNorthSq01SavedWestWaterScreenshot20261011{
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
  var west=GameObject.Find("North West Cove Continuous Water Extension 20261011");if(!west||!west.GetComponent<MeshFilter>()||AssetDatabase.GetAssetPath(west.GetComponent<MeshFilter>().sharedMesh)!="Assets/World/WorldExtensions/Generated/NorthWestCoveWater20261011/WestCoveW5Safe.asset")throw new Exception("Saved west water patch missing");
  string p="Preview/NorthSq01WestWaterSaved20261011";Directory.CreateDirectory(p);
  Shot(p+"/top.png",new Vector3(-875,340,1221),new Vector3(-875,0,1221),true,94);
  Shot(p+"/sea.png",new Vector3(-875,110,1372),new Vector3(-875,9,1225),false,55);
  Shot(p+"/low.png",new Vector3(-855,65,1325),new Vector3(-870,8,1218),false,63);
  Debug.Log("SQ01_WEST_WATER_SAVED_RENDER_PASS sceneDirty="+s.isDirty);
 }
}

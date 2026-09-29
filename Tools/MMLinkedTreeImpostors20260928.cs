using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MMLinkedTreeImpostors20260928
{
 const string Root="Assets/World/WorldExtensions/Generated/LinkedTreeImpostors";
 static string Key(Mesh mesh,Material[] materials) { string guid;long id;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out guid,out id);return guid+"_"+id+"_"+Hash128.Compute(string.Join("|",materials.Select(m=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m))))).ToString().Substring(0,8); }
 static Material Bake(MeshFilter source,Scene scratch,Camera camera,RenderTexture rt)
 {
  string key=Key(source.sharedMesh,source.GetComponent<Renderer>().sharedMaterials);
  string path=Root+"/"+key+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing)return existing;
  var go=new GameObject("TEMP Bake Tree");SceneManager.MoveGameObjectToScene(go,scratch);go.layer=31;
  go.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;
  var bounds=source.sharedMesh.bounds;float size=Mathf.Max(bounds.size.y,new Vector2(bounds.size.x,bounds.size.z).magnitude)*1.1f;
  var atlas=new Texture2D(1024,128,TextureFormat.RGBA32,false);var image=new Texture2D(128,128,TextureFormat.RGBA32,false);var previous=RenderTexture.active;
  try {
   camera.orthographicSize=size/2;camera.nearClipPlane=.01f;camera.farClipPlane=size*6+1;
   for(int i=0;i<8;i++) {float angle=i*Mathf.PI/4;camera.transform.position=bounds.center+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*size*2;camera.transform.LookAt(bounds.center);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();atlas.SetPixels(i*128,0,128,128,image.GetPixels());}
   atlas.Apply();File.WriteAllBytes(Root+"/"+key+".png",atlas.EncodeToPNG());
  } finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(atlas);UnityEngine.Object.DestroyImmediate(image);}
  AssetDatabase.ImportAsset(Root+"/"+key+".png",ImportAssetOptions.ForceSynchronousImport);
  var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/"+key+".png");importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.3f;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  var material=new Material(Shader.Find("MMUnity/Linked Far Tree Impostor"));material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/"+key+".png");material.SetVector("_Center",bounds.center);material.SetFloat("_Size",size);material.enableInstancing=true;AssetDatabase.CreateAsset(material,path);return material;
 }
 public static void Apply()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Saved linked Edit Mode scene required");
  if(!File.Exists("Backups/BeforeFarTreeImpostors_20260928/manifest.json"))throw new Exception("Backup required");
  if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","LinkedTreeImpostors");
  var groups=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true)).Where(g=>g.transform.Find("Linked distance LOD 2")&&g.GetComponent<MeshFilter>()).ToArray();
  var originalLights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();var enabled=originalLights.Select(l=>l.enabled).ToArray();
  var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var ambientMode=RenderSettings.ambientMode;var ambientColor=RenderSettings.ambientLight;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.46f,.46f,.46f);
  foreach(var originalLight in originalLights)originalLight.enabled=false;
  var cameraGO=new GameObject("TEMP Impostor Camera");SceneManager.MoveGameObjectToScene(cameraGO,scratch);var camera=cameraGO.AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;camera.allowHDR=false;camera.allowMSAA=false;
  var lightGO=new GameObject("TEMP Impostor Sun");SceneManager.MoveGameObjectToScene(lightGO,scratch);var light=lightGO.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.7f;light.cullingMask=1<<31;lightGO.transform.rotation=Quaternion.Euler(43,318,0);
  var rt=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;
  var materials=new Dictionary<string,Material>();
  try {foreach(var group in groups){var f=group.GetComponent<MeshFilter>();string key=Key(f.sharedMesh,f.GetComponent<Renderer>().sharedMaterials);if(!materials.ContainsKey(key))materials[key]=Bake(f,scratch,camera,rt);}}
  finally {for(int i=0;i<originalLights.Length;i++)originalLights[i].enabled=enabled[i];RenderSettings.ambientMode=ambientMode;RenderSettings.ambientLight=ambientColor;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.CloseScene(scratch,true);SceneManager.SetActiveScene(scene);}
  foreach(var group in groups) {
   var f=group.GetComponent<MeshFilter>();string key=Key(f.sharedMesh,f.GetComponent<Renderer>().sharedMaterials);var material=materials[key];
   string meshPath=Root+"/"+key+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
   if(!mesh) {var center=f.sharedMesh.bounds.center;float size=material.GetFloat("_Size");mesh=new Mesh();mesh.name="Far tree quad";mesh.vertices=new[]{center+new Vector3(-size/2,-size/2,0),center+new Vector3(size/2,-size/2,0),center+new Vector3(size/2,size/2,0),center+new Vector3(-size/2,size/2,0)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.bounds=new Bounds(center,Vector3.one*size);AssetDatabase.CreateAsset(mesh,meshPath);}
   var far=group.transform.Find("Linked distance LOD 2");far.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=far.GetComponent<MeshRenderer>();renderer.sharedMaterials=new[]{material};renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
   var lods=group.GetLODs();if(lods.Length!=3)throw new Exception("Unexpected LOD count");lods[0].screenRelativeTransitionHeight=.4f;lods[1].screenRelativeTransitionHeight=.04f;lods[2].screenRelativeTransitionHeight=.0015f;group.SetLODs(lods);group.RecalculateBounds();
  }
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  File.WriteAllText("Validation/EdgeGrid20260923/Resume_TreeImpostors.txt","groups="+groups.Length+" sharedAtlases="+materials.Count+" farTrianglesPerRenderer=2 placementWrites=0 originalMeshWrites=0");
 }
}

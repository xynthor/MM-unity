using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityMeshSimplifier;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMLinkedTreeLOD20260928 {
 const string Root="Assets/World/WorldExtensions/Generated/LinkedTreeLOD";
 static Mesh Reduced(Mesh source,float quality) {
  string guid;long id;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out guid,out id);
  string path=Root+"/"+guid+"_"+id+"_"+Mathf.RoundToInt(quality*1000)+".asset";
  var cached=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(cached)return cached;
  var simp=new MeshSimplifier();simp.Initialize(source);simp.SimplifyMesh(quality);
  var mesh=simp.ToMesh();mesh.name=source.name+" Distance "+quality;
  mesh.RecalculateBounds();
  if(mesh.subMeshCount!=source.subMeshCount||mesh.vertexCount<3)throw new Exception("Invalid tree reduction "+source.name);
  AssetDatabase.CreateAsset(mesh,path);return mesh;
 }
 public static void Apply(Scene scene) {
  if(scene.name!="Enroth"||!File.Exists("Backups/BeforeLinkedTreeLOD_20260928/manifest.json"))throw new Exception("Linked world and backup required");
  if(!AssetDatabase.IsValidFolder(Root))AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","LinkedTreeLOD");
  var candidates=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).Where(f=>f.sharedMesh&&!f.GetComponentInParent<LODGroup>()).Where(f=>{
   var n=f.sharedMesh.name.ToLowerInvariant();return n.Contains("searsia")||n.Contains("jacaranda")||n.Contains("island_tree");
  }).Where(f=>f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled&&f.gameObject.activeInHierarchy).ToArray();
  var meshes=new Dictionary<Mesh,Mesh[]>();
  foreach(var mesh in candidates.Select(f=>f.sharedMesh).Distinct())meshes[mesh]=new[]{Reduced(mesh,.15f),Reduced(mesh,.025f)};
  foreach(var f in candidates){
   var original=f.GetComponent<MeshRenderer>();if(!original)continue;
   var renderers=new Renderer[3];renderers[0]=original;
   for(int i=0;i<2;i++){
    var child=new GameObject("Linked distance LOD "+(i+1));child.transform.SetParent(f.transform,false);child.layer=f.gameObject.layer;
    child.AddComponent<MeshFilter>().sharedMesh=meshes[f.sharedMesh][i];
    var r=child.AddComponent<MeshRenderer>();r.sharedMaterials=original.sharedMaterials;
    r.shadowCastingMode=original.shadowCastingMode;r.receiveShadows=original.receiveShadows;
    r.lightProbeUsage=original.lightProbeUsage;r.reflectionProbeUsage=original.reflectionProbeUsage;renderers[i+1]=r;
   }
   var group=f.gameObject.AddComponent<LODGroup>();
   group.SetLODs(new[]{new LOD(.65f,new[]{renderers[0]}),new LOD(.18f,new[]{renderers[1]}),new LOD(.003f,new[]{renderers[2]})});group.RecalculateBounds();
  }
  AssetDatabase.SaveAssets();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
  File.WriteAllText("Validation/EdgeGrid20260923/Takeover_TreeLOD_Result.txt","groups="+candidates.Length+" sharedSources="+meshes.Count+" originalMeshWrites=0 placementWrites=0\n"+string.Join("\n",meshes.Select(p=>p.Key.name+" triangles="+p.Key.triangles.Length/3+" -> "+p.Value[0].triangles.Length/3+" -> "+p.Value[1].triangles.Length/3)));
 }
}

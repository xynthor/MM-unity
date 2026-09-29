using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class MMRenderCostAudit20260929
{
 static string PathOf(Transform t){string p=t.name;while(t.parent){t=t.parent;p=t.name+"/"+p;}return p;}
 static string Category(MeshRenderer r,Mesh mesh){string p=PathOf(r.transform).ToLowerInvariant();if(r.sharedMaterials.Any(m=>m&&m.shader.name.ToLowerInvariant().Contains("water")))return "water";if(p.Contains("vegetation")||p.Contains("tree")||p.Contains("pine")||p.Contains("searsia")||p.Contains("jacaranda"))return "trees";if(p.Contains("bridge"))return "bridges";if(p.Contains("wall"))return "walls";if(AssetDatabase.GetAssetPath(mesh).Contains("/Objects/")||p.Contains("building"))return "houses";return "props";}
 public static void Run()
 {
  var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/World/Enroth.unity"||EditorApplication.isPlaying)throw new Exception("Linked Edit Mode scene required");
  Vector3 target=new Vector3(948,15,-602),position=target+new Vector3(-45,32,-60);Quaternion rotation=Quaternion.LookRotation(target-position);
  var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MMThirdPersonController>(true)).First(p=>p.gameObject.activeInHierarchy);
  float fov=player.playerCamera.fieldOfView,far=player.playerCamera.farClipPlane;
  var planes=GeometryUtility.CalculateFrustumPlanes(Matrix4x4.Perspective(fov,1.6f,.1f,far)*Matrix4x4.Scale(new Vector3(1,1,-1))*Matrix4x4.TRS(position,rotation,Vector3.one).inverse);
  var roots=scene.GetRootGameObjects();var selected=new HashSet<Renderer>();var controlled=new HashSet<Renderer>();var lodCounts=new int[8];
  foreach(var group in roots.SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true))){var lods=group.GetLODs();foreach(var lod in lods)foreach(var renderer in lod.renderers)if(renderer)controlled.Add(renderer);if(!group.enabled||!group.gameObject.activeInHierarchy)continue;Vector3 s=group.transform.lossyScale;float size=group.size*Mathf.Max(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z));float distance=Vector3.Distance(position,group.transform.TransformPoint(group.localReferencePoint));float height=size*QualitySettings.lodBias/(2*Mathf.Max(.01f,distance)*Mathf.Tan(fov*Mathf.Deg2Rad*.5f));for(int i=0;i<lods.Length;i++)if(height>=lods[i].screenRelativeTransitionHeight){foreach(var r in lods[i].renderers)if(r)selected.Add(r);lodCounts[Mathf.Min(i,7)]++;break;}}
  var sums=new Dictionary<string,long>();var counts=new Dictionary<string,int>();var meshes=new Dictionary<string,long>();var meshInstances=new Dictionary<string,int>();var duplicateKeys=new HashSet<string>();int duplicates=0,shadowCasters=0,visible=0;
  foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))){if(!r.gameObject.activeInHierarchy||!(controlled.Contains(r)?selected.Contains(r):r.enabled)||!GeometryUtility.TestPlanesAABB(planes,r.bounds))continue;var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;var mesh=f.sharedMesh;long triangles=0;for(int k=0;k<mesh.subMeshCount;k++)triangles+=(long)mesh.GetIndexCount(k)/3;string category=Category(r,mesh);if(!sums.ContainsKey(category)){sums[category]=0;counts[category]=0;}sums[category]+=triangles;counts[category]++;string key=AssetDatabase.GetAssetPath(mesh)+" | "+mesh.name;if(!meshes.ContainsKey(key)){meshes[key]=0;meshInstances[key]=0;}meshes[key]+=triangles;meshInstances[key]++;if(!duplicateKeys.Add(mesh.GetEntityId()+"|"+r.localToWorldMatrix.ToString("R")))duplicates++;if(r.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.Off)shadowCasters++;visible++;}
  var rows=new List<string>{"Estimated main-camera mesh triangles at exact runtime waypoint0; excludes terrain tessellation, shadow/depth passes and Editor SceneView. Not a GPU timer.","position="+position+" target="+target+" fov="+fov+" far="+far+" lodBias="+QualitySettings.lodBias,"frustumMeshRenderers="+visible+" shadowCasters="+shadowCasters+" duplicateMeshTransforms="+duplicates,"selectedLODGroupsAllScene="+string.Join(",",lodCounts.Select((n,i)=>"LOD"+i+":"+n))};
  rows.AddRange(sums.OrderByDescending(k=>k.Value).Select(k=>k.Key+" renderers="+counts[k.Key]+" mainPassTriangles="+k.Value));rows.Add("TOP MESH COSTS");rows.AddRange(meshes.OrderByDescending(k=>k.Value).Take(25).Select(k=>k.Value+" triangles / "+meshInstances[k.Key]+" instances / "+k.Key));
  rows.Add("activeTerrains="+Terrain.activeTerrains.Length+" frustumTerrains="+Terrain.activeTerrains.Count(t=>GeometryUtility.TestPlanesAABB(planes,new Bounds(t.transform.position+t.terrainData.bounds.center,t.terrainData.bounds.size))));rows.Add("activeParticleSystems="+roots.SelectMany(g=>g.GetComponentsInChildren<ParticleSystem>(false)).Count());rows.Add("shadowDistance="+QualitySettings.shadowDistance+" cascades="+QualitySettings.shadowCascades);
  File.WriteAllLines("Validation/EdgeGrid20260923/Resume_NS_RenderCost_20260929.txt",rows);Debug.Log(string.Join("\n",rows.Take(12)));
 }
}

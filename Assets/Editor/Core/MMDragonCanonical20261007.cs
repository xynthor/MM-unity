using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using static MMNorthAuthoredTools20261007;

// Reference-led additions inside the existing island. Coast, crater and mainland are protected.
public static class MMDragonCanonical20261007
{
 static readonly Vector4[][] Chains={
  new[]{new Vector4(-1724,546,25,24),new Vector4(-1710,582,39,30),new Vector4(-1720,626,28,28),new Vector4(-1686,682,38,32),new Vector4(-1656,727,25,30)},
  new[]{new Vector4(-1554,552,23,27),new Vector4(-1575,619,32,30),new Vector4(-1545,665,39,30),new Vector4(-1538,719,27,26)},
  new[]{new Vector4(-1656,759,32,30),new Vector4(-1640,814,51,35),new Vector4(-1616,866,62,34),new Vector4(-1595,925,53,32),new Vector4(-1550,970,39,31)},
  new[]{new Vector4(-1470,791,26,26),new Vector4(-1463,848,44,28),new Vector4(-1454,912,53,30),new Vector4(-1422,960,31,26)},
  new[]{new Vector4(-1595,925,53,20),new Vector4(-1550,937,36,26),new Vector4(-1514,944,17,24)},
  new[]{new Vector4(-1454,912,53,20),new Vector4(-1490,930,28,25),new Vector4(-1514,944,17,24)}
 };
 static bool Island(float x,float z){return z>=500&&z<=1260&&(z>=900||x< -1435);}
 static float Add(float x,float z,float y)
 {
  if(!Island(x,z)||y<.6f)return 0;
  float ridge=0;foreach(var c in Chains)ridge=Mathf.Max(ridge,Ridge(x,z,c));
  float lake=Vector2.Distance(new Vector2(x,z),new Vector2(-1523.5f,876.1f));
  float king=Vector2.Distance(new Vector2(x,z),new Vector2(-1618,590));
  float edge=S(0,18,Mathf.Min(x+1792,-1280-x))*S(490,525,z)*S(1280,1245,z);
  float detail=4*(Mathf.PerlinNoise(x*.067f+83,z*.067f+19)-.5f);
  return Mathf.Max(0,ridge+detail*S(4,20,ridge))*S(.6f,9,y)*S(36,57,lake)*S(22,40,king)*edge;
 }
 static GameObject Forest(Terrain[] ts)
 {
  var root=new GameObject("Dragon Isle Reference Forest 20261007");root.hideFlags=HideFlags.HideAndDontSave;
  var mature=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TempDragonTemplate/TracePine.prefab");var young=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TempDragonTemplate/TraceYoungPine.prefab");if(!mature||!young)throw new Exception("Missing production forest");
  var random=new System.Random(97131);int count=0;
  for(float z=514;z<975;z+=6.8f)for(float x=-1778;x< -1400;x+=6.8f)
  {
   float px=x+(float)random.NextDouble()*5-2.5f,pz=z+(float)random.NextDouble()*5-2.5f;if(!Island(px,pz))continue;
   var t=ts.First(t=>pz>=t.transform.position.z&&pz<t.transform.position.z+512);float y=Ground(t,px,pz),slope=t.terrainData.GetSteepness((px+1792)/512,(pz-t.transform.position.z)/512);
   if(y<2.5f||y>59||slope>31)continue;
   float lake=Vector2.Distance(new Vector2(px,pz),new Vector2(-1523.5f,876.1f));if(lake<37)continue;
   float king=Vector2.Distance(new Vector2(px,pz),new Vector2(-1618,590));if(king<37)continue;
   float belt=(1-S(900,978,pz))*(.42f+.68f*Mathf.PerlinNoise(px*.018f+7,pz*.018f+31))*(1-S(36,59,y));
   // Leave the central valley traversable; woodland concentrates on its sides.
   float valley=Mathf.Abs(px-(-1638+.26f*(pz-650)));if(pz<805)belt*=Mathf.Lerp(.26f,1,S(12,35,valley));
   if(random.NextDouble()>belt)continue;
   var source=count%4==0?young:mature;var tree=UnityEngine.Object.Instantiate(source,root.transform);tree.name="DragonAuthoredPine_"+count++;tree.hideFlags=HideFlags.HideAndDontSave;
   tree.transform.position=new Vector3(px,y,pz);tree.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0)*source.transform.rotation;tree.transform.localScale=source.transform.localScale*(1.15f+(float)random.NextDouble()*.38f);
   var managed=new HashSet<Renderer>();foreach(var group in tree.GetComponentsInChildren<LODGroup>()){var lods=group.GetLODs();var selected=lods.Take(2).ToArray();if(selected.Length>0){selected[selected.Length-1].screenRelativeTransitionHeight=.0015f;group.SetLODs(selected);foreach(var l in selected)foreach(var r in l.renderers)if(r)managed.Add(r);}}
   foreach(var r in tree.GetComponentsInChildren<Renderer>())if(!managed.Contains(r))r.gameObject.SetActive(false);
   var rs=tree.GetComponentsInChildren<Renderer>();if(rs.Length>0){var bounds=rs[0].bounds;foreach(var r in rs.Skip(1))bounds.Encapsulate(r.bounds);tree.transform.position+=Vector3.up*(y-bounds.min.y-.12f);}
  }
  return root;
 }
 static GameObject Rocks(Terrain[] ts)
 {
  var root=new GameObject("Dragon Isle Exposed Rock 20261007");root.hideFlags=HideFlags.HideAndDontSave;
  var material=new Material(Shader.Find("MMUnity/DragonReferenceRock20261007"));material.SetTexture("_Rock",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_c.png"));material.SetTexture("_RockNormal",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_n.png"));
  for(int i=0;i<ts.Length;i++){var t=ts[i];var d=t.terrainData;var vertices=new Vector3[513*513];var normals=new Vector3[vertices.Length];var tangents=new Vector4[vertices.Length];var tri=new List<int>();for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){int k=z*513+x;var n=d.GetInterpolatedNormal(x/512f,z/512f);vertices[k]=new Vector3(x,d.GetHeight(x,z)+.18f,z);normals[k]=n;var tx=new Vector3(n.y,-n.x,0).normalized;tangents[k]=new Vector4(tx.x,tx.y,tx.z,-1);}for(int z=0;z<512;z++)for(int x=0;x<512;x++){int a=z*513+x,b=a+1,c=a+513,e=c+1;if(!Island(-1792+x,t.transform.position.z+z)||vertices[a].y<26||vertices[b].y<26||vertices[c].y<26||vertices[e].y<26)continue;tri.Add(a);tri.Add(c);tri.Add(e);tri.Add(a);tri.Add(e);tri.Add(b);}var mesh=new UnityEngine.Mesh{name="DragonRockSurface"+i,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;mesh.SetTriangles(tri,0);mesh.RecalculateBounds();var go=new GameObject("DragonRockSurface"+i);go.hideFlags=HideFlags.HideAndDontSave;go.transform.SetParent(root.transform);go.transform.position=t.transform.position;go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
  return root;
 }
 public static void Run(bool keep=false)
 {
  var scene=SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Need clean Enroth");if(GameObject.Find("Dragon Isle Reference Forest 20261007"))throw new Exception("Already saved: inspect rather than cumulatively rebuild");
  var ts=Terrain.activeTerrains.Where(t=>t.transform.position.x==-1792&&(t.transform.position.z==256||t.transform.position.z==768)).OrderBy(t=>t.transform.position.z).ToArray();if(ts.Length!=2)throw new Exception("Expected two Dragon terrains");
  var original=ts.Select(t=>t.terrainData).ToArray();var clones=original.Select(d=>UnityEngine.Object.Instantiate(d)).ToArray();var moved=new List<Tuple<Transform,Vector3>>();var hidden=new List<GameObject>();GameObject forest=null,rocks=null;bool saved=false;
  try
  {
   for(int i=0;i<2;i++){clones[i].hideFlags=HideFlags.HideAndDontSave;var h=original[i].GetHeights(0,0,513,513);for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){float wx=-1792+x,wz=ts[i].transform.position.z+z,y=h[z,x]*320-24;h[z,x]+=(Add(wx,wz,y))/320;}var uneroded=(float[,])h.Clone();Erode(h);for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){float wx=-1792+x,wz=ts[i].transform.position.z+z;float oldY=original[i].GetHeight(x,z)-24;float w=.55f*S(2,8,Add(wx,wz,oldY))*S(0,25,Mathf.Min(z,512-z));h[z,x]=Mathf.Lerp(uneroded[z,x],h[z,x],w);}clones[i].SetHeights(0,0,h);
ts[i].terrainData=clones[i];ts[i].GetComponent<TerrainCollider>().terrainData=clones[i];ts[i].Flush();}
   foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>()){var p=tr.position;if(!Island(p.x,p.z)||p.x< -1792||p.x>=-1280)continue;var n=tr.name;if(n.StartsWith("SourceTree_")||n.StartsWith("Eco_")||n.StartsWith("Pine_")||n.StartsWith("YoungPine_")){hidden.Add(tr.gameObject);tr.gameObject.SetActive(false);}else if(n.StartsWith("Rock_")||n.StartsWith("Bush_")){int i=p.z>=768?1:0;float u=(p.x+1792)/512,v=(p.z-ts[i].transform.position.z)/512;float delta=clones[i].GetInterpolatedHeight(u,v)-original[i].GetInterpolatedHeight(u,v);moved.Add(Tuple.Create(tr,p));tr.position+=Vector3.up*delta;}}
   forest=Forest(ts);rocks=Rocks(ts);string dir="Preview/DragonCanonical20261007/Candidate/";Directory.CreateDirectory(dir);
   Capture(dir,"north_top",new Vector3(-1536,850,1024),new Vector3(-1536,0,1024),true);Capture(dir,"south_top",new Vector3(-1536,850,512),new Vector3(-1536,0,512),true);Capture(dir,"whole",new Vector3(-2150,750,150),new Vector3(-1536,25,780),false);Capture(dir,"lake",new Vector3(-1595,48,800),new Vector3(-1523,5,890),false);Capture(dir,"valley",new Vector3(-1740,55,500),new Vector3(-1600,24,700),false);
   if(keep){string backup="Backups/DragonCanonical20261007/Before";Directory.CreateDirectory(backup);foreach(var path in original.Select(AssetDatabase.GetAssetPath).Concat(new[]{scene.path})){var dest=Path.Combine(backup,Path.GetFileName(path));if(!File.Exists(dest))File.Copy(path,dest);}for(int i=0;i<2;i++){original[i].SetHeights(0,0,clones[i].GetHeights(0,0,513,513));EditorUtility.SetDirty(original[i]);AssetDatabase.SaveAssetIfDirty(original[i]);ts[i].terrainData=original[i];ts[i].GetComponent<TerrainCollider>().terrainData=original[i];}Directory.CreateDirectory("Assets/World/WorldExtensions/Generated/DragonCanonical20261007");foreach(var f in rocks.GetComponentsInChildren<MeshFilter>()){AssetDatabase.CreateAsset(f.sharedMesh,"Assets/World/WorldExtensions/Generated/DragonCanonical20261007/"+f.name+".asset");}AssetDatabase.CreateAsset(rocks.GetComponentInChildren<MeshRenderer>().sharedMaterial,"Assets/World/WorldExtensions/Generated/DragonCanonical20261007/ReferenceRock.mat");foreach(var tr in rocks.GetComponentsInChildren<Transform>(true))tr.gameObject.hideFlags=HideFlags.None;foreach(var tr in forest.GetComponentsInChildren<Transform>(true))tr.gameObject.hideFlags=HideFlags.None;EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");saved=true;File.WriteAllText("Validation/SequentialRepair/dragon_canonical_kept.txt","Interior ridge additions, sheltered production forest count="+forest.transform.childCount+". Existing shore and crater footprint protected; conforming warm rock surface on exposed slopes; alphamaps unchanged. Mainland excluded.");}
  }
  finally{for(int i=0;i<2;i++){ts[i].terrainData=original[i];ts[i].GetComponent<TerrainCollider>().terrainData=original[i];ts[i].Flush();UnityEngine.Object.DestroyImmediate(clones[i]);}if(!saved){if(rocks){var mat=rocks.GetComponentInChildren<MeshRenderer>().sharedMaterial;foreach(var f in rocks.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(f.sharedMesh);UnityEngine.Object.DestroyImmediate(rocks);UnityEngine.Object.DestroyImmediate(mat);}if(forest)UnityEngine.Object.DestroyImmediate(forest);foreach(var s in moved)if(s.Item1)s.Item1.position=s.Item2;foreach(var g in hidden)if(g)g.SetActive(true);}}
 }
}

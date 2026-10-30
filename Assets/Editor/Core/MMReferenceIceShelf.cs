using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// Separate ICE geometry over existing ocean, not replacement terrain or
// guessed square slabs. White floes follow the reference coastline mask.
public static class MMReferenceIceShelf {
 const string GroupName="Reference Ice Floes - Fractured Shelf";
 const string MeshRoot="Assets/World/WorldExtensions/Generated/";
 const string MaterialRoot="Assets/Materials/RealisticWorld/";
 static readonly string[] NorthNames={"SweetWater_North","Kriegspire_North","FrozenHighlands_North","SilverCove_North"};
 static float Ease(float a,float b,float v){
  float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3f-2f*t);
 }
 static Material GetIceMaterial(string asset,Color tint,Texture2D tex,float gloss){
  var mat=AssetDatabase.LoadAssetAtPath<Material>(asset);
  if(!mat){
   var sh=Shader.Find("MMUnity/Iceberg");
   if(!sh||!sh.isSupported)throw new Exception("Supported ice shader unavailable");
   mat=new Material(sh);AssetDatabase.CreateAsset(mat,asset);
  } else {
   var sh=Shader.Find("MMUnity/Iceberg");
   if(sh&&sh.isSupported&&mat.shader!=sh)mat.shader=sh;
  }
  mat.name=Path.GetFileNameWithoutExtension(asset);
  var snow02=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/World/WorldExtensions/Generated/LinkedSnow02/Snow02_SeamlessAlbedoSmoothness.png");
  if(snow02)tex=snow02;
  mat.color=tint;
  if(tex){mat.mainTexture=tex;mat.mainTextureScale=new Vector2(2.75f,2.75f);}
  var normal=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/World/WorldExtensions/Generated/LinkedSnow02/Snow02_SeamlessNormal.png");
  if(normal&&mat.HasProperty("_BumpMap")){mat.SetTexture("_BumpMap",normal);mat.SetFloat("_BumpScale",.55f);mat.EnableKeyword("_NORMALMAP");}
  if(mat.HasProperty("_Glossiness"))mat.SetFloat("_Glossiness",gloss);
  EditorUtility.SetDirty(mat);return mat;
 }
 static void AddFloe(List<Vector3> v,List<Vector2> uv,List<int> top,List<int> side,
  System.Random rng,float cx,float cz,float radius){
  int n=9+rng.Next(5),start=v.Count;
  float angle=(float)rng.NextDouble()*Mathf.PI*2f;
  float topY=.37f+(float)rng.NextDouble()*.18f;
  v.Add(new Vector3(cx,topY+.04f,cz));uv.Add(new Vector2(.5f,.5f));
  var angles=new float[n];var rr=new float[n];
  for(int i=0;i<n;i++){
   float a=angle+i*2f*Mathf.PI/n;
   float r=radius*(.87f+(float)rng.NextDouble()*.22f);
   angles[i]=a;rr[i]=r;
   v.Add(new Vector3(cx+Mathf.Cos(a)*r,topY+(float)rng.NextDouble()*.08f,
      cz+Mathf.Sin(a)*r));
   uv.Add(new Vector2(.5f+Mathf.Cos(a)*.46f,.5f+Mathf.Sin(a)*.46f));
  }
  for(int i=0;i<n;i++){
   top.Add(start);top.Add(start+1+(i+1)%n);top.Add(start+1+i);
  }
  int lower=v.Count;
  for(int i=0;i<n;i++){
   float a=angles[i],r=rr[i]*.90f;
   v.Add(new Vector3(cx+Mathf.Cos(a)*r,-.31f-(float)rng.NextDouble()*.2f,
     cz+Mathf.Sin(a)*r));
   uv.Add(new Vector2(i/(float)n,0f));
  }
  for(int i=0;i<n;i++){
   int p=start+1+i,q=start+1+(i+1)%n,b=lower+i,d=lower+(i+1)%n;
   side.Add(p);side.Add(q);side.Add(b);
   side.Add(q);side.Add(d);side.Add(b);
  }
 }
 static Mesh Generate(TerrainData td,MMReferenceEdgeProfiles.Mask mask,int index,out int count){
  var vertices=new List<Vector3>();var uvs=new List<Vector2>();
  var top=new List<int>();var side=new List<int>();
  var rng=new System.Random(94017+index*613);
  var heights=td.GetHeights(0,0,513,513);
  count=0;
  // Near-hexagonal clusters of larger irregular floes, separated by visible
  // dark-water fissures; no random sparse dots or overlapping meshes.
  var placed=new List<Vector3>();
  const int spacing=33;
  for(int z=18;z<494;z+=spacing)for(int x=19+((z/spacing)%2)*16;x<493;x+=spacing){
   int ix=Mathf.Clamp(x+rng.Next(-1,2),0,512);
   int iz=Mathf.Clamp(z+rng.Next(-1,2),0,512);
   float signed=(mask.At(ix,iz).r-128f)*512f/(148f*3f);
   float y=-24f+heights[iz,ix]*320f;
   if(signed> -11f||signed< -140f||y>-.70f)continue;
   float shelf=Ease(-140f,-55f,signed)*(1f-Ease(-18f,-8f,signed));
   float likelihood=.76f+.21f*shelf;
   if(rng.NextDouble()>likelihood)continue;
   float radius=Mathf.Min(16.4f,-signed-3.0f);
   radius=Mathf.Min(radius,14.1f+(float)rng.NextDouble()*2.0f);
   if(radius<6.0f)continue;
   var center=new Vector3(ix,0,iz);
   if(placed.Any(p=>Vector2.Distance(new Vector2(p.x,p.z),new Vector2(ix,iz))<radius+p.y+1.8f))continue;
   if(ix<radius+4||ix>512-radius-4||iz<radius+4||iz>512-radius-4)continue;
   AddFloe(vertices,uvs,top,side,rng,ix-256f,iz-256f,radius);
   placed.Add(new Vector3(ix,radius,iz));
   count++;
  }
  if(!top.Any())throw new Exception("No ice shelf floes generated for tile "+index);
  var mesh=new Mesh{name=NorthNames[index]+"FracturedIce"};
  mesh.indexFormat=vertices.Count>65000?
   UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;
  mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.subMeshCount=2;
  mesh.SetTriangles(top,0);mesh.SetTriangles(side,1);
  mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 [Serializable] sealed class ApprovedIcePolygon{
  public Vector2[] outline;
  public float topY,bottomY;
 }
 [Serializable] sealed class ApprovedIcePack{
  public ApprovedIcePolygon[] floes;
 }
 static Mesh GenerateApprovedVoronoi(int index,out int count){
  string file=Path.GetFullPath(Path.Combine(
    "Validation","EdgeGrid20260923","VoronoiIceApproved_20260924","North_"+index+".json"));
  if(!File.Exists(file))throw new FileNotFoundException("Approved northern ice polygons missing",file);
  var pack=JsonUtility.FromJson<ApprovedIcePack>(File.ReadAllText(file));
  if(pack==null||pack.floes==null||pack.floes.Length<10)
   throw new Exception("Invalid approved Voronoi polygons for north tile "+index);
  var verts=new List<Vector3>();var uv=new List<Vector2>();
  var top=new List<int>();var side=new List<int>();
  count=0;int sourceIndex=0;
  foreach(var item in pack.floes){
   int rawIndex=sourceIndex++;
   if(item==null||item.outline==null||item.outline.Length<3)continue;
   uint selectHash=(uint)unchecked(index*73856093+rawIndex*19349663+83492791);
   int keepDivisor=index==0?6:(index==1?4:(index==2?5:2));
   if(selectHash%(uint)keepDivisor!=0)continue;
   var points=(Vector2[])item.outline.Clone();
   int n=points.Length;
   float signedArea=0f;
   for(int i=0;i<n;i++){
    var a=points[i];var b=points[(i+1)%n];
    signedArea+=a.x*b.y-b.x*a.y;
   }
   if(Mathf.Abs(signedArea)<.001f)continue;
   if(signedArea<0f)Array.Reverse(points);
   var center=Vector2.zero;
   foreach(var point in points)center+=point;
   center/=n;

   // Keep the approved reference footprint, but turn each Voronoi cell into
   // a separated three-dimensional berg instead of a thin white paving slab.
   int hash=unchecked(index*73856093+rawIndex*19349663+83492791);
   float h01=(hash&1023)/1023f;
   float separationScale=.70f+.18f*h01;
   for(int i=0;i<n;i++)points[i]=center+(points[i]-center)*separationScale;

   float meanRadius=0f;
   for(int i=0;i<n;i++)meanRadius+=(points[i]-center).magnitude;
   meanRadius/=n;
   bool tabular=((hash>>11)&3)==0;
   float rise=tabular?
     Mathf.Clamp(2.1f+meanRadius*.13f+(((hash>>15)&255)/255f)*1.6f,3.2f,6.0f):
     Mathf.Clamp(3.4f+meanRadius*.25f+(((hash>>15)&255)/255f)*2.8f,5.2f,11.5f);
   if(((hash>>20)&7)==0)rise=Mathf.Min(15.0f,rise*1.42f);
   float rimY=.68f+(((hash>>23)&255)/255f)*.48f;
   float depth=Mathf.Clamp(2.2f+rise*.68f,3.5f,10.5f);
   float tiltX=(((hash>>8)&255)/255f-.5f)*.014f;
   float tiltZ=(((hash>>16)&255)/255f-.5f)*.014f;
   float innerScale=.28f+.16f*(((hash>>4)&255)/255f);

   int outer=verts.Count;
   for(int i=0;i<n;i++){
    var p=points[i];
    float jitter=(((hash+i*97)&255)/255f-.5f)*.16f;
    float py=rimY+jitter+(p.x-center.x)*tiltX+(p.y-center.y)*tiltZ;
    verts.Add(new Vector3(p.x,py,p.y));uv.Add(p/10f);
   }
   int inner=verts.Count;
   for(int i=0;i<n;i++){
    var p=center+(points[i]-center)*innerScale;
    float jitter=(((hash+i*193+71)&255)/255f-.5f)*.20f;
    float py=rimY+rise*.58f+jitter+(p.x-center.x)*tiltX+(p.y-center.y)*tiltZ;
    verts.Add(new Vector3(p.x,py,p.y));uv.Add(p/10f);
   }
   float apexAngle=(((hash>>5)&1023)/1023f)*Mathf.PI*2f;
   float apexOffset=meanRadius*(.07f+.09f*(((hash>>19)&255)/255f));
   var apexPos=center+new Vector2(Mathf.Cos(apexAngle),Mathf.Sin(apexAngle))*apexOffset;
   int apex=verts.Count;
   verts.Add(new Vector3(apexPos.x,rimY+rise,apexPos.y));uv.Add(apexPos/10f);

   for(int i=0;i<n;i++){
    int oi=outer+i,on=outer+(i+1)%n,ii=inner+i,inn=inner+(i+1)%n;
    top.Add(oi);top.Add(ii);top.Add(on);
    top.Add(on);top.Add(ii);top.Add(inn);
    top.Add(apex);top.Add(inn);top.Add(ii);
   }

   int lower=verts.Count;
   for(int i=0;i<n;i++){
    var p=center+(points[i]-center)*(.84f+.05f*(((hash+i*31)&255)/255f));
    float by=-depth-(((hash+i*53)&127)/127f)*.18f;
    verts.Add(new Vector3(p.x,by,p.y));uv.Add(new Vector2(i/(float)n,0f));
   }
   for(int i=0;i<n;i++){
    int p=outer+i,q=outer+(i+1)%n,b=lower+i,d=lower+(i+1)%n;
    side.Add(p);side.Add(q);side.Add(b);
    side.Add(q);side.Add(d);side.Add(b);
   }
   count++;
  }
  if(count<3)throw new Exception("Too few valid iceberg polygons for tile "+index+": "+count);
  var mesh=new Mesh{name=NorthNames[index]+"FracturedIce"};
  mesh.indexFormat=verts.Count>65000?
   UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16;
  mesh.SetVertices(verts);mesh.SetUVs(0,uv);mesh.subMeshCount=2;
  mesh.SetTriangles(top,0);mesh.SetTriangles(side,1);
  mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 static void OverwriteMeshData(Mesh dst,Mesh src){
  string keepName=dst.name;
  dst.Clear();
  dst.indexFormat=src.indexFormat;
  dst.vertices=src.vertices;
  dst.uv=src.uv;
  dst.subMeshCount=src.subMeshCount;
  for(int i=0;i<src.subMeshCount;i++)dst.SetTriangles(src.GetTriangles(i),i,true);
  dst.RecalculateNormals();
  dst.RecalculateTangents();
  dst.RecalculateBounds();
  dst.name=keepName;
  dst.UploadMeshData(false);
  EditorUtility.SetDirty(dst);
 }
 public static void DressOne(int index,List<string> report){
  if(index<0||index>3)throw new ArgumentOutOfRangeException("index");
  string name=NorthNames[index];
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity",OpenSceneMode.Single);
  var root=scene.GetRootGameObjects().FirstOrDefault(g=>
   g.name.Contains("Open World"));
  if(!root)throw new Exception("Edge root not found "+name);
  var terrain=root.GetComponentInChildren<Terrain>(true);
  if(!terrain)throw new Exception("Northern terrain not found "+name);
  var mask=MMReferenceEdgeProfiles.Load(name);
  var snow=terrain.terrainData.terrainLayers.FirstOrDefault(t=>
    t&&t.name.IndexOf("Snow",StringComparison.OrdinalIgnoreCase)>=0);
  if(!snow||!snow.diffuseTexture)throw new Exception("Existing snow texture missing "+name);
  Directory.CreateDirectory(MaterialRoot);
  var topMat=GetIceMaterial(MaterialRoot+"ReferenceIceTop.mat",
    new Color(.92f,.95f,.98f,1),snow.diffuseTexture,.14f);
  var sideMat=GetIceMaterial(MaterialRoot+"ReferenceIceSide.mat",
    new Color(.63f,.74f,.83f,1),snow.diffuseTexture,.22f);
  int placed;
  var generated=GenerateApprovedVoronoi(index,out placed);
  string meshPath=MeshRoot+name+"FracturedIce.asset";
  var oldMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
  Mesh persisted;
  if(oldMesh){
   OverwriteMeshData(oldMesh,generated);
   oldMesh.name=Path.GetFileNameWithoutExtension(meshPath);
   EditorUtility.SetDirty(oldMesh);persisted=oldMesh;
   UnityEngine.Object.DestroyImmediate(generated);
  } else {
   generated.name=Path.GetFileNameWithoutExtension(meshPath);
   AssetDatabase.CreateAsset(generated,meshPath);persisted=generated;
  }
  var oldGroup=root.transform.Find(GroupName);
  if(oldGroup)UnityEngine.Object.DestroyImmediate(oldGroup.gameObject);
  var group=new GameObject(GroupName);group.transform.SetParent(root.transform,false);
  var obj=new GameObject("Nonoverlapping Fractured Snow Floe Mesh");
  obj.transform.SetParent(group.transform,false);
  var mf=obj.AddComponent<MeshFilter>();mf.sharedMesh=persisted;
  var mr=obj.AddComponent<MeshRenderer>();
  mr.sharedMaterials=new[]{topMat,sideMat};
  mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
  mr.receiveShadows=true;
  EditorSceneManager.MarkSceneDirty(scene);
  if(!EditorSceneManager.SaveScene(scene))throw new IOException("Cannot save ice "+name);
  report.Add(name+",floe_count="+placed+",triangles="+persisted.triangles.Length/3+
   ",mesh="+meshPath);
 }
 public static void RefreshIceMaterialsOnly(){
  // Shared ice materials are persistent assets. Never rewrite them during
  // rebuild/QA, because Unity may have them open for import/rendering.
  var top=AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot+"ReferenceIceTop.mat");
  var side=AssetDatabase.LoadAssetAtPath<Material>(MaterialRoot+"ReferenceIceSide.mat");
  if(!top||!side)throw new FileNotFoundException("Reference ice materials missing");
  Debug.Log("REFERENCE_ICE_MATERIALS_OK");
 }
 public static void RebuildApprovedMeshAssetsOnly(){
  var report=new List<string>();
  for(int i=0;i<4;i++){
   int placed;var generated=GenerateApprovedVoronoi(i,out placed);
   string meshPath=MeshRoot+NorthNames[i]+"FracturedIce.asset";
   var current=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
   if(!current)throw new FileNotFoundException("Current linked ice mesh missing",meshPath);
   OverwriteMeshData(current,generated);
   current.name=Path.GetFileNameWithoutExtension(meshPath);
   EditorUtility.SetDirty(current);UnityEngine.Object.DestroyImmediate(generated);
   report.Add(NorthNames[i]+",floe_count="+placed+",triangles="+current.triangles.Length/3);
  }
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();
  Directory.CreateDirectory("Validation/EdgeGrid20260923/NorthIceSeparation20261001");
  File.WriteAllLines("Validation/EdgeGrid20260923/NorthIceSeparation20261001/mesh_rebuild.csv",report);
  Debug.Log("NORTH_ICE_SEPARATED_MESHES_COMPLETE "+string.Join(" | ",report));
 }
 [MenuItem("MMUnity/World/Add Reference Fractured Northern Ice Shelf")]
 public static void DressNorthAll(){
  // Ice materials are persistent shared assets and must not be rewritten here.
  // Only rebuild the linked iceberg meshes.
  RebuildApprovedMeshAssetsOnly();
  Debug.Log("REFERENCE_FRACTURED_ICE_SHELF_COMPLETE iceberg meshes refreshed");
 }
}


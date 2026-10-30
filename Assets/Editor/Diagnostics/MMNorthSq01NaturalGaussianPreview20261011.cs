using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class MMNorthSq01NaturalGaussianPreview20261011
{
 const string Data="Validation/NorthSq01CoastSeam20261011";
 const string Out="Preview/NorthSq01NaturalGaussian20261011";
 static float[,] Read(string p){byte[] b=File.ReadAllBytes(Data+"/"+p);if(b.Length!=513*513*4)throw new Exception("Bad input "+p);var a=new float[513,513];Buffer.BlockCopy(b,0,a,0,b.Length);return a;}
 static float S(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
 static float Sample(float[,] h,float x,float z){
  x=Mathf.Clamp(x,0,512);z=Mathf.Clamp(z,0,512);int i=Mathf.Min(511,(int)x),j=Mathf.Min(511,(int)z);float a=x-i,b=z-j;
  return Mathf.Lerp(Mathf.Lerp(h[j,i],h[j,i+1],a),Mathf.Lerp(h[j+1,i],h[j+1,i+1],a),b);
 }
 static bool Dress(Transform t)=>t.name.Contains("AuthoredPine_")||t.name.StartsWith("TransitionPine_")||t.name.StartsWith("ReferenceTallPine_")||t.name.StartsWith("SourceTree_")||t.name.StartsWith("Eco_")||t.name.StartsWith("AuthoredBoulder_")||t.name.StartsWith("Bush_")||t.name.StartsWith("Pine_");
 static void CameraShot(string p,Vector3 pos,Vector3 target,bool top,float size){
  var o=new GameObject("TEMP_QA");var c=o.AddComponent<Camera>();c.transform.position=pos;c.transform.rotation=Quaternion.LookRotation(target-pos,Vector3.up);
  c.orthographic=top;if(top)c.orthographicSize=size;else c.fieldOfView=size;c.nearClipPlane=.1f;c.farClipPlane=3500;c.clearFlags=CameraClearFlags.Skybox;
  var rt=new RenderTexture(1250,850,24);var old=RenderTexture.active;c.targetTexture=rt;c.Render();RenderTexture.active=rt;
  var im=new Texture2D(1250,850,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1250,850),0,0);im.Apply();File.WriteAllBytes(p,im.EncodeToPNG());
  RenderTexture.active=old;c.targetTexture=null;UnityEngine.Object.DestroyImmediate(im);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(o);
 }
 static void Shots(string key){
  string dir=Out+"/"+key;Directory.CreateDirectory(dir);
  CameraShot(dir+"/top.png",new Vector3(-771,410,1192),new Vector3(-771,0,1192),true,158);
  CameraShot(dir+"/sea.png",new Vector3(-768,125,1390),new Vector3(-768,9,1194),false,55);
  CameraShot(dir+"/oblique.png",new Vector3(-680,220,1370),new Vector3(-775,11,1178),false,55);
  CameraShot(dir+"/low.png",new Vector3(-750,70,1275),new Vector3(-770,9,1192),false,63);
 }
 public static void Run(){
  var sc=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);if(sc.isDirty)throw new Exception("Dirty scene, abort");
  var terrain=new Terrain[2];var original=new float[2][,];var datas=new TerrainData[2];var colliders=new TerrainCollider[2];
  var filters=new MeshFilter[2];var meshes=new Mesh[2];
  for(int k=0;k<2;k++){
   float x=-1280+512*k;
   var t=Terrain.activeTerrains.Single(q=>Mathf.Abs(q.transform.position.x-x)<.01f&&Mathf.Abs(q.transform.position.z-768)<.01f);
   terrain[k]=t;datas[k]=t.terrainData;colliders[k]=t.GetComponent<TerrainCollider>();
   original[k]=Read("sq"+k+"_current.f32");
   var rock=GameObject.Find("North Alpine Rock Surface "+k+" 20261007");if(!rock||!rock.activeSelf)throw new Exception("Missing Square "+k+" rock");
   filters[k]=rock.GetComponent<MeshFilter>();meshes[k]=filters[k].sharedMesh;
   if(meshes[k].vertexCount!=263169)throw new Exception("Bad rock mesh "+k);
  }
  Shots("baseline");
  string report="";
  foreach(string variant in new[]{"U1","U2"}){
   var next=new[]{Read(variant+"_sq0.f32"),Read(variant+"_sq1.f32")};
   var newDatas=new TerrainData[2];var tmpMesh=new Mesh[2];var moved=new Dictionary<Transform,Vector3>();
   int changed=0;float maxDelta=0,seam=0,maxBorder=0;int verts=0;
   for(int z=0;z<=512;z++)seam=Mathf.Max(seam,Mathf.Abs(next[0][z,512]-next[1][z,0]));
   if(seam>.00001f)throw new Exception("Proposed square seam diverges: "+seam);
   try{
    for(int k=0;k<2;k++){
     var raw=datas[k].GetHeights(0,0,513,513);
     for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){
      float d=next[k][z,x]-original[k][z,x];raw[z,x]=(next[k][z,x]-terrain[k].transform.position.y)/datas[k].size.y;
      if(Mathf.Abs(d)>.01f){changed++;maxDelta=Mathf.Max(maxDelta,Mathf.Abs(d));}
      if(z==0||z==512||(k==0&&x==0)||(k==1&&x==512))maxBorder=Mathf.Max(maxBorder,Mathf.Abs(d));
     }
     if(maxBorder>.0005f)throw new Exception("Outer border changed: "+maxBorder);
     var clone=UnityEngine.Object.Instantiate(datas[k]);clone.hideFlags=HideFlags.HideAndDontSave;clone.SetHeights(0,0,raw);newDatas[k]=clone;
     var m=UnityEngine.Object.Instantiate(meshes[k]);m.hideFlags=HideFlags.HideAndDontSave;
     var v=m.vertices;var n=m.normals;
     for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){
      float delta=next[k][z,x]-original[k][z,x];if(Mathf.Abs(delta)<.01f)continue;
      int i=z*513+x;float y0=original[k][z,x]-terrain[k].transform.position.y,y1=next[k][z,x]-terrain[k].transform.position.y;
      float off0=Mathf.Lerp(.1f,.6f,S(4,28,y0-24)),off1=Mathf.Lerp(.1f,.6f,S(4,28,y1-24));
      v[i].y+=delta+off1-off0;n[i]=clone.GetInterpolatedNormal(x/512f,z/512f);verts++;
     }
     m.vertices=v;m.normals=n;m.RecalculateBounds();tmpMesh[k]=m;
     terrain[k].terrainData=clone;colliders[k].terrainData=clone;filters[k].sharedMesh=m;terrain[k].Flush();
    }
    foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)){
     if(!Dress(tr)||tr.GetComponentsInParent<Transform>().Skip(1).Any(Dress))continue;
     var p=tr.position;if(p.z<768||p.z>=1280||p.x< -1280||p.x>=-256)continue;
     int k=p.x < -768?0:1;float lx=p.x-terrain[k].transform.position.x,lz=p.z-768;
     float delta=Sample(next[k],lx,lz)-Sample(original[k],lx,lz);if(Mathf.Abs(delta)<.02f)continue;
     moved[tr]=p;tr.position=p+Vector3.up*delta;
    }
    Shots(variant);
    report+=variant+" saved=False changedSamples="+changed+" maxDeltaM="+maxDelta.ToString("F3")+" outerBorderM="+maxBorder.ToString("F6")+" jointSeamM="+seam.ToString("F6")+" temporaryRockVertexUpdates="+verts+" temporaryDressingMoved="+moved.Count+"\n";
   }finally{
    foreach(var kv in moved)if(kv.Key)kv.Key.position=kv.Value;
    for(int k=0;k<2;k++){
     terrain[k].terrainData=datas[k];colliders[k].terrainData=datas[k];filters[k].sharedMesh=meshes[k];terrain[k].Flush();
     if(tmpMesh[k])UnityEngine.Object.DestroyImmediate(tmpMesh[k]);
     if(newDatas[k])UnityEngine.Object.DestroyImmediate(newDatas[k]);
    }
   }
  }
  Directory.CreateDirectory(Data);File.WriteAllText(Data+"/preview.txt",report);
  Debug.Log("SQ01_PAIRED_COVE_PREVIEW_DONE "+report.Replace("\n",";")+"sceneDirty="+sc.isDirty);
 }
}

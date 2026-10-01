using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MMBootlegWaterAudit20261001
{
 const int N=128; const string ScenePath="Assets/Scenes/World/Enroth.unity";
 const string TilePath="Assets/World/BootlegBay/Data/tilemap_u8.bin";
 const string SemPath="Assets/World/BootlegBay/Data/tile_semantics_u8.bin";
 static byte[] sem; static bool Water(byte tile)=>(sem[tile]&1)!=0;
 public static void Run(){
  var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  var rt=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Bootleg Bay - LINKED REFERENCE");
  var terrain=rt.GetComponentInChildren<Terrain>(true);
  var water=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="Linked connected sea and existing lowland waters 20260929");
  if(!water)throw new Exception("Connected water object missing");
  var mf=water.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)throw new Exception("Connected water mesh missing");
  var probe=new GameObject("TEMP Bootleg water audit collider");var col=probe.AddComponent<MeshCollider>();col.sharedMesh=mf.sharedMesh;
  probe.transform.position=water.position;probe.transform.rotation=water.rotation;probe.transform.localScale=water.lossyScale;
  sem=File.ReadAllBytes(SemPath);var tiles=File.ReadAllBytes(TilePath);
  int target=0,covered=0,missing=0,dryHigh=0,nonWaterCovered=0;float maxMissingGround=float.MinValue,minMissingGround=float.MaxValue;
  var img=new Texture2D(N,N,TextureFormat.RGBA32,false,true);var pix=new Color32[N*N];
  try{
   for(int sy=0;sy<N;sy++)for(int sx=0;sx<N;sx++){
    float wx=terrain.transform.position.x+(sx+.5f)*4f;
    float wz=terrain.transform.position.z+(N-sy-.5f)*4f;
    float gy=terrain.SampleHeight(new Vector3(wx,0,wz))+terrain.transform.position.y;
    bool hit=col.Raycast(new Ray(new Vector3(wx,100,wz),Vector3.down),out var h,200f);
    bool isw=Water(tiles[sy*N+sx]);
    Color32 c;
    if(isw){target++;if(hit){covered++;c=new Color32(40,130,220,255);}else{missing++;if(gy>.10f){dryHigh++;c=new Color32(250,40,40,255);}else c=new Color32(255,180,30,255);maxMissingGround=Mathf.Max(maxMissingGround,gy);minMissingGround=Mathf.Min(minMissingGround,gy);}}
    else{if(hit){nonWaterCovered++;c=new Color32(180,70,210,255);}else c=new Color32(45,60,45,255);}
    pix[(N-1-sy)*N+sx]=c;
   }
  }finally{UnityEngine.Object.DestroyImmediate(probe);}
  img.SetPixels32(pix);img.Apply();Directory.CreateDirectory("Validation/EdgeGrid20260923/BootlegBay20261001");File.WriteAllBytes("Validation/EdgeGrid20260923/BootlegBay20261001/water_semantic_coverage.png",img.EncodeToPNG());UnityEngine.Object.DestroyImmediate(img);
  string report=$"sourceWaterCells={target} covered={covered} missing={missing} missingWithGroundAboveWater={dryHigh} missingGroundMinM={(missing>0?minMissingGround:0):F3} missingGroundMaxM={(missing>0?maxMissingGround:0):F3} nonWaterCellsCovered={nonWaterCovered} connectedWaterMeshTriangles={mf.sharedMesh.triangles.Length/3} terrainAsset={AssetDatabase.GetAssetPath(terrain.terrainData)}";
  File.WriteAllText("Validation/EdgeGrid20260923/BootlegBay20261001/water_audit.txt",report+"\n");Debug.Log(report);
 }
}
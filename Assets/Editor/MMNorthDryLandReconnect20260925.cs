using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
// Only fill the false submerged trough INSIDE photo-confirmed north mainland.
// Canonical 15 source scenes and all source TerrainData are strictly read-only.
public static class MMNorthDryLandReconnect20260925 {
 const string V="Validation/EdgeGrid20260923/";
 const string G="Assets/World/WorldExtensions/Generated/";
 const string Backup=@"C:\MMUnityPort\Backups\BeforeNorthDryJoin_20260925\manifest.json";
 static readonly string[] Names={"SweetWater","Kriegspire","FrozenHighlands","SilverCove"};
 static float S(float a,float b,float v){
  float t=Mathf.Clamp01((v-a)/(b-a));return t*t*(3f-2f*t);
 }
 static Color32[] Mask(string n){
  var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  var p=V+"ReferenceMasks/"+n+"_North.png";
  if(!t.LoadImage(File.ReadAllBytes(p))||t.width!=513||t.height!=513)
   throw new Exception("Missing authority map "+p);
  var px=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return px;
 }
 [MenuItem("MMUnity/Reference 2026/Restore Dry Northern Mainland At Source Join")]
 public static void Apply(){
  if(!File.Exists(Backup))throw new Exception("Pre-change asset backup required");
  var sc=SceneManager.GetActiveScene();
  if(sc.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||sc.isDirty)
   throw new Exception("Saved linked world must be active");
  var tdList=new List<TerrainData>();var heights=new List<float[,]>();
  var lines=new List<string>();
  foreach(string n in Names){
   var td=AssetDatabase.LoadAssetAtPath<TerrainData>(G+n+"_NorthTerrain.asset");
   var original=AssetDatabase.LoadAssetAtPath<TerrainData>(
     "Assets/World/"+n+"/Generated/"+n+"Terrain.asset");
   if(!td||!original)throw new Exception("Terrain missing: "+n);
   var h=td.GetHeights(0,0,513,513);
   var prior=(float[,])h.Clone();
   var src=original.GetHeights(0,511,513,2);
   var photo=Mask(n);var severity=new float[513];
   float beforeWet=0,afterWet=0,maxMove=0,seam=0;
   int columns=0,vertices=0;
   for(int x=0;x<513;x++){
    float edge=-24f+320f*h[0,x];
    float inland=-24f+320f*h[64,x];
    if(edge<.10f||inland<3f||photo[16*513+x].r<143)continue;
    float min=999f;
    for(int z=1;z<=24;z++)min=Mathf.Min(min,-24f+320f*h[z,x]);
    if(min<-.1f)severity[x]=S(.2f,1.1f,.2f-min);
   }
   var smooth=new float[513];
   for(int x=8;x<=504;x++){
    float v=0f,total=0f;
    for(int dx=-8;dx<=8;dx++){
     float k=(9f-Mathf.Abs(dx));v+=k*severity[x+dx];total+=k;
    }
    smooth[x]=v/total;
   }
   for(int x=8;x<=504;x++){
    float edge=-24f+320f*h[0,x];
    float inland=-24f+320f*h[64,x];
    if(edge<.10f||inland<3f||photo[16*513+x].r<143)continue;
    float wx=S(8f,30f,x)*S(8f,30f,512f-x)*smooth[x];
    if(wx<.005f)continue;
    columns++;
    for(int z=1;z<64;z++){
     var m=photo[z*513+x].r;
     float land=S(137f,155f,m);
     if(land<.001f)continue;
     float t=z/64f,guide=h[0,x]+(h[64,x]-h[0,x])*S(0f,1f,t);
     float old=h[z,x];
     float lift=Mathf.Max(0f,guide-old)*wx*land;
     h[z,x]=old+lift;maxMove=Mathf.Max(maxMove,lift*320f);
     if(lift*320f>.01f)vertices++;
    }
   }
   float slopeJump=0f;
   for(int x=0;x<513;x++){
    seam=Mathf.Max(seam,Mathf.Abs(h[0,x]-src[1,x])*320f);
    slopeJump=Mathf.Max(slopeJump,Mathf.Abs(
      (h[1,x]-h[0,x])-(src[1,x]-src[0,x]))*320f);
    if(x<8||x>504||smooth[x]<.005f)continue;
    for(int z=1;z<=32;z++){
     if(photo[z*513+x].r<155)continue;
     if(-24f+320f*prior[z,x]<0f)beforeWet++;
     if(-24f+320f*h[z,x]<0f)afterWet++;
    }
   }
   if(seam>.02f||maxMove>18f)
    throw new Exception("Reject unsafe north correction "+n+
      " source_edge="+seam+" movement="+maxMove);
   tdList.Add(td);heights.Add(h);
   lines.Add(n+"_North columns="+columns+" vertices="+vertices+
    " inland_submerged_before="+beforeWet+" after="+afterWet+
    " max_height_move_m="+maxMove.ToString("F3")+
    " source_edge_error_m="+seam.ToString("F6")+
    " source_slope_change_m_per_m="+slopeJump.ToString("F3")+
    " canonical_source_terrain_edits=0");
  }
  for(int i=0;i<tdList.Count;i++){
   tdList[i].SetHeights(0,0,heights[i]);EditorUtility.SetDirty(tdList[i]);
  }
  AssetDatabase.SaveAssets();
  File.WriteAllLines(V+"north_dry_land_reconnect_20260925.csv",lines);
  Debug.Log("NORTH_DRY_LAND_RECONNECTED "+string.Join(" | ",lines));
 }
}

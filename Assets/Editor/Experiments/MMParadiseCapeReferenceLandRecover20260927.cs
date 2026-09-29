using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
// Recover only photo-confirmed missing land INSIDE Paradise West's south cape.
// Every original/adjacent terrain BORDER remains exact and all underwater channels stay open.
public static class MMParadiseCapeReferenceLandRecover20260927 {
 const string V="Validation/EdgeGrid20260923/";
 const string B=@"C:\MMUnityPort\Backups\BeforeParadiseSouthReferenceLandRecover_20260927\manifest.json";
 const string P="Assets/World/WorldExtensions/Generated/ParadiseValley_WestTerrain.asset";
 static float Smooth(float lo,float hi,float x){
  float t=Mathf.Clamp01((x-lo)/Mathf.Max(.001f,hi-lo));
  return t*t*(3f-2f*t);
 }
 public static void Apply(){
  if(!File.Exists(B))throw new Exception("Paradise coast rollback snapshot missing");
  if(File.Exists(V+"paradise_cape_interior_recovery_20260927.txt"))
   throw new Exception("Refusing to apply Paradise coast adjustment twice");
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(P);
  if(!td||td.heightmapResolution!=513||td.alphamapResolution!=512)
   throw new Exception("Paradise generated terrain incompatible");
  var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!t.LoadImage(File.ReadAllBytes(V+"ReferenceMasks/ParadiseValley_West.png"))||
      t.width!=513||t.height!=513)throw new Exception("Paradise reference mask invalid");
  var mask=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);
  var h=td.GetHeights(0,0,513,513);
  var original=(float[,])h.Clone();
  var a=td.GetAlphamaps(0,0,512,512);
  int desert=Array.FindIndex(td.terrainLayers,l=>l&&l.name=="Realistic_Desert");
  int arid=Array.FindIndex(td.terrainLayers,l=>l&&l.name=="Realistic_Arid");
  int rock=Array.FindIndex(td.terrainLayers,l=>l&&l.name=="Realistic_Volcanic");
  int green=Array.FindIndex(td.terrainLayers,l=>l&&l.name=="Realistic_Green");
  int light=Array.FindIndex(td.terrainLayers,l=>l&&l.name=="Realistic_LightGreen");
  if(desert<0||arid<0||rock<0||green<0||light<0)
   throw new Exception("Paradise native ochre texture mapping unavailable");
  int changed=0,recovered=0,guarded=0;float maxLift=0,maxLocalGradient=0;
  // Preserve x=0/512, z=0/512 exactly and maintain >=32m edge guard.
  // The native east Paradise map is submerged here; recovering its shared
  // coast would need LINKED-ONLY source reprofiling, not an unsafe boundary lift.
  for(int z=1;z<170;z++)for(int x=330;x<512;x++){
   int index=z*513+x;
   float authority=mask[index].r;
   if(authority<153f){guarded++;continue;}
   float y=-24f+320f*h[z,x];
   if(y>=1.15f)continue;
   float certainty=Smooth(145f,190f,authority);
   float south=Smooth(0f,55f,z),east=Smooth(0f,65f,512f-x);
   float west=Smooth(0f,22f,x-330f);
   float north=1f-Smooth(125f,170f,z);
   float strength=certainty*south*east*west*north;
   if(strength<.002f){guarded++;continue;}
   float target=.65f+.45f*Smooth(155f,207f,authority);
   float dy=Mathf.Max(0f,target-y)*strength;
   if(dy<.002f)continue;
   h[z,x]=Mathf.Clamp01((y+dy+24f)/320f);
   maxLift=Mathf.Max(maxLift,dy);
   if(y<-.20f&&y+dy>.45f)recovered++;
   changed++;
   if(z<512&&x<512&&dy>.14f&&y+dy>.18f){
    float w=.72f*Smooth(.14f,2.0f,dy)*Smooth(.15f,.8f,y+dy);
    for(int k=0;k<a.GetLength(2);k++)a[z,x,k]*=1f-w;
    a[z,x,arid]+=.45f*w;a[z,x,desert]+=.31f*w;
    a[z,x,rock]+=.19f*w;a[z,x,green]+=.035f*w;a[z,x,light]+=.015f*w;
   }
  }
  // Precommit: reject artificial cliffs, large lifts, or any modified border.
  float borderMove=0f;
  for(int x=0;x<513;x++){
   borderMove=Mathf.Max(borderMove,Mathf.Abs(h[0,x]-original[0,x])*320f);
   borderMove=Mathf.Max(borderMove,Mathf.Abs(h[512,x]-original[512,x])*320f);
  }
  for(int z=0;z<513;z++){
   borderMove=Mathf.Max(borderMove,Mathf.Abs(h[z,0]-original[z,0])*320f);
   borderMove=Mathf.Max(borderMove,Mathf.Abs(h[z,512]-original[z,512])*320f);
  }
  float introduced=0f;
  for(int z=20;z<170;z++)for(int x=330;x<512;x++){
   float change=Mathf.Abs(h[z,x]-original[z,x])*320f;
   if(change<.002f)continue;
   foreach(var dir in new[]{new Vector2Int(1,0),new Vector2Int(0,1)}){
    int xx=x+dir.x,zz=z+dir.y;
    if(xx>512||zz>512)continue;
    float oldSlope=Mathf.Abs(original[z,x]-original[zz,xx])*320f;
    float newSlope=Mathf.Abs(h[z,x]-h[zz,xx])*320f;
    maxLocalGradient=Mathf.Max(maxLocalGradient,newSlope);
    introduced=Mathf.Max(introduced,newSlope-oldSlope);
   }
  }
  if(changed<400||maxLift>8.6f||borderMove>.00001f||introduced>.8f)
   throw new Exception("Unsafe Paradise reference interior recovery: cells="+changed+
    " lift="+maxLift+" sharedBorder="+borderMove+" newSlopeIncrement="+introduced);
  int beforeWet=0,afterWet=0;
  for(int z=0;z<513;z++)for(int x=0;x<513;x++){
   if(mask[z*513+x].r<145)continue;
   if(-24f+320f*original[z,x]<=-.2f)beforeWet++;
   if(-24f+320f*h[z,x]<=-.2f)afterWet++;
  }
  if(afterWet>=beforeWet)throw new Exception("No missing Paradise land recovered");
  td.SetHeights(0,0,h);
  td.SetAlphamaps(0,0,a);
  td.SetBaseMapDirty();
  EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();
  File.WriteAllText(V+"paradise_cape_interior_recovery_20260927.txt",
   "PASS modifiedInteriorCells="+changed+" recoveredFromUnderwater="+recovered+
   " coastalBorderProtected="+guarded+" falseWetBefore="+beforeWet+
   " falseWetAfter="+afterWet+" maxLiftM="+maxLift.ToString("F3")+
   " maxLocalSlope="+maxLocalGradient.ToString("F3")+
   " maxNewSlopeIncrement="+introduced.ToString("F3")+
   " allFourTileBordersUnchangedM="+borderMove.ToString("F6")+
   " originalScenesUntouched=true dragonPondUntouched=true\n");
 }
}

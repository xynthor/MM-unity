using UnityEngine;
using UnityEditor;
using System;
using System.IO;
// Darken only source-map-registered volcanic vents and crater rims.
// Keep pond floor, shore, authentic height and canonical MM6 scenes unchanged.
public static class MMPhotoVolcanoAsh20260926 {
 const string V="Validation/EdgeGrid20260923/";
 const string B=@"C:\MMUnityPort\Backups\BeforePhotoMatchedVolcanoAsh_20260926\manifest.json";
 static float Smooth(float lo,float hi,float x){
  float t=Mathf.Clamp01((x-lo)/(hi-lo));return t*t*(3f-2f*t);
 }
 public static void Apply(string n,TerrainData d){
  if(n!="Kriegspire"&&n!="SweetWater")return;
  if(!File.Exists(B))throw new Exception("Missing protected volcano rollback");
  string path=V+"UnifiedNorthReferenceBiomes_20260926/"+n+"_North.png";
  var image=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!image.LoadImage(File.ReadAllBytes(path))||image.width!=513)
   throw new Exception("Reference ash authority missing "+path);
  var px=image.GetPixels32();UnityEngine.Object.DestroyImmediate(image);
  int rock=Array.FindIndex(d.terrainLayers,l=>l&&l.name=="Realistic_Volcanic");
  int soil=Array.FindIndex(d.terrainLayers,l=>l&&l.name=="Realistic_Arid");
  if(rock<0||soil<0)throw new Exception(n+" wrong ash layer");
  int cx=n=="Kriegspire"?189:462,cz=n=="Kriegspire"?331:263;
  float sx=n=="Kriegspire"?69f:46f,sz=n=="Kriegspire"?59f:39f;
  var a=d.GetAlphamaps(0,0,512,512);
  int count=0;float maxDelta=0;
  for(int z=Mathf.Max(17,cz-90);z<Mathf.Min(496,cz+90);z++)
   for(int x=Mathf.Max(17,cx-94);x<Mathf.Min(496,cx+94);x++){
    float dx=(x-cx)/sx,dz=(z-cz)/sz;
    float rr=Mathf.Sqrt(dx*dx+dz*dz);
    float inner=Smooth(.30f,.72f,rr),outer=1f-Smooth(1.00f,1.42f,rr);
    float detail=Mathf.PerlinNoise(x*.048f+23f,z*.052f+11f);
    float photoRock=px[z*513+x].g/255f;
    float strength=inner*outer*Smooth(.28f,.72f,photoRock)*(.70f+.30f*detail);
    if(strength<.08f)continue;
    float ashGoal=Mathf.Lerp(a[z,x,rock],.91f,.83f*strength);
    float delta=ashGoal-a[z,x,rock];
    if(delta<.001f)continue;
    float rest=1f-a[z,x,rock],newRest=1f-ashGoal;
    if(rest>.001f)for(int k=0;k<d.alphamapLayers;k++)
      if(k!=rock)a[z,x,k]*=newRest/rest;
    a[z,x,rock]=ashGoal;
    maxDelta=Mathf.Max(maxDelta,delta);count++;
   }
  if(count<250)throw new Exception(n+" photo ash coverage insufficient "+count);
  d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();EditorUtility.SetDirty(d);
  File.AppendAllText(V+"photo_volcano_ash_20260926.txt",
   n+" rockAdjustedPixels="+count+" maxRockIncrease="+maxDelta.ToString("F3")+
   " center=("+cx+","+cz+") originalHeightWrites=0 canonicalSourceWrites=0\n");
 }
}

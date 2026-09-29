using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

// The map is MASK DATA only. Visible ground uses the original tiled materials.
public static class MMReferenceTiledGround {
 const string E="Assets/World/WorldExtensions/Generated/";
 const string D="Assets/World/DragonIsle/Generated/";
 const int RES=512;
 public static bool Enabled => true;
 static readonly string[] Names={
  "SweetWater_North","Kriegspire_North","FrozenHighlands_North",
  "SilverCove_North","ParadiseValley_West",
  "DragonIsle_North","DragonIsle_South"
 };
 static float S(float a,float b,float v){
  float t=Mathf.Clamp01((v-a)/Mathf.Max(.0001f,b-a));
  return t*t*(3f-2f*t);
 }
 static int Find(TerrainLayer[] ls,params string[] terms){
  return Array.FindIndex(ls,l=>l && terms.Any(t=>
   l.name.IndexOf(t,StringComparison.OrdinalIgnoreCase)>=0));
 }
 static string TerrainPath(string n){
  if(n=="DragonIsle_North")return D+"DragonIsleNorthTerrain.asset";
  if(n=="DragonIsle_South")return D+"DragonIsleSouthTerrain.asset";
  return E+n+"Terrain.asset";
 }

 static void Paint(float[] w,int[] ix,bool north,bool west,
  Color32 p,float y,float slope,int x,int z){
  float d=(p.r-128f)*512f/(148f*3f);
  float land=S(-5f,10f,d),shore=(1f-S(1f,24f,d))*land;
  float steep=S(15f,43f,slope),pale=p.g/255f,shade=p.b/255f;
  float grass=0f,light=0f,sand=0f,rock=0f,snow=0f,dirt=0f;
  if(north){
   // Mainland is predominantly snow; source photo only controls coverage.
   float highpass=Mathf.Clamp01((128f-p.a)/115f);
   snow=land*(.79f+.36f*pale+.30f*shore)*(1f-.30f*steep);
   rock=land*(.10f+1.42f*steep+.30f*shade+.27f*highpass);
   grass=land*(.09f+.28f*(1f-pale))*(1f-S(12f,95f,z));
   light=.28f*grass;
   dirt=land*.075f; sand=.24f*shore;
  }else if(west){
   // Reference mainland: forest floor, narrow beach and exposed cliff.
   grass=land*(1.12f-.52f*steep);
   light=land*(.40f-.18f*steep);
   rock=land*(.14f+1.32f*steep+.13f*shade);
   sand=.95f*shore+.09f*(1f-land);
   dirt=land*.12f;
  }else{
   // Dragon: pale arid north; greener southern island and eastern mainland.
   bool south=z<256;
   grass=land*(south?.74f:.23f)*(1f-.48f*steep);
   sand=land*(south?.28f:.74f)+.72f*shore;
   dirt=land*(south?.27f:.42f);
   rock=land*(.14f+1.28f*steep+.14f*shade);
   snow=land*Mathf.Max(0f,pale-.77f)*.35f;
  }

  sand+=(1f-land)*1.1f;
  rock+=(1f-land)*.09f;
  Add(w,ix[0],grass);Add(w,ix[1],light);Add(w,ix[2],sand);
  Add(w,ix[3],rock);Add(w,ix[4],snow);Add(w,ix[5],dirt);
  Normalize(w);
 }
 static void Add(float[] w,int i,float v){
  if(i>=0)w[i]+=Mathf.Max(0f,v);
 }
 static void Normalize(float[] w){
  float sum=0f;
  foreach(float v in w)sum+=v;
  if(sum<.000001f){w[0]=1f;return;}
  for(int i=0;i<w.Length;i++)w[i]/=sum;
 }
 public static void ApplyOne(string name){
  if(!Names.Contains(name))throw new ArgumentException("Not a generated reference tile: "+name);
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath(name));
  if(!td||td.heightmapResolution!=513||td.alphamapResolution!=512)
   throw new Exception("Generated TerrainData absent: "+name);
  var oldLayers=td.terrainLayers;
  var layers=oldLayers.Where(l=>l && !l.name.EndsWith("_ReferenceTerrainLayer",
   StringComparison.OrdinalIgnoreCase)).ToArray();
  if(name=="DragonIsle_South")layers=layers.Where(l=>
   !l.name.StartsWith("Realistic_",StringComparison.OrdinalIgnoreCase) &&
   !l.name.StartsWith("SweetWaterWest_",StringComparison.OrdinalIgnoreCase) &&
   !l.name.StartsWith("ParadiseNorth_",StringComparison.OrdinalIgnoreCase)).ToArray();
  if(layers.Length!= (name.StartsWith("DragonIsle")?6:7))
   throw new Exception("Unexpected tiled layer set "+name+": "+layers.Length);
  foreach(var l in layers){
   if(!l.diffuseTexture||l.tileSize.x>20f||l.tileSize.y>20f)
    throw new Exception("Not a detailed repeating ground material: "+l.name);
  }
  var old=td.GetAlphamaps(0,0,RES,RES);
  var h=td.GetHeights(0,0,513,513);
  var map=MMReferenceEdgeProfiles.Load(name);
  if(map==null)throw new Exception("Coastline mask missing "+name);

  bool north=name=="SweetWater_North"||name=="Kriegspire_North"||
   name=="FrozenHighlands_North"||name=="SilverCove_North";
  bool west=name=="ParadiseValley_West";
  int[] ix={Find(layers,"_Green","Grass"),Find(layers,"LightGreen"),
   Find(layers,"Desert","Sand"),Find(layers,"Volcanic","Rock"),
   Find(layers,"Snow"),Find(layers,"Arid","Dirt")};
  if(ix[0]<0||ix[2]<0||ix[3]<0||ix[5]<0||(north&&ix[4]<0))
   throw new Exception("Canonical ground layers missing for "+name);
  int[] oldIx=layers.Select(l=>Array.IndexOf(oldLayers,l)).ToArray();
  var updated=new float[RES,RES,layers.Length];
  var w=new float[layers.Length];
  int landSamples=0;float weightedSnow=0f;float maxPhoto=0f;
  int photoIndex=Array.FindIndex(oldLayers,l=>l&&
   l.name.EndsWith("_ReferenceTerrainLayer",StringComparison.OrdinalIgnoreCase));
  for(int z=0;z<RES;z++)for(int x=0;x<RES;x++){
   Array.Clear(w,0,w.Length);
   Color32 p=map.At(x,z);
   float d=(p.r-128f)*512f/(148f*3f);
   float dx=(h[z,Mathf.Min(512,x+1)]-h[z,Mathf.Max(0,x-1)])*160f;
   float dz=(h[Mathf.Min(512,z+1),x]-h[Mathf.Max(0,z-1),x])*160f;
   float slope=Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz))*Mathf.Rad2Deg;
   float y=-24f+h[z,x]*320f;
   Paint(w,ix,north,west,p,y,slope,x,z);
   // Match only the immediate border of the unchanged source region.
   float border=north?1f-S(0f,12f,z):1f-S(0f,12f,511f-x);
   if(west)border=Mathf.Max(border,Mathf.Max(
    1f-S(0f,7f,z),1f-S(0f,7f,511f-z)));
   if(name=="DragonIsle_South")border=Mathf.Max(border,1f-S(0f,8f,z));
   if(border>.00001f){
    float sum=0f;
    for(int k=0;k<w.Length;k++)sum+=old[z,x,oldIx[k]];
    if(sum>.01f){
     for(int k=0;k<w.Length;k++)
      w[k]=Mathf.Lerp(w[k],old[z,x,oldIx[k]]/sum,border);
     Normalize(w);
    }
   }

   for(int k=0;k<w.Length;k++)updated[z,x,k]=w[k];
   if(d>0f)landSamples++;
   if(ix[4]>=0)weightedSnow+=w[ix[4]];
   if(photoIndex>=0)maxPhoto=Mathf.Max(maxPhoto,old[z,x,photoIndex]);
  }
  // TerrainData and referenced scene GUIDs stay unchanged; only alphamaps
  // and the generated tile's layer array change. No height write.
  td.terrainLayers=layers;
  td.SetAlphamaps(0,0,updated);
  EditorUtility.SetDirty(td);
  float biggestTile=layers.Max(l=>Mathf.Max(l.tileSize.x,l.tileSize.y));
  string line=name+",tiled_layers="+layers.Length+
   ",max_tile_m="+biggestTile.ToString("F1")+
   ",land_mask_pct="+(landSamples*100f/(RES*RES)).ToString("F2")+
   ",mean_snow="+(weightedSnow/(RES*RES)).ToString("F3")+
   ",photo_weight_removed="+maxPhoto.ToString("F3")+
   ",heightmap_writes=0";
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.AppendAllText("Validation/EdgeGrid20260923/tiled_ground_repaint.csv",
   line+Environment.NewLine);
  if(name=="DragonIsle_South")MMDragonSouthSweetWaterMaterialBlend.Apply();
  Debug.Log("TILED_REFERENCE_GROUND "+line);
 }
 [MenuItem("MMUnity/World/Replace Blurry Reference Photo With Tiled Ground")]
 public static void ApplyAll(){
  Directory.CreateDirectory("Validation/EdgeGrid20260923");
  File.WriteAllText("Validation/EdgeGrid20260923/tiled_ground_repaint.csv",
   "tile,tiles,spacing,mask_land,snow,old_photo,heightmap"+Environment.NewLine);
  foreach(string n in Names)ApplyOne(n);
  AssetDatabase.SaveAssets();
  Debug.Log("TILED_REFERENCE_GROUND_COMPLETE "+Names.Length+"/7");
 }
}


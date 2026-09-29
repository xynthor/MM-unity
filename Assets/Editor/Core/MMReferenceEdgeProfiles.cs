using UnityEngine;
using System;
using System.IO;

// Coastline and biome follow the supplied Enroth graphic, never generated
// by an arbitrary shoreline algorithm. The source JPEG and sampling grid
// are documented in Validation/EdgeGrid20260923/ReferenceMasks/manifest.json.
public static class MMReferenceEdgeProfiles {
 public sealed class Mask {
  public readonly Color32[] data;
  public readonly string name;
  public Mask(Color32[] rgba,string id){data=rgba;name=id;}
  public Color32 At(int x,int z){return data[Mathf.Clamp(z,0,512)*513+Mathf.Clamp(x,0,512)];}
 }
 public static Mask Load(string scene) {
  bool required=scene=="SweetWater_North"||scene=="Kriegspire_North"||
     scene=="FrozenHighlands_North"||scene=="SilverCove_North"||
     scene=="ParadiseValley_West"||scene=="DragonIsle_North"||scene=="DragonIsle_South";
  if(!required)return null;
  string path=Path.GetFullPath(Path.Combine("Validation","EdgeGrid20260923","ReferenceMasks",scene+".png"));
  if(!File.Exists(path))throw new FileNotFoundException("Authoritative Enroth reference trace missing",path);
  var tex=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
  if(!tex.LoadImage(File.ReadAllBytes(path))||tex.width!=513||tex.height!=513)
   throw new IOException("Wrong Enroth reference mask dimensions "+path);
  var colors=tex.GetPixels32();UnityEngine.Object.DestroyImmediate(tex);
  return new Mask(colors,scene);
 }
 static float Smooth(float lo,float hi,float v){
  float t=Mathf.Clamp01((v-lo)/Mathf.Max(.0001f,hi-lo));
  return t*t*(3f-2f*t);
 }
 public static float Height(Mask m,int x,int z,float seamY,int col,int row,bool north){
  var p=m.At(x,z);
  float signedMeters=(p.r-128f)*512f/(148f*3f);
  float snow=p.g/255f,shade=p.b/255f;
  float coast=Smooth(-11f,11f,signedMeters);
  float interior=Smooth(2f,57f,signedMeters);
  float worldX=(col-2)*512f-256f+x;
  float worldZ=(row-1)*512f-256f+z;
  float n0=Mathf.PerlinNoise(worldX*.012f+30.2f,worldZ*.012f+98.4f);
  float n1=Mathf.PerlinNoise(worldX*.033f+18.7f,worldZ*.033f+42.5f);
  float relief=north ? 10f+42f*snow+8f*shade+(n0-.5f)*14f+(n1-.5f)*3f :
                       2f+11f*shade+(n0-.5f)*8f;
  float y=-7f+14f*coast+Mathf.Max(0,relief)*interior;
  if(!north){
   // The coast is traced in the reference MASK, not inferred from the
   // canonical tile's western edge elevation. That old global elevation
   // gate erased the southwest headland in a conspicuous straight line.
   // Fade only the final southern 105m into the adjacent ocean square.
   float southFade=Smooth(18f,108f,z);
   y=Mathf.Lerp(-7f,y,southFade);
  }
  // Keep the authoritative canonical 512m source tile seam EXACT.
  float u=north ? z/512f : (512f-x)/512f;
  // Keep the traced western peninsula, blending its last 77m into the
  // unmodified canonical source edge without importing inland geometry.
  // Keep the source border exact, but do not drag a generic transition tens of metres inland.
  // The former 0.12 north fade made a conspicuous straight gray band across all four tiles.
  float fade=Smooth(0f,north?.045f:.15f,u);
  return Mathf.Lerp(seamY,y,fade);
 }
 public static void Biome(Mask m,int x,int z,bool north,float slope,
   ref float green,ref float light,ref float sand,ref float volcanic,
   ref float snow,ref float arid) {
  var p=m.At(x,z);
  float d=(p.r-128f)*512f/(148f*3f);
  float land=Smooth(-5f,11f,d);
  float pale=p.g/255f;
  float rock=Smooth(18f,37f,slope);
  if(north) {
   float coastIce=(1f-Smooth(24f,115f,d))*land;
   // Reference north coast is predominantly white fractured snow, not
   // the gray soil band visible in the previous overview. The snow term
   // follows the photograph's snow mask and increases near the ice shelf.
   snow+=3.6f*pale*land+2.0f*coastIce;
   // A is a source-photo high-pass channel. Shadows paint exposed rock and
   // bright peaks paint snow; the R coastline and G/B relief are unchanged.
   // Feather source-photo rock detail into the preserved original region
   // over the last 110m, so there is no abrupt east-west texture bar.
   float inlandDetail=Smooth(8f,48f,d)*land*Smooth(18f,110f,z);
   float photoRock=Mathf.Clamp01((128f-p.a)/100f);
   float photoSnow=Mathf.Clamp01((p.a-128f)/100f);
   volcanic+=1.55f*photoRock*inlandDetail;
   snow+=.68f*photoSnow*inlandDetail;
   sand*=1f-.92f*coastIce;
   volcanic+=.38f*rock*land;
   green*=1f-.96f*pale*land;
   light*=1f-.90f*pale*land;
   arid*=1f-.60f*pale*land;
   sand*=1f-.85f*pale*land;
  } else {
   // The reference shows a predominantly wooded Paradise Valley western
   // peninsula with rock on cliffs and sand only along the waterline.
   green+=1.52f*land*(1f-rock);
   light+=.54f*land*(1f-rock);
   volcanic+=.42f*rock*land;
   arid+=.13f*land;
   sand+=.20f*(1f-land);
  }
 }
}

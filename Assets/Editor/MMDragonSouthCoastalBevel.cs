using UnityEngine;using UnityEditor;using System;using System.IO;
public static class MMDragonSouthCoastalBevel{
 const string T="Assets/World/DragonIsle/Generated/DragonIsleSouthTerrain.asset";
 const string M="Validation/EdgeGrid20260923/ReferenceMasks/DragonSouth_SweetWaterWest_MainlandOnly.png";
 const int R=18;
 static float Ease(float t){t=Mathf.Clamp01(t);return t*t*(3f-2f*t);}
 [MenuItem("MMUnity/World/Refine Dragon South Authority Shore Height Ramp")]
 public static void Apply(){
  var td=AssetDatabase.LoadAssetAtPath<TerrainData>(T);if(!td||td.heightmapResolution!=513)throw new Exception("Dragon South terrain unavailable");
  var tex=new Texture2D(2,2);tex.LoadImage(File.ReadAllBytes(Path.GetFullPath(M)));if(tex.width!=513||tex.height!=513)throw new Exception("Bad shoreline mask");
  var m=tex.GetPixels32();UnityEngine.Object.DestroyImmediate(tex);var h=td.GetHeights(0,0,513,513);var old=(float[,])h.Clone();
  int changed=0;float maxMove=0,edgeBefore=0,edgeAfter=0;
  for(int z=13;z<500;z++)for(int x=13;x<500;x++){
   if(m[z*513+x].r<128)continue;float best=R+1;
   for(int dz=-R;dz<=R;dz++){int zz=z+dz;if(zz<0||zz>512)continue;int rem=Mathf.FloorToInt(Mathf.Sqrt(R*R-dz*dz));
    for(int dx=-rem;dx<=rem;dx++){int xx=x+dx;if(xx<0||xx>512)continue;if(m[zz*513+xx].r>=128)continue;float ds=Mathf.Sqrt(dx*dx+dz*dz);if(ds<best)best=ds;}}
   if(best>R)continue;float y=-24f+old[z,x]*320f;float s=Ease((best-1f)/(R-1f));
   float shoreY=.20f;float target=shoreY*(1f-s)+y*s;
   float ny=Mathf.Max(shoreY-.10f,target);float hn=Mathf.Clamp01((ny+24f)/320f);
   if(Mathf.Abs(hn-h[z,x])>.000001f){maxMove=Mathf.Max(maxMove,Mathf.Abs(hn-h[z,x])*320f);h[z,x]=hn;changed++;}
  }
  for(int z=1;z<512;z++)for(int x=1;x<512;x++){if(m[z*513+x].r<128)continue;bool edge=false;
   for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)if(m[(z+dz)*513+x+dx].r<128)edge=true;
   if(edge){edgeBefore=Mathf.Max(edgeBefore,-24f+old[z,x]*320f);edgeAfter=Mathf.Max(edgeAfter,-24f+h[z,x]*320f);}}
  td.SetHeights(0,0,h);EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();
  Directory.CreateDirectory("Validation/EdgeGrid20260923");File.WriteAllText("Validation/EdgeGrid20260923/dragon_south_coastal_bevel.txt",
   "changed_vertices="+changed+"\nmax_height_change_m="+maxMove.ToString("F3")+"\nedge_max_before_m="+edgeBefore.ToString("F3")+"\nedge_max_after_m="+edgeAfter.ToString("F3")+"\nprotected_border_m=12\ncanonical_sweetwater_edits=0\n");
  Debug.Log("DRAGON_SOUTH_COASTAL_BEVEL_OK changed="+changed+" edge_max "+edgeBefore.ToString("F2")+" -> "+edgeAfter.ToString("F2")+" maxmove="+maxMove.ToString("F2"));
 }
}
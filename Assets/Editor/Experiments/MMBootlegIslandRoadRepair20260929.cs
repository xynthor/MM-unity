using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class MMBootlegIslandRoadRepair20260929
{
 const string Path="Assets/World/WorldExtensions/Generated/LinkedSurfaceInputs/Terrain/945f7afc4026cbb418809d32f2d282cd.asset";
 const string Backup="Backups/BeforeBootlegIslandRoadRepair_20260929/945f7afc4026cbb418809d32f2d282cd.asset";
 public static void Apply(bool refineOwnPatch=false)
 {
  if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
  string guard=refineOwnPatch?"Backups/BeforeBootlegShoreRefinement_20260929/945f7afc4026cbb418809d32f2d282cd.asset":Backup;
  using(var hash=SHA256.Create())if(!hash.ComputeHash(File.ReadAllBytes(Path)).SequenceEqual(hash.ComputeHash(File.ReadAllBytes(guard))))throw new Exception("Bootleg terrain changed since verified backup");
  var t=Terrain.activeTerrains.Single(t=>AssetDatabase.GetAssetPath(t.terrainData)==Path);var d=t.terrainData;
  int road=Array.FindIndex(d.terrainLayers,l=>l.name=="Realistic_RoadOverlay"),green=Array.FindIndex(d.terrainLayers,l=>l.name=="Realistic_Green"),light=Array.FindIndex(d.terrainLayers,l=>l.name=="Realistic_LightGreen"),sand=Array.FindIndex(d.terrainLayers,l=>l.name=="Realistic_Desert"),soil=Array.FindIndex(d.terrainLayers,l=>l.name=="Realistic_Arid");
  if(new[]{road,green,light,sand,soil}.Any(k=>k<0))throw new Exception("Required named channels missing");
  var tile=File.ReadAllBytes("Assets/World/BootlegBay/Data/tilemap_u8.bin");var groups=File.ReadAllBytes("Assets/World/BootlegBay/Data/tile_groups_u8.bin");
  var a=d.GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);int changed=0,underwater=0,protectedRoad=0;double removed=0;float maxError=0;
  if(refineOwnPatch){
   // Read our pre-patch paint through Unity, without restoring any world file.
   // The SHA guard above rules out intervening manual changes.
   const string temp="Assets/Editor/MMTemporaryBootlegPaintRead.asset";
   if(File.Exists(temp))throw new Exception("Temporary baseline asset already exists");
   try{FileUtil.CopyFileOrDirectory(Backup,temp);AssetDatabase.ImportAsset(temp,ImportAssetOptions.ForceSynchronousImport);a=AssetDatabase.LoadAssetAtPath<TerrainData>(temp).GetAlphamaps(0,0,d.alphamapWidth,d.alphamapHeight);}
   finally{AssetDatabase.DeleteAsset(temp);}
  }
  int width=a.GetLength(1),rows=a.GetLength(0);var coastDistance=new int[width*rows];var queue=new Queue<int>();
  for(int z=0;z<rows;z++)for(int x=0;x<width;x++){int k=z*width+x;bool water=t.transform.position.y+d.GetInterpolatedHeight((x+.5f)/width,(z+.5f)/rows)<.1f;coastDistance[k]=water?0:100000;if(water)queue.Enqueue(k);}
  while(queue.Count>0){int k=queue.Dequeue(),x=k%width,z=k/width;Visit(x-1,z,k);Visit(x+1,z,k);Visit(x,z-1,k);Visit(x,z+1,k);}
  void Visit(int x,int z,int previous){if(x<0||z<0||x>=width||z>=rows)return;int k=z*width+x;if(coastDistance[k]>coastDistance[previous]+1){coastDistance[k]=coastDistance[previous]+1;queue.Enqueue(k);}}
  for(int z=0;z<a.GetLength(0);z++)for(int x=0;x<a.GetLength(1);x++){
   float lx=(x+.5f)*512/a.GetLength(1),lz=(z+.5f)*512/a.GetLength(0);int sx=Mathf.Clamp((int)(lx/4),0,127),sy=Mathf.Clamp(127-(int)(lz/4),0,127);int group=groups[tile[sy*128+sx]];
   float height=t.transform.position.y+d.GetInterpolatedHeight(lx/512,lz/512);float roadWeight=a[z,x,road];
   if(group==22&&height>=.1f){protectedRoad++;continue;}
   // Group 8 is the broad island substrate here, not the narrow group-22 road.
   // Retain current non-road manual work; transfer only the mistaken road share.
   bool island=group==8;if(!island&&height>=.1f)continue;if(roadWeight<.01f)continue;
   float protection=0;
   if(height>=.1f)for(int dz=-2;dz<=2;dz++)for(int dx=-2;dx<=2;dx++){int xx=sx+dx,yy=sy+dz;if(xx<0||yy<0||xx>=128||yy>=128||groups[tile[yy*128+xx]]!=22)continue;float px=Mathf.Clamp(lx,xx*4,(xx+1)*4),pz=Mathf.Clamp(lz,(127-yy)*4,(128-yy)*4);float distance=Vector2.Distance(new Vector2(lx,lz),new Vector2(px,pz));protection=Mathf.Max(protection,1-Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/2)));}
   float amount=roadWeight*(1-protection);if(amount<.0001f)continue;
   a[z,x,road]-=amount;
   float beach=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((coastDistance[z*width+x]*512f/width-1f)/3f));
   a[z,x,sand]+=amount*beach*.8f;a[z,x,soil]+=amount*(.1f+beach*.1f);a[z,x,green]+=amount*(1-beach)*.68f;a[z,x,light]+=amount*(1-beach)*.22f;
   float sum=0;for(int k=0;k<a.GetLength(2);k++)sum+=a[z,x,k];for(int k=0;k<a.GetLength(2);k++)a[z,x,k]/=sum;
   // Store an exact 255-unit partition so Unity control textures remain
   // normalized after serialization and reimport.
   int used=0;var units=new int[a.GetLength(2)];var fractions=new float[units.Length];
   for(int k=0;k<units.Length;k++){float value=a[z,x,k]*255;units[k]=Mathf.FloorToInt(value);fractions[k]=value-units[k];used+=units[k];}
   for(int q=used;q<255;q++){int best=0;for(int k=1;k<units.Length;k++)if(fractions[k]>fractions[best])best=k;units[best]++;fractions[best]=-1;}
   for(int k=0;k<units.Length;k++)a[z,x,k]=units[k]/255f;
   maxError=Mathf.Max(maxError,Mathf.Abs(sum-1));changed++;removed+=amount;if(height<.1f)underwater++;
  }
  d.SetAlphamaps(0,0,a);d.SetBaseMapDirty();d.SyncTexture(TerrainData.AlphamapTextureName);foreach(var control in d.alphamapTextures)EditorUtility.SetDirty(control);EditorUtility.SetDirty(d);t.Flush();AssetDatabase.SaveAssets();
  File.WriteAllText("Validation/EdgeGrid20260923/WaterQA20260929/BootlegRoadRepair.txt","Changed current paint cells="+changed+" underwaterCells="+underwater+" group22RoadCellsPreserved="+protectedRoad+" removedRoadWeight="+removed.ToString("F3")+" maxPreNormalizationError="+maxError+" heightWrites=0 layerOrderWrites=0 transformWrites=0 canonicalWrites=0\n");Debug.Log(File.ReadAllText("Validation/EdgeGrid20260923/WaterQA20260929/BootlegRoadRepair.txt"));
 }
}

using System;using System.IO;using System.Linq;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEditorInternal;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{
 public void Execute(ExecutionResult result){var s=SceneManager.GetActiveScene();if(s.name!="Enroth"||s.isDirty)throw new Exception("Requires clean linked scene");var rows=new List<string>{"terrain,changed_samples,dry_changes,wet_mask_changes,edge_changes,alphamap_changes,max_dry_delta_m"};
 foreach(var t in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))){var td=t.terrainData;var path="Backups/Coast17_20260919/"+Path.GetFileName(AssetDatabase.GetAssetPath(td));var loaded=InternalEditorUtility.LoadSerializedFileAndForget(path);try{var old=loaded.OfType<TerrainData>().Single();if(old.heightmapResolution!=td.heightmapResolution||old.size!=td.size)throw new Exception("Resolution/size changed");int n=td.heightmapResolution;var a=old.GetHeights(0,0,n,n);var b=td.GetHeights(0,0,n,n);int changed=0,dry=0,wet=0,edge=0,alpha=0;float max=0;
 for(int z=0;z<n;z++)for(int x=0;x<n;x++){float ya=a[z,x]*td.size.y+t.transform.position.y,yb=b[z,x]*td.size.y+t.transform.position.y;if(a[z,x]!=b[z,x]){changed++;if(ya>=.12f){dry++;max=Mathf.Max(max,Mathf.Abs(ya-yb));}if(x<4||z<4||x>=n-4||z>=n-4)edge++;}if((ya<.12f)!=(yb<.12f))wet++;}
 var aa=old.GetAlphamaps(0,0,old.alphamapWidth,old.alphamapHeight);var ab=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);if(aa.Length!=ab.Length)throw new Exception("Alphamap size changed");for(int z=0;z<td.alphamapHeight;z++)for(int x=0;x<td.alphamapWidth;x++)for(int k=0;k<td.alphamapLayers;k++)if(aa[z,x,k]!=ab[z,x,k])alpha++;
 rows.Add(td.name+","+changed+","+dry+","+wet+","+edge+","+alpha+","+max.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
 }finally{foreach(var o in loaded)if(o)UnityEngine.Object.DestroyImmediate(o);}}
 File.WriteAllLines("Validation/Coast17/saved_terrain_audit.csv",rows);result.Log("Saved terrain audit written");
 }
}

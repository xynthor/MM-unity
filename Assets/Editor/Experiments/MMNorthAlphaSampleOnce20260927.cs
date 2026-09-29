using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
[InitializeOnLoad]
internal static class MMNorthAlphaSampleOnce20260927 {
 const string V="Validation/EdgeGrid20260923/";
 static MMNorthAlphaSampleOnce20260927(){EditorApplication.delayCall+=Run;}
 static void Run(){
  string flag=V+"RUN_NORTH_ALPHA_SAMPLE_20260927.flag";
  if(!File.Exists(flag))return;File.Delete(flag);
  var sb=new StringBuilder();
  try{
   foreach(var n in new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"}){
    var data=AssetDatabase.LoadAssetAtPath<TerrainData>(
      "Assets/World/WorldExtensions/Generated/LinkedSourceTransitions/"+
       n+"_LinkedNorthProfile.asset");
    if(!data)throw new Exception("Missing "+n);
    var a=data.GetAlphamaps(0,0,512,512);
    var h=data.GetHeights(0,0,513,513);
    int snow=Array.FindIndex(data.terrainLayers,l=>l&&l.name=="Realistic_Snow");
    int forest=Array.FindIndex(data.terrainLayers,l=>l&&l.name=="Realistic_Green");
    int light=Array.FindIndex(data.terrainLayers,l=>l&&l.name=="Realistic_LightGreen");
    int rock=Array.FindIndex(data.terrainLayers,l=>l&&l.name=="Realistic_Volcanic");
    sb.AppendLine(n);
    foreach(int z in new[]{293,423,503}){
     sb.AppendLine("z="+z+" height_x0="+(-24f+h[z,0]*320f).ToString("F2")+
       " x40="+(-24f+h[z,40]*320f).ToString("F2")+
       " x470="+(-24f+h[z,470]*320f).ToString("F2")+
       " x512="+(-24f+h[z,512]*320f).ToString("F2"));
     foreach(int x in new[]{0,2,10,20,32,42,60,90,421,461,470,480,500,509,511})
      sb.AppendLine("x="+x+" snow="+a[z,x,snow].ToString("F3")+
       " forest="+(a[z,x,forest]+a[z,x,light]).ToString("F3")+
       " rock="+a[z,x,rock].ToString("F3"));
    }
   }
  }catch(Exception ex){sb.AppendLine("FAIL "+ex);}
  File.WriteAllText(V+"north_alpha_edge_samples_20260927.txt",sb.ToString());
 }
}

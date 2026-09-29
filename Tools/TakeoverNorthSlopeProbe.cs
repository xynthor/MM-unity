using UnityEngine;
using UnityEditor;
using System.Linq;
internal class CommandScript : IRunCommand {
 public void Execute(ExecutionResult result){
  foreach(var name in new[]{"SweetWater","Kriegspire","FrozenHighlands","SilverCove"}){
   var a=Terrain.activeTerrains.Single(t=>t.terrainData.name==name+"_LinkedNorthProfile");var b=Terrain.activeTerrains.Single(t=>t.terrainData.name==name+"_NorthTerrain");var ad=a.terrainData;var bd=b.terrainData;
   float worst=0;int at=0;for(int x=0;x<=512;x++){float u=x/512f;if(ad.GetInterpolatedHeight(u,1)+a.transform.position.y<.2||bd.GetInterpolatedHeight(u,0)+b.transform.position.y<.2)continue;float angle=Vector3.Angle(ad.GetInterpolatedNormal(u,1),bd.GetInterpolatedNormal(u,0));if(angle>worst){worst=angle;at=x;}}
   var s=ad.GetHeights(at,504,1,9);var n=bd.GetHeights(at,0,1,9);
   result.Log(name+" worstNormal="+worst+" x="+at+" south8m="+string.Join(",",Enumerable.Range(0,9).Select(i=>(s[i,0]*ad.size.y+a.transform.position.y).ToString("F3")))+" north8m="+string.Join(",",Enumerable.Range(0,9).Select(i=>(n[i,0]*bd.size.y+b.transform.position.y).ToString("F3"))));
  }
 }
}

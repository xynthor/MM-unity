using UnityEngine;
using UnityEditor;
using System.Linq;
internal class CommandScript : IRunCommand {
 public void Execute(ExecutionResult result){
  var terrains=Terrain.activeTerrains;int models=0,missing=0,nonfinite=0,uvBad=0;
  foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).Where(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/World/NewSorpigal/Objects/"))){
   var mesh=f.sharedMesh;var verts=mesh.vertices;var uv=mesh.uv;models++;
   foreach(var p in verts)if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))nonfinite++;
   if(uv.Length!=verts.Length)uvBad++;
   float low=verts.Min(v=>f.transform.TransformPoint(v).y);float min=float.PositiveInfinity,max=float.NegativeInfinity;int wet=0,samples=0;
   foreach(var v in verts){var p=f.transform.TransformPoint(v);if(p.y>low+.25f)continue;
    var t=terrains.FirstOrDefault(t=>p.x>=t.transform.position.x&&p.x<=t.transform.position.x+t.terrainData.size.x&&p.z>=t.transform.position.z&&p.z<=t.transform.position.z+t.terrainData.size.z);
    if(!t){missing++;continue;}float h=t.SampleHeight(p)+t.transform.position.y;float gap=p.y-h;
    min=Mathf.Min(min,gap);max=Mathf.Max(max,gap);if(h<0)wet++;samples++;
   }
   if(min>1f||max< -2f||wet>0)result.Log("CONTACT_REVIEW "+f.name+" baseSamples="+samples+" gapMin="+min.ToString("F3")+" gapMax="+max.ToString("F3")+" belowSeaGround="+wet+" asset="+AssetDatabase.GetAssetPath(mesh));
  }
  result.Log("NewSorpigal models="+models+" nonFiniteVertices="+nonfinite+" UVCountMismatches="+uvBad+" baseSamplesOutsideTerrain="+missing+"; contacts are candidates: bridges/walls require semantic review.");
 }
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using static MMNorthAuthoredTools20261007;

public static class MMNorthForestFinish20261007
{
    static bool InWater(float x,float z,UnityEngine.Mesh mesh)
    {
        var v=mesh.vertices;var triangles=mesh.triangles;
        for(int i=0;i<triangles.Length;i+=3)
        {
            var a=v[triangles[i]];var b=v[triangles[i+1]];var c=v[triangles[i+2]];
            float d=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(d)<.00001f)continue;
            float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/d;
            float w=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/d;
            if(u>=-.015f&&w>=-.015f&&u+w<=1.015f)return true;
        }
        return false;
    }
    public static void Run(int square,bool keep=false)
    {
        var scene=SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Need clean Enroth");
        string report="Validation/SequentialRepair/sq"+square+"_forest_finished.txt";if(File.Exists(report))throw new Exception("Already saved");
        var terrain=Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x-(-1280+512*square))<.1f&&Mathf.Abs(t.transform.position.z-768)<.1f);
        var root=UnityEngine.Object.FindObjectsByType<Transform>().First(t=>t.name.StartsWith("SQ"+square+" Authored")&&t.name.Contains("Forest 20261007"));
        var moved=new List<System.Tuple<Transform,Vector3,Vector3>>();var hidden=new List<GameObject>();var lodSaved=new List<System.Tuple<LODGroup,LOD[]>>();
        var waters=new[]{"Sq0AuthoredRiver20261006.asset","NorthInlandIcyWater20261006.asset"}.Select(n=>AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>("Assets/World/WorldExtensions/Generated/NorthReference20261001/"+n)).ToArray();
        bool saved=false;GameObject sea=null;int removed=0;
        try
        {
            foreach(Transform tr in root)
            {
                if(!tr.gameObject.activeSelf)continue;
                var p=tr.position;float y=Ground(terrain,p.x,p.z);
                float slope=terrain.terrainData.GetSteepness((p.x-terrain.transform.position.x)/512,(p.z-768)/512);
                if(y<.8f||y>86||slope>36||waters.Any(m=>InWater(p.x,p.z,m))){hidden.Add(tr.gameObject);tr.gameObject.SetActive(false);removed++;continue;}
                moved.Add(System.Tuple.Create(tr,p,tr.localScale));tr.localScale*=1.28f;
                var renderers=tr.GetComponentsInChildren<Renderer>();if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);tr.position+=Vector3.up*(y-bounds.min.y-.12f);}
                foreach(var group in tr.GetComponentsInChildren<LODGroup>())
                {
                    var lods=group.GetLODs();lodSaved.Add(System.Tuple.Create(group,lods));
                    if(lods.Length>=2){var replacement=new[]{lods[0],lods[1]};replacement[1].screenRelativeTransitionHeight=.0015f;group.SetLODs(replacement);}
                }
            }
            if(square<=1)foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>())
            {
                var p=tr.position;if(p.x<terrain.transform.position.x||p.x>=terrain.transform.position.x+512||p.z<768||p.z>1280)continue;
                if(tr.name.StartsWith("Pine_")||tr.name.StartsWith("YoungPine_")){hidden.Add(tr.gameObject);tr.gameObject.SetActive(false);}
            }
            var dressing=MMNorthValleyForest20261007.GroundDressing(terrain);foreach(var item in dressing)moved.Add(System.Tuple.Create(item.Key,item.Value,item.Key.localScale));
            sea=SeaPreview(terrain);string dir="Preview/NorthAuthored20261007/ForestSq"+square+"/";Directory.CreateDirectory(dir);float center=terrain.transform.position.x+256;
            Capture(dir,"top",new Vector3(center,850,1024),new Vector3(center,0,1024),true);
            Capture(dir,"oblique",new Vector3(center-346,225,740),new Vector3(center+14,55,1050),false);
            if(keep)
            {
                string backup="Backups/NorthAuthored20261007/ForestSq"+square+"Before";Directory.CreateDirectory(backup);string dest=Path.Combine(backup,"Enroth.unity");if(!File.Exists(dest))File.Copy(scene.path,dest);
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");saved=true;
                File.WriteAllText(report,"Grounded production conifers; mesh LOD retained overhead; removed="+removed+" active="+root.Cast<Transform>().Count(t=>t.gameObject.activeSelf));
            }
        }
        finally
        {
            if(sea){var mesh=sea.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sea);UnityEngine.Object.DestroyImmediate(mesh);}
            if(!saved){foreach(var item in moved)if(item.Item1){item.Item1.position=item.Item2;item.Item1.localScale=item.Item3;}foreach(var go in hidden)if(go)go.SetActive(true);foreach(var item in lodSaved)if(item.Item1)item.Item1.SetLODs(item.Item2);}
        }
    }
}


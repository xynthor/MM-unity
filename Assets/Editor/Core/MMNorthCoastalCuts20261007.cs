using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using static MMNorthAuthoredTools20261007;

public static class MMNorthCoastalCuts20261007
{
    // Each surveyed cut is independent; these do not regenerate the four-square region.
    static readonly Vector4[][] Central={
        new[]{new Vector4(-150,1240,0,21),new Vector4(-144,1206,15,24),new Vector4(-122,1168,33,21),new Vector4(-135,1124,38,18)},
        new[]{new Vector4(8,1245,0,23),new Vector4(-6,1210,10,26),new Vector4(15,1172,29,22),new Vector4(0,1128,32,17)}
    };
    static float Lower(float x,float z,float y,Vector4[] nodes)
    {
        float best=999,bed=0,width=0;
        for(int i=0;i<nodes.Length-1;i++)
        {
            var a=nodes[i];var b=nodes[i+1];var ab=new Vector2(b.x-a.x,b.y-a.y);
            float q=Mathf.Clamp01(Vector2.Dot(new Vector2(x-a.x,z-a.y),ab)/ab.sqrMagnitude);
            float d=Vector2.Distance(new Vector2(x,z),new Vector2(a.x+q*ab.x,a.y+q*ab.y));
            if(d<best){best=d;bed=Mathf.Lerp(a.z,b.z,q);width=Mathf.Lerp(a.w,b.w,q);}
        }
        return Mathf.Min(y,Mathf.Lerp(bed,y,S(0,width*2.7f,best)));
    }
    public static void PreviewCentral(bool keep=false)
    {
        var scene=SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Need clean Enroth");
        if(keep&&File.Exists("Validation/SequentialRepair/sq2_coastal_cuts_kept.txt"))throw new Exception("Already saved");
        var t=Terrain.activeTerrains.First(t=>AssetDatabase.GetAssetPath(t.terrainData).EndsWith("/FrozenHighlands_NorthTerrain.asset"));
        var data=t.terrainData;var clone=UnityEngine.Object.Instantiate(data);clone.hideFlags=HideFlags.HideAndDontSave;
        var h=data.GetHeights(0,0,513,513);
        for(int z=330;z<=512;z++)for(int x=24;x<489;x++)
        {
            float y=h[z,x]*320-24;if(y<.15f)continue;float candidate=y;
            foreach(var cut in Central)candidate=Lower(x-256,z+768,candidate,cut);
            float weight=S(24,60,x)*(1-S(452,488,x));h[z,x]=Mathf.Lerp(y,candidate,weight)/320+24f/320;
        }
        var collider=t.GetComponent<TerrainCollider>();bool saved=false;GameObject sea=null;var moved=new List<KeyValuePair<Transform,Vector3>>();
        try
        {
            clone.SetHeights(0,0,h);t.terrainData=clone;if(collider)collider.terrainData=clone;t.Flush();sea=SeaPreview(t);
            foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>().Where(tr=>tr.name.StartsWith("AuthoredPine_")&&tr.position.x>-256&&tr.position.x<256&&tr.position.z>1098&&tr.position.z<1280))
            {moved.Add(new KeyValuePair<Transform,Vector3>(tr,tr.position));tr.position+=Vector3.up*(Ground(t,tr.position.x,tr.position.z)-GroundHeight(data,tr.position.x,tr.position.z));}
            string dir="Preview/NorthAuthored20261007/Sq2CoastalCuts/";Directory.CreateDirectory(dir);
            Capture(dir,"top",new Vector3(0,850,1024),new Vector3(0,0,1024),true);
            Capture(dir,"shore",new Vector3(0,24,1450),new Vector3(0,55,1180),false);
            Capture(dir,"oblique",new Vector3(-346,225,740),new Vector3(14,55,1050),false);
            Capture(dir,"cross",new Vector3(124,220,1350),new Vector3(224,45,950),false);
            Capture(dir,"transition",new Vector3(0,155,530),new Vector3(0,35,950),false);
            if(keep)
            {
                string dirBackup="Backups/NorthAuthored20261007/Sq2CoastalCutsBefore";Directory.CreateDirectory(dirBackup);
                foreach(var path in new[]{AssetDatabase.GetAssetPath(data),scene.path}){string dst=Path.Combine(dirBackup,Path.GetFileName(path));if(!File.Exists(dst))File.Copy(path,dst);}
                data.SetHeights(0,0,h);EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);t.terrainData=data;if(collider)collider.terrainData=data;
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");saved=true;
                File.WriteAllText("Validation/SequentialRepair/sq2_coastal_cuts_kept.txt","Two bounded snow-filled seaward passes; lake, river, paint and neighbor edges unchanged.");
            }
        }
        finally
        {
            if(sea){var mesh=sea.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sea);UnityEngine.Object.DestroyImmediate(mesh);}
            if(!saved)foreach(var item in moved)if(item.Key)item.Key.position=item.Value;
            t.terrainData=data;if(collider)collider.terrainData=data;t.Flush();UnityEngine.Object.DestroyImmediate(clone);
        }
    }
    static float GroundHeight(TerrainData data,float x,float z){return data.GetInterpolatedHeight((x+256)/512,(z-768)/512)-24;}
}

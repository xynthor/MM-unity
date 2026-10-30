using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using static MMNorthAuthoredTools20261007;

public static class MMNorthSquare1Authored20261007
{
    const string Dir="Preview/NorthAuthored20261007/Sq1Candidate3/";
    static readonly Vector4[][] Chains={
        new[]{new Vector4(-785,1050,75,110),new Vector4(-711,1103,147,98),new Vector4(-661,1118,120,105),new Vector4(-608,1160,220,112),new Vector4(-557,1186,275,100),new Vector4(-515,1211,171,85),new Vector4(-454,1225,250,106),new Vector4(-408,1195,140,86),new Vector4(-352,1233,238,91),new Vector4(-289,1193,115,100),new Vector4(-230,1210,175,105)},
        new[]{new Vector4(-780,860,42,105),new Vector4(-712,907,98,98),new Vector4(-681,950,61,88),new Vector4(-649,1001,133,91),new Vector4(-589,1041,72,94),new Vector4(-559,1090,125,83),new Vector4(-557,1186,275,100)},
        new[]{new Vector4(-280,780,8,90),new Vector4(-330,860,65,95),new Vector4(-314,918,115,86),new Vector4(-372,970,80,90),new Vector4(-348,1021,148,94),new Vector4(-400,1079,100,90),new Vector4(-421,1137,185,85),new Vector4(-454,1225,250,106)},
        new[]{new Vector4(-649,1001,133,48),new Vector4(-700,1020,61,54),new Vector4(-742,1040,30,48)},
        new[]{new Vector4(-559,1090,125,42),new Vector4(-510,1052,58,49),new Vector4(-482,1011,19,45)},
        new[]{new Vector4(-557,1186,275,49),new Vector4(-501,1140,119,62),new Vector4(-484,1083,28,48)},
        new[]{new Vector4(-348,1021,148,42),new Vector4(-400,988,67,52),new Vector4(-449,958,17,44)},
        new[]{new Vector4(-712,907,98,40),new Vector4(-663,866,42,46),new Vector4(-610,845,8,40)}
    };
    static readonly Vector2[] CoastStations={new Vector2(-768,1200),new Vector2(-720,1180),new Vector2(-682,1206),new Vector2(-640,1230),new Vector2(-590,1250),new Vector2(-540,1265),new Vector2(-490,1260),new Vector2(-450,1270),new Vector2(-410,1250),new Vector2(-370,1265),new Vector2(-320,1250),new Vector2(-280,1245),new Vector2(-256,1240)};
    static float Coast(float x){for(int i=0;i<CoastStations.Length-1;i++)if(x<=CoastStations[i+1].x)return Mathf.Lerp(CoastStations[i].y,CoastStations[i+1].y,Mathf.InverseLerp(CoastStations[i].x,CoastStations[i+1].x,x))+2*Mathf.Sin(x*.14f);return 1240;}
    static float RiverX(float z){float q=Mathf.InverseLerp(1018,1160,z);return Mathf.Lerp(-60,-310,q)+18*Mathf.Sin(q*6.283f+1.4f)+5.04f*Mathf.Sin(q*15.1f+.7f);}
    static float RiverY(float z){return Mathf.Lerp(21.05f,25.05f,Mathf.InverseLerp(1018,1160,z));}
    static float Cut(float x,float z,float y,Vector4[] nodes){float distance=999,bed=0,width=0;for(int i=0;i<nodes.Length-1;i++){var a=nodes[i];var b=nodes[i+1];Vector2 ab=new Vector2(b.x-a.x,b.y-a.y);float q=Mathf.Clamp01(Vector2.Dot(new Vector2(x-a.x,z-a.y),ab)/ab.sqrMagnitude);float d=Vector2.Distance(new Vector2(x,z),new Vector2(a.x+ab.x*q,a.y+ab.y*q));if(d<distance){distance=d;bed=Mathf.Lerp(a.z,b.z,q);width=Mathf.Lerp(a.w,b.w,q);}}if(distance<width*2.7f)y=Mathf.Min(y,Mathf.Lerp(bed,y,S(0,width*2.7f,distance)));return y;}
    public static void Preview(bool keep=false)
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty)throw new Exception("Preserve active scene; expected clean Enroth");
        if(keep&&File.Exists("Validation/SequentialRepair/sq1_kept.txt"))throw new Exception("Square1 already kept; inspect current state before another mutation");
        var t=Terrain.activeTerrains.First(t=>AssetDatabase.GetAssetPath(t.terrainData).EndsWith("/Kriegspire_NorthTerrain.asset"));
        var south=Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x+768)<.1f&&Mathf.Abs(t.transform.position.z-256)<.1f);
        var west=Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x+1280)<.1f&&Mathf.Abs(t.transform.position.z-768)<.1f);
        var east=Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x+256)<.1f&&Mathf.Abs(t.transform.position.z-768)<.1f);
        var original=t.terrainData;var clone=UnityEngine.Object.Instantiate(original);clone.hideFlags=HideFlags.HideAndDontSave;
        var collider=t.GetComponent<TerrainCollider>();var h=new float[513,513];
        for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)
        {
            float wx=-768+x,wz=768+z,edge=Ground(south,wx,768),gradient=Mathf.Clamp((edge-Ground(south,wx,744))/24,-.18f,.18f);
            float floor=9+7*S(820,1100,wz)+2.5f*Mathf.PerlinNoise(wx*.015f,wz*.015f),y=floor;
            foreach(var chain in Chains)y=Mathf.Max(y,floor+Ridge(wx,wz,chain));
            y+=Mathf.Clamp01((y-18)/65)*(9*(Mathf.PerlinNoise(wx*.037f,wz*.037f)-.5f)+4*(Mathf.PerlinNoise(wx*.083f,wz*.083f)-.5f));
            y=Mathf.Lerp(edge+gradient*Mathf.Min(z,60),y,S(0,105,z));
            y=Cut(wx,wz,y,new[]{new Vector4(-684,1210,0,24),new Vector4(-668,1170,12,23),new Vector4(-687,1128,32,21),new Vector4(-662,1084,25,18)});y=Cut(wx,wz,y,new[]{new Vector4(-496,1280,0,20),new Vector4(-484,1231,10,20),new Vector4(-499,1188,29,18),new Vector4(-477,1137,37,17),new Vector4(-489,1106,22,16)});float shore=Coast(wx)-wz;
            if(shore<0)y=-7.5f+7.62f*Mathf.Exp(shore/9);
            else y=Mathf.Min(y,.12f+shore*(.65f+1.65f*Mathf.PerlinNoise(wx*.014f,3.7f))+.0035f*shore*shore);
            if(x<48)y=Mathf.Lerp(Ground(west,-768-x,wz),y,S(0,48,x));
            if(x>480)y=Mathf.Lerp(y,Ground(east,-256+512-x,wz),S(480,512,x));
            if(wz>=1018&&wz<=1177){float d=Mathf.Abs(wx-RiverX(wz)),surface=RiverY(wz);if(d<49)y=Mathf.Min(y,Mathf.Lerp(surface-.85f,y,S(11,49,d)));}
            if(z==0)y=edge;
            h[z,x]=Mathf.Clamp01((y+24)/320);
        }
        var before=(float[,])h.Clone();Erode(h);for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)h[z,x]=Mathf.Lerp(before[z,x],h[z,x],.34f);
        bool kept=false;GameObject sea=null,forest=null;var hidden=new List<GameObject>();
        if(keep){string backup="Backups/NorthAuthored20261007/Sq1Before";Directory.CreateDirectory(backup);foreach(var path in new[]{AssetDatabase.GetAssetPath(original),scene.path}){var dest=Path.Combine(backup,Path.GetFileName(path));if(!File.Exists(dest))File.Copy(path,dest);}}
        try
        {
            clone.SetHeights(0,0,h);t.terrainData=clone;if(collider)collider.terrainData=clone;t.Flush();
            sea=SeaPreview(t);
            forest=MMNorthValleyForest20261007.Preview(t,hidden,(x,z,y)=>z<1140&&!(z>=1018&&z<=1177&&Mathf.Abs(x-RiverX(z))<14),71008);
            Directory.CreateDirectory(Dir);
            Capture(Dir,"top",new Vector3(-512,850,1024),new Vector3(-512,0,1024),true);
            Capture(Dir,"transition",new Vector3(-512,155,530),new Vector3(-512,30,950),false);
            Capture(Dir,"oblique",new Vector3(-858,225,740),new Vector3(-498,65,1050),false);
            Capture(Dir,"shore",new Vector3(-588,26,1475),new Vector3(-538,95,1180),false);
            Capture(Dir,"cross",new Vector3(-388,220,1350),new Vector3(-288,45,950),false);
            if(keep)
            {
                original.SetHeights(0,0,h);EditorUtility.SetDirty(original);AssetDatabase.SaveAssetIfDirty(original);
                forest.name="SQ1 Authored Valley Conifer Forest 20261007";foreach(var tr in forest.GetComponentsInChildren<Transform>(true))tr.gameObject.hideFlags=HideFlags.None;
                int count=forest.transform.childCount;forest=null;t.terrainData=original;if(collider)collider.terrainData=original;
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
                kept=true;File.WriteAllText("Validation/SequentialRepair/sq1_kept.txt","Square1 authored mountain rim + forest kept. Trees="+count+". No alphamaps, ocean or other TerrainData changes.");
            }
            Debug.Log("SQ1_AUTHORED "+(kept?"KEPT":"PREVIEW ONLY"));
        }
        finally
        {
            if(sea){var mesh=sea.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sea);UnityEngine.Object.DestroyImmediate(mesh);}
            if(forest)UnityEngine.Object.DestroyImmediate(forest);if(!kept)foreach(var go in hidden)if(go)go.SetActive(true);
            t.terrainData=original;if(collider)collider.terrainData=original;t.Flush();UnityEngine.Object.DestroyImmediate(clone);
        }
    }
}




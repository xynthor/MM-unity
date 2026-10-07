using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using static MMNorthAuthoredTools20261007;

public static class MMNorthSquare3Authored20261007
{
    const string Dir="Preview/NorthAuthored20261007/Sq3Candidate2/";
    static readonly Vector4[][] Chains={
        new[]{new Vector4(230,1050,130,105),new Vector4(290,1090,210,94),new Vector4(335,1070,117,92),new Vector4(370,1100,175,85),new Vector4(420,1040,98,83),new Vector4(465,1020,130,81),new Vector4(510,990,65,77),new Vector4(550,985,85,70),new Vector4(590,960,36,65)},
        new[]{new Vector4(255,850,60,90),new Vector4(310,905,110,80),new Vector4(345,945,65,80),new Vector4(375,980,135,74),new Vector4(420,1040,98,83)},
        new[]{new Vector4(290,1090,210,48),new Vector4(300,1015,95,55),new Vector4(280,960,27,55)},
        new[]{new Vector4(370,1100,175,43),new Vector4(405,1115,70,52),new Vector4(440,1080,25,48)},
        new[]{new Vector4(465,1020,130,42),new Vector4(445,963,56,53),new Vector4(470,915,13,50)},
        new[]{new Vector4(550,985,85,38),new Vector4(585,925,26,48),new Vector4(640,945,16,48)}
    };
    static readonly Vector2[] Top={new Vector2(256,1192),new Vector2(310,1180),new Vector2(350,1140),new Vector2(395,1155),new Vector2(450,1100),new Vector2(500,1080),new Vector2(545,1030),new Vector2(590,1055),new Vector2(625,1000),new Vector2(680,1025),new Vector2(715,980),new Vector2(735,925),new Vector2(768,880)};
    static readonly Vector2[] Bottom={new Vector2(256,690),new Vector2(400,725),new Vector2(440,768),new Vector2(500,820),new Vector2(550,865),new Vector2(600,880),new Vector2(650,910),new Vector2(700,925),new Vector2(735,925),new Vector2(768,970)};
    static float Line(Vector2[] nodes,float x){for(int i=0;i<nodes.Length-1;i++)if(x<=nodes[i+1].x)return Mathf.Lerp(nodes[i].y,nodes[i+1].y,Mathf.InverseLerp(nodes[i].x,nodes[i+1].x,x));return nodes[nodes.Length-1].y;}
    static float Shore(float x,float z){return Mathf.Min(Line(Top,x)-z,z-Line(Bottom,x),735-x)+2*Mathf.Sin(x*.14f)*Mathf.Sin(z*.06f);}
    static float RiverX(float z){float q=Mathf.InverseLerp(1020,1148,z);return Mathf.Lerp(50,265,q)+16*Mathf.Sin(q*6.283f+2.2f)+4.48f*Mathf.Sin(q*15.1f+1.1f);}
    static float RiverY(float z){return Mathf.Lerp(21.05f,24.55f,Mathf.InverseLerp(1020,1148,z));}
    static float Cut(float x,float z,float y,Vector4[] nodes){float distance=999,bed=0,width=0;for(int i=0;i<nodes.Length-1;i++){var a=nodes[i];var b=nodes[i+1];Vector2 ab=new Vector2(b.x-a.x,b.y-a.y);float q=Mathf.Clamp01(Vector2.Dot(new Vector2(x-a.x,z-a.y),ab)/ab.sqrMagnitude);float d=Vector2.Distance(new Vector2(x,z),new Vector2(a.x+ab.x*q,a.y+ab.y*q));if(d<distance){distance=d;bed=Mathf.Lerp(a.z,b.z,q);width=Mathf.Lerp(a.w,b.w,q);}}if(distance<width*2.7f)y=Mathf.Min(y,Mathf.Lerp(bed,y,S(0,width*2.7f,distance)));return y;}
    public static void Preview(bool keep=false)
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty)throw new Exception("Preserve active scene; expected clean Enroth");
        if(keep&&File.Exists("Validation/SequentialRepair/sq3_kept.txt"))throw new Exception("Square3 already kept; inspect current state before another mutation");
        var t=Terrain.activeTerrains.First(t=>AssetDatabase.GetAssetPath(t.terrainData).EndsWith("/SilverCove_NorthTerrain.asset"));
        var south=Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x-256)<.1f&&Mathf.Abs(t.transform.position.z-256)<.1f);
        var west=Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x-(-256))<.1f&&Mathf.Abs(t.transform.position.z-768)<.1f);
        var east=Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x-768)<.1f&&Mathf.Abs(t.transform.position.z-768)<.1f);
        var original=t.terrainData;var clone=UnityEngine.Object.Instantiate(original);clone.hideFlags=HideFlags.HideAndDontSave;
        var collider=t.GetComponent<TerrainCollider>();var h=new float[513,513];
        for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)
        {
            float wx=256+x,wz=768+z,edge=Ground(south,wx,768),gradient=Mathf.Clamp((edge-Ground(south,wx,744))/24,-.18f,.18f);
            float floor=5+7*S(820,1050,wz)+2.5f*Mathf.PerlinNoise(wx*.015f,wz*.015f),y=floor;
            foreach(var chain in Chains)y=Mathf.Max(y,floor+Ridge(wx,wz,chain));
            y+=Mathf.Clamp01((y-18)/65)*(9*(Mathf.PerlinNoise(wx*.037f,wz*.037f)-.5f)+4*(Mathf.PerlinNoise(wx*.083f,wz*.083f)-.5f));
            y=Mathf.Lerp(edge+gradient*Mathf.Min(z,60),y,S(0,105,z));
            float shore=Shore(wx,wz);
            if(shore<0)y=-7.5f+7.62f*Mathf.Exp(shore/9);
            else y=Mathf.Min(y,.12f+shore*(.15f+.6f*Mathf.PerlinNoise(wx*.014f,3.7f))+.0015f*shore*shore);
            if(x<48)y=Mathf.Lerp(Ground(west,256-x,wz),y,S(0,48,x));
            if(x>480)y=Mathf.Lerp(y,Ground(east,768+512-x,wz),S(480,512,x));
            if(wz>=1020&&wz<=1149){float d=Mathf.Abs(wx-RiverX(wz)),surface=RiverY(wz);if(d<49)y=Mathf.Min(y,Mathf.Lerp(surface-.85f,y,S(11,49,d)));}
            if(z==0)y=edge;
            h[z,x]=Mathf.Clamp01((y+24)/320);
        }
        var before=(float[,])h.Clone();Erode(h);for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)h[z,x]=Mathf.Lerp(before[z,x],h[z,x],.34f);
        bool kept=false;GameObject sea=null,forest=null;var hidden=new List<GameObject>();var moved=new List<KeyValuePair<Transform,Vector3>>();
        if(keep){string backup="Backups/NorthAuthored20261007/Sq3Before";Directory.CreateDirectory(backup);foreach(var path in new[]{AssetDatabase.GetAssetPath(original),scene.path}){var dest=Path.Combine(backup,Path.GetFileName(path));if(!File.Exists(dest))File.Copy(path,dest);}}
        try
        {
            clone.SetHeights(0,0,h);t.terrainData=clone;if(collider)collider.terrainData=clone;t.Flush();
            sea=SeaPreview(t);moved=MMNorthValleyForest20261007.GroundDressing(t);
            forest=MMNorthValleyForest20261007.Preview(t,hidden,(x,z,y)=>z<1160&&!(z>=1020&&z<=1149&&Mathf.Abs(x-RiverX(z))<14),71010);
            Directory.CreateDirectory(Dir);
            Capture(Dir,"top",new Vector3(512,850,1024),new Vector3(512,0,1024),true);
            Capture(Dir,"transition",new Vector3(430,155,530),new Vector3(430,30,950),false);
            Capture(Dir,"oblique",new Vector3(180,225,740),new Vector3(475,55,1000),false);
            Capture(Dir,"shore",new Vector3(760,20,1080),new Vector3(595,35,970),false);
            Capture(Dir,"cross",new Vector3(665,220,1200),new Vector3(300,45,990),false);
            if(keep)
            {
                original.SetHeights(0,0,h);EditorUtility.SetDirty(original);AssetDatabase.SaveAssetIfDirty(original);
                forest.name="SQ3 Authored Valley Conifer Forest 20261007";foreach(var tr in forest.GetComponentsInChildren<Transform>(true))tr.gameObject.hideFlags=HideFlags.None;
                int count=forest.transform.childCount;forest=null;t.terrainData=original;if(collider)collider.terrainData=original;
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
                kept=true;File.WriteAllText("Validation/SequentialRepair/sq3_kept.txt","Square3 authored mountain rim + forest kept. Trees="+count+". No alphamaps, ocean or other TerrainData changes.");
            }
            Debug.Log("SQ3_AUTHORED "+(kept?"KEPT":"PREVIEW ONLY"));
        }
        finally
        {
            if(sea){var mesh=sea.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sea);UnityEngine.Object.DestroyImmediate(mesh);}
            if(forest)UnityEngine.Object.DestroyImmediate(forest);if(!kept){foreach(var go in hidden)if(go)go.SetActive(true);foreach(var entry in moved)if(entry.Key)entry.Key.position=entry.Value;}
            t.terrainData=original;if(collider)collider.terrainData=original;t.Flush();UnityEngine.Object.DestroyImmediate(clone);
        }
    }
}





using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using static MMNorthAuthoredTools20261007;

public static class MMNorthSquare2Authored20261007
{
    const string Dir="Preview/NorthAuthored20261007/Sq2Candidate1/";
    const string WaterPath="Assets/World/WorldExtensions/Generated/NorthReference20261001/NorthInlandIcyWater20261006.asset";
    static readonly Vector4[][] Chains={
        new[]{new Vector4(-270,1210,175,105),new Vector4(-191,1212,245,105),new Vector4(-147,1187,161,98),new Vector4(-92,1220,239,99),new Vector4(-34,1195,147,92),new Vector4(15,1218,201,90),new Vector4(55,1181,105,85),new Vector4(107,1204,71,85),new Vector4(158,1188,189,94),new Vector4(216,1164,141,94),new Vector4(275,1180,150,100)},
        new[]{new Vector4(-207,790,12,87),new Vector4(-220,891,97,88),new Vector4(-187,951,128,89),new Vector4(-211,1012,162,85),new Vector4(-182,1071,99,92),new Vector4(-194,1135,205,90),new Vector4(-147,1187,161,98)},
        new[]{new Vector4(195,768,50,100),new Vector4(182,842,98,100),new Vector4(201,901,148,95),new Vector4(160,949,104,88),new Vector4(180,1007,181,97),new Vector4(206,1065,118,90),new Vector4(176,1115,204,89),new Vector4(216,1164,141,94)},
        new[]{new Vector4(-194,1135,205,45),new Vector4(-137,1110,82,50),new Vector4(-80,1091,34,48)},
        new[]{new Vector4(-187,951,128,44),new Vector4(-127,923,59,56),new Vector4(-90,889,14,44)},
        new[]{new Vector4(201,901,148,44),new Vector4(142,876,60,53),new Vector4(96,853,13,44)},
        new[]{new Vector4(180,1007,181,40),new Vector4(123,957,71,50),new Vector4(76,940,28,40)},
        new[]{new Vector4(176,1115,204,38),new Vector4(134,1090,80,46),new Vector4(91,1065,30,35)}
    };
    static readonly Vector2[] Shore={new Vector2(-256,1240),new Vector2(-205,1260),new Vector2(-150,1230),new Vector2(-100,1255),new Vector2(-50,1240),new Vector2(0,1235),new Vector2(50,1210),new Vector2(95,1216),new Vector2(125,1228),new Vector2(175,1205),new Vector2(215,1220),new Vector2(256,1192)};
    static float Coast(float x){for(int i=0;i<Shore.Length-1;i++)if(x<=Shore[i+1].x)return Mathf.Lerp(Shore[i].y,Shore[i+1].y,Mathf.InverseLerp(Shore[i].x,Shore[i+1].x,x))+2*Mathf.Sin(x*.12f);return 1192;}
    static float LakeDistance(float x,float z,Vector3[] v)
    {
        float dist=999;bool inside=false;
        for(int i=1,j=72;i<=72;j=i++)
        {
            var a=v[j];var b=v[i];var ab=new Vector2(b.x-a.x,b.z-a.z);float q=Mathf.Clamp01(Vector2.Dot(new Vector2(x-a.x,z-a.z),ab)/ab.sqrMagnitude);
            dist=Mathf.Min(dist,Vector2.Distance(new Vector2(x,z),new Vector2(a.x+ab.x*q,a.z+ab.y*q)));
            if((a.z>z)!=(b.z>z)&&x<(b.x-a.x)*(z-a.z)/(b.z-a.z)+a.x)inside=!inside;
        }
        return inside?-dist:dist;
    }
    static void Channel(float x,float z,Vector3[] verts,out float dist,out float surface,out float half)
    {
        dist=999;surface=0;half=0;
        foreach(var section in new[]{new Vector2Int(73,29),new Vector2Int(131,25),new Vector2Int(181,23)})
        for(int i=0;i<section.y-1;i++)
        {
            int k=section.x+i*2;var a=(verts[k]+verts[k+1])*.5f;var b=(verts[k+2]+verts[k+3])*.5f;
            var ab=new Vector2(b.x-a.x,b.z-a.z);float q=Mathf.Clamp01(Vector2.Dot(new Vector2(x-a.x,z-a.z),ab)/ab.sqrMagnitude);
            float d=Vector2.Distance(new Vector2(x,z),new Vector2(a.x+ab.x*q,a.z+ab.y*q));
            if(d<dist){dist=d;surface=Mathf.Lerp(a.y,b.y,q);half=Mathf.Lerp(Vector3.Distance(verts[k],verts[k+1]),Vector3.Distance(verts[k+2],verts[k+3]),q)*.5f;}
        }
    }
    public static void Preview(bool keep=false)
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty)throw new Exception("Preserve active scene; expected clean Enroth");
        if(keep&&File.Exists("Validation/SequentialRepair/sq2_kept.txt"))throw new Exception("Square2 already kept");
        var t=Terrain.activeTerrains.First(t=>AssetDatabase.GetAssetPath(t.terrainData).EndsWith("/FrozenHighlands_NorthTerrain.asset"));
        var south=Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x+256)<.1f&&Mathf.Abs(t.transform.position.z-256)<.1f);
        var west=Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x+768)<.1f&&Mathf.Abs(t.transform.position.z-768)<.1f);
        var east=Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x-256)<.1f&&Mathf.Abs(t.transform.position.z-768)<.1f);
        var original=t.terrainData;var clone=UnityEngine.Object.Instantiate(original);clone.hideFlags=HideFlags.HideAndDontSave;var collider=t.GetComponent<TerrainCollider>();
        var water=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(WaterPath);var vertices=water.vertices;if(vertices.Length!=227)throw new Exception("Unexpected inland water topology; inspect rather than overwrite");
        // Main northern branch becomes the lake outlet. Both side tributaries keep their original geometry.
        for(int i=0;i<29;i++)
        {
            float q=i/28f,center=Mathf.Lerp(-10,115,q)+23*Mathf.Sin(q*6.283f+.5f)+6.44f*Mathf.Sin(q*15.1f+.25f);
            float width=9*(.82f+.18f*Mathf.Sin(q*9.7f+.5f));float z=Mathf.Lerp(1010,1226,q),y=Mathf.Lerp(21.05f,.12f,q);
            vertices[73+i*2]=new Vector3(center-width,y,z);vertices[74+i*2]=new Vector3(center+width,y,z);
        }
        var triangles=water.triangles;for(int i=0;i<72;i++){int k=i*3;int swap=triangles[k+1];triangles[k+1]=triangles[k+2];triangles[k+2]=swap;}
        var h=new float[513,513];
        for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)
        {
            float wx=-256+x,wz=768+z,edge=Ground(south,wx,768),gradient=Mathf.Clamp((edge-Ground(south,wx,744))/24,-.18f,.18f);
            float floor=17+7*S(840,1020,wz)+2*Mathf.PerlinNoise(wx*.014f+2,wz*.014f),y=floor;
            foreach(var chain in Chains)y=Mathf.Max(y,floor+Ridge(wx,wz,chain));
            y+=Mathf.Clamp01((y-25)/70)*(9*(Mathf.PerlinNoise(wx*.037f,wz*.037f)-.5f)+4*(Mathf.PerlinNoise(wx*.083f,wz*.083f)-.5f));
            y=Mathf.Lerp(edge+gradient*Mathf.Min(z,60),y,S(0,110,z));
            float shore=Coast(wx)-wz;
            if(shore<0)y=-7.5f+7.62f*Mathf.Exp(shore/9);
            else y=Mathf.Min(y,.12f+shore*(.7f+1.55f*Mathf.PerlinNoise(wx*.014f,3.7f))+.0035f*shore*shore);
            if(x<48)y=Mathf.Lerp(Ground(west,-256-x,wz),y,S(0,48,x));
            if(x>480)y=Mathf.Lerp(y,Ground(east,256+512-x,wz),S(480,512,x));
            float lake=LakeDistance(wx,wz,vertices);
            if(lake<0)y=21.05f-1.1f-4*S(0,36,-lake);
            else if(lake<33)y=Mathf.Lerp(21.3f+lake*.055f,y,S(0,33,lake));
            Channel(wx,wz,vertices,out float d,out float surface,out float half);
            if(d<half+38)y=Mathf.Min(y,Mathf.Lerp(surface-.85f,y,S(half+1,half+38,d)));
            if(z==0)y=edge;h[z,x]=Mathf.Clamp01((y+24)/320);
        }
        var before=(float[,])h.Clone();Erode(h);for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)h[z,x]=Mathf.Lerp(before[z,x],h[z,x],.28f);
        // Enforce the surveyed water footprint after erosion, keeping banks above the water and beds below it.
        for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){float wx=-256+x,wz=768+z;Channel(wx,wz,vertices,out float d,out float surface,out float half);if(d<half+1)h[z,x]=Mathf.Min(h[z,x],(surface-.7f+24)/320);}
        var filter=UnityEngine.Object.FindObjectsByType<MeshFilter>().First(f=>f.sharedMesh==water);var previewWater=UnityEngine.Object.Instantiate(water);previewWater.hideFlags=HideFlags.HideAndDontSave;previewWater.vertices=vertices;previewWater.triangles=triangles;previewWater.RecalculateNormals();previewWater.RecalculateBounds();
        bool kept=false;GameObject sea=null,forest=null;var hidden=new List<GameObject>();var moved=new List<KeyValuePair<Transform,Vector3>>();
        if(keep){string backup="Backups/NorthAuthored20261007/Sq2Before";Directory.CreateDirectory(backup);foreach(var path in new[]{AssetDatabase.GetAssetPath(original),WaterPath,scene.path}){var dest=Path.Combine(backup,Path.GetFileName(path));if(!File.Exists(dest))File.Copy(path,dest);}}
        try
        {
            clone.SetHeights(0,0,h);t.terrainData=clone;if(collider)collider.terrainData=clone;t.Flush();filter.sharedMesh=previewWater;sea=SeaPreview(t);moved=MMNorthValleyForest20261007.GroundDressing(t);
            forest=MMNorthValleyForest20261007.Preview(t,hidden,(x,z,y)=>{if(LakeDistance(x,z,vertices)<5)return false;Channel(x,z,vertices,out float d,out float surface,out float half);return d>half+4&&z<1160;},71009);
            Directory.CreateDirectory(Dir);
            Capture(Dir,"top",new Vector3(0,850,1024),new Vector3(0,0,1024),true);
            Capture(Dir,"transition",new Vector3(0,155,530),new Vector3(0,35,950),false);
            Capture(Dir,"oblique",new Vector3(-346,225,740),new Vector3(14,55,1050),false);
            Capture(Dir,"shore",new Vector3(65,26,1475),new Vector3(20,70,1170),false);
            Capture(Dir,"cross",new Vector3(124,220,1350),new Vector3(224,45,950),false);
            Capture(Dir,"lake",new Vector3(-42,48,918),new Vector3(-20,25,1080),false);
            if(keep)
            {
                original.SetHeights(0,0,h);EditorUtility.SetDirty(original);AssetDatabase.SaveAssetIfDirty(original);
                water.vertices=vertices;water.triangles=triangles;water.RecalculateNormals();water.RecalculateBounds();EditorUtility.SetDirty(water);AssetDatabase.SaveAssetIfDirty(water);filter.sharedMesh=water;
                forest.name="SQ2 Authored Basin Conifer Forest 20261007";foreach(var tr in forest.GetComponentsInChildren<Transform>(true))tr.gameObject.hideFlags=HideFlags.None;
                int count=forest.transform.childCount;forest=null;t.terrainData=original;if(collider)collider.terrainData=original;
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");
                kept=true;File.WriteAllText("Validation/SequentialRepair/sq2_kept.txt","Square2 authored lake basin + descending northern outlet + forest kept. Trees="+count+". No alphamaps, ocean or other TerrainData changes.");
            }
            Debug.Log("SQ2_AUTHORED "+(kept?"KEPT":"PREVIEW ONLY"));
        }
        finally
        {
            filter.sharedMesh=water;UnityEngine.Object.DestroyImmediate(previewWater);
            if(sea){var mesh=sea.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sea);UnityEngine.Object.DestroyImmediate(mesh);}
            if(forest)UnityEngine.Object.DestroyImmediate(forest);if(!kept){foreach(var go in hidden)if(go)go.SetActive(true);foreach(var entry in moved)if(entry.Key)entry.Key.position=entry.Value;}
            t.terrainData=original;if(collider)collider.terrainData=original;t.Flush();UnityEngine.Object.DestroyImmediate(clone);
        }
    }
}


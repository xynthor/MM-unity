using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using static MMNorthAuthoredTools20261007;

public static class MMNorthTransitionJoin20261007
{
    static float DrainageDistance(float x,float z,Vector3[] v)
    {
        float distance=999;
        foreach(var range in new[]{new Vector2Int(73,29),new Vector2Int(131,25),new Vector2Int(181,23)})
        for(int i=0;i<range.y-1;i++)
        {
            int k=range.x+2*i;var a=(v[k]+v[k+1])*.5f;var b=(v[k+2]+v[k+3])*.5f;
            var ab=new Vector2(b.x-a.x,b.z-a.z);float q=Mathf.Clamp01(Vector2.Dot(new Vector2(x-a.x,z-a.z),ab)/ab.sqrMagnitude);
            distance=Mathf.Min(distance,Vector2.Distance(new Vector2(x,z),new Vector2(a.x+q*ab.x,a.z+q*ab.y)));
        }
        return distance;
    }
    public static void Run(int westSquare,bool keep=false)
    {
        var scene=SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Need clean Enroth");
        string report="Validation/SequentialRepair/join"+westSquare+"_kept.txt";if(File.Exists(report))throw new Exception("Join already saved");
        float boundary=-768+512*westSquare;
        var terrains=new[]{Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x-(boundary-512))<.1f&&Mathf.Abs(t.transform.position.z-768)<.1f),Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x-boundary)<.1f&&Mathf.Abs(t.transform.position.z-768)<.1f)};
        var data=terrains.Select(t=>t.terrainData).ToArray();var clones=data.Select(d=>UnityEngine.Object.Instantiate(d)).ToArray();var height=data.Select(d=>d.GetHeights(0,0,513,513)).ToArray();
        var spines=new[]{new Vector4(boundary-35,835,70,75),new Vector4(boundary-12,885,108,80),new Vector4(boundary+24,945,73,80),new Vector4(boundary+14,1005,144,88),new Vector4(boundary-26,1067,89,82),new Vector4(boundary-14,1120,142,86),new Vector4(boundary+30,1172,94,80),new Vector4(boundary+16,1200,65,70)};
        if(westSquare==1)spines=new[]{new Vector4(-285,820,60,76),new Vector4(-245,875,110,78),new Vector4(-280,940,88,85),new Vector4(-245,1005,135,85),new Vector4(-275,1060,94,80),new Vector4(-235,1100,110,82),new Vector4(-270,1168,145,80),new Vector4(-245,1210,95,76)};
        if(westSquare==2)spines=new[]{new Vector4(238,825,100,85),new Vector4(270,900,140,90),new Vector4(240,960,85,80),new Vector4(280,1010,170,90),new Vector4(245,1070,100,80),new Vector4(285,1110,185,90),new Vector4(250,1170,105,80)};
        var water=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>("Assets/World/WorldExtensions/Generated/NorthReference20261001/NorthInlandIcyWater20261006.asset").vertices;
        var coastTypes=new[]{typeof(MMNorthSquare0Authored20261007),typeof(MMNorthSquare1Authored20261007),typeof(MMNorthSquare2Authored20261007)};
        for(int z=1;z<=512;z++)for(int dx=-64;dx<=64;dx++)
        {
            float y=14+Ridge(boundary+dx,768+z,spines);
            float hydro=S(18,48,DrainageDistance(boundary+dx,768+z,water));
            y+=3*(Mathf.PerlinNoise((boundary+dx)*.035f,(768+z)*.035f)-.5f);
            float weight=S(0,95,z)*hydro*(1-S(42,64,Mathf.Abs(dx)));
            int side=dx<0?0:1,index=dx<0?512+dx:dx;
            float old=height[side][z,index]*320-24;
            if(old<.15f)weight=0;
            var coastMethod=coastTypes[Mathf.Min(westSquare+side,2)].GetMethod("Coast",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            float coastalDistance=(float)coastMethod.Invoke(null,new object[]{boundary+(float)dx})-(768+z);
            if(westSquare==2&&side==1)coastalDistance=(float)typeof(MMNorthSquare3Authored20261007).GetMethod("Shore",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{boundary+(float)dx,768+(float)z});
            y=Mathf.Min(y,.12f+Mathf.Max(0,coastalDistance)*.8f+.004f*Mathf.Max(0,coastalDistance)*Mathf.Max(0,coastalDistance));
            y=Mathf.Max(old,y);
            float value=(Mathf.Lerp(old,y,weight)+24)/320;
            height[side][z,index]=value;if(dx==0)height[0][z,512]=value;
        }
        var erosion=new float[513,513];for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)erosion[z,x]=x<256?height[0][z,x+256]:height[1][z,x-256];
        Erode(erosion);
        for(int z=1;z<=512;z++)for(int dx=-64;dx<=64;dx++)
        {
            int side=dx<0?0:1,index=dx<0?512+dx:dx;
            float weight=.28f*(1-S(42,64,Mathf.Abs(dx)))*S(18,48,DrainageDistance(boundary+dx,768+z,water));
            float value=Mathf.Lerp(height[side][z,index],erosion[z,dx+256],weight);height[side][z,index]=value;if(dx==0)height[0][z,512]=value;
        }
        bool saved=false;var sea=new List<GameObject>();var moved=new List<System.Tuple<Transform,Vector3>>();var hidden=new List<GameObject>();
        try
        {
            for(int i=0;i<2;i++){clones[i].hideFlags=HideFlags.HideAndDontSave;clones[i].SetHeights(0,0,height[i]);terrains[i].terrainData=clones[i];var c=terrains[i].GetComponent<TerrainCollider>();if(c)c.terrainData=clones[i];terrains[i].Flush();sea.Add(SeaPreview(terrains[i]));}
            foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>().Where(tr=>tr.name.StartsWith("AuthoredPine_")&&Mathf.Abs(tr.position.x-boundary)<64&&tr.position.z>=768&&tr.position.z<1280))
            {
                int side=tr.position.x<boundary?0:1;float u=(tr.position.x-terrains[side].transform.position.x)/512,v=(tr.position.z-768)/512;
                float delta=clones[side].GetInterpolatedHeight(u,v)-data[side].GetInterpolatedHeight(u,v);moved.Add(System.Tuple.Create(tr,tr.position));tr.position+=Vector3.up*delta;
                if(clones[side].GetSteepness(u,v)>36){hidden.Add(tr.gameObject);tr.gameObject.SetActive(false);}
            }
            string dir="Preview/NorthAuthored20261007/Join"+westSquare+"/";Directory.CreateDirectory(dir);
            Capture(dir,"top",new Vector3(boundary,850,1024),new Vector3(boundary,0,1024),true);
            Capture(dir,"oblique",new Vector3(boundary-290,200,760),new Vector3(boundary,55,1040),false);
            Capture(dir,"shore",new Vector3(boundary,24,1450),new Vector3(boundary,65,1190),false);
            Capture(dir,"transition",new Vector3(boundary,130,590),new Vector3(boundary,35,960),false);
            if(keep)
            {
                string backup="Backups/NorthAuthored20261007/Join"+westSquare+"Before";Directory.CreateDirectory(backup);
                foreach(var path in data.Select(AssetDatabase.GetAssetPath).Concat(new[]{scene.path})){string dest=Path.Combine(backup,Path.GetFileName(path));if(!File.Exists(dest))File.Copy(path,dest);}
                for(int i=0;i<2;i++){data[i].SetHeights(0,0,height[i]);EditorUtility.SetDirty(data[i]);AssetDatabase.SaveAssetIfDirty(data[i]);terrains[i].terrainData=data[i];var c=terrains[i].GetComponent<TerrainCollider>();if(c)c.terrainData=data[i];}
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");saved=true;
                File.WriteAllText(report,"Bounded 128m transition join; drainage corridors preserved; outer authored geometry and alphamaps unchanged.");
            }
        }
        finally
        {
            foreach(var go in sea){var mesh=go.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(mesh);}
            if(!saved){foreach(var item in moved)if(item.Item1)item.Item1.position=item.Item2;foreach(var go in hidden)if(go)go.SetActive(true);}
            for(int i=0;i<2;i++){terrains[i].terrainData=data[i];var c=terrains[i].GetComponent<TerrainCollider>();if(c)c.terrainData=data[i];terrains[i].Flush();UnityEngine.Object.DestroyImmediate(clones[i]);}
        }
    }
}


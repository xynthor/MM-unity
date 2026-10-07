using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

// Conforming, bounded surface treatment; the original TerrainData paint stays intact.
public static class MMNorthAlpineSurface20261007
{
    const string AssetDir="Assets/World/WorldExtensions/Generated/NorthAlpine20261007";
    public static void Run(int square,bool keep=false)
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty)throw new Exception("Expected clean Enroth");
        string rootName="North Alpine Rock Surface "+square+" 20261007";
        if(GameObject.Find(rootName))throw new Exception("Surface already saved");
        var t=Terrain.activeTerrains.First(t=>Mathf.Abs(t.transform.position.x-(-1280+512*square))<.1f&&Mathf.Abs(t.transform.position.z-768)<.1f);
        float originalPixelError=t.heightmapPixelError;t.heightmapPixelError=1;var data=t.terrainData;var vertices=new Vector3[513*513];var normals=new Vector3[vertices.Length];var tangents=new Vector4[vertices.Length];var triangles=new List<int>();
        for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)
        {
            int k=z*513+x;var n=data.GetInterpolatedNormal(x/512f,z/512f);
            vertices[k]=new Vector3(x,data.GetHeight(x,z),z)+Vector3.up*Mathf.Lerp(.04f,.65f,MMNorthAuthoredTools20261007.S(3,30,data.GetHeight(x,z)-24));normals[k]=n;var tangent=new Vector3(n.y,-n.x,0).normalized;tangents[k]=new Vector4(tangent.x,tangent.y,tangent.z,-1);
        }
        for(int z=0;z<512;z++)for(int x=0;x<512;x++)
        {
            int a=z*513+x,b=a+1,c=a+513,d=c+1;
            if(vertices[a].y-24<.25f||vertices[b].y-24<.25f||vertices[c].y-24<.25f||vertices[d].y-24<.25f)continue;
            if(z<12)continue;
            // Same two triangles as the heightfield: no floating plane across gullies.
            triangles.Add(a);triangles.Add(c);triangles.Add(d);triangles.Add(a);triangles.Add(d);triangles.Add(b);
        }
        var mesh=new UnityEngine.Mesh{name="NorthAlpineSurface"+square,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
        mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
        var material=new Material(Shader.Find("MMUnity/NorthAlpineSurface20261007"));
        material.SetTexture("_Rock",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EnvironmentAssets/Biomes/cold_rock.png"));
        material.SetTexture("_RockNormal",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EnvironmentAssets/UnitySamples/rock_boulder_cracked_n.png"));
        material.SetTexture("_Snow",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/World/WorldExtensions/Generated/LinkedSnow02/Snow02_SeamlessAlbedoSmoothness.png"));
        var root=new GameObject(rootName);root.hideFlags=HideFlags.HideAndDontSave;root.transform.position=t.transform.position;
        root.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        bool saved=false;GameObject sea=null;
        try
        {
            sea=MMNorthAuthoredTools20261007.SeaPreview(t);
            string dir="Preview/NorthAuthored20261007/SurfaceSq"+square+"/";Directory.CreateDirectory(dir);
            float center=t.transform.position.x+256;
            MMNorthAuthoredTools20261007.Capture(dir,"top",new Vector3(center,850,1024),new Vector3(center,0,1024),true);
            MMNorthAuthoredTools20261007.Capture(dir,"oblique",new Vector3(center-346,225,740),new Vector3(center+14,55,1050),false);
            MMNorthAuthoredTools20261007.Capture(dir,"shore",new Vector3(center,26,1475),new Vector3(center,70,1170),false);
            if(square==2)MMNorthAuthoredTools20261007.Capture(dir,"lake",new Vector3(-42,48,918),new Vector3(-20,25,1080),false);
            if(keep)
            {
                string backup="Backups/NorthAuthored20261007/SurfaceBefore";Directory.CreateDirectory(backup);
                string dest=Path.Combine(backup,"Enroth.unity");if(!File.Exists(dest))File.Copy(scene.path,dest);
                Directory.CreateDirectory(AssetDir);AssetDatabase.CreateAsset(mesh,AssetDir+"/Surface"+square+".asset");
                AssetDatabase.CreateAsset(material,AssetDir+"/Surface"+square+".mat");
                root.hideFlags=HideFlags.None;EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssetIfDirty(mesh);AssetDatabase.SaveAssetIfDirty(material);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Scene save failed");saved=true;
            }
        }
        finally
        {
            if(sea){var m=sea.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sea);UnityEngine.Object.DestroyImmediate(m);}
            if(!saved){t.heightmapPixelError=originalPixelError;UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);}
        }
        Debug.Log("NORTH_ALPINE_SURFACE square="+square+" saved="+saved);
    }
}






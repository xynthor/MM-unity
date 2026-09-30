using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class MMHighResTreeImpostors20260930
{
    const string Root="Assets/World/WorldExtensions/Generated/LinkedTreeImpostors256";
    const string Backup="Backups/BeforeTreeImpostor256_20260930/manifest.json";
    static readonly string[] TargetNear={
        "Assets/World/WorldExtensions/Generated/LinkedJacarandaRepairV2/JacarandaNear.asset",
        "Assets/Optimization/GeneratedMeshes/bd0d6a6b_island_tree_01_LOD0_q50.asset",
        "Assets/Optimization/GeneratedMeshes/cc6eeadc_island_tree_02_LOD0_q50.asset",
        "Assets/Optimization/GeneratedMeshes/d37345cb_island_tree_03_LOD0_q50.asset"
    };

    static string Key(Mesh mesh,Material[] materials)
    {
        string guid;long id;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh,out guid,out id);
        string mh=Hash128.Compute(string.Join("|",materials.Select(m=>m?AssetDatabase.GetAssetPath(m):"null"))).ToString().Substring(0,10);
        return guid+"_"+id+"_"+mh;
    }
    static Material Bake(MeshFilter source,Scene scratch,Camera camera,RenderTexture rt)
    {
        var renderer=source.GetComponent<MeshRenderer>();
        string key=Key(source.sharedMesh,renderer.sharedMaterials);
        string matPath=Root+"/"+key+".mat";
        var existing=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(existing)return existing;

        var temp=new GameObject("TEMP Tree256");
        SceneManager.MoveGameObjectToScene(temp,scratch);temp.layer=31;
        temp.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
        var tr=temp.AddComponent<MeshRenderer>();tr.sharedMaterials=renderer.sharedMaterials;

        var bounds=source.sharedMesh.bounds;
        float size=Mathf.Max(bounds.size.y,new Vector2(bounds.size.x,bounds.size.z).magnitude)*1.1f;
        const int cell=256;
        var atlas=new Texture2D(cell*9,cell,TextureFormat.RGBA32,false);
        var image=new Texture2D(cell,cell,TextureFormat.RGBA32,false);
        var prior=RenderTexture.active;
        try
        {
            camera.orthographicSize=size/2;camera.nearClipPlane=.01f;camera.farClipPlane=size*6+1;
            for(int i=0;i<9;i++)
            {
                float angle=i*Mathf.PI/4;
                camera.transform.position=bounds.center+(i==8?Vector3.up:new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)))*size*2;
                camera.transform.LookAt(bounds.center,i==8?Vector3.forward:Vector3.up);
                camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,cell,cell),0,0);image.Apply();
                atlas.SetPixels(i*cell,0,cell,cell,image.GetPixels());
            }
            atlas.Apply();
            File.WriteAllBytes(Root+"/"+key+".png",atlas.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=prior;
            UnityEngine.Object.DestroyImmediate(temp);
            UnityEngine.Object.DestroyImmediate(atlas);
            UnityEngine.Object.DestroyImmediate(image);
        }
        string texPath=Root+"/"+key+".png";
        AssetDatabase.ImportAsset(texPath,ImportAssetOptions.ForceSynchronousImport);
        var imp=(TextureImporter)AssetImporter.GetAtPath(texPath);
        imp.textureType=TextureImporterType.Default;
        imp.sRGBTexture=true;
        imp.alphaSource=TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency=true;
        imp.mipmapEnabled=true;
        imp.mipMapsPreserveCoverage=true;
        imp.alphaTestReferenceValue=.3f;
        imp.wrapMode=TextureWrapMode.Clamp;
        imp.filterMode=FilterMode.Bilinear;
        imp.anisoLevel=2;
        imp.textureCompression=TextureImporterCompression.Uncompressed;
        imp.maxTextureSize=4096;
        imp.SaveAndReimport();

        var material=new Material(Shader.Find("MMUnity/Linked Far Tree Impostor"));
        material.name="Tree256 "+key;
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        material.SetVector("_Center",bounds.center);
        material.SetFloat("_Size",size);
        material.SetFloat("_Cutoff",.3f);
        material.enableInstancing=true;
        AssetDatabase.CreateAsset(material,matPath);
        return material;
    }

    public static void Apply()
    {
        if(!File.Exists(Backup))throw new Exception("Backup missing");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty||EditorApplication.isPlaying)
            throw new Exception("Saved linked Edit Mode scene required");
        if(!AssetDatabase.IsValidFolder(Root))
            AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","LinkedTreeImpostors256");

        var groups=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<LODGroup>(true))
            .Where(g=>g.transform.Find("Linked distance LOD 2")&&g.GetComponent<MeshFilter>())
            .Where(g=>TargetNear.Contains(AssetDatabase.GetAssetPath(g.GetComponent<MeshFilter>().sharedMesh)))
            .ToArray();
        if(groups.Length<100)throw new Exception("Too few target groups "+groups.Length);

        var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
        var enabled=lights.Select(l=>l.enabled).ToArray();
        var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var oldAmbientMode=RenderSettings.ambientMode;var oldAmbient=RenderSettings.ambientLight;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.46f,.46f,.46f);
        foreach(var l in lights)l.enabled=false;

        var cameraGO=new GameObject("TEMP Impostor256 Camera");SceneManager.MoveGameObjectToScene(cameraGO,scratch);
        var camera=cameraGO.AddComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;camera.allowHDR=false;camera.allowMSAA=false;
        var lightGO=new GameObject("TEMP Impostor256 Sun");SceneManager.MoveGameObjectToScene(lightGO,scratch);
        var light=lightGO.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.7f;light.cullingMask=1<<31;
        lightGO.transform.rotation=Quaternion.Euler(43,318,0);
        var rt=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;

        var cache=new Dictionary<string,Material>();
        try
        {
            foreach(var group in groups)
            {
                var f=group.GetComponent<MeshFilter>();string key=Key(f.sharedMesh,f.GetComponent<MeshRenderer>().sharedMaterials);
                if(!cache.ContainsKey(key))cache[key]=Bake(f,scratch,camera,rt);
            }
        }
        finally
        {
            for(int i=0;i<lights.Length;i++)lights[i].enabled=enabled[i];
            RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientLight=oldAmbient;
            camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);
            EditorSceneManager.CloseScene(scratch,true);SceneManager.SetActiveScene(scene);
        }

        int changed=0;
        var report=new List<string>();
        foreach(var group in groups)
        {
            var f=group.GetComponent<MeshFilter>();string key=Key(f.sharedMesh,f.GetComponent<MeshRenderer>().sharedMaterials);
            var far=group.transform.Find("Linked distance LOD 2");
            var r=far.GetComponent<MeshRenderer>();
            string old=r.sharedMaterial?AssetDatabase.GetAssetPath(r.sharedMaterial):"";
            r.sharedMaterial=cache[key];r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;EditorUtility.SetDirty(r);changed++;
            report.Add(AssetDatabase.GetAssetPath(f.sharedMesh)+" | old="+old+" | new="+AssetDatabase.GetAssetPath(cache[key]));
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        report.Insert(0,"PASS groups="+groups.Length+" changed="+changed+" sharedAtlases="+cache.Count+" cell=256 terrainWrites=0 transformWrites=0 canonicalWrites=0");
        File.WriteAllLines("Validation/EdgeGrid20260923/tree_impostor256_20260930.txt",report);
        Debug.Log(report[0]);
    }
}

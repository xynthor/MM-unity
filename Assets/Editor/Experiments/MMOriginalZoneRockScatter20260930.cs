using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class MMOriginalZoneRockScatter20260930
{
    const string ScenePath="Assets/Scenes/World/Enroth.unity";
    const string WaterMask="Validation/EdgeGrid20260923/WaterQA20260929/CurrentWaterCoverage.png";
    const string RootName="Original Regions Rock Scatter 20260930";
    const float X0=-1792f,Z0=-1280f,Step=2f;

    sealed class R
    {
        public string fbx,albedo,mask,normal,matPath;
        public Mesh mesh;
        public Material mat;
    }
    static readonly R[] Rocks={
        new R{
            fbx="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Rock_Granite_rcCwC.fbx",
            albedo="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Materials/Rock_Granite_rcCwC_albedo.tif",
            mask="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Materials/Rock_Granite_rcCwC_MaskMap.tif",
            normal="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Materials/Rock_Granite_rcCwC_Normal.tif",
            matPath="Assets/Materials/RealisticWorld/Rocks/Rock_Granite_rcCwC_Standard.mat"},
        new R{
            fbx="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_reFto/Rock_Granite_reFto.FBX",
            albedo="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_reFto/Materials/Rock_Granite_reFto_albedo.tif",
            mask="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_reFto/Materials/Rock_Granite_reFto_MaskMap.tif",
            normal=null,
            matPath="Assets/Materials/RealisticWorld/Rocks/Rock_Granite_reFto_Standard.mat"},
        new R{
            fbx="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rgAsy/Aset_rock_granite_M_rgAsy.fbx",
            albedo="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rgAsy/Materials/Aset_rock_granite_M_rgAsy_Albedo.tif",
            mask="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rgAsy/Materials/Aset_rock_granite_M_rgAsy_MaskMap.tif",
            normal=null,
            matPath="Assets/Materials/RealisticWorld/Rocks/Rock_Granite_rgAsy_Standard.mat"}
    };

    struct C { public Terrain t; public float x,z,y,slope,score; public Vector3 normal; public uint h; public int variant; }

    static uint H(int a,int b,int c){
        uint v=(uint)(a*73856093 ^ b*19349663 ^ c*83492791);
        v^=v>>16;v*=0x7feb352d;v^=v>>15;v*=0x846ca68b;return v^(v>>16);
    }
    static bool IsWater(Color32[] px,int w,int h,float x,float z){
        int ix=Mathf.FloorToInt((x-X0)/Step), iz=Mathf.FloorToInt((z-Z0)/Step);
        if(ix<0||iz<0||ix>=w||iz>=h)return true;
        var c=px[iz*w+ix];
        return (c.r==40&&c.g==100&&c.b==135)||(c.r==240&&c.g==75&&c.b==35);
    }
    static bool IsOriginalTile(Terrain t){
        var p=t.transform.parent;
        string n=p?p.name:"";
        bool source=n.IndexOf("LINKED REFERENCE",StringComparison.OrdinalIgnoreCase)>=0||
                    n.IndexOf("LINKED ADJUSTABLE",StringComparison.OrdinalIgnoreCase)>=0;
        bool dragon=n.IndexOf("Dragon Isle",StringComparison.OrdinalIgnoreCase)>=0;
        return source&&!dragon;
    }
    static Material BuildMat(R r){
        var m=AssetDatabase.LoadAssetAtPath<Material>(r.matPath);
        if(!m){m=new Material(Shader.Find("Standard")){name=Path.GetFileNameWithoutExtension(r.matPath)};AssetDatabase.CreateAsset(m,r.matPath);}
        m.shader=Shader.Find("Standard");
        var a=AssetDatabase.LoadAssetAtPath<Texture2D>(r.albedo);
        var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(r.mask);
        if(!a||!mask)throw new Exception("Missing rock textures "+r.fbx);
        m.SetTexture("_MainTex",a);m.SetTexture("_MetallicGlossMap",mask);m.SetTexture("_OcclusionMap",mask);
        m.EnableKeyword("_METALLICGLOSSMAP");m.SetFloat("_Metallic",0f);m.SetFloat("_GlossMapScale",.22f);m.SetFloat("_Glossiness",.16f);m.SetFloat("_OcclusionStrength",1f);
        if(!string.IsNullOrEmpty(r.normal)){
            var n=AssetDatabase.LoadAssetAtPath<Texture2D>(r.normal);if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
        }
        m.SetColor("_Color",new Color(.96f,.96f,.96f,1));m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static void Prepare(){
        Directory.CreateDirectory("Assets/Materials/RealisticWorld/Rocks");
        foreach(var r in Rocks){
            r.mesh=AssetDatabase.LoadAllAssetsAtPath(r.fbx).OfType<Mesh>()
                .FirstOrDefault(m=>m.name.IndexOf("LOD2",StringComparison.OrdinalIgnoreCase)>=0);
            if(!r.mesh) r.mesh=AssetDatabase.LoadAllAssetsAtPath(r.fbx).OfType<Mesh>()
                .OrderBy(m=>m.triangles.Length).FirstOrDefault();
            if(!r.mesh)throw new Exception("No mesh "+r.fbx);
            r.mat=BuildMat(r);
        }
        // Durable fix for the exact Quixel source material the user is manually placing.
        string srcPath="Assets/Art/Environment/_ExternalContent/Quixel/Megascans/Rocks/Rock_Granite_rcCwC/Materials/Rock_Granite_rcCwC_mat.mat";
        var src=AssetDatabase.LoadAssetAtPath<Material>(srcPath); if(!src)throw new Exception("Missing rcCwC source material");
        var good=Rocks[0].mat;
        src.shader=Shader.Find("Standard");
        foreach(var p in new[]{"_MainTex","_BumpMap","_MetallicGlossMap","_OcclusionMap"}) if(src.HasProperty(p)&&good.HasProperty(p)) src.SetTexture(p,good.GetTexture(p));
        src.EnableKeyword("_NORMALMAP");src.EnableKeyword("_METALLICGLOSSMAP");
        src.SetFloat("_Metallic",0f);src.SetFloat("_GlossMapScale",.22f);src.SetFloat("_Glossiness",.16f);src.SetFloat("_OcclusionStrength",1f);src.color=new Color(.96f,.96f,.96f,1);src.enableInstancing=true;EditorUtility.SetDirty(src);
    }
    static float RoadWeight(Terrain t,float lx,float lz){
        var td=t.terrainData;var layers=td.terrainLayers;
        var ids=Enumerable.Range(0,layers.Length).Where(i=>layers[i]&&layers[i].name.IndexOf("Road",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        if(ids.Length==0)return 0;
        int ax=Mathf.Clamp(Mathf.RoundToInt(lx/td.size.x*(td.alphamapWidth-1)),0,td.alphamapWidth-1);
        int az=Mathf.Clamp(Mathf.RoundToInt(lz/td.size.z*(td.alphamapHeight-1)),0,td.alphamapHeight-1);
        var a=td.GetAlphamaps(ax,az,1,1);return ids.Sum(i=>a[0,0,i]);
    }
    static float RockAffinity(Terrain t,float lx,float lz){
        var td=t.terrainData;var layers=td.terrainLayers;
        int ax=Mathf.Clamp(Mathf.RoundToInt(lx/td.size.x*(td.alphamapWidth-1)),0,td.alphamapWidth-1);
        int az=Mathf.Clamp(Mathf.RoundToInt(lz/td.size.z*(td.alphamapHeight-1)),0,td.alphamapHeight-1);
        var a=td.GetAlphamaps(ax,az,1,1);float s=0;
        for(int i=0;i<layers.Length;i++){
            string n=layers[i]?layers[i].name.ToLowerInvariant():"";
            float w=a[0,0,i];
            if(n.Contains("rock")||n.Contains("volcan"))s+=w;
            else if(n.Contains("arid")||n.Contains("desert")||n.Contains("snow"))s+=w*.45f;
        }
        return Mathf.Clamp01(s);
    }
    static bool NearArchitecture(Vector3 p,Bounds[] bs){
        foreach(var b0 in bs){
            var b=b0;b.Expand(new Vector3(12,4,12));
            if(p.x>=b.min.x&&p.x<=b.max.x&&p.z>=b.min.z&&p.z<=b.max.z)return true;
        }return false;
    }
    static bool Architecture(Renderer r){
        var f=r.GetComponent<MeshFilter>();string p=f&&f.sharedMesh?AssetDatabase.GetAssetPath(f.sharedMesh).ToLowerInvariant():"";
        string n=r.name.ToLowerInvariant();
        string s=p+"|"+n;
        return s.Contains("house")||s.Contains("building")||s.Contains("architecture")||s.Contains("castle")||s.Contains("tower")||s.Contains("temple")||s.Contains("bridge")||s.Contains("wall")||s.Contains("dock")||s.Contains("gate");
    }

    public static void Run(){
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        if(scene.GetRootGameObjects().Any(g=>g.name==RootName))throw new Exception("Rock scatter already exists");
        Prepare();
        if(!File.Exists(WaterMask))throw new Exception("Water mask missing");
        var im=new Texture2D(2,2,TextureFormat.RGBA32,false,true);im.LoadImage(File.ReadAllBytes(WaterMask));var px=im.GetPixels32();int mw=im.width,mh=im.height;UnityEngine.Object.DestroyImmediate(im);

        var arch=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&Architecture(r)).Select(r=>r.bounds).ToArray();
        var terrains=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).Where(IsOriginalTile).OrderBy(t=>t.transform.position.z).ThenBy(t=>t.transform.position.x).ToArray();
        if(terrains.Length!=15)throw new Exception("Expected 15 original terrains, got "+terrains.Length);

        var root=new GameObject(RootName);var report=new List<string>();int total=0;long tris=0;
        for(int ti=0;ti<terrains.Length;ti++){
            var t=terrains[ti];var td=t.terrainData;var cand=new List<C>();
            for(int gz=0;gz<20;gz++)for(int gx=0;gx<20;gx++){
                uint h=H(gx,gz,ti+117);float lx=18f+gx*25f+(((h&1023)/1023f)-.5f)*10f,lz=18f+gz*25f+((((h>>10)&1023)/1023f)-.5f)*10f;
                lx=Mathf.Clamp(lx,14,498);lz=Mathf.Clamp(lz,14,498);
                float wx=t.transform.position.x+lx,wz=t.transform.position.z+lz;if(IsWater(px,mw,mh,wx,wz))continue;
                float rw=RoadWeight(t,lx,lz);if(rw>.025f)continue;
                float slope=td.GetSteepness(lx/512f,lz/512f);if(slope<5.5f||slope>48f)continue;
                float y=t.SampleHeight(new Vector3(wx,0,wz))+t.transform.position.y;if(y<.18f)continue;
                var pos=new Vector3(wx,y,wz);if(NearArchitecture(pos,arch))continue;
                float affinity=RockAffinity(t,lx,lz);
                float score=Mathf.Clamp01((slope-5.5f)/24f)*.58f+affinity*.42f;
                if((h%1000)/1000f>Mathf.Lerp(.10f,.52f,score))continue;
                cand.Add(new C{t=t,x=wx,z=wz,y=y,slope=slope,score=score,normal=td.GetInterpolatedNormal(lx/512f,lz/512f),h=h,variant=(int)((h>>19)%3)});
            }
            int target=6;var chosen=cand.OrderByDescending(c=>c.score+.15f*((c.h&255)/255f)).Take(target).ToArray();
            foreach(var c in chosen){
                var rr=Rocks[c.variant];var go=new GameObject($"OriginalRock_{ti:00}_{total:000}");go.transform.SetParent(root.transform,true);
                go.AddComponent<MeshFilter>().sharedMesh=rr.mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=rr.mat;mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;
                float maxDim=Mathf.Max(.0001f,Mathf.Max(rr.mesh.bounds.size.x,Mathf.Max(rr.mesh.bounds.size.y,rr.mesh.bounds.size.z)));
                float desired=Mathf.Lerp(1.35f,3.8f,((c.h>>8)&255)/255f);float sc=desired/maxDim;
                var align=Quaternion.FromToRotation(Vector3.up,Vector3.Slerp(Vector3.up,c.normal,.30f));
                go.transform.rotation=align*Quaternion.Euler(-90f,(c.h>>3)%360,0);
                go.transform.localScale=Vector3.one*sc;go.transform.position=new Vector3(c.x,c.y-.07f*desired,c.z);
                tris+=rr.mesh.triangles.Length/3;total++;
            }
            report.Add($"{t.terrainData.name} pos={t.transform.position} candidates={cand.Count} placed={chosen.Length}");
        }
        if(total<55)throw new Exception("Too few original-zone rocks "+total);
        EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");AssetDatabase.SaveAssets();
        report.Insert(0,$"PASS originalTerrains={terrains.Length} rocks={total} sourceTriangles={tris} architectureBounds={arch.Length} terrainWrites=0 heightWrites=0 alphamapWrites=0 terrainLayerWrites=0");
        File.WriteAllLines("Validation/EdgeGrid20260923/original_zone_rock_scatter_20260930.txt",report);Debug.Log(report[0]);
    }
}

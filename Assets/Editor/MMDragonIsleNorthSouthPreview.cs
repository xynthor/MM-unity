using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class MMDragonIsleNorthSouthPreview {
    const string SOURCE="Assets/Scenes/DragonIsle_Reference.unity";
    const string TDROOT="Assets/World/DragonIsle/Generated/";
    const string OUTROOT="Assets/Scenes/";
    const float SEA=-6.7f, BASE=-24f, HEIGHT=320f;
    static float Smooth(float t) {t=Mathf.Clamp01(t);return t*t*(3f-2f*t);}
    static Bounds BoundsOf(GameObject go) {
        var rr=go.GetComponentsInChildren<Renderer>(true);
        if(rr.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rr[0].bounds;for(int i=1;i<rr.Length;i++)b.Encapsulate(rr[i].bounds);
        return b;
    }
    static int KeepOneSide(Transform parent,bool north) {
        int n=0;
        foreach(Transform c in parent.Cast<Transform>().ToArray()) {
            var center=BoundsOf(c.gameObject).center.z;
            if((center>=0f)!=north){UnityEngine.Object.DestroyImmediate(c.gameObject);continue;}
            n++;
        }
        return n;
    }
    static float[,] SplitHeights(TerrainData src,bool north) {
        int n=src.heightmapResolution;if(n!=513)throw new Exception("Dragon source must be 513");
        var h=src.GetHeights(0,0,n,n);var outH=new float[n,n];float sea=(SEA-BASE)/HEIGHT;
        for(int z=0;z<n;z++)for(int x=0;x<n;x++) {
            if(north && z<=256)outH[z,x]=h[z+256,x];
            else if(!north && z>=256)outH[z,x]=h[z-256,x];
            else {
                int edge=north?512:0;
                float u=(north?z-256:256-z)/64f;
                outH[z,x]=Mathf.Lerp(h[edge,x],sea,Smooth(u));
            }
        }
        return outH;
    }
    static float[,,] SplitAlpha(TerrainData src,bool north){
        int m=src.alphamapResolution,L=src.alphamapLayers;
        if(m!=512)throw new Exception("Dragon alpha must be 512");
        var a=src.GetAlphamaps(0,0,m,m);var b=new float[m,m,L];
        int sand=Array.FindIndex(src.terrainLayers,x=>x&&x.name.Contains("Desert"));
        if(sand<0)sand=0;
        for(int z=0;z<m;z++)for(int x=0;x<m;x++){
            bool keep=north?z<256:z>=256;
            if(keep){int old=north?z+256:z-256;
                for(int k=0;k<L;k++)b[z,x,k]=a[old,x,k];
            }else b[z,x,sand]=1f;
        }
        return b;
    }
    static string SceneName(bool north)=>north?"__TEST_DragonIsle_North":"__TEST_DragonIsle_South";
    static string TerrainName(bool north)=>north?"__TEST_DragonIsleNorthTerrain":"__TEST_DragonIsleSouthTerrain";
    static void BuildHalf(bool north,List<string> report) {
        var sc=EditorSceneManager.OpenScene(SOURCE,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().First(x=>x.name.Contains("Open World"));
        var old=root.GetComponentInChildren<Terrain>(true);
        if(!old)throw new Exception("Source Dragon terrain missing");
        var srcTD=old.terrainData;
        string asset=TDROOT+TerrainName(north)+".asset";
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(asset);
        if(!td){td=new TerrainData();td.heightmapResolution=513;td.alphamapResolution=512;
            td.baseMapResolution=512;td.size=new Vector3(512,320,512);AssetDatabase.CreateAsset(td,asset);
        }
        td.terrainLayers=srcTD.terrainLayers;
        td.SetHeights(0,0,SplitHeights(srcTD,north));td.SetAlphamaps(0,0,SplitAlpha(srcTD,north));
        td.detailPrototypes=srcTD.detailPrototypes;
        EditorUtility.SetDirty(td);
        var test=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var clone=UnityEngine.Object.Instantiate(root);clone.name=(north?"Dragon Isle North":"Dragon Isle South")+" - Open World TEST";
        SceneManager.MoveGameObjectToScene(clone,test);
        clone.transform.position=Vector3.zero;clone.transform.rotation=Quaternion.identity;
        var terrain=clone.GetComponentInChildren<Terrain>(true);terrain.terrainData=td;
        var collider=terrain.GetComponent<TerrainCollider>();if(collider)collider.terrainData=td;
        var water=clone.transform.Find("Ocean + Central Lake Water");
        if(water){water.localScale=new Vector3(51.2f,water.localScale.y,51.2f);}
        int landmarks=0,geo=0,understory=0;
        var marks=clone.transform.Find("Landmarks - Reference");
        if(marks){landmarks=KeepOneSide(marks,north);marks.localPosition+=Vector3.forward*(north?-256f:256f);}
        var trace=clone.transform.Find("Dragon Isle - Reference Trace");
        if(trace){
            var geology=trace.Find("Geological formations - traced from reference");
            if(geology)foreach(Transform group in geology.Cast<Transform>().ToArray())
                geo+=KeepOneSide(group,north);
            var under=trace.Find("Understory patches - sheltered ground");
            if(under)understory=KeepOneSide(under,north);
            trace.localPosition+=Vector3.forward*(north?-256f:256f);
        }
        EditorSceneManager.SetActiveScene(test);
        string path=OUTROOT+SceneName(north)+".unity";
        EditorSceneManager.MarkSceneDirty(test);
        if(!EditorSceneManager.SaveScene(test,path))throw new IOException("Failed "+path);
        AssetDatabase.SaveAssets();
        report.Add(SceneName(north)+",landmarks="+landmarks+",geo="+geo+",understory="+understory+",asset="+asset);
        EditorSceneManager.CloseScene(sc,true);
    }
    static void PreviewCombined(){
        var combined=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        for(int i=0;i<2;i++){
            bool north=i==0;
            var src=EditorSceneManager.OpenScene(OUTROOT+SceneName(north)+".unity",OpenSceneMode.Additive);
            var rt=src.GetRootGameObjects().First(x=>x.name.Contains("Open World"));
            var clone=UnityEngine.Object.Instantiate(rt);
            SceneManager.MoveGameObjectToScene(clone,combined);
            clone.transform.position=new Vector3(0,0,north?256f:-256f);
            foreach(var l in clone.GetComponentsInChildren<Light>(true))l.enabled=false;
            EditorSceneManager.CloseScene(src,true);
        }
        var cgo=new GameObject("TEMP - Dragon Split Preview Camera");
        SceneManager.MoveGameObjectToScene(cgo,combined);
        var cam=cgo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=552;
        cam.transform.position=new Vector3(0,1300,0);cam.transform.rotation=Quaternion.Euler(90,0,0);
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.19f,.20f);
        cam.nearClipPlane=.1f;cam.farClipPlane=2400;
        var sun=new GameObject("TEMP - Preview Sun");
        SceneManager.MoveGameObjectToScene(sun,combined);
        sun.transform.rotation=Quaternion.Euler(48,330,0);
        var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.62f;
        var rtTex=new RenderTexture(940,1180,24);cam.targetTexture=rtTex;cam.Render();
        RenderTexture.active=rtTex;var tex=new Texture2D(rtTex.width,rtTex.height,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,rtTex.width,rtTex.height),0,0);tex.Apply();
        Directory.CreateDirectory("Preview");
        File.WriteAllBytes("Preview/DragonIsle_NorthSouth_TEST.png",tex.EncodeToPNG());
        RenderTexture.active=null;cam.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(rtTex);UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(cgo);UnityEngine.Object.DestroyImmediate(sun);
    }
    [MenuItem("MMUnity/World/Preview Dragon Isle North South Split")]
    public static void Build(){
        Directory.CreateDirectory("Validation/EdgeGrid20260923");
        var rows=new List<string>();BuildHalf(true,rows);BuildHalf(false,rows);
        var north=AssetDatabase.LoadAssetAtPath<TerrainData>(TDROOT+TerrainName(true)+".asset");
        var south=AssetDatabase.LoadAssetAtPath<TerrainData>(TDROOT+TerrainName(false)+".asset");
        var a=north.GetHeights(0,0,513,1);var b=south.GetHeights(0,512,513,1);
        float err=0;for(int x=0;x<513;x++)err=Mathf.Max(err,Mathf.Abs(a[0,x]-b[0,x])*HEIGHT);
        if(err>0.0001f)throw new Exception("Dragon half seam mismatch "+err);
        rows.Add("height_seam_error_m="+err.ToString("F6"));
        PreviewCombined();
        File.WriteAllLines("Validation/EdgeGrid20260923/dragon_split_test_report.txt",rows);
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("DRAGON_NORTH_SOUTH_TEST_DONE seam="+err+" files="+rows.Count);
    }
}

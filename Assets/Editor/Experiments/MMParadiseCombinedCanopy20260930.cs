using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMParadiseCombinedCanopy20260930
{
    const string Root="Assets/World/WorldExtensions/Generated/ParadiseCombinedCanopy20260930";
    const string MaskBiome="Validation/EdgeGrid20260923/ReferenceBiomeCandidates_20260925/ParadiseValley_West_ReferenceBiomes.png";
    const string MaskLand="Validation/EdgeGrid20260923/ReferenceMasks/ParadiseValley_West.png";
    const int Target=300;
    const float CellSize=128f;

    class Part {
        public readonly List<Vector3> v=new List<Vector3>();
        public readonly List<Vector3> n=new List<Vector3>();
        public readonly List<Vector2> uv=new List<Vector2>();
        public readonly List<Vector4> tan=new List<Vector4>();
        public readonly List<int> tri=new List<int>();
    }
    class Cell {
        public readonly Dictionary<Material,Part> parts=new Dictionary<Material,Part>();
        public int trees;
    }
    class Choice {
        public Transform root;
        public MeshRenderer renderer;
        public Mesh mesh;
        public Matrix4x4 meshToRoot;
        public Vector3 scale;
        public float bottom;
        public string kind;
    }

    static Color32[] Load(string p){var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);if(!t.LoadImage(File.ReadAllBytes(p))||t.width!=513||t.height!=513)throw new Exception("Bad mask "+p);var a=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return a;}
    static Bounds Visual(Transform t){var rs=t.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(rs.Length==0)rs=t.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)throw new Exception("No renderer "+t.name);var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}
    static bool Apart(Vector2 p,List<Vector2> used,float d){float q=d*d;foreach(var u in used)if((p-u).sqrMagnitude<q)return false;return true;}
    static void BuildPoints(List<Vector2> pts){var rng=new System.Random(20260930);for(int z=10;z<503;z+=3)for(int x=10;x<490;x+=3)pts.Add(new Vector2(x+((float)rng.NextDouble()-.5f)*2.4f,z+((float)rng.NextDouble()-.5f)*2.4f));for(int i=pts.Count-1;i>0;i--){int j=rng.Next(i+1);var q=pts[i];pts[i]=pts[j];pts[j]=q;}}

    static Choice From(Transform root,int lodIndex,string kind)
    {
        var g=root.GetComponent<LODGroup>()??root.GetComponentsInChildren<LODGroup>(true).FirstOrDefault();
        if(!g)throw new Exception("Missing LODGroup "+root.name);
        var lods=g.GetLODs();lodIndex=Mathf.Clamp(lodIndex,0,lods.Length-1);
        var r=lods[lodIndex].renderers.OfType<MeshRenderer>().FirstOrDefault();
        if(!r)throw new Exception("Missing LOD renderer "+root.name);
        var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)throw new Exception("Missing mesh "+root.name);
        var b=Visual(root);
        return new Choice{root=root,renderer=r,mesh=f.sharedMesh,meshToRoot=root.worldToLocalMatrix*r.localToWorldMatrix,scale=root.lossyScale,bottom=root.position.y-b.min.y,kind=kind};
    }
    static int RoadIndex(Terrain t)
    {
        var ls=t.terrainData.terrainLayers;
        for(int i=0;i<ls.Length;i++)if(ls[i]){
            string n=ls[i].name??"";string p=ls[i].diffuseTexture?AssetDatabase.GetAssetPath(ls[i].diffuseTexture):"";
            if(n.IndexOf("road",StringComparison.OrdinalIgnoreCase)>=0||p.IndexOf("stone_ground",StringComparison.OrdinalIgnoreCase)>=0)return i;
        }
        return -1;
    }
    static void AddSubmesh(Cell cell,Material mat,Mesh mesh,int sub,Matrix4x4 m,Vector3 cellOrigin)
    {
        if(!cell.parts.TryGetValue(mat,out var part)){part=new Part();cell.parts[mat]=part;}
        var vs=mesh.vertices;var ns=mesh.normals;var us=mesh.uv;var ts=mesh.tangents;var ids=mesh.GetTriangles(sub);var map=new Dictionary<int,int>();
        foreach(int id in ids){
            if(!map.TryGetValue(id,out int next)){
                next=part.v.Count;map[id]=next;
                part.v.Add(m.MultiplyPoint3x4(vs[id])-cellOrigin);
                part.n.Add(ns!=null&&ns.Length==vs.Length?m.MultiplyVector(ns[id]).normalized:Vector3.up);
                part.uv.Add(us!=null&&us.Length==vs.Length?us[id]:Vector2.zero);
                if(ts!=null&&ts.Length==vs.Length){var q=m.MultiplyVector(new Vector3(ts[id].x,ts[id].y,ts[id].z)).normalized;part.tan.Add(new Vector4(q.x,q.y,q.z,ts[id].w));}
                else part.tan.Add(new Vector4(1,0,0,1));
            }
            part.tri.Add(next);
        }
    }

    static Terrain GetTerrain(Scene scene,Transform source)
    {
        var t=source.parent.GetComponentsInChildren<Terrain>(true).FirstOrDefault(x=>x.terrainData&&x.terrainData.name.IndexOf("ParadiseValley_WestTerrain",StringComparison.OrdinalIgnoreCase)>=0);
        if(!t)t=source.parent.GetComponentInChildren<Terrain>(true);
        if(!t)throw new Exception("Paradise terrain missing");
        return t;
    }

    public static void Generate()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Saved linked Edit Mode scene required");
        if(AssetDatabase.IsValidFolder(Root))throw new Exception("Generated candidate folder already exists; do not overwrite blindly");
        AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","ParadiseCombinedCanopy20260930");

        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var source=all.Single(t=>t.name=="Paradise Photo-Verified Tall Woodland 20260926");
        var terrain=GetTerrain(scene,source);
        var pines=source.Cast<Transform>().Where(t=>t.name.StartsWith("ReferenceTallPine_")).Select(t=>From(t,2,"pine")).ToArray();
        var firstBroad=source.Cast<Transform>().First(t=>t.name.StartsWith("ReferenceForestTree_"));
        var broad=firstBroad.GetComponentsInChildren<LODGroup>(true).Take(5).Select(g=>From(g.transform,1,"broad")).ToArray();
        if(pines.Length<200||broad.Length<4)throw new Exception("Source templates incomplete");

        var forest=Load(MaskBiome);var land=Load(MaskLand);var used=source.Cast<Transform>().Select(t=>new Vector2(t.position.x,t.position.z)).ToList();
        int road=RoadIndex(terrain);var alpha=road>=0?terrain.terrainData.GetAlphamaps(0,0,terrain.terrainData.alphamapWidth,terrain.terrainData.alphamapHeight):null;
        var pts=new List<Vector2>();BuildPoints(pts);var rng=new System.Random(20261130);
        var cells=new Dictionary<Vector2Int,Cell>();int added=0,pineN=0,broadN=0,roadSkip=0;
        foreach(var v in pts)
        {
            if(added>=Target)break;
            int x=Mathf.Clamp(Mathf.RoundToInt(v.x),0,512),z=Mathf.Clamp(Mathf.RoundToInt(v.y),0,512);var c=forest[z*513+x];var l=land[z*513+x];
            if(c.g<150||c.r>145||l.r<150)continue;
            if(road>=0){int ax=Mathf.Clamp(Mathf.RoundToInt(v.x/512f*(terrain.terrainData.alphamapWidth-1)),0,terrain.terrainData.alphamapWidth-1),az=Mathf.Clamp(Mathf.RoundToInt(v.y/512f*(terrain.terrainData.alphamapHeight-1)),0,terrain.terrainData.alphamapHeight-1);if(alpha[az,ax,road]>.06f){roadSkip++;continue;}}
            float wx=terrain.transform.position.x+v.x,wz=terrain.transform.position.z+v.y,elev=terrain.SampleHeight(new Vector3(wx,0,wz))+terrain.transform.position.y;
            if(elev<.75f||elev>70f||terrain.terrainData.GetSteepness(v.x/512f,v.y/512f)>31f)continue;
            var loc=new Vector2(wx,wz);if(!Apart(loc,used,3.45f))continue;
            bool isPine=rng.NextDouble()<.72;var src=isPine?pines[rng.Next(pines.Length)]:broad[rng.Next(broad.Length)];
            float s=.88f+(float)rng.NextDouble()*.24f;float yaw=(float)rng.NextDouble()*360f;
            float y=elev+src.bottom*s-.02f;var matrix=Matrix4x4.TRS(new Vector3(wx,y,wz),Quaternion.Euler(0,yaw,0),src.scale*s)*src.meshToRoot;
            int cx=Mathf.FloorToInt(v.x/CellSize),cz=Mathf.FloorToInt(v.y/CellSize);var key=new Vector2Int(cx,cz);if(!cells.TryGetValue(key,out var cell)){cell=new Cell();cells[key]=cell;}
            var origin=new Vector3(terrain.transform.position.x+cx*CellSize,0,terrain.transform.position.z+cz*CellSize);
            var mats=src.renderer.sharedMaterials;
            for(int sub=0;sub<src.mesh.subMeshCount;sub++){var mat=mats[Mathf.Min(sub,mats.Length-1)];if(!mat)continue;AddSubmesh(cell,mat,src.mesh,sub,matrix,origin);}
            cell.trees++;used.Add(loc);added++;if(isPine)pineN++;else broadN++;
        }
        if(added!=Target)throw new Exception("Could place only "+added);

        long totalTris=0;int estimatedDraws=0;var report=new List<string>();
        foreach(var kv in cells.OrderBy(k=>k.Key.y).ThenBy(k=>k.Key.x))
        {
            var key=kv.Key;var cell=kv.Value;var mesh=new Mesh{indexFormat=IndexFormat.UInt32,name="ParadiseCanopyCell_"+key.x+"_"+key.y};
            var verts=new List<Vector3>();var norms=new List<Vector3>();var uvs=new List<Vector2>();var tans=new List<Vector4>();var subTris=new List<int[]>();var materials=new List<Material>();
            foreach(var mp in cell.parts.OrderBy(p=>AssetDatabase.GetAssetPath(p.Key)))
            {
                int b=verts.Count;verts.AddRange(mp.Value.v);norms.AddRange(mp.Value.n);uvs.AddRange(mp.Value.uv);tans.AddRange(mp.Value.tan);subTris.Add(mp.Value.tri.Select(i=>i+b).ToArray());materials.Add(mp.Key);
            }
            mesh.SetVertices(verts);mesh.SetNormals(norms);mesh.SetUVs(0,uvs);mesh.SetTangents(tans);mesh.subMeshCount=subTris.Count;for(int i=0;i<subTris.Count;i++)mesh.SetTriangles(subTris[i],i,false);mesh.RecalculateBounds();
            string ap=Root+"/Cell_"+key.x+"_"+key.y+".asset";AssetDatabase.CreateAsset(mesh,ap);
            long tris=subTris.Sum(a=>(long)a.Length/3);totalTris+=tris;estimatedDraws+=subTris.Count;
            File.WriteAllLines(Root+"/Cell_"+key.x+"_"+key.y+".materials.txt",materials.Select(AssetDatabase.GetAssetPath));
            report.Add(key.x+","+key.y+" trees="+cell.trees+" tris="+tris+" submeshes="+subTris.Count+" asset="+ap);
        }
        AssetDatabase.SaveAssets();
        report.Insert(0,"PASS target="+Target+" added="+added+" pines="+pineN+" broadSingles="+broadN+" cells="+cells.Count+" triangles="+totalTris+" estimatedDraws="+estimatedDraws+" roadLayer="+road+" roadSkipped="+roadSkip+" terrainWrites=0 heightWrites=0");
        File.WriteAllLines("Validation/EdgeGrid20260923/paradise_combined_canopy_generate_20260930.txt",report);
        Debug.Log(report[0]);
    }

    static Dictionary<Vector2Int,Material[]> LoadMaterials()
    {
        var d=new Dictionary<Vector2Int,Material[]>();
        foreach(string txt in Directory.GetFiles(Root,"Cell_*.materials.txt"))
        {
            string n=Path.GetFileName(txt);n=n.Substring("Cell_".Length,n.Length-"Cell_".Length-".materials.txt".Length);var p=n.Split('_');var key=new Vector2Int(int.Parse(p[0]),int.Parse(p[1]));
            d[key]=File.ReadAllLines(txt).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray();
        }
        return d;
    }
    static GameObject BuildRoot(string name,Transform parent,Terrain terrain)
    {
        if(!AssetDatabase.IsValidFolder(Root))throw new Exception("Generate first");
        var mats=LoadMaterials();var root=new GameObject(name);if(parent)root.transform.SetParent(parent,false);
        foreach(string ap in AssetDatabase.FindAssets("t:Mesh",new[]{Root}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(x=>x))
        {
            string n=Path.GetFileNameWithoutExtension(ap);if(!n.StartsWith("Cell_"))continue;var p=n.Substring(5).Split('_');var key=new Vector2Int(int.Parse(p[0]),int.Parse(p[1]));
            var go=new GameObject(n);go.transform.SetParent(root.transform,false);go.transform.position=new Vector3(terrain.transform.position.x+key.x*CellSize,0,terrain.transform.position.z+key.y*CellSize);
            go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(ap);var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterials=mats[key];mr.shadowCastingMode=ShadowCastingMode.On;mr.receiveShadows=true;
        }
        return root;
    }
    static void CaptureForest(Scene main,Scene scratch,Terrain terrain,string path)
    {
        var originalLights=main.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Where(l=>l.type==LightType.Directional).ToArray();var originalEnabled=originalLights.Select(l=>l.enabled).ToArray();
        var lightGO=new GameObject("TEMP Combined Forest Sun");SceneManager.MoveGameObjectToScene(lightGO,scratch);lightGO.transform.rotation=Quaternion.Euler(43,318,0);var sun=lightGO.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.7f;
        var cameraGO=new GameObject("TEMP Combined Forest Camera");SceneManager.MoveGameObjectToScene(cameraGO,scratch);var cam=cameraGO.AddComponent<Camera>();cam.fieldOfView=60;cam.nearClipPlane=.2f;cam.farClipPlane=650;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.60f,.72f,.8f);cam.allowHDR=false;cam.depthTextureMode=DepthTextureMode.Depth;
        var rt=new RenderTexture(1280,800,24);rt.antiAliasing=2;var prev=RenderTexture.active;cam.targetTexture=rt;var oldMode=RenderSettings.ambientMode;var oldLight=RenderSettings.ambientLight;
        try{foreach(var l in originalLights)if(l)l.enabled=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.46f,.46f,.46f);var c=terrain.transform.position+new Vector3(350,terrain.terrainData.GetInterpolatedHeight(350f/512,350f/512),350);cam.transform.position=c+new Vector3(-140,75,-70);cam.transform.LookAt(c+new Vector3(0,5,0));cam.Render();RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}finally{cam.targetTexture=null;RenderTexture.active=prev;RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldLight;for(int i=0;i<originalLights.Length;i++)if(originalLights[i])originalLights[i].enabled=originalEnabled[i];UnityEngine.Object.DestroyImmediate(rt);}
    }
    public static void Preview()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Saved linked Edit Mode scene required");
        var source=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Paradise Photo-Verified Tall Woodland 20260926");var terrain=GetTerrain(scene,source);
        var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var root=BuildRoot("TEMP Paradise Combined Canopy",null,terrain);SceneManager.MoveGameObjectToScene(root,scratch);SceneManager.SetActiveScene(scene);
        try{string d="Validation/EdgeGrid20260923/ParadiseCombinedCanopyPreview";Directory.CreateDirectory(d);CaptureForest(scene,scratch,terrain,d+"/Reference_ParadiseWest_Forest_Oblique.png");}
        finally{EditorSceneManager.CloseScene(scratch,true);SceneManager.SetActiveScene(scene);MMReferenceDetailPerspectiveQA20260925.CaptureAll();}
    }
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Saved linked Edit Mode scene required");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();if(all.Any(t=>t.name=="Paradise Combined Reference Canopy 20260930"))throw new Exception("Combined canopy already exists");
        var source=all.Single(t=>t.name=="Paradise Photo-Verified Tall Woodland 20260926");var terrain=GetTerrain(scene,source);var root=BuildRoot("Paradise Combined Reference Canopy 20260930",source.parent,terrain);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText("Validation/EdgeGrid20260923/paradise_combined_canopy_applied_20260930.txt","PASS cells="+root.transform.childCount+" terrainWrites=0 heightWrites=0 terrainLayerWrites=0 canonicalWrites=0\n");
    }
}

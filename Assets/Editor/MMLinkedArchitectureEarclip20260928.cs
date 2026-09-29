using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMLinkedArchitectureEarclip20260928
{
    const string Root="Assets/World/WorldExtensions/Generated/LinkedArchitectureEarclip";
    [Serializable] class Entry { public string zone,source,candidate,source_sha256; public int fixed_facets; }
    [Serializable] class Entries { public Entry[] items; }
    static Entry[] Read() => JsonUtility.FromJson<Entries>("{\"items\":"+File.ReadAllText("Validation/EdgeGrid20260923/OtherArchitectureEarclip/candidates.json")+"}").items;
    static void Folder(string p) { if(AssetDatabase.IsValidFolder(p))return;int n=p.LastIndexOf('/');Folder(p.Substring(0,n));AssetDatabase.CreateFolder(p.Substring(0,n),p.Substring(n+1)); }
    static string Hash(string p) { using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant(); }
    public static void ImportCandidates()
    {
        var entries=Read();
        foreach(var e in entries)if(Hash(e.source)!=e.source_sha256)throw new Exception("Source changed: "+e.source);
        foreach(var e in entries)
        {
            string dir=Root+"/"+e.zone;Folder(dir);
            string dest=dir+"/"+Path.GetFileName(e.source);
            if(File.Exists(dest))throw new Exception("Candidate exists; inspect before retry: "+dest);
            foreach(var line in File.ReadLines(e.source).Where(l=>l.StartsWith("mtllib ")))
            {
                string mtl=line.Substring(7).Trim();string from=Path.GetDirectoryName(e.source).Replace('\\','/')+"/"+mtl;
                if(File.Exists(from)&&!File.Exists(dir+"/"+mtl))AssetDatabase.CopyAsset(from,dir+"/"+mtl);
            }
            if(!AssetDatabase.CopyAsset(e.source,dest))throw new Exception("Copy failed "+e.source);
            File.WriteAllText(dest,File.ReadAllText(e.candidate));
            AssetDatabase.ImportAsset(dest,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        }
        Debug.Log("Imported "+entries.Length+" corrected model copies; linked scene unchanged.");
    }
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/Enroth_Linked_OpenWorld.unity"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Saved linked scene required");
        var replacements=new Dictionary<Mesh,Mesh>();var report=new List<string>();
        foreach(var e in Read())
        {
            if(Hash(e.source)!=e.source_sha256)throw new Exception("Source changed "+e.source);
            var a=AssetDatabase.LoadAssetAtPath<GameObject>(e.source).GetComponentsInChildren<MeshFilter>(true);
            var b=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/"+e.zone+"/"+Path.GetFileName(e.source)).GetComponentsInChildren<MeshFilter>(true);
            if(a.Length!=1||b.Length!=1)throw new Exception("Unexpected multi-mesh import "+e.source);
            var am=a[0].sharedMesh;var bm=b[0].sharedMesh;
            var ma=a[0].GetComponent<Renderer>().sharedMaterials.Select(m=>m?m.name:"").ToArray();
            var mb=b[0].GetComponent<Renderer>().sharedMaterials.Select(m=>m?m.name:"").ToArray();
            if(!ma.SequenceEqual(mb)||am.subMeshCount!=bm.subMeshCount||(am.bounds.center-bm.bounds.center).magnitude>.0001f||(am.bounds.size-bm.bounds.size).magnitude>.0001f)
                throw new Exception("Candidate material/bounds mismatch "+e.source+" old="+string.Join(",",ma)+" new="+string.Join(",",mb));
            replacements.Add(am,bm);report.Add(e.source+" submeshes="+bm.subMeshCount+" boundsUnchanged=true materialOrderUnchanged=true");
        }
        int filters=0,colliders=0;
        foreach(var root in scene.GetRootGameObjects())
        {
            foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))if(f.sharedMesh&&replacements.TryGetValue(f.sharedMesh,out var replacement)){Undo.RecordObject(f,"Correct concave source polygons");f.sharedMesh=replacement;EditorUtility.SetDirty(f);filters++;}
            foreach(var c in root.GetComponentsInChildren<MeshCollider>(true))if(c.sharedMesh&&replacements.TryGetValue(c.sharedMesh,out var replacement)){Undo.RecordObject(c,"Correct concave collider polygons");c.sharedMesh=replacement;EditorUtility.SetDirty(c);colliders++;}
        }
        if(filters==0)throw new Exception("No linked instances matched");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        report.Add("filters="+filters+" colliders="+colliders+" transformsMaterialsTerrainWrites=0 canonicalMeshWrites=0");
        File.WriteAllLines("Validation/EdgeGrid20260923/Resume_ArchitectureApplied.txt",report);
        Debug.Log(report.Last());
    }
    public static void Capture(string stage)
    {
        var scene=SceneManager.GetActiveScene();
        var filters=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).ToArray();
        string dir="Validation/EdgeGrid20260923/ResumeArchitecture/"+stage;Directory.CreateDirectory(dir);
        var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var go=new GameObject("TEMP Architecture QA");SceneManager.MoveGameObjectToScene(go,scratch);
        var camera=go.AddComponent<Camera>();camera.fieldOfView=45;camera.nearClipPlane=.05f;camera.farClipPlane=2000;
        camera.depthTextureMode=DepthTextureMode.Depth;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.6f,.7f,.8f);
        var rt=new RenderTexture(768,768,24);camera.targetTexture=rt;var previous=RenderTexture.active;
        try {
            foreach(var e in Read()) {
                string corrected=Root+"/"+e.zone+"/"+Path.GetFileName(e.source);
                var f=filters.FirstOrDefault(x=>x.sharedMesh&&x.gameObject.activeInHierarchy&&(AssetDatabase.GetAssetPath(x.sharedMesh)==e.source||AssetDatabase.GetAssetPath(x.sharedMesh)==corrected));
                if(!f)continue;
                var bounds=f.GetComponent<Renderer>().bounds;float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
                camera.transform.position=bounds.center+new Vector3(-1,.65f,-1)*size*1.4f;camera.transform.LookAt(bounds.center);
                camera.Render();RenderTexture.active=rt;var image=new Texture2D(768,768,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,768,768),0,0);image.Apply();File.WriteAllBytes(dir+"/"+e.zone+"_"+Path.GetFileNameWithoutExtension(e.source)+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
        } finally { camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.CloseScene(scratch,true);SceneManager.SetActiveScene(scene); }
    }
}

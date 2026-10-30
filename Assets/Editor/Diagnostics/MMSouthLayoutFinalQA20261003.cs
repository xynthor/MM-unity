using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MMSouthLayoutFinalQA20261003
{
    const string ScenePath="Assets/Scenes/World/Enroth.unity";
    const string OutDir="Validation/EdgeGrid20260923/SouthLayoutShift20261003";

    static Transform Find(Scene s,string name)
    {
        return s.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t=>t.name==name);
    }

    static Terrain ByData(Scene s,string dataName)
    {
        return s.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))
            .FirstOrDefault(t=>t.terrainData&&t.terrainData.name==dataName);
    }

    static float Seam(Terrain a,Terrain b)
    {
        var ap=a.transform.position; var bp=b.transform.position;
        var asz=a.terrainData.size; var bsz=b.terrainData.size;
        bool ns=Mathf.Abs((ap.z+asz.z)-bp.z)<.05f || Mathf.Abs((bp.z+bsz.z)-ap.z)<.05f;
        bool ew=Mathf.Abs((ap.x+asz.x)-bp.x)<.05f || Mathf.Abs((bp.x+bsz.x)-ap.x)<.05f;
        if(!ns&&!ew)throw new Exception("Terrains are not adjacent: "+a.terrainData.name+" / "+b.terrainData.name);

        float max=0f;
        for(int i=0;i<=512;i++)
        {
            float u=i/512f,wx,wz;
            if(ns)
            {
                wx=Mathf.Lerp(Mathf.Max(ap.x,bp.x),Mathf.Min(ap.x+asz.x,bp.x+bsz.x),u);
                wz=Mathf.Abs((ap.z+asz.z)-bp.z)<.05f?bp.z:ap.z;
            }
            else
            {
                wx=Mathf.Abs((ap.x+asz.x)-bp.x)<.05f?bp.x:ap.x;
                wz=Mathf.Lerp(Mathf.Max(ap.z,bp.z),Mathf.Min(ap.z+asz.z,bp.z+bsz.z),u);
            }
            float ah=a.SampleHeight(new Vector3(wx,0,wz))+ap.y;
            float bh=b.SampleHeight(new Vector3(wx,0,wz))+bp.y;
            max=Mathf.Max(max,Mathf.Abs(ah-bh));
        }
        return max;
    }

    static int ElongatedLandComponents(TerrainData d,List<string> details)
    {
        int n=d.heightmapResolution;
        var h=d.GetHeights(0,0,n,n);
        var land=new bool[n,n];
        var seen=new bool[n,n];
        for(int z=0;z<n;z++)for(int x=0;x<n;x++)
            land[z,x]=(-24f+h[z,x]*320f)>.55f;

        int[] dx={1,-1,0,0},dz={0,0,1,-1};
        int suspicious=0;
        for(int z=0;z<n;z++)for(int x=0;x<n;x++)
        {
            if(!land[z,x]||seen[z,x])continue;
            int count=0,minX=x,maxX=x,minZ=z,maxZ=z;
            var q=new Queue<Vector2Int>();
            q.Enqueue(new Vector2Int(x,z)); seen[z,x]=true;
            while(q.Count>0)
            {
                var p=q.Dequeue(); count++;
                minX=Math.Min(minX,p.x); maxX=Math.Max(maxX,p.x);
                minZ=Math.Min(minZ,p.y); maxZ=Math.Max(maxZ,p.y);
                for(int k=0;k<4;k++)
                {
                    int xx=p.x+dx[k],zz=p.y+dz[k];
                    if(xx<0||zz<0||xx>=n||zz>=n||seen[zz,xx]||!land[zz,xx])continue;
                    seen[zz,xx]=true; q.Enqueue(new Vector2Int(xx,zz));
                }
            }
            int sx=maxX-minX+1,sz=maxZ-minZ+1;
            float aspect=Mathf.Max(sx,sz)/(float)Mathf.Max(1,Mathf.Min(sx,sz));
            if(count>=100 && aspect>=8f)
            {
                suspicious++;
                details.Add(d.name+" count="+count+" span="+sx+"x"+sz+" aspect="+aspect.ToString("F2"));
            }
        }
        return suspicious;
    }

    static void Shot(Camera cam,string path,int w,int h)
    {
        var rt=new RenderTexture(w,h,24){antiAliasing=2};
        var old=RenderTexture.active; cam.targetTexture=rt; RenderTexture.active=rt; cam.Render();
        var tex=new Texture2D(w,h,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,w,h),0,0); tex.Apply();
        File.WriteAllBytes(path,tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        cam.targetTexture=null; RenderTexture.active=old; UnityEngine.Object.DestroyImmediate(rt);
    }

    static void Capture(Scene s)
    {
        var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        GameObject go=null;
        try
        {
            go=new GameObject("TEMP South Layout Final QA");
            SceneManager.MoveGameObjectToScene(go,scratch);
            var cam=go.AddComponent<Camera>();
            cam.allowHDR=false; cam.nearClipPlane=.3f; cam.farClipPlane=5000f;
            cam.clearFlags=CameraClearFlags.Skybox;

            cam.orthographic=true;
            cam.orthographicSize=1200f;
            cam.transform.position=new Vector3(0,1900,-950);
            cam.transform.rotation=Quaternion.Euler(90,0,0);
            Shot(cam,OutDir+"/South_Overhead_FINAL.png",1800,1100);

            cam.orthographic=false; cam.fieldOfView=48f;
            cam.transform.position=new Vector3(1200,650,-1900);
            cam.transform.LookAt(new Vector3(250,0,-750));
            Shot(cam,OutDir+"/South_Oblique_FINAL.png",1600,900);

            cam.orthographic=true; cam.orthographicSize=620f;
            cam.transform.position=new Vector3(768,1300,-1024);
            cam.transform.rotation=Quaternion.Euler(90,0,0);
            Shot(cam,OutDir+"/Archipelago_Overhead_FINAL.png",1600,1000);
        }
        finally
        {
            if(go)UnityEngine.Object.DestroyImmediate(go);
            EditorSceneManager.CloseScene(scratch,true);
        }
    }

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var lines=new List<string>();

        var mist=Find(s,"Misty Islands South - LINKED EXTENSION");
        var oldIron=Find(s,"Castle Ironfist South - LINKED EXTENSION");
        var oldMire=Find(s,"Mire of the Damned South - LINKED EXTENSION");
        var westRoot=Find(s,"Archipelago of the Ancients West - LINKED EXTENSION");
        var eastRoot=Find(s,"Archipelago of the Ancients East - LINKED EXTENSION");

        if(!mist||!westRoot||!eastRoot)throw new Exception("Required final south roots missing");
        if(oldIron||oldMire)throw new Exception("Old south filler/root still exists");

        lines.Add("MistyRoot="+mist.position);
        lines.Add("ArchWestRoot="+westRoot.position);
        lines.Add("ArchEastRoot="+eastRoot.position);

        if(Vector3.Distance(mist.position,new Vector3(0,0,-1024))>.01f)throw new Exception("Misty Islands South position wrong");
        if(Vector3.Distance(westRoot.position,new Vector3(512,0,-1024))>.01f)throw new Exception("Archipelago West position wrong");
        if(Vector3.Distance(eastRoot.position,new Vector3(1024,0,-1024))>.01f)throw new Exception("Archipelago East position wrong");

        var mistT=mist.GetComponentInChildren<Terrain>(true);
        var west=westRoot.GetComponentInChildren<Terrain>(true);
        var east=eastRoot.GetComponentInChildren<Terrain>(true);
        var mire=ByData(s,"MireOfTheDamnedTerrain");
        var castle=ByData(s,"CastleIronfistTerrain");
        var newSorpigal=ByData(s,"NewSorpigalTerrain");
        var dragonsandSouth=ByData(s,"DragonsandSouth_Terrain");
        if(!mistT||!west||!east||!mire||!castle||!newSorpigal||!dragonsandSouth)
            throw new Exception("Required QA terrain missing");

        var seams=new Dictionary<string,float>{
            {"DragonsandSouth->Misty",Seam(dragonsandSouth,mistT)},
            {"Misty->ArchWest",Seam(mistT,west)},
            {"Misty->Mire",Seam(mistT,mire)},
            {"ArchWest->Ironfist",Seam(west,castle)},
            {"ArchEast->NewSorpigal",Seam(east,newSorpigal)},
            {"ArchWest->ArchEast",Seam(west,east)}
        };
        foreach(var kv in seams)lines.Add("SEAM "+kv.Key+"="+kv.Value.ToString("F6"));
        if(seams.Values.Any(v=>v>.001f))
            throw new Exception("Final south terrain seam exceeds 1 mm: "+string.Join(", ",seams.Select(k=>k.Key+"="+k.Value.ToString("F6"))));

        var suspicious=new List<string>();
        int bad=ElongatedLandComponents(west.terrainData,suspicious)+ElongatedLandComponents(east.terrainData,suspicious);
        lines.Add("ElongatedLandComponents="+bad);
        foreach(var x in suspicious)lines.Add("ELONGATED "+x);
        if(bad>0)throw new Exception("Strip-like Archipelago land artifact remains: "+string.Join("; ",suspicious));

        var waterRenderers=s.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Renderer>(true))
            .Where(rr=>rr.sharedMaterials.Any(m=>m&&m.shader&&m.shader.name=="MMUnity/Linked Natural Water"))
            .ToArray();
        lines.Add("LinkedWaterRenderers="+waterRenderers.Length);
        var waterMats=waterRenderers.SelectMany(rr=>rr.sharedMaterials)
            .Where(m=>m&&m.shader&&m.shader.name=="MMUnity/Linked Natural Water")
            .Select(m=>AssetDatabase.GetAssetPath(m)).Distinct().ToArray();
        lines.Add("LinkedWaterMaterials="+string.Join("|",waterMats));

        var global=Find(s,"Linked connected sea and existing lowland waters 20260929");
        if(!global)throw new Exception("Global connected water missing");
        var mf=global.GetComponent<MeshFilter>();
        if(!mf||!mf.sharedMesh)throw new Exception("Global connected water mesh missing");
        lines.Add("GlobalWaterMesh="+AssetDatabase.GetAssetPath(mf.sharedMesh));
        lines.Add("GlobalWaterVerts="+mf.sharedMesh.vertexCount);
        lines.Add("GlobalWaterTris="+(mf.sharedMesh.triangles.Length/3));
        if(AssetDatabase.GetAssetPath(mf.sharedMesh)!="Assets/World/WorldExtensions/Generated/ArchipelagoWestEast20261001/ArchipelagoConnectedWater.asset")
            throw new Exception("Global water is not using Archipelago patched mesh");

        Capture(s);
        lines.Insert(0,"PASS");
        File.WriteAllLines(OutDir+"/final_qa.txt",lines);
        Debug.Log("SOUTH_LAYOUT_FINAL_QA_PASS");
    }
}

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MMSouthLayoutShift20261003
{
    const string ScenePath="Assets/Scenes/World/Enroth.unity";
    const string GeneratedRoot="Assets/World/WorldExtensions/Generated/SouthLayoutShift20261003";
    const string MistTerrainPath=GeneratedRoot+"/MistyIslandsSouth_Terrain.asset";
    const float Tile=512f;

    static Transform Find(Scene s,string name)
    {
        return s.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t=>t.name==name);
    }

    static Terrain FindTerrain(Scene s,string dataName)
    {
        return s.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))
            .FirstOrDefault(t=>t.terrainData&&t.terrainData.name==dataName);
    }

    static float Smooth01(float x)
    {
        x=Mathf.Clamp01(x);
        return x*x*(3f-2f*x);
    }

    static TerrainData EnsureMireClone(Terrain terrain)
    {
        if(!AssetDatabase.IsValidFolder(GeneratedRoot))
        {
            if(!AssetDatabase.IsValidFolder("Assets/World/WorldExtensions/Generated/SouthLayoutShift20261003"))
                AssetDatabase.CreateFolder("Assets/World/WorldExtensions/Generated","SouthLayoutShift20261003");
        }

        var existing=AssetDatabase.LoadAssetAtPath<TerrainData>(MistTerrainPath);
        if(existing)return existing;

        var clone=UnityEngine.Object.Instantiate(terrain.terrainData);
        clone.name="MistyIslandsSouth_Terrain";
        AssetDatabase.CreateAsset(clone,MistTerrainPath);
        return clone;
    }

    static void BlendNorthEdge(Terrain south,Terrain north,float bandMeters)
    {
        var sd=south.terrainData;
        var nd=north.terrainData;
        int sr=sd.heightmapResolution;
        int nr=nd.heightmapResolution;
        if(sr!=nr)throw new Exception("South-layout seam resolution mismatch");

        var sh=sd.GetHeights(0,0,sr,sr);
        var nh=nd.GetHeights(0,0,nr,nr);
        float metersPerSample=sd.size.z/(sr-1);
        int band=Mathf.Clamp(Mathf.RoundToInt(bandMeters/metersPerSample),1,sr-1);

        for(int x=0;x<sr;x++)
        {
            float southWorld=south.transform.position.y+sh[sr-1,x]*sd.size.y;
            float northWorld=north.transform.position.y+nh[0,x]*nd.size.y;
            float deltaNorm=(northWorld-southWorld)/sd.size.y;

            for(int d=0;d<=band;d++)
            {
                int z=sr-1-d;
                float w=1f-Smooth01(d/(float)band);
                sh[z,x]=Mathf.Clamp01(sh[z,x]+deltaNorm*w);
            }
        }

        sd.SetHeights(0,0,sh);
        EditorUtility.SetDirty(sd);
        south.Flush();
    }

    static void BlendNorthShelf(
        Terrain south,
        Terrain north,
        float shelfWorldY,
        float edgeRelaxMeters,
        float returnMeters)
    {
        var sd=south.terrainData;
        var nd=north.terrainData;
        int sr=sd.heightmapResolution;
        int nr=nd.heightmapResolution;
        if(sr!=nr)throw new Exception("South-layout shelf seam resolution mismatch");

        var sh=sd.GetHeights(0,0,sr,sr);
        var nh=nd.GetHeights(0,0,nr,nr);

        float seamMax=0f;
        for(int x=0;x<sr;x++)
        {
            float sy=south.transform.position.y+sh[sr-1,x]*sd.size.y;
            float ny=north.transform.position.y+nh[0,x]*nd.size.y;
            seamMax=Mathf.Max(seamMax,Mathf.Abs(sy-ny));
        }
        if(seamMax<=.001f)return;

        float metersPerSample=sd.size.z/(sr-1);
        int edgeBand=Mathf.Clamp(Mathf.RoundToInt(edgeRelaxMeters/metersPerSample),1,sr-2);
        int returnBand=Mathf.Clamp(Mathf.RoundToInt(returnMeters/metersPerSample),edgeBand+1,sr-1);

        for(int x=0;x<sr;x++)
        {
            float edgeY=north.transform.position.y+nh[0,x]*nd.size.y;

            for(int d=0;d<=returnBand;d++)
            {
                int z=sr-1-d;
                float originalY=south.transform.position.y+sh[z,x]*sd.size.y;
                float targetY;

                if(d==0)
                {
                    targetY=edgeY;
                }
                else if(d<=edgeBand)
                {
                    targetY=Mathf.Lerp(edgeY,shelfWorldY,Smooth01(d/(float)edgeBand));
                }
                else
                {
                    // Preserve genuine islands/land from the accepted Archipelago.
                    if(originalY>.55f)continue;
                    float u=Smooth01((d-edgeBand)/(float)(returnBand-edgeBand));
                    targetY=Mathf.Lerp(shelfWorldY,originalY,u);
                }

                sh[z,x]=Mathf.Clamp01((targetY-south.transform.position.y)/sd.size.y);
            }
        }

        sd.SetHeights(0,0,sh);
        EditorUtility.SetDirty(sd);
        south.Flush();
    }

    static void BlendWestEdge(Terrain right,Terrain left,float bandMeters)
    {
        var rd=right.terrainData;
        var ld=left.terrainData;
        int rr=rd.heightmapResolution;
        int lr=ld.heightmapResolution;
        if(rr!=lr)throw new Exception("South-layout lateral seam resolution mismatch");

        var rh=rd.GetHeights(0,0,rr,rr);
        var lh=ld.GetHeights(0,0,lr,lr);
        float metersPerSample=rd.size.x/(rr-1);
        int band=Mathf.Clamp(Mathf.RoundToInt(bandMeters/metersPerSample),1,rr-1);

        for(int z=0;z<rr;z++)
        {
            float rightWorld=right.transform.position.y+rh[z,0]*rd.size.y;
            float leftWorld=left.transform.position.y+lh[z,lr-1]*ld.size.y;
            float deltaNorm=(leftWorld-rightWorld)/rd.size.y;

            for(int d=0;d<=band;d++)
            {
                float w=1f-Smooth01(d/(float)band);
                rh[z,d]=Mathf.Clamp01(rh[z,d]+deltaNorm*w);
            }
        }

        rd.SetHeights(0,0,rh);
        EditorUtility.SetDirty(rd);
        right.Flush();
    }

    static void RelinkTerrains(Scene s)
    {
        var ts=s.GetRootGameObjects()
            .SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))
            .Where(t=>Mathf.Abs(t.terrainData.size.x-Tile)<.05f &&
                      Mathf.Abs(t.terrainData.size.z-Tile)<.05f)
            .ToArray();

        foreach(var t in ts)
        {
            // Terrain.SetNeighbors is runtime-only. Persist auto-connect metadata
            // so the corrected world grid reconnects after scene/editor reload.
            t.groupingID=20261003;
            t.allowAutoConnect=true;
            EditorUtility.SetDirty(t);

            var p=t.transform.position;
            var sz=t.terrainData.size;
            Terrain left=null,right=null,top=null,bottom=null;
            foreach(var o in ts)
            {
                if(o==t)continue;
                var q=o.transform.position;
                var os=o.terrainData.size;
                if(Mathf.Abs(q.z-p.z)<.05f&&Mathf.Abs(os.z-sz.z)<.05f)
                {
                    if(Mathf.Abs((q.x+os.x)-p.x)<.05f)left=o;
                    if(Mathf.Abs((p.x+sz.x)-q.x)<.05f)right=o;
                }
                if(Mathf.Abs(q.x-p.x)<.05f&&Mathf.Abs(os.x-sz.x)<.05f)
                {
                    if(Mathf.Abs((p.z+sz.z)-q.z)<.05f)top=o;
                    if(Mathf.Abs((q.z+os.z)-p.z)<.05f)bottom=o;
                }
            }
            t.SetNeighbors(left,top,right,bottom);
            t.Flush();
        }
        Terrain.SetConnectivityDirty();
    }

    [MenuItem("MM/World/South Layout/Shift Archipelago West 20261003")]
    public static void Run()
    {
        var s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);

        var oldMire=Find(s,"Mire of the Damned South - LINKED EXTENSION");
        var mist=Find(s,"Misty Islands South - LINKED EXTENSION");
        var iron=Find(s,"Castle Ironfist South - LINKED EXTENSION");
        var west=Find(s,"Archipelago of the Ancients West - LINKED EXTENSION");
        var east=Find(s,"Archipelago of the Ancients East - LINKED EXTENSION");

        // First run has both old Mire-South and Ironfist-South. Subsequent runs
        // already have former Ironfist-South renamed into the Mire slot.
        if(iron)
        {
            if(oldMire)UnityEngine.Object.DestroyImmediate(oldMire.gameObject);
        }
        else if(mist)
        {
            iron=mist;
            if(oldMire&&oldMire!=mist)UnityEngine.Object.DestroyImmediate(oldMire.gameObject);
        }
        else
        {
            iron=oldMire;
        }

        if(!iron||!west||!east)throw new Exception("Required south-layout roots missing");

        iron.position=new Vector3(0f,iron.position.y,-1024f);
        iron.name="Misty Islands South - LINKED EXTENSION";
        var mireSouth=iron.GetComponentInChildren<Terrain>(true);
        if(!mireSouth)throw new Exception("Former Ironfist-South terrain missing");

        var mireClone=EnsureMireClone(mireSouth);
        mireSouth.terrainData=mireClone;
        var mireCollider=mireSouth.GetComponent<TerrainCollider>();
        if(mireCollider)mireCollider.terrainData=mireClone;
        mireSouth.name="Terrain_MistyIslands_South";

        west.position=new Vector3(512f,west.position.y,-1024f);
        east.position=new Vector3(1024f,east.position.y,-1024f);

        var westTerrain=west.GetComponentInChildren<Terrain>(true);
        var eastTerrain=east.GetComponentInChildren<Terrain>(true);
        var mire=FindTerrain(s,"MireOfTheDamnedTerrain");
        var castle=FindTerrain(s,"CastleIronfistTerrain");
        var newSorpigal=FindTerrain(s,"NewSorpigalTerrain");
        if(!westTerrain||!eastTerrain||!mire||!castle||!newSorpigal)
            throw new Exception("South-layout seam neighbor missing");

        // New adjacency created by the requested west shift.
        // Apply a smooth vertical offset that is exact at the shared edge
        // and decays inward; no unrelated terrain is touched.
        BlendNorthEdge(mireSouth,mire,64f);
        BlendNorthEdge(westTerrain,castle,64f);
        // New Sorpigal's south edge is fully underwater (~-5.5 m). Do not
        // extrude its per-x profile deep into Archipelago East; that creates
        // long underwater bars. Use a short exact edge and shallow shelf.
        BlendNorthShelf(eastTerrain,newSorpigal,-6.2f,12f,128f);

        // Keep the shifted south row laterally exact as well.
        BlendWestEdge(westTerrain,mireSouth,32f);
        BlendWestEdge(eastTerrain,westTerrain,48f);

        var layout=iron.parent;
        if(layout&&layout.name.Contains("14 EXTENSION TILES"))
            layout.name=layout.name.Replace("14 EXTENSION TILES","13 EXTENSION TILES");

        RelinkTerrains(s);
        EditorSceneManager.MarkSceneDirty(s);
        if(!EditorSceneManager.SaveScene(s))throw new IOException("Could not save shifted south layout");
        AssetDatabase.SaveAssets();

        // Rebuild only the global connected-water mesh from its clean baseline
        // for the new shifted Archipelago footprint.
        MMArchipelagoWestEast20261001.PatchCurrentWaterOnly();
        s=SceneManager.GetActiveScene();
        RelinkTerrains(s);
        EditorSceneManager.MarkSceneDirty(s);
        if(!EditorSceneManager.SaveScene(s))throw new IOException("Could not save persistent south-layout terrain connectivity");
        AssetDatabase.SaveAssets();

        Directory.CreateDirectory("Validation/EdgeGrid20260923/SouthLayoutShift20261003");
        File.WriteAllText(
            "Validation/EdgeGrid20260923/SouthLayoutShift20261003/apply.txt",
            "PASS oldSouthFillerRemoved=TRUE formerIronfistSouthRenamedMistyIslandsSouth=TRUE shiftMeters=-512 archipelagoWestCenter=512 archipelagoEastCenter=1024 northSeamsBlended=TRUE waterRebuiltFromCleanBaseline=TRUE\n");

        Debug.Log("SOUTH_LAYOUT_SHIFT_20261003_DONE");
    }
}

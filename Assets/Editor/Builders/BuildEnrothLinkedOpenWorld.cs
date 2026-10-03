using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class BuildEnrothLinkedOpenWorld
{
    const string ScenePath="Assets/Scenes/World/Enroth.unity";
    const float RegionSize=512f;

    sealed class Layout
    {
        public string id,scene,up,down,left,right;
        public int col,row;
    }

    static readonly Layout[] L = new[]
    {
        // Canonical Enroth orientation from the supplied world reference.
        // Unity: -X = west, +X = east, +Z = north.
        // NORTH: Sweet Water | Kriegspire | White Cap | Silver Cove | Eel Infested Waters
        // MIDDLE: Paradise Valley | Blackshire | Free Haven | Bootleg Bay | Misty Islands
        // SOUTH: Hermit's Isle | Dragonsand | Mire of the Damned | Castle Ironfist | New Sorpigal
        new Layout{id="Sweet Water",scene="SweetWater",col=0,row=2,right="Kriegspire",down="Paradise Valley"},
        new Layout{id="Kriegspire",scene="Kriegspire",col=1,row=2,left="Sweet Water",right="Frozen Highlands",down="Blackshire"},
        new Layout{id="Frozen Highlands",scene="FrozenHighlands",col=2,row=2,left="Kriegspire",right="Silver Cove",down="Free Haven"},
        new Layout{id="Silver Cove",scene="SilverCove",col=3,row=2,left="Frozen Highlands",right="Eel Infested Waters",down="Bootleg Bay"},
        new Layout{id="Eel Infested Waters",scene="EelInfestedWaters",col=4,row=2,left="Silver Cove",down="Misty Islands"},

        new Layout{id="Paradise Valley",scene="ParadiseValley",col=0,row=1,right="Blackshire",up="Sweet Water",down="Hermits Isle"},
        new Layout{id="Blackshire",scene="Blackshire",col=1,row=1,left="Paradise Valley",right="Free Haven",up="Kriegspire",down="Dragonsand"},
        new Layout{id="Free Haven",scene="FreeHaven",col=2,row=1,left="Blackshire",right="Bootleg Bay",up="Frozen Highlands",down="Mire of the Damned"},
        new Layout{id="Bootleg Bay",scene="BootlegBay",col=3,row=1,left="Free Haven",right="Misty Islands",up="Silver Cove",down="Castle Ironfist"},
        new Layout{id="Misty Islands",scene="MistyIslands",col=4,row=1,left="Bootleg Bay",up="Eel Infested Waters",down="New Sorpigal"},

        new Layout{id="Hermits Isle",scene="HermitsIsle",col=0,row=0,right="Dragonsand",up="Paradise Valley"},
        new Layout{id="Dragonsand",scene="Dragonsand",col=1,row=0,left="Hermits Isle",right="Mire of the Damned",up="Blackshire"},
        new Layout{id="Mire of the Damned",scene="MireOfTheDamned",col=2,row=0,left="Dragonsand",right="Castle Ironfist",up="Free Haven"},
        new Layout{id="Castle Ironfist",scene="CastleIronfist",col=3,row=0,left="Mire of the Damned",right="New Sorpigal",up="Bootleg Bay"},
        new Layout{id="New Sorpigal",scene="NewSorpigal",col=4,row=0,left="Castle Ironfist",up="Misty Islands"},

        // The original Dragon Isle is preserved as an untouched reference scene.
        // Two new canonical half-tiles reconstruct its exact original land positions.
        new Layout{id="Dragon Isle North",scene="DragonIsle_North",col=-99,row=-99,down="Dragon Isle South"},
        new Layout{id="Dragon Isle South",scene="DragonIsle_South",col=-99,row=-99,up="Dragon Isle North"}
    };

    sealed class VisualEdge
    {
        public string id,scene;
        public int col,row;
    }

    static readonly VisualEdge[] EdgeTiles = new[]
    {
        new VisualEdge{id="Sweet Water North",scene="SweetWater_North",col=0,row=3},
        new VisualEdge{id="Kriegspire North",scene="Kriegspire_North",col=1,row=3},
        new VisualEdge{id="Frozen Highlands North",scene="FrozenHighlands_North",col=2,row=3},
        new VisualEdge{id="Silver Cove North",scene="SilverCove_North",col=3,row=3},
        new VisualEdge{id="Eel Infested North",scene="EelInfestedWaters_North",col=4,row=3},
        new VisualEdge{id="Paradise Valley West",scene="ParadiseValley_West",col=-1,row=1},
        new VisualEdge{id="Hermits Isle West",scene="HermitsIsle_West",col=-1,row=0},
        new VisualEdge{id="Southwest Ocean",scene="SouthwestOcean",col=-1,row=-1},
        new VisualEdge{id="Hermits Isle South",scene="HermitsIsle_South",col=0,row=-1},
        new VisualEdge{id="Dragonsand South",scene="Dragonsand_South",col=1,row=-1},
        // South layout correction 20261003:
        // remove the old south filler, move former Ironfist-South into that slot,
        // rename it Misty Islands South, then move the expanded Archipelago one full tile west.
        new VisualEdge{id="Misty Islands South",scene="CastleIronfist_South",col=2,row=-1},
        new VisualEdge{id="Archipelago of the Ancients",scene="ArchipelagoOfTheAncients",col=3,row=-1},
    };

    static MMRegionWorldStreamer.Region[] Regions()
    {
        return L.Select(r=>new MMRegionWorldStreamer.Region
        {
            id=r.id,sceneName=r.scene,up=r.up,down=r.down,left=r.left,right=r.right,mirrorX=false
        }).ToArray();
    }

    static string SceneAsset(Layout r)
    {
        if(r.id=="New Sorpigal") return "Assets/Scenes/Regions/NewSorpigal.unity";
        if(r.id=="Castle Ironfist") return "Assets/Scenes/Regions/CastleIronfist.unity";
        return "Assets/Scenes/"+r.scene+".unity";
    }

    static bool TryReadExistingDragonTransform(out Vector3 pos,out Quaternion rot,out Vector3 scale)
    {
        pos=new Vector3(-1560f,0f,479f);rot=Quaternion.identity;scale=Vector3.one;
        if(!File.Exists(Path.GetFullPath(ScenePath)))return false;
        var sc=SceneManager.GetSceneByPath(ScenePath);
        if(!sc.IsValid()||!sc.isLoaded)sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var tr=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t=>t.name.StartsWith("Dragon Isle - LINKED",StringComparison.OrdinalIgnoreCase));
        if(!tr)tr=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t=>t.name.StartsWith("Dragon Isle North - LINKED",StringComparison.OrdinalIgnoreCase));
        if(!tr)return false;
        pos=tr.position;rot=tr.rotation;scale=tr.localScale;
        if(tr.name.StartsWith("Dragon Isle North",StringComparison.OrdinalIgnoreCase))
            pos-=rot*Vector3.Scale(new Vector3(0,0,256f),scale);
        return true;
    }

    [MenuItem("MMUnity/Build Linked Enroth Source Grid")]
    public static void Build()
    {
        if (File.Exists("Validation/EdgeGrid20260923/MANUAL_LINKED_WORLD_PROTECTED.txt"))
            throw new InvalidOperationException("Linked world contains newer manual corrections. Full rebuild blocked; use scoped edits preserving the current scene and terrain. See MANUAL_LINKED_WORLD_PROTECTED.txt.");
        Directory.CreateDirectory("Assets/Scenes");
        // Both Dragon Isle tiles occupy the northwest 512m grid; never restore the old
        // overlapping hand-placed transform on rebuild.
        var master=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        master.name="Enroth";
        var root=new GameObject("ENROTH - LINKED SOURCE REGIONS");
        var preview=new GameObject("EDITOR LAYOUT - 15 MM6 REGIONS + DRAGON NORTH/SOUTH + 12 EXTENSION TILES");
        preview.transform.SetParent(root.transform);
        preview.AddComponent<MMEditorPreviewOnly>();

        GameObject persistentPlayer=null;
        Camera persistentCamera=null;
        foreach(var r in L)
        {
            string path=SceneAsset(r);
            if(!File.Exists(Path.GetFullPath(path))) throw new FileNotFoundException("Region scene missing",path);
            var sc=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var rr=FindRegionRoot(sc,r.id);
            bool dragonNorth=r.id=="Dragon Isle North",dragonSouth=r.id=="Dragon Isle South";
            Vector3 pos=(dragonNorth||dragonSouth)
                ? new Vector3(-1536f,0f,dragonNorth?1024f:512f)
                : new Vector3((r.col-2)*RegionSize,0f,(r.row-1)*RegionSize);
            string displayName=r.id=="Frozen Highlands"?"White Cap / Frozen Highlands":r.id;
            CloneRegion(rr,master,preview.transform,pos,displayName+" - LINKED REFERENCE");
            var linked=preview.transform.GetChild(preview.transform.childCount-1);
            // Only the linked display clone gets a softened north cliff toe.
            // The 15 original scene assets and canonical source TerrainData are NEVER edited.
            MMLinkedSourceNorthProfiles.Apply(linked.gameObject,r.id);
            // The 16m ridge-preserving source profile already completes this join.
            // Sweet Water's old WEST beach/seabed was an outer-world cap. It became
            // an internal border once Dragon South / Sweet Water West was added.
            // Disable it only on the linked clone; the canonical standalone stays intact.
            if(r.id=="Sweet Water")DisableDirectionalOuterShore(linked.gameObject,"West");
            bool hasNorthExtension=r.id=="Sweet Water"||r.id=="Kriegspire"||
                r.id=="Frozen Highlands"||r.id=="Silver Cove";
            if(hasNorthExtension)DisableDirectionalOuterShore(linked.gameObject,"North");
            if(dragonNorth||dragonSouth){linked.position=pos;linked.rotation=Quaternion.identity;linked.localScale=Vector3.one;}
            if(r.id=="Castle Ironfist")
                linked.name="Castle Ironfist - LINKED ADJUSTABLE";
            if(r.id=="New Sorpigal" && persistentPlayer==null)
                persistentPlayer=ClonePersistentPlayer(rr,master,root.transform,out persistentCamera);
            EditorSceneManager.CloseScene(sc,true);
        }

        // One-tile-thick visual edge extensions. These are deliberately NOT streamer regions:
        // they complete the world silhouette without changing the existing region travel graph.
        foreach(var e in EdgeTiles)
        {
            string path="Assets/Scenes/"+e.scene+".unity";
            if(!File.Exists(Path.GetFullPath(path))) throw new FileNotFoundException("Edge scene missing",path);
            var sc=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            string sourceId=e.id=="Misty Islands South"?"Castle Ironfist South":e.id;
            var rr=FindRegionRoot(sc,sourceId);
            Vector3 pos=new Vector3((e.col-2)*RegionSize,0f,(e.row-1)*RegionSize);
            CloneRegion(rr,master,preview.transform,pos,e.id+" - LINKED EXTENSION");
            var edgeLinked=preview.transform.GetChild(preview.transform.childCount-1);
            var edgeTerrain=edgeLinked.GetComponentInChildren<Terrain>(true);
            if(edgeTerrain&&e.row==3&&e.col>=0&&e.col<=3)
            {
                var southTerrain=preview.GetComponentsInChildren<Terrain>(true)
                    .FirstOrDefault(t=>t!=edgeTerrain&&
                        Mathf.Abs(t.transform.position.x-edgeTerrain.transform.position.x)<.05f&&
                        Mathf.Abs((t.transform.position.z+t.terrainData.size.z)-edgeTerrain.transform.position.z)<.05f);
                if(southTerrain)
                {
                    edgeTerrain.heightmapPixelError=southTerrain.heightmapPixelError;
                    edgeTerrain.basemapDistance=southTerrain.basemapDistance;
                    edgeTerrain.drawInstanced=southTerrain.drawInstanced;
                    edgeTerrain.shadowCastingMode=southTerrain.shadowCastingMode;
                    edgeTerrain.materialTemplate=southTerrain.materialTemplate;
                    edgeTerrain.allowAutoConnect=southTerrain.allowAutoConnect;
                    edgeTerrain.groupingID=southTerrain.groupingID;
                    edgeTerrain.Flush();
                }
            }
            EditorSceneManager.CloseScene(sc,true);
        }

        MMLinkedSurfaceInputs20260928.Apply(master);
        MMLinkedSnowMaterial20260928.Apply(master);
        MMLinkedFoliageCoverage20260928.Apply(master);
        MMLinkedLeafShading20260928.Apply(master);
        MMLinkedTreeLOD20260928.Apply(master);
        LinkTerrainNeighbors(master);

        if(!persistentPlayer) throw new Exception("Persistent New Sorpigal player missing");

        var streamer=root.AddComponent<MMRegionWorldStreamer>();
        streamer.player=persistentPlayer.transform;
        streamer.regions=Regions();
        streamer.startRegion="New Sorpigal";
        streamer.edge=245f;
        streamer.inset=18f;
        if(persistentCamera)
        {
            persistentCamera.farClipPlane=2600f;
            persistentCamera.tag="MainCamera";
        }

        SetupMasterRenderSettings();
        SetupMasterLight(root.transform);
        MMLinkedRuntimeSetup20260928.Apply(master);
        RenderLayoutPreview(master);
        EditorSceneManager.SetActiveScene(master);
        if(!EditorSceneManager.SaveScene(master,ScenePath))
        {
            const string staging="Assets/Scenes/Enroth_STAGING.unity";
            Debug.LogWarning("LINKED_SAVE_DIRECT_FAILED using staging replacement");
            if(!EditorSceneManager.SaveScene(master,staging)) throw new IOException("Could not save staging linked scene");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            File.Copy(Path.GetFullPath(staging),Path.GetFullPath(ScenePath),true);
            File.Delete(Path.GetFullPath(staging));
            string sm=Path.GetFullPath(staging)+".meta"; if(File.Exists(sm)) File.Delete(sm);
            AssetDatabase.Refresh();
        }
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("LINKED_ENROTH17_DONE DragonNorthSouth=SEPARATE SweetWater=NW EelInfestedWaters=NE HermitsIsle=SW NewSorpigal=SE DragonIsle=SNAPPED_NW_512M localRotations=SOURCE WhiteCap=FrozenHighlands");
    }

    static void LinkTerrainNeighbors(Scene master)
    {
        var ts=master.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true))
            .Where(t=>Mathf.Abs(t.terrainData.size.x-512f)<.05f&&Mathf.Abs(t.terrainData.size.z-512f)<.05f)
            .ToArray();
        int links=0;
        foreach(var t in ts)
        {
            var p=t.transform.position;var sz=t.terrainData.size;
            Terrain left=null,right=null,top=null,bottom=null;
            foreach(var o in ts)
            {
                if(o==t)continue;var q=o.transform.position;var os=o.terrainData.size;
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
            t.SetNeighbors(left,top,right,bottom);t.Flush();
            if(left)links++;if(right)links++;if(top)links++;if(bottom)links++;
        }
        File.WriteAllText("Validation/EdgeGrid20260923/linked_terrain_neighbors_20260926.txt",
            "terrainCount="+ts.Length+" directedNeighborLinks="+links+" expectedDirectedLinks=98\\n");
        if(ts.Length!=30||links!=98)
            throw new Exception("Linked terrain neighbor graph incomplete terrains="+ts.Length+" links="+links);
    }

    static GameObject FindRegionRoot(Scene scene,string label)
    {
        var r=scene.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0);
        if(!r) throw new Exception(label+" root missing");
        return r;
    }
    static void DisableDirectionalOuterShore(GameObject clone,string side)
    {
        var rs=clone.GetComponentsInChildren<Renderer>(true);
        int disabled=0;
        foreach(var r in rs)
        {
            bool outer=false;
            for(var p=r.transform.parent;p!=null;p=p.parent)
                if(p.name=="Outer Ocean + Shore Extension"){outer=true;break;}
            if(!outer)continue;
            if(!r.name.EndsWith(" - "+side,StringComparison.OrdinalIgnoreCase))continue;
            r.gameObject.SetActive(false);disabled++;
        }
        if(disabled==0)Debug.LogWarning("No "+side+" outer-shore renderers found on "+clone.name);
        else Debug.Log("LINKED_INTERNAL_SHORE_DISABLED "+clone.name+" side="+side+" count="+disabled);
    }

    static void CloneRegion(GameObject src,Scene master,Transform parent,Vector3 pos,string name)
    {
        var clone=UnityEngine.Object.Instantiate(src);
        clone.name=name;
        SceneManager.MoveGameObjectToScene(clone,master);
        clone.transform.SetParent(parent,true);
        clone.transform.position=pos;
        // Region positions follow the Enroth mosaic, but each authoritative source-grid scene keeps its own orientation.
        // Do not rotate the individual maps just because their world-grid position changed.
        clone.transform.rotation=Quaternion.identity;
        clone.transform.localScale=Vector3.one;
        foreach(var p in clone.GetComponentsInChildren<MMThirdPersonController>(true)) p.gameObject.SetActive(false);
        foreach(var c in clone.GetComponentsInChildren<Camera>(true)) c.gameObject.SetActive(false);
        foreach(var a in clone.GetComponentsInChildren<AudioListener>(true)) a.enabled=false;
        foreach(var l in clone.GetComponentsInChildren<Light>(true)) l.enabled=false;
        UnifyLinkedWater(clone);
        // Keep the authoritative standalone region visually intact in the linked scene.
        // Global mesh/instancing optimizations already handle performance; do not thin vegetation,
        // disable shadows, or lower terrain quality here.
    }

    static Material ReferenceLinkedWater(){
        const string referencePath="Assets/Materials/Terrain/EnrothReferenceLinkedWater.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(referencePath);
        var shader=Shader.Find("MMUnity/EnrothReferenceBlueWater");
        if(!shader||!shader.isSupported)
            throw new Exception("Reference-linked deep blue water shader missing");
        if(!material){
            var original=AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Materials/Terrain/UnifiedEnrothWater.mat");
            if(!original)throw new Exception("Original water material missing");
            material=new Material(original){name="EnrothReferenceLinkedWater"};
            material.shader=shader;
            AssetDatabase.CreateAsset(material,referencePath);
        }
        if(material.shader!=shader)material.shader=shader;
        material.SetColor("_ShallowColor",new Color(.035f,.095f,.155f,.96f));
        material.SetColor("_MidColor",new Color(.014f,.075f,.125f,.97f));
        material.SetColor("_DeepColor",new Color(.004f,.027f,.075f,.99f));
        material.SetColor("_FoamColor",new Color(.72f,.85f,.95f,.9f));
        material.SetColor("_ReflectionColor",new Color(.11f,.19f,.25f,1f));
        material.SetFloat("_ReflectionStrength",.23f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void UnifyLinkedWater(GameObject clone)
    {
        // Change ONLY linked clones. Original standalone scenes and their
        // shared UnifiedEnrothWater material retain their existing appearance.
        var mat=ReferenceLinkedWater();

        var renderers=clone.GetComponentsInChildren<Renderer>(true);
        var internalWater=renderers.FirstOrDefault(r=>r.name=="Internal Water - Smooth");
        bool useInternal=internalWater && internalWater.enabled && internalWater.gameObject.activeInHierarchy;

        foreach(var r in renderers)
        {
            bool water=r.name.StartsWith("Water -",StringComparison.OrdinalIgnoreCase) ||
                       r.name=="Internal Water - Smooth" ||
                       r.name=="Ocean + Central Lake Water";
            if(!water) continue;

            r.sharedMaterial=mat;

            // Source-grid scenes contain two nearly coincident transparent water surfaces:
            // legacy Water - Smoothed at Y=.12 and final Internal Water - Smooth at Y=.10.
            // Rendering both darkens regions and creates visible colour seams.
            if(useInternal && r.name.StartsWith("Water - Smoothed",StringComparison.OrdinalIgnoreCase))
                r.enabled=false;
        }
    }

    static void SimplifyLinkedClone(GameObject clone)
    {
        var terrain=clone.GetComponentInChildren<Terrain>(true);
        if(terrain)
        {
            terrain.heightmapPixelError=Mathf.Max(4f,terrain.heightmapPixelError);
            terrain.basemapDistance=Mathf.Min(420f,terrain.basemapDistance);
            terrain.detailObjectDistance=Mathf.Min(60f,terrain.detailObjectDistance);
            terrain.treeDistance=Mathf.Min(260f,terrain.treeDistance);
        }
        var roots=new[]{
            clone.transform.Find("Vegetation - Source Anchored Final"),
            clone.transform.Find("Vegetation - Preserved User Additions")
        }.Where(x=>x).ToArray();
        if(!terrain||roots.Length==0)return;
        Vector3 tp=terrain.transform.position,sz=terrain.terrainData.size;
        float cx=tp.x+sz.x*.5f,cz=tp.z+sz.z*.5f;
        int kept=0,removed=0;
        foreach(var vr in roots)
        {
            foreach(Transform c in vr.Cast<Transform>().ToList())
            {
                var rs=c.GetComponentsInChildren<Renderer>(true).Where(q=>q.enabled&&q.gameObject.activeInHierarchy).ToArray();
                if(rs.Length==0){UnityEngine.Object.DestroyImmediate(c.gameObject);removed++;continue;}
                Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);
                bool edge=Mathf.Abs(b.center.x-cx)>sz.x*.5f-34f||Mathf.Abs(b.center.z-cz)>sz.z*.5f-34f;
                int h=c.name.GetHashCode()^Mathf.RoundToInt(b.center.x*7f)^Mathf.RoundToInt(b.center.z*11f);
                if(!edge&&Mathf.Abs(h)%5!=0){UnityEngine.Object.DestroyImmediate(c.gameObject);removed++;continue;}
                foreach(var co in c.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(co);
                foreach(var rr in c.GetComponentsInChildren<Renderer>(true)){rr.shadowCastingMode=ShadowCastingMode.Off;rr.receiveShadows=false;}
                kept++;
            }
        }
        Debug.Log($"LINKED_PROXY {clone.name} vegKept={kept} vegRemoved={removed}");
    }

    static GameObject ClonePersistentPlayer(GameObject nsRoot,Scene master,Transform parent,out Camera cam)
    {
        var src=nsRoot.GetComponentInChildren<MMThirdPersonController>(true);
        if(!src) throw new Exception("New Sorpigal player missing");
        var player=UnityEngine.Object.Instantiate(src.gameObject);
        player.name="Persistent Player - 3of5 Scale";
        SceneManager.MoveGameObjectToScene(player,master);
        player.transform.SetParent(parent,true);
        var ctrl=player.GetComponent<MMThirdPersonController>();
        MMHeroSetup.ReplacePlayerVisual(ctrl);
        ctrl.playerCamera=null;
        cam=null;
        foreach(var stale in player.GetComponentsInChildren<Camera>(true))
            UnityEngine.Object.DestroyImmediate(stale.gameObject);
        Camera sc=src.playerCamera?src.playerCamera:nsRoot.GetComponentInChildren<Camera>(true);
        if(sc)
        {
            var cgo=UnityEngine.Object.Instantiate(sc.gameObject);
            cgo.name="Persistent Player Camera";
            SceneManager.MoveGameObjectToScene(cgo,master);
            cgo.transform.SetParent(parent,true);
            cam=cgo.GetComponent<Camera>();
            cam.gameObject.SetActive(true);
            ctrl.playerCamera=cam;
            var al=cam.GetComponent<AudioListener>();
            if(!al) al=cam.gameObject.AddComponent<AudioListener>();
            al.enabled=true;
        }
        return player;
    }


    static void RenderLayoutPreview(Scene scene)
    {
        Directory.CreateDirectory("Preview");
        var cgo=new GameObject("TEMP - Enroth Layout Camera");
        SceneManager.MoveGameObjectToScene(cgo,scene);
        var cam=cgo.AddComponent<Camera>();
        cam.orthographic=true;
        cam.orthographicSize=1600f;
        cam.transform.position=new Vector3(-256f,2400f,0f);
        cam.transform.rotation=Quaternion.Euler(90f,0f,0f);
        cam.clearFlags=CameraClearFlags.SolidColor;
        cam.backgroundColor=new Color(.14f,.16f,.17f);
        cam.nearClipPlane=.1f; cam.farClipPlane=4000f;
        var lgo=new GameObject("TEMP - Enroth Layout Light");
        SceneManager.MoveGameObjectToScene(lgo,scene);
        var light=lgo.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.1f;
        lgo.transform.rotation=Quaternion.Euler(55f,-35f,0f);
        var rt=new RenderTexture(1400,850,24); cam.targetTexture=rt; cam.Render();
        RenderTexture.active=rt; var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); tex.Apply();
        File.WriteAllBytes("C:/MMUnityPort/Preview/Enroth.png",tex.EncodeToPNG());
        RenderTexture.active=null; cam.targetTexture=null; UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(cgo); UnityEngine.Object.DestroyImmediate(lgo);
        Debug.Log("LINKED_LAYOUT_PREVIEW C:/MMUnityPort/Preview/Enroth.png");
    }


    static void SetupMasterLight(Transform parent)
    {
        var go=new GameObject("ENROTH - Master Sun");
        go.transform.SetParent(parent);
        go.transform.rotation=Quaternion.Euler(52f,-32f,0f);
        var l=go.AddComponent<Light>();
        l.type=LightType.Directional;
        l.intensity=1.05f;
        l.shadows=LightShadows.Soft;
    }

    static void SetupMasterRenderSettings()
    {
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.58f,.71f,.84f);
        RenderSettings.ambientEquatorColor=new Color(.42f,.53f,.44f);
        RenderSettings.ambientGroundColor=new Color(.20f,.23f,.17f);
        RenderSettings.fog=true;
        RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogStartDistance=1450f;
        RenderSettings.fogEndDistance=2500f;
        RenderSettings.fogColor=new Color(.65f,.76f,.82f);
    }

    static void EnsureBuildSettings()
    {
        var wanted=new List<string>{ScenePath};
        wanted.AddRange(L.Select(SceneAsset));
        EditorBuildSettings.scenes=wanted.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
    }
}

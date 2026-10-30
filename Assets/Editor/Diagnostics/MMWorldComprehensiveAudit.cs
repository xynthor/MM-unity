using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMWorldComprehensiveAudit
{
    sealed class Z { public string key,scene; public Z(string k,string s){key=k;scene=s;} }
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),
        new Z("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),
        new Z("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),
        new Z("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),
        new Z("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity"),
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity")};
    const int N=128;
    static string Safe(string s)=>s.Replace(',',';');
    static bool IsWaterProp(string n)
    {
        n=n.ToLowerInvariant();
        return n.Contains("bridge")||n.Contains("pier")||n.Contains("dock")||n.Contains("ship")||
               n.Contains("shp")||n.Contains("buoy")||n.Contains("bouy")||n.Contains("fountain")||n.Contains("well");
    }
    static bool IgnoreArchitectureHelper(string n)
    {
        n=n.ToLowerInvariant();return n.Contains("invistop")||n.Contains("antifly")||n.Contains("trigger")||n.Contains("proxy");
    }
    static bool WaterCompatibleArchitecture(string n)
    {
        n=n.ToLowerInvariant();return IsWaterProp(n)||n.Contains("wall")||n.Contains("wopr")||n.Contains("wopl")||n.Contains("wer");
    }
    static bool IsVegName(string n)
    {
        n=n.ToLowerInvariant();
        return n.Contains("tree")||n.Contains("pine")||n.Contains("shrub")||n.Contains("fern")||
               n.Contains("cactus")||n.Contains("agave")||n.Contains("yucca")||n.Contains("yacca");
    }
    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static bool WaterAt(byte[] tile,byte[] sem,float x,float z)
    {
        int sx=Mathf.Clamp(Mathf.RoundToInt(x/4f+64f),0,N-1),sy=Mathf.Clamp(Mathf.RoundToInt(64f-z/4f),0,N-1);
        return (sem[tile[sy*N+sx]]&1)!=0;
    }
    static bool RoadAt(byte[] tile,byte[] grp,byte[] sem,int sx,int sy)
    {
        byte raw=tile[sy*N+sx],g=grp[raw],f=sem[raw];
        return (f&8)!=0||(g>=8&&g<255);
    }
    static string AssetPath(GameObject go)
    {
        var src=PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;
        return src?AssetDatabase.GetAssetPath(src):"";
    }
    static bool BadTreeAsset(GameObject go)
    {
        string p=AssetPath(go).ToLowerInvariant(),n=go.name.ToLowerInvariant();
        string[] bad={"treehigh001","treehigh002","treehigh003","banyantree","tree_small_02","treesmall02","dead_tree_trunk_02"};
        return bad.Any(x=>p.Contains(x)||n.Contains(x));
    }
    static bool TextureRequired(Material m)
    {
        if(!m)return true;string n=m.name.ToLowerInvariant();
        return !(n.Contains("water")||n.Contains("solidcore")||n.Contains("proxy"));
    }
    static bool PointInAny(List<Bounds> bs,float x,float z)
    {
        foreach(var b in bs)if(x>=b.min.x&&x<=b.max.x&&z>=b.min.z&&z<=b.max.z)return true;
        return false;
    }
    [MenuItem("MMUnity/Audit EVERYTHING - Objects Textures Roads Terrain")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation");
        var details=new List<string>{"zone,category,name,value,detail"};
        var summary=new List<string>{"zone,architectureIssues,vegetationIssues,textureIssues,roadIssues,transitionIssues,coastIssues,mountainIssues,flatTopIssues"};
        foreach(var z in Zones)AuditZone(z,details,summary);
        File.WriteAllLines("Validation/Everything_Audit_Details.csv",details);
        File.WriteAllLines("Validation/Everything_Audit_Summary.csv",summary);
        Debug.Log("EVERYTHING_AUDIT_DONE zones="+Zones.Length+" details="+(details.Count-1));
    }
    static void AuditZone(Z z,List<string> d,List<string> s)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var t=root.GetComponentInChildren<Terrain>(true);if(!t){d.Add($"{z.key},TERRAIN,missing,1,no terrain");return;}
        string dir=$"Assets/World/{z.key}/Data";
        byte[] tile=File.ReadAllBytes(dir+"/tilemap_u8.bin"),grp=File.ReadAllBytes(dir+"/tile_groups_u8.bin"),sem=File.ReadAllBytes(dir+"/tile_semantics_u8.bin");
        int ai=0,vi=0,ti=0,ri=0,xi=0,ci=0,mi=0,fi=0;
        var arch=root.transform.Find("Architecture - MM6 Original Layout Expanded");
        var archBounds=new List<Bounds>();
        if(arch)foreach(Transform c in arch.Cast<Transform>())
        {
            if(!c||c.name.StartsWith("SolidCore_",StringComparison.OrdinalIgnoreCase)||IgnoreArchitectureHelper(c.name))continue;
            Bounds b=BoundsOf(c.gameObject);if(b.size==Vector3.zero)continue;archBounds.Add(b);
            if(b.size.y<.08f)continue; // source floor/slab polygons intentionally bridge underlying terrain/water cells
            if(IsWaterProp(c.name))continue;
            float minGap=999f,maxGap=-999f;int wet=0,samples=0;
            for(int iz=0;iz<5;iz++)for(int ix=0;ix<5;ix++)
            {
                float x=Mathf.Lerp(b.min.x,b.max.x,(ix+.5f)/5f),zz=Mathf.Lerp(b.min.z,b.max.z,(iz+.5f)/5f);
                float y=t.SampleHeight(new Vector3(x,0,zz))+t.transform.position.y,gap=b.min.y-y;
                minGap=Mathf.Min(minGap,gap);maxGap=Mathf.Max(maxGap,gap);if(WaterAt(tile,sem,x,zz))wet++;samples++;
            }
            bool badWet=wet>0&&!WaterCompatibleArchitecture(c.name);
            if(maxGap>.38f||minGap<-.85f||badWet)
            {
                ai++;d.Add($"{z.key},ARCH_CONTACT,{Safe(c.name)},1,minGap={minGap:F2};maxGap={maxGap:F2};water={wet}/{samples}");
            }
        }
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if(!r.enabled||!r.gameObject.activeInHierarchy)continue;string ap=AssetPath(r.gameObject);
            if(BadTreeAsset(r.gameObject)){vi++;d.Add($"{z.key},BAD_VEGETATION_ASSET,{Safe(r.gameObject.name)},1,{Safe(ap)}");}
            foreach(var m in r.sharedMaterials)
            {
                if(!m){ti++;d.Add($"{z.key},NULL_MATERIAL,{Safe(r.gameObject.name)},1,{Safe(ap)}");continue;}
                if(TextureRequired(m)&&m.HasProperty("_MainTex")&&!m.mainTexture){ti++;d.Add($"{z.key},NULL_TEXTURE,{Safe(r.gameObject.name)},1,mat={Safe(m.name)};asset={Safe(ap)}");}
            }
        }
        foreach(var item in PlacedVegetation(root.transform))
        {
            if(!item)continue;Bounds b=BoundsOf(item.gameObject);if(b.size==Vector3.zero)continue;
            float x=b.center.x,zz=b.center.z,y=t.SampleHeight(new Vector3(x,0,zz))+t.transform.position.y;
            bool wet=WaterAt(tile,sem,x,zz)||y<=.22f,road=RoadWorld(tile,grp,sem,x,zz);
            float gap=b.min.y-y,tilt=Vector3.Angle(item.up,Vector3.up);
            if(wet||road||gap>.20f||gap<-.35f||tilt>20f)
            {
                vi++;d.Add($"{z.key},VEGETATION_CONTACT,{Safe(item.name)},1,wet={wet};road={road};gap={gap:F2};tilt={tilt:F1};asset={Safe(AssetPath(item.gameObject))}");
            }
        }
        var td=t.terrainData;var alpha=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);
        int roadLayer=-1;for(int i=0;i<td.terrainLayers.Length;i++)if(td.terrainLayers[i]&&td.terrainLayers[i].name.IndexOf("road",StringComparison.OrdinalIgnoreCase)>=0){roadLayer=i;break;}
        if(roadLayer<0&&td.alphamapLayers>2)roadLayer=2;
        int roadCells=0,roadPaintBad=0,roadGradeBad=0;
        for(int sy=0;sy<N;sy++)for(int sx=0;sx<N;sx++)if(RoadAt(tile,grp,sem,sx,sy))
        {
            roadCells++;float wx=(sx+.5f-64f)*4f,wz=(64f-(sy+.5f))*4f;
            int ax=Mathf.Clamp(Mathf.FloorToInt((wx-t.transform.position.x)/td.size.x*td.alphamapWidth),0,td.alphamapWidth-1);
            int ay=Mathf.Clamp(Mathf.FloorToInt((wz-t.transform.position.z)/td.size.z*td.alphamapHeight),0,td.alphamapHeight-1);
            if(roadLayer>=0&&roadLayer<td.alphamapLayers&&alpha[ay,ax,roadLayer]<.35f)roadPaintBad++;
            float h=t.SampleHeight(new Vector3(wx,0,wz))+t.transform.position.y;
            float maxDh=0f;int[,] q={{1,0},{-1,0},{0,1},{0,-1}};
            for(int k=0;k<4;k++){int nx=sx+q[k,0],ny=sy+q[k,1];if(nx<0||ny<0||nx>=N||ny>=N||!RoadAt(tile,grp,sem,nx,ny))continue;float nh=t.SampleHeight(new Vector3((nx+.5f-64f)*4f,0,(64f-(ny+.5f))*4f))+t.transform.position.y;maxDh=Mathf.Max(maxDh,Mathf.Abs(nh-h));}
            if(maxDh>2.0f)roadGradeBad++;
        }
        if(roadPaintBad>0){ri++;d.Add($"{z.key},ROAD_PAINT,sourceRoads,{roadPaintBad},badOf={roadCells}");}
        if(roadGradeBad>0){ri++;d.Add($"{z.key},ROAD_GRADE,sourceRoads,{roadGradeBad},neighborHeightDeltaGt2m");}
        int hardSeams=0;
        for(int y=0;y<td.alphamapHeight-1;y+=2)for(int x=0;x<td.alphamapWidth-1;x+=2)
        {
            float dx=0f,dz=0f;for(int k=0;k<td.alphamapLayers;k++){dx+=Mathf.Abs(alpha[y,x,k]-alpha[y,x+1,k]);dz+=Mathf.Abs(alpha[y,x,k]-alpha[y+1,x,k]);}
            if(dx>1.55f)hardSeams++;if(dz>1.55f)hardSeams++;
        }
        if(hardSeams>50){xi++;d.Add($"{z.key},HARD_TERRAIN_TRANSITIONS,alphamap,{hardSeams},L1jumpGt1.55 sampledEvery2px");}
        bool needsInternal=true;
        if(needsInternal&&!root.transform.Find("Internal Water - Smooth")){ci++;d.Add($"{z.key},COAST_INTERNAL,missing,1,Internal Water - Smooth missing");}
        bool perimeter=z.key=="SweetWater"||z.key=="Kriegspire"||z.key=="FrozenHighlands"||z.key=="SilverCove"||z.key=="EelInfestedWaters"||
                       z.key=="HermitsIsle"||z.key=="Dragonsand"||z.key=="MireOfTheDamned"||z.key=="CastleIronfist"||z.key=="NewSorpigal"||
                       z.key=="ParadiseValley"||z.key=="MistyIslands";
        if(perimeter&&!root.transform.Find("Outer Ocean + Shore Extension")){ci++;d.Add($"{z.key},COAST_OUTER,missing,1,outer curved coast missing");}
        var tf=root.transform.Find("Terrain Forms - Final");
        if(tf||root.GetComponentsInChildren<Transform>(true).Any(q=>q.name.StartsWith("SnowMountain_")||q.name.StartsWith("DesertMountain_")))
        {mi++;d.Add($"{z.key},FORBIDDEN_SQUARE_MOUNTAIN,terrainForms,1,square-base insert still present");}
        int flatHigh=0;
        for(int sy=1;sy<N-1;sy++)for(int sx=1;sx<N-1;sx++)
        {
            byte raw=tile[sy*N+sx];if((sem[raw]&9)!=0)continue;
            float x=(sx+.5f-64f)*4f,zz=(64f-(sy+.5f))*4f;
            if(PointInAny(archBounds,x,zz))continue;
            float h=t.SampleHeight(new Vector3(x,0,zz))+t.transform.position.y;if(h<8f)continue;
            float spread=0f;
            for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)if(ox!=0||oy!=0)
            {
                float nh=t.SampleHeight(new Vector3(x+ox*4f,0,zz-oy*4f))+t.transform.position.y;spread=Mathf.Max(spread,Mathf.Abs(nh-h));
            }
            if(spread<.06f)flatHigh++;
        }
        if(flatHigh>12){fi++;d.Add($"{z.key},FLAT_HIGH_TERRAIN,terrain,{flatHigh},elevationGt8m and 8-neighbor spreadLt0.06m");}
        s.Add($"{z.key},{ai},{vi},{ti},{ri},{xi},{ci},{mi},{fi}");
        Debug.Log($"EVERYTHING_AUDIT {z.key} arch={ai} veg={vi} tex={ti} road={ri} transition={xi} coast={ci} mountain={mi} flat={fi}");
    }
    static bool RoadWorld(byte[] tile,byte[] grp,byte[] sem,float x,float z)
    {
        int sx=Mathf.Clamp(Mathf.RoundToInt(x/4f+64f),0,N-1),sy=Mathf.Clamp(Mathf.RoundToInt(64f-z/4f),0,N-1);
        return RoadAt(tile,grp,sem,sx,sy);
    }
    static IEnumerable<Transform> PlacedVegetation(Transform root)
    {
        var outList=new List<Transform>();
        string[] names={"Vegetation - Source Anchored Final","Biome Vegetation - Source Grid","Vegetation - MM Anchors + Natural Groves"};
        foreach(string n in names)
        {
            var r=root.Find(n);if(!r)continue;
            foreach(Transform c in r.Cast<Transform>())
            {
                if(c.name=="Trees"||c.name=="Understory")
                {
                    foreach(Transform q in c.Cast<Transform>())
                        if(q.GetComponentsInChildren<Renderer>(true).Length>0)outList.Add(q);
                }
                else if(c.GetComponentsInChildren<Renderer>(true).Length>0)outList.Add(c);
            }
        }
        var p=root.Find("Vegetation - Preserved User Additions");
        if(p)foreach(Transform g in p.Cast<Transform>())foreach(Transform q in g.Cast<Transform>())if(q.GetComponentsInChildren<Renderer>(true).Length>0)outList.Add(q);
        return outList.Distinct();
    }
}

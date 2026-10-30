using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class AuditWorldSourceExact
{
    sealed class Zone { public string key,scene; public Zone(string k,string s){key=k;scene=s;} }
    sealed class P { public string file,kind; public float cx,cz,baseOffset; }
    static readonly Zone[] Z={
        new Zone("NewSorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),
        new Zone("CastleIronfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Zone("MireOfTheDamned","Assets/Scenes/Regions/MireOfTheDamned.unity"),
        new Zone("Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Zone("HermitsIsle","Assets/Scenes/Regions/HermitsIsle.unity"),
        new Zone("MistyIslands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Zone("BootlegBay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Zone("FreeHaven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Zone("Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Zone("ParadiseValley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Zone("EelInfestedWaters","Assets/Scenes/Regions/EelInfestedWaters.unity"),
        new Zone("SilverCove","Assets/Scenes/Regions/SilverCove.unity"),
        new Zone("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Zone("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity"),
        new Zone("SweetWater","Assets/Scenes/Regions/SweetWater.unity")
    };
    const int N=128;
    static float F(string s){return float.Parse(s,CultureInfo.InvariantCulture);}
    static List<P> ReadPlacements(Zone z)
    {
        string p=$"Assets/World/{z.key}/Data/model_placement_audit.csv";
        var r=new List<P>(); foreach(string line in File.ReadAllLines(p).Skip(1))
        {
            var q=line.Split(','); if(q.Length<12)continue;
            r.Add(new P{file=q[0],kind=q[1],cx=F(q[2]),cz=F(q[3]),baseOffset=F(q[11])});
        }
        return r;
    }
    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true); if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static bool IsHouse(string n)
    {
        n=n.ToLowerInvariant();
        if(n.Contains("sign")||n.Contains("sgn")) return false;
        return n.Contains("house")||n.Contains("hse")||n.Contains("tav")||n.Contains("inn")||n.Contains("shop")||n.Contains("bank")||
               n.Contains("smith")||n.Contains("blacksm")||n.Contains("magic")||n.Contains("merc")||n.Contains("armory")||n.Contains("town")||n.Contains("training")||
               n.Contains("guild")||n.Contains("temple")||n.Contains("stable")||n.Contains("stbl")||n.Contains("stor")||n.Contains("luck")||n.Contains("keep")||n.Contains("d18")||n.Contains("thiev");
    }
    static bool ShouldHaveCore(string n,Bounds b)
    {
        return IsHouse(n) && b.size.x>=1.8f && b.size.z>=1.8f && b.size.x<=60f && b.size.z<=60f && b.size.y>=1.8f && b.size.y<=45f;
    }
    static int ExpectedLayer(byte g,byte f)
    {
        if((f&8)!=0 || (g>=8&&g<255))return 7;
        if((f&1)!=0)return 8;
        if((f&2)!=0 || g==5)return 2;
        if(g==0)return 0;if(g==1)return 1;if(g==2)return 2;if(g==3)return 3;if(g==4)return 4;if(g==6)return 5;if(g==7)return 6;
        return 4;
    }
    static bool RawFusedWall(string n)
    {
        n=n.ToLowerInvariant();
        // Ignore obsolete mirrored split-wall exports; audit the authoritative raw wall objects.
        return n.Contains("__seg_")&&(n.Contains("_wopr")||n.Contains("_wopl")||n.Contains("_wer"));
    }
    static string Q(string s){return "\""+s.Replace("\"","\"\"")+"\"";}
    public static void Run()
    {
        string dir="Validation/SourceExactAudit";Directory.CreateDirectory(dir);
        var sum=new List<string>{"zone,source_rows,scene_originals,missing,extra,xz_bad,scale_bad,y_bad,houses,houses_without_core,tile_mismatch,height_max_abs,height_mean_abs"};
        foreach(var z in Z) AuditZone(z,dir,sum);
        File.WriteAllLines(Path.Combine(dir,"summary.csv"),sum);
        Debug.Log("SOURCE_EXACT_AUDIT_DONE zones="+Z.Length+" summary="+Path.Combine(dir,"summary.csv"));
    }
    static void AuditZone(Zone z,string dir,List<string> sum)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var terrain=root.GetComponentInChildren<Terrain>(true);if(!terrain)throw new Exception(z.key+" terrain missing");
        var arch=root.transform.Find("Architecture - MM6 Original Layout Expanded");if(!arch)throw new Exception(z.key+" architecture missing");
        var placements=ReadPlacements(z);var pBy=placements.ToDictionary(p=>Path.GetFileNameWithoutExtension(p.file),StringComparer.OrdinalIgnoreCase);
        var original=arch.Cast<Transform>().Where(t=>!t.name.StartsWith("SolidCore_",StringComparison.OrdinalIgnoreCase)&&!t.name.StartsWith("HouseMass_",StringComparison.OrdinalIgnoreCase)&&!t.name.StartsWith("HouseRoof_",StringComparison.OrdinalIgnoreCase)).ToList();
        var sceneBy=original.ToDictionary(t=>t.name,StringComparer.OrdinalIgnoreCase);
        var detail=new List<string>{"name,kind,expected_cx,expected_cz,scene_cx,scene_cz,dx,dz,scale_x,scale_y,scale_z,current_min_y,target_min_y,dy,is_house,has_core,status"};
        int missing=0,extra=0,xzBad=0,scaleBad=0,yBad=0,houses=0,noCore=0;
        foreach(var p in placements)
        {
            string n=Path.GetFileNameWithoutExtension(p.file); if(RawFusedWall(n))continue;
            if(!sceneBy.TryGetValue(n,out var t)){missing++;detail.Add(Q(n)+","+p.kind+","+p.cx.ToString("F4",CultureInfo.InvariantCulture)+","+p.cz.ToString("F4",CultureInfo.InvariantCulture)+",,,,,,,,,,,,MISSING");continue;}
            Bounds b=BoundsOf(t.gameObject);float dx=b.center.x-p.cx,dz=b.center.z-p.cz;
            Vector3 s=t.lossyScale;bool sb=Mathf.Abs(s.x-1f)>.001f||Mathf.Abs(s.y-1f)>.001f||Mathf.Abs(s.z-1f)>.001f;
            float surface=p.kind=="LAND"?terrain.SampleHeight(new Vector3(b.center.x,0,b.center.z))+terrain.transform.position.y:.12f;
            float target=surface+p.baseOffset,dy=b.min.y-target;bool xb=Mathf.Abs(dx)>.04f||Mathf.Abs(dz)>.04f;bool specialY=n.ToLowerInvariant().Contains("bridge")||n.ToLowerInvariant().Contains("trigger");bool yb=!specialY&&Mathf.Abs(dy)>.65f;
            bool house=ShouldHaveCore(n,b);bool core=arch.Find("SolidCore_"+n)!=null;if(house){houses++;if(!core)noCore++;}
            if(xb)xzBad++;if(sb)scaleBad++;if(yb)yBad++;
            string status=xb||sb||yb?"CHECK":"OK";
            detail.Add(string.Join(",",new[]{Q(n),p.kind,p.cx.ToString("F4",CultureInfo.InvariantCulture),p.cz.ToString("F4",CultureInfo.InvariantCulture),b.center.x.ToString("F4",CultureInfo.InvariantCulture),b.center.z.ToString("F4",CultureInfo.InvariantCulture),dx.ToString("F4",CultureInfo.InvariantCulture),dz.ToString("F4",CultureInfo.InvariantCulture),s.x.ToString("F4",CultureInfo.InvariantCulture),s.y.ToString("F4",CultureInfo.InvariantCulture),s.z.ToString("F4",CultureInfo.InvariantCulture),b.min.y.ToString("F4",CultureInfo.InvariantCulture),target.ToString("F4",CultureInfo.InvariantCulture),dy.ToString("F4",CultureInfo.InvariantCulture),house?"1":"0",core?"1":"0",status}));
        }
        foreach(var t in original) if(!pBy.ContainsKey(t.name)){extra++;detail.Add(Q(t.name)+",EXTRA,,,,,,,,,,,,,,,EXTRA");}
        File.WriteAllLines(Path.Combine(dir,z.key+"_buildings.csv"),detail);

        string data=$"Assets/World/{z.key}/Data";byte[] tile=File.ReadAllBytes(data+"/tilemap_u8.bin");byte[] group=File.ReadAllBytes(data+"/tile_groups_u8.bin");byte[] sem=File.ReadAllBytes(data+"/tile_semantics_u8.bin");byte[] height=File.ReadAllBytes(data+"/heightmap_u8.bin");
        var td=terrain.terrainData;var a=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);int tileMismatch=0;double hsum=0;float hmax=0;int hn=0;
        var tilesOut=new List<string>{"sx,sy,tile,group,sem,expected_layer,actual_layer,expected_weight,actual_weight,height_source,height_scene,height_diff,status"};
        for(int sy=0;sy<N;sy++)for(int sx=0;sx<N;sx++)
        {
            byte raw=tile[sy*N+sx],g=group[raw],f=sem[raw];int exp=ExpectedLayer(g,f);
            int ax=Mathf.Clamp(sx*4+2,0,td.alphamapWidth-1),ay=Mathf.Clamp((N-1-sy)*4+2,0,td.alphamapHeight-1);int act=0;float best=-1f;
            for(int k=0;k<a.GetLength(2);k++)if(a[ay,ax,k]>best){best=a[ay,ax,k];act=k;}
            float ew=((f&1)!=0&&exp==8)?.62f:1f; bool expAvailable=exp>=0 && exp<a.GetLength(2); bool tm=!expAvailable || act!=exp || Mathf.Abs(a[ay,ax,exp]-ew)>.08f; if(tm)tileMismatch++;
            float wx=(sx-64f)*4f,wz=(64f-sy)*4f;float hs=height[sy*N+sx]*.25f;float hc=terrain.SampleHeight(new Vector3(wx,0,wz))+terrain.transform.position.y;float hd=hc-hs;
            if((f&1)==0){hsum+=Mathf.Abs(hd);hmax=Mathf.Max(hmax,Mathf.Abs(hd));hn++;}
            if(tm||Mathf.Abs(hd)>2.5f)tilesOut.Add(string.Join(",",new[]{sx.ToString(),sy.ToString(),raw.ToString(),g.ToString(),f.ToString(),exp.ToString(),act.ToString(),ew.ToString("F2",CultureInfo.InvariantCulture),(expAvailable?a[ay,ax,exp]:0f).ToString("F2",CultureInfo.InvariantCulture),hs.ToString("F3",CultureInfo.InvariantCulture),hc.ToString("F3",CultureInfo.InvariantCulture),hd.ToString("F3",CultureInfo.InvariantCulture),tm?"TILE_MISMATCH":"HEIGHT_CHECK"}));
        }
        File.WriteAllLines(Path.Combine(dir,z.key+"_tiles.csv"),tilesOut);
        double hmean=hn>0?hsum/hn:0;
        sum.Add(string.Join(",",new[]{z.key,placements.Count.ToString(),original.Count.ToString(),missing.ToString(),extra.ToString(),xzBad.ToString(),scaleBad.ToString(),yBad.ToString(),houses.ToString(),noCore.ToString(),tileMismatch.ToString(),hmax.ToString("F3",CultureInfo.InvariantCulture),hmean.ToString("F3",CultureInfo.InvariantCulture)}));
        Debug.Log($"AUDIT_ZONE {z.key} models={original.Count} missing={missing} extra={extra} xzBad={xzBad} scaleBad={scaleBad} yBad={yBad} houses={houses} noCore={noCore} tileMismatch={tileMismatch} hmax={hmax:F3} hmean={hmean:F3}");
    }
}


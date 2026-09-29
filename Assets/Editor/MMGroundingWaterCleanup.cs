using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMGroundingWaterCleanup
{
    sealed class Z { public string key,scene; public Z(string k,string s){key=k;scene=s;} }
    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity"),
        new Z("CastleIronfist","Assets/Scenes/CastleIronfist_SourceGrid.unity"),
        new Z("MireOfTheDamned","Assets/Scenes/MireOfTheDamned_SourceGrid.unity"),
        new Z("Dragonsand","Assets/Scenes/Dragonsand_SourceGrid.unity"),
        new Z("HermitsIsle","Assets/Scenes/HermitsIsle_SourceGrid.unity"),
        new Z("MistyIslands","Assets/Scenes/MistyIslands_SourceGrid.unity"),
        new Z("BootlegBay","Assets/Scenes/BootlegBay_SourceGrid.unity"),
        new Z("FreeHaven","Assets/Scenes/FreeHaven_SourceGrid.unity"),
        new Z("Blackshire","Assets/Scenes/Blackshire_SourceGrid.unity"),
        new Z("ParadiseValley","Assets/Scenes/ParadiseValley_SourceGrid.unity"),
        new Z("EelInfestedWaters","Assets/Scenes/EelInfestedWaters_SourceGrid.unity"),
        new Z("SilverCove","Assets/Scenes/SilverCove_SourceGrid.unity"),
        new Z("FrozenHighlands","Assets/Scenes/FrozenHighlands_SourceGrid.unity"),
        new Z("Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity"),
        new Z("SweetWater","Assets/Scenes/SweetWater_SourceGrid.unity")};
    [MenuItem("MMUnity/Ground Buildings And Remove Water Vegetation")]
    public static void ApplyAll()
    {
        foreach(var z in Zones) Apply(z);
        AssetDatabase.SaveAssets();
        Debug.Log("GROUND_WATER_CLEANUP_ALL_DONE zones="+Zones.Length);
    }

    static void Apply(Z z)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0) ?? sc.GetRootGameObjects().First();
        var t=root.GetComponentInChildren<Terrain>(true); if(!t)throw new Exception(z.key+" terrain missing"); var tf=root.transform.Find("Terrain Forms - Final"); if(tf)UnityEngine.Object.DestroyImmediate(tf.gameObject);
        string d=$"Assets/World/{z.key}/Data";
        byte[] tile=File.ReadAllBytes(d+"/tilemap_u8.bin"),grp=File.ReadAllBytes(d+"/tile_groups_u8.bin"),sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");
        int platforms=PlatformArchitecture(root.transform,t);
        int moved=GroundArchitecture(root.transform,t);
        int bridgeFixes=GroundBridgeSupports(root.transform,t);
        int hidden=HideCores(root.transform);
        int removed=CullAndGroundVegetation(root.transform,t,tile,grp,sem);
        EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc,z.scene);
        Debug.Log($"GROUND_WATER_CLEANUP {z.key} platforms={platforms} moved={moved} bridgeFixes={bridgeFixes} hiddenCores={hidden} removedUnsafeVegetation={removed}");
    }

    static Bounds B(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static bool Skip(string n)
    {
        n=n.ToLowerInvariant();
        return n.Contains("bridge")||n.Contains("pier")||n.Contains("dock")||n.Contains("ship")||n.Contains("shp")||
               n.Contains("fountain")||n.Contains("well")||n.Contains("buoy")||n.Contains("bouy")||n.Contains("antifly")||n.Contains("invistop")||n.Contains("trigger");
    }
    static bool BuildingOrWall(string n)
    {
        n=n.ToLowerInvariant();
        if(n.Contains("sign")||n.Contains("sgn")||n.Contains("tree")||n.Contains("rock"))return false;
        return n.Contains("house")||n.Contains("hse")||n.Contains("tav")||n.Contains("inn")||n.Contains("shop")||n.Contains("bank")||
               n.Contains("smith")||n.Contains("magic")||n.Contains("merc")||n.Contains("armory")||n.Contains("town")||n.Contains("training")||
               n.Contains("guild")||n.Contains("temple")||n.Contains("stable")||n.Contains("stbl")||n.Contains("stor")||n.Contains("luck")||
               n.Contains("keep")||n.Contains("d18")||n.Contains("thiev")||n.Contains("wall")||n.Contains("wopr")||n.Contains("wopl")||n.Contains("wer")||
               n.Contains("_d0")||n.Contains("_d1")||n.Contains("_d2")||n.Contains("dunent")||n.Contains("t05ent")||n.Contains("drudi")||n.Contains("c4")||n.Contains("c5")||n.Contains("_m042__1");
    }
    static List<float> DrySamples(Terrain t,Bounds b,int n,float expand)
    {
        var v=new List<float>();float minx=b.min.x-expand,maxx=b.max.x+expand,minz=b.min.z-expand,maxz=b.max.z+expand;
        for(int iz=0;iz<n;iz++)for(int ix=0;ix<n;ix++)
        {
            float x=Mathf.Lerp(minx,maxx,(ix+.5f)/n),z=Mathf.Lerp(minz,maxz,(iz+.5f)/n);
            float y=t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;if(y>.20f)v.Add(y);
        }
        return v;
    }
    static float Median(List<float> v){v.Sort();int m=v.Count/2;return (v.Count&1)==1?v[m]:(v[m-1]+v[m])*.5f;}
    static int PlatformArchitecture(Transform root,Terrain t)
    {
        var arch=root.Find("Architecture - MM6 Original Layout Expanded");if(!arch)return 0;
        var td=t.terrainData;int r=td.heightmapResolution;var hm=td.GetHeights(0,0,r,r);Vector3 tp=t.transform.position,sz=td.size;int done=0;
        foreach(Transform c in arch.Cast<Transform>().ToList())
        {
            if(!c||c.name.StartsWith("SolidCore_",StringComparison.OrdinalIgnoreCase)||Skip(c.name)||!BuildingOrWall(c.name))continue;
            Bounds b=B(c.gameObject);if(b.size==Vector3.zero||b.size.x<1.2f||b.size.z<1.2f||b.size.x>140f||b.size.z>140f)continue;
            var s=DrySamples(t,b,5,0f);if(s.Count<3)s=DrySamples(t,b,7,4f);if(s.Count==0)continue;
            float target=Mathf.Max(.32f,Median(s)),feather=1.8f;
            float minx=b.min.x-feather,maxx=b.max.x+feather,minz=b.min.z-feather,maxz=b.max.z+feather;
            int x0=Mathf.Clamp(Mathf.FloorToInt((minx-tp.x)/sz.x*(r-1)),0,r-1),x1=Mathf.Clamp(Mathf.CeilToInt((maxx-tp.x)/sz.x*(r-1)),0,r-1);
            int z0=Mathf.Clamp(Mathf.FloorToInt((minz-tp.z)/sz.z*(r-1)),0,r-1),z1=Mathf.Clamp(Mathf.CeilToInt((maxz-tp.z)/sz.z*(r-1)),0,r-1);
            for(int iz=z0;iz<=z1;iz++)for(int ix=x0;ix<=x1;ix++)
            {
                float wx=tp.x+ix/(float)(r-1)*sz.x,wz=tp.z+iz/(float)(r-1)*sz.z;
                float dx=wx<b.min.x?b.min.x-wx:(wx>b.max.x?wx-b.max.x:0f),dz=wz<b.min.z?b.min.z-wz:(wz>b.max.z?wz-b.max.z:0f);
                float dist=Mathf.Sqrt(dx*dx+dz*dz),w=1f-Mathf.SmoothStep(0f,1f,dist/feather);if(w<=0)continue;
                float cur=tp.y+hm[iz,ix]*sz.y,ny=Mathf.Lerp(cur,target,w);hm[iz,ix]=Mathf.Clamp01((ny-tp.y)/sz.y);
            }
            done++;
        }
        td.SetHeights(0,0,hm);t.Flush();EditorUtility.SetDirty(td);return done;
    }
    static int GroundArchitecture(Transform root,Terrain t)
    {
        var arch=root.Find("Architecture - MM6 Original Layout Expanded");if(!arch)return 0;int moved=0;
        foreach(Transform c in arch.Cast<Transform>().ToList())
        {
            if(!c||c.name.StartsWith("SolidCore_",StringComparison.OrdinalIgnoreCase)||Skip(c.name))continue;
            Bounds b=B(c.gameObject);if(b.size==Vector3.zero||b.size.y<.2f)continue;
            var s=DrySamples(t,b,BuildingOrWall(c.name)?5:3,0f);if(s.Count==0)s=DrySamples(t,b,5,3f);if(s.Count==0)continue;
            float support=Median(s),dy=(support-.05f)-b.min.y;
            if(Mathf.Abs(dy)>.02f){c.position+=Vector3.up*dy;moved++;}
        }
        return moved;
    }
    static List<Vector3> BridgeEndPoints(Transform obj,Bounds b,bool xAxis,bool high)
    {
        var pts=new List<Vector3>();float edge=xAxis?(high?b.max.x:b.min.x):(high?b.max.z:b.min.z);
        float band=Mathf.Max(.75f,(xAxis?b.size.x:b.size.z)*.16f);
        foreach(var mf in obj.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!mf.sharedMesh)continue;
            foreach(var v in mf.sharedMesh.vertices)
            {
                Vector3 w=mf.transform.TransformPoint(v),q=w;float a=xAxis?w.x:w.z;
                if(Mathf.Abs(a-edge)<=band)pts.Add(q);
            }
        }
        if(pts.Count==0){Vector3 q=b.center;if(xAxis)q.x=edge;else q.z=edge;pts.Add(q);}
        int keep=Mathf.Max(1,pts.Count/8);return pts.OrderBy(q=>q.y).Take(keep).ToList();
    }
    static float BridgeEndGap(Transform obj,Terrain t,Bounds b,bool xAxis,bool high)
    {
        var pts=BridgeEndPoints(obj,b,xAxis,high);float sum=0f;
        foreach(var q in pts)sum+=q.y-(t.SampleHeight(q)+t.transform.position.y);
        return sum/Mathf.Max(1,pts.Count);
    }
    static void RaiseBridgeBank(Terrain t,Bounds b,bool xAxis,bool high,float gap)
    {
        if(gap<=.55f)return;var td=t.terrainData;int r=td.heightmapResolution;var hm=td.GetHeights(0,0,r,r);
        Vector3 tp=t.transform.position,sz=td.size;float edge=xAxis?(high?b.max.x:b.min.x):(high?b.max.z:b.min.z);
        float cross=xAxis?b.center.z:b.center.x,crossR=Mathf.Clamp((xAxis?b.extents.z:b.extents.x)+1.2f,2.5f,8f),alongR=3.5f;
        Vector3 cp=b.center;if(xAxis)cp.x=edge;else cp.z=edge;
        float target=t.SampleHeight(cp)+tp.y+Mathf.Min(gap,4f);
        for(int iz=0;iz<r;iz++)for(int ix=0;ix<r;ix++)
        {
            float wx=tp.x+ix/(float)(r-1)*sz.x,wz=tp.z+iz/(float)(r-1)*sz.z;
            float along=Mathf.Abs((xAxis?wx:wz)-edge)/alongR,crossD=Mathf.Abs((xAxis?wz:wx)-cross)/crossR;
            float d=Mathf.Sqrt(along*along+crossD*crossD);if(d>=1f)continue;
            float w=1f-Mathf.SmoothStep(0f,1f,d),cur=tp.y+hm[iz,ix]*sz.y;
            float ny=Mathf.Lerp(cur,Mathf.Max(cur,target),w);hm[iz,ix]=Mathf.Clamp01((ny-tp.y)/sz.y);
        }
        td.SetHeights(0,0,hm);t.Flush();EditorUtility.SetDirty(td);
    }
    static int GroundBridgeSupports(Transform root,Terrain t)
    {
        var arch=root.Find("Architecture - MM6 Original Layout Expanded");if(!arch)return 0;int fixedN=0;
        foreach(Transform c in arch.Cast<Transform>().ToList())
        {
            string n=c.name.ToLowerInvariant();if(!n.Contains("bridge")||n.Contains("trigger"))continue;
            Bounds b=B(c.gameObject);if(b.size==Vector3.zero)continue;bool xAxis=b.size.x>=b.size.z;
            float a=BridgeEndGap(c,t,b,xAxis,false),d=BridgeEndGap(c,t,b,xAxis,true);
            bool bothHigh=a>.55f&&d>.55f,bothLow=a<-.80f&&d<-.80f;
            if((bothHigh||bothLow)&&Mathf.Abs(a-d)<1f)
            {
                float dy=-(a+d)*.5f;c.position+=Vector3.up*dy;fixedN++;b=B(c.gameObject);
                a=BridgeEndGap(c,t,b,xAxis,false);d=BridgeEndGap(c,t,b,xAxis,true);
            }
            if(a>.55f){RaiseBridgeBank(t,b,xAxis,false,a);fixedN++;}
            if(d>.55f){RaiseBridgeBank(t,b,xAxis,true,d);fixedN++;}
        }
        return fixedN;
    }

    static int HideCores(Transform root)
    {
        var arch=root.Find("Architecture - MM6 Original Layout Expanded");if(!arch)return 0;int n=0;
        foreach(Transform c in arch.Cast<Transform>())if(c.name.StartsWith("SolidCore_",StringComparison.OrdinalIgnoreCase))
        {
            var r=c.GetComponent<MeshRenderer>();if(r&&r.enabled){r.enabled=false;n++;}
        }
        return n;
    }
    static bool WaterAt(byte[] tile,byte[] sem,float x,float z)
    {
        int sx=Mathf.Clamp(Mathf.RoundToInt(x/4f+64f),0,127),sy=Mathf.Clamp(Mathf.RoundToInt(64f-z/4f),0,127);
        return (sem[tile[sy*128+sx]]&1)!=0;
    }
    static bool DrySpot(Terrain t,byte[] tile,byte[] sem,Bounds b)
    {
        float r=Mathf.Clamp(Mathf.Max(b.extents.x,b.extents.z)*.35f,.35f,1.25f);
        Vector2[] o={Vector2.zero,new Vector2(r,0),new Vector2(-r,0),new Vector2(0,r),new Vector2(0,-r)};
        foreach(var d in o)
        {
            float x=b.center.x+d.x,z=b.center.z+d.y;if(WaterAt(tile,sem,x,z))return false;
            float y=t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;if(y<=.22f)return false;
        }
        return true;
    }
    static bool RoadAt(byte[] tile,byte[] grp,byte[] sem,float x,float z)
    {
        int sx=Mathf.Clamp(Mathf.RoundToInt(x/4f+64f),0,127),sy=Mathf.Clamp(Mathf.RoundToInt(64f-z/4f),0,127);
        byte raw=tile[sy*128+sx],f=sem[raw],g=grp[raw];return (f&8)!=0||(g>=8&&g<255);
    }
    static bool RoadSpot(byte[] tile,byte[] grp,byte[] sem,Bounds b)
    {
        float r=Mathf.Clamp(Mathf.Max(b.extents.x,b.extents.z)*.35f,.35f,1.25f);
        Vector2[] o={Vector2.zero,new Vector2(r,0),new Vector2(-r,0),new Vector2(0,r),new Vector2(0,-r)};
        foreach(var d in o)if(RoadAt(tile,grp,sem,b.center.x+d.x,b.center.z+d.y))return true;
        return false;
    }
    static bool GroundOrRemove(Transform item,Terrain t,byte[] tile,byte[] grp,byte[] sem)
    {
        Bounds b=B(item.gameObject);if(b.size==Vector3.zero)return false;
        if(!DrySpot(t,tile,sem,b)||RoadSpot(tile,grp,sem,b)){UnityEngine.Object.DestroyImmediate(item.gameObject);return true;}
        float y=t.SampleHeight(new Vector3(b.center.x,0,b.center.z))+t.transform.position.y;
        item.position+=Vector3.up*(y-b.min.y);float yaw=item.eulerAngles.y;item.rotation=Quaternion.Euler(0,yaw,0);return false;
    }
    static int ProcessDirect(Transform c,Terrain t,byte[] tile,byte[] grp,byte[] sem)
    {
        if(!c)return 0;int removed=0;
        foreach(Transform item in c.Cast<Transform>().ToList())if(item)removed+=GroundOrRemove(item,t,tile,grp,sem)?1:0;
        return removed;
    }
    static int ProcessGrouped(Transform c,Terrain t,byte[] tile,byte[] grp,byte[] sem)
    {
        if(!c)return 0;int removed=0;
        foreach(Transform g in c.Cast<Transform>().ToList())foreach(Transform item in g.Cast<Transform>().ToList())if(item)removed+=GroundOrRemove(item,t,tile,grp,sem)?1:0;
        return removed;
    }
    static int CullAndGroundVegetation(Transform root,Terrain t,byte[] tile,byte[] grp,byte[] sem)
    {
        int removed=0;
        removed+=ProcessDirect(root.Find("Vegetation - Source Anchored Final"),t,tile,grp,sem);
        removed+=ProcessDirect(root.Find("Biome Vegetation - Source Grid"),t,tile,grp,sem);
        removed+=ProcessGrouped(root.Find("Vegetation - Preserved User Additions"),t,tile,grp,sem);
        var old=root.Find("Vegetation - MM Anchors + Natural Groves");
        if(old)
        {
            removed+=ProcessDirect(old.Find("Trees"),t,tile,grp,sem);
            removed+=ProcessDirect(old.Find("Understory"),t,tile,grp,sem);
            removed+=ProcessDirect(old.Find("Rocks"),t,tile,grp,sem);
        }
        return removed;
    }
}

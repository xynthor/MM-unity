using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMWestNorthForbiddenPineCleanup
{
    class Z{public string key,scene,linked;public Z(string k,string s,string l){key=k;scene=s;linked=l;}}
    static readonly Z[] Zones={
        new Z("SweetWater","Assets/Scenes/Regions/SweetWater.unity","Sweet Water - LINKED REFERENCE"),
        new Z("Kriegspire","Assets/Scenes/Regions/Kriegspire.unity","Kriegspire - LINKED REFERENCE"),
        new Z("FrozenHighlands","Assets/Scenes/Regions/FrozenHighlands.unity","White Cap / Frozen Highlands - LINKED REFERENCE")
    };
    static readonly string[] Containers={"Vegetation - Source Anchored Final","Realistic Ecosystem Supplementary"};
    const string LinkedScene="Assets/Scenes/World/Enroth.unity";
    const string Report="Validation/EnvironmentRealism/west_north_forbidden_pine_cleanup.csv";

    [MenuItem("MMUnity/Environment/Cleanup West North Forbidden Pines")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var rows=new List<string>{"scope,zone,container,name,class,old_asset,new_asset,x_before,z_before,x_after,z_after,rot_match,scale_match,ground_gap,status"};
        int standalone=0,linked=0;

        foreach(var z in Zones)
        {
            var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
            var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
            var terrain=root?root.GetComponentInChildren<Terrain>(true):null;
            if(!root||!terrain)throw new Exception("Missing root/terrain "+z.key);
            string dd="Assets/World/"+z.key+"/Data";
            byte[] tile=File.ReadAllBytes(dd+"/tilemap_u8.bin"),grp=File.ReadAllBytes(dd+"/tile_groups_u8.bin"),sem=File.ReadAllBytes(dd+"/tile_semantics_u8.bin");
            foreach(string cn in Containers)
            {
                var c=root.transform.Find(cn);if(!c)continue;
                foreach(var w in c.Cast<Transform>().ToList())
                {
                    string old=ForbiddenAsset(w.gameObject);if(string.IsNullOrEmpty(old))continue;
                    int cls=ClassAt(z.key,tile,grp,sem,w.position.x,w.position.z);
                    if(ReplaceVisual(w,terrain,cls,z.key,out string np,out float gap,out bool rot,out bool scl))
                    {
                        standalone++;
                        rows.Add(Row("standalone",z.key,cn,w.name,cls,old,np,w.position.x,w.position.z,w.position.x,w.position.z,rot,scl,gap,"PASS"));
                    }
                }
            }
            if(sc.isDirty)EditorSceneManager.SaveScene(sc);
        }

        var lsc=EditorSceneManager.OpenScene(LinkedScene,OpenSceneMode.Single);
        var world=lsc.GetRootGameObjects().FirstOrDefault();
        foreach(var z in Zones)
        {
            var rr=Find(world.transform,z.linked);if(!rr)continue;
            var terrain=rr.GetComponentInChildren<Terrain>(true);if(!terrain)continue;
            string dd="Assets/World/"+z.key+"/Data";
            byte[] tile=File.ReadAllBytes(dd+"/tilemap_u8.bin"),grp=File.ReadAllBytes(dd+"/tile_groups_u8.bin"),sem=File.ReadAllBytes(dd+"/tile_semantics_u8.bin");
            foreach(string cn in Containers)
            {
                var c=rr.Find(cn);if(!c)continue;
                foreach(var w in c.Cast<Transform>().ToList())
                {
                    string old=ForbiddenAsset(w.gameObject);if(string.IsNullOrEmpty(old))continue;
                    float lx=w.position.x-rr.position.x,lz=w.position.z-rr.position.z;
                    int cls=ClassAt(z.key,tile,grp,sem,lx,lz);
                    Vector3 p0=w.position; Quaternion r0=w.localRotation; Vector3 s0=w.localScale;
                    if(ReplaceVisual(w,terrain,cls,z.key,out string np,out float gap,out bool rot,out bool scl))
                    {
                        if(Mathf.Abs(w.position.x-p0.x)>.0001f||Mathf.Abs(w.position.z-p0.z)>.0001f)throw new Exception("XZ changed "+w.name);
                        linked++;
                        rows.Add(Row("linked",z.key,cn,w.name,cls,old,np,p0.x,p0.z,w.position.x,w.position.z,rot,scl,gap,"PASS"));
                    }
                }
            }
        }
        if(lsc.isDirty)EditorSceneManager.SaveScene(lsc);
        File.WriteAllLines(Report,rows);
        AssetDatabase.SaveAssets();
        Debug.Log($"MM_WN_FORBIDDEN_PINE_DONE standalone={standalone} linked={linked}");
    }

    static bool ReplaceVisual(Transform wrapper,Terrain terrain,int cls,string zone,out string newPath,out float gap,out bool rotMatch,out bool scaleMatch)
    {
        Vector3 p0=wrapper.position,s0=wrapper.localScale;Quaternion r0=wrapper.localRotation;
        float oldH=BoundsOf(wrapper.gameObject).size.y;
        GameObject prefab=ChoosePrefab(cls,zone,wrapper.name);
        if(!prefab){newPath="";gap=999;rotMatch=false;scaleMatch=false;return false;}
        newPath=AssetDatabase.GetAssetPath(prefab);

        foreach(Transform ch in wrapper.Cast<Transform>().ToList())UnityEngine.Object.DestroyImmediate(ch.gameObject);
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        visual.transform.SetParent(wrapper,false);
        visual.transform.localPosition=Vector3.zero;
        visual.transform.localRotation=Quaternion.identity;
        visual.transform.localScale=Vector3.one;
        float nh=BoundsOf(wrapper.gameObject).size.y;
        if(nh>.05f&&oldH>.05f)visual.transform.localScale*=Mathf.Clamp(oldH/nh,.55f,1.8f);
        if(cls==MMRealisticTerrainBiomePass.Snow)ApplySnowFirMaterials(visual);

        Bounds b=BoundsOf(wrapper.gameObject);
        float gy=terrain.SampleHeight(new Vector3(wrapper.position.x,0,wrapper.position.z))+terrain.transform.position.y;
        wrapper.position+=Vector3.up*(gy-b.min.y);
        wrapper.position=new Vector3(p0.x,wrapper.position.y,p0.z);
        wrapper.localRotation=r0;wrapper.localScale=s0;
        b=BoundsOf(wrapper.gameObject);
        gy=terrain.SampleHeight(new Vector3(wrapper.position.x,0,wrapper.position.z))+terrain.transform.position.y;
        gap=b.min.y-gy;
        rotMatch=Quaternion.Angle(wrapper.localRotation,r0)<.0001f;
        scaleMatch=(wrapper.localScale-s0).sqrMagnitude<1e-10f;
        if(Mathf.Abs(gap)>.04f||!rotMatch||!scaleMatch)
            throw new Exception("Replacement verification failed "+zone+"/"+wrapper.name+" gap="+gap);
        EditorUtility.SetDirty(wrapper);
        return true;
    }

    static GameObject ChoosePrefab(int cls,string zone,string name)
    {
        if(cls==MMRealisticTerrainBiomePass.Volcanic||cls==MMRealisticTerrainBiomePass.Arid||cls==MMRealisticTerrainBiomePass.Desert)
        {
            string p=((Stable(name)&1)==0)?
              "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_02.prefab":
              "Assets/Art/Environment/Vegetation/Trees/Pines/PineDead_001/PineDead_03.prefab";
            return AssetDatabase.LoadAssetAtPath<GameObject>(p);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/FinalWorldVegetation/fir_sapling.prefab");
    }

    static void ApplySnowFirMaterials(GameObject go)
    {
        var branch=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RealisticWorld/SnowTrees/fir_sapling_branches.mat");
        var twig=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/RealisticWorld/SnowTrees/fir_sapling_twigs.mat");
        if(!branch||!twig)return;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats=r.sharedMaterials;if(mats==null||mats.Length==0)continue;
            for(int i=0;i<mats.Length;i++)
            {
                string n=(mats[i]?mats[i].name:"").ToLowerInvariant();
                mats[i]=n.Contains("twig")?twig:branch;
            }
            r.sharedMaterials=mats;
        }
    }

    static int ClassAt(string zone,byte[] tile,byte[] grp,byte[] sem,float x,float z)
    {
        int sx=Mathf.Clamp(Mathf.RoundToInt(x/4f+64f),0,127),sy=Mathf.Clamp(Mathf.RoundToInt(64f-z/4f),0,127);
        byte raw=tile[sy*128+sx];
        return MMRealisticTerrainBiomePass.BaseClassAt(zone,grp[raw],sem[raw],sy);
    }
    static string ForbiddenAsset(GameObject go)
    {
        foreach(var mf in go.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!mf.sharedMesh)continue;string p=AssetDatabase.GetAssetPath(mf.sharedMesh).ToLowerInvariant();
            if(p.Contains("/pine_007/")||p.Contains("/pine_b/")||p.Contains("/pine_c/")||p.Contains("/pine_d/")||
               p.Contains("tree_small_02")||p.Contains("searsia_lucida"))return p;
        }
        return "";
    }
    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static Transform Find(Transform root,string n){if(!root)return null;if(root.name==n)return root;foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
    static int Stable(string s){unchecked{int h=17;foreach(char c in s)h=h*31+c;return h;}}
    static string Row(string scope,string zone,string container,string name,int cls,string oldp,string newp,float xb,float zb,float xa,float za,bool r,bool s,float g,string status)
      =>string.Join(",",new[]{scope,zone,container.Replace(',',';'),name.Replace(',',';'),MMRealisticTerrainBiomePass.ClassName(cls),oldp.Replace(',',';'),newp.Replace(',',';'),F(xb),F(zb),F(xa),F(za),r.ToString(),s.ToString(),F(g),status});
    static string F(float v)=>v.ToString("0.######",CultureInfo.InvariantCulture);
}

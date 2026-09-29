using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMSourceObject3DPass
{
    sealed class Zone { public string key, display, scene; public Zone(string k,string d,string s){key=k;display=d;scene=s;} }
    static readonly Zone[] Zones={
        new Zone("NewSorpigal","New Sorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity"),
        new Zone("CastleIronfist","Castle Ironfist","Assets/Scenes/CastleIronfist_SourceGrid.unity"),
        new Zone("MireOfTheDamned","Mire of the Damned","Assets/Scenes/MireOfTheDamned_SourceGrid.unity"),
        new Zone("Dragonsand","Dragonsand","Assets/Scenes/Dragonsand_SourceGrid.unity"),
        new Zone("HermitsIsle","Hermit's Isle","Assets/Scenes/HermitsIsle_SourceGrid.unity"),
        new Zone("MistyIslands","Misty Islands","Assets/Scenes/MistyIslands_SourceGrid.unity"),
        new Zone("BootlegBay","Bootleg Bay","Assets/Scenes/BootlegBay_SourceGrid.unity"),
        new Zone("FreeHaven","Free Haven","Assets/Scenes/FreeHaven_SourceGrid.unity"),        new Zone("Blackshire","Blackshire","Assets/Scenes/Blackshire_SourceGrid.unity"),
        new Zone("ParadiseValley","Paradise Valley","Assets/Scenes/ParadiseValley_SourceGrid.unity"),
        new Zone("EelInfestedWaters","Eel Infested Waters","Assets/Scenes/EelInfestedWaters_SourceGrid.unity"),
        new Zone("SilverCove","Silver Cove","Assets/Scenes/SilverCove_SourceGrid.unity"),
        new Zone("FrozenHighlands","White Cap / Frozen Highlands","Assets/Scenes/FrozenHighlands_SourceGrid.unity"),
        new Zone("Kriegspire","Kriegspire","Assets/Scenes/Kriegspire_SourceGrid.unity"),
        new Zone("SweetWater","Sweet Water","Assets/Scenes/SweetWater_SourceGrid.unity")};

    [MenuItem("MMUnity/Replace MM6 Sprites With 3D Objects")]
    public static void ApplyAll()
    {
        AssetDatabase.Refresh();
        foreach(var z in Zones) ApplyZone(z);
        AssetDatabase.SaveAssets();
        Debug.Log("SOURCE_OBJECTS_3D_ALL_DONE zones="+Zones.Length);
    }

    static bool StartMarker(string n)
    {
        n=n.Trim().ToLowerInvariant();
        return n=="party start" || n.EndsWith(" start");
    }

    static bool Existing3DAnchor(string n)
    {
        n=n.ToLowerInvariant();
        return n.StartsWith("6tree")||n.StartsWith("tree")||n.StartsWith("6rock");
    }
    static void ApplyZone(Zone z)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)
                 ?? sc.GetRootGameObjects().FirstOrDefault();
        if(!root) throw new Exception(z.display+" root missing");
        var terrain=root.GetComponentInChildren<Terrain>(true);
        if(!terrain) throw new Exception(z.display+" terrain missing");
        foreach(string oldName in new[]{"Source Sprites - OneToOne","Source Objects - OneToOne"})
        {
            var old=root.transform.Find(oldName); if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        var group=new GameObject("Source Objects - OneToOne"); group.transform.SetParent(root.transform,false);
        string path=$"Assets/World/{z.key}/Data/decorations.csv";
        var lines=File.ReadAllLines(path); int made=0,omitted=0;
        for(int i=1;i<lines.Length;i++)
        {
            var q=lines[i].Split(','); if(q.Length<10) continue;
            string name=q[1].Trim().ToLowerInvariant();
            if(string.IsNullOrEmpty(name)||StartMarker(name)||Existing3DAnchor(name)) continue;
            if(!F(q[4],out float ox)||!F(q[5],out float oy)||!F(q[6],out float oz)) continue;
            F(q[7],out float yaw); float x=ox,zp=-oz;
            var go=Create3D(name,i,group.transform); if(!go){omitted++;continue;}
            go.name=$"SRC3D_{i:000}_{name}"; go.transform.rotation=Quaternion.Euler(0f,-yaw,0f);
            float y=WaterProp(name)?.12f:terrain.SampleHeight(new Vector3(x,0,zp))+terrain.transform.position.y;
            Ground(go,new Vector3(x,y,zp)); made++;
        }
        EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc,z.scene);
        Debug.Log($"SOURCE_OBJECTS_3D {z.display} made={made} omitted={omitted}");
    }
    static bool F(string s,out float v)=>float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out v);
    static bool WaterProp(string n)=>n.Contains("bouy")||n.Contains("buoy");

    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true);
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds); return b;
    }

    static void Ground(GameObject go,Vector3 p)
    {
        go.transform.position=p; Bounds b=BoundsOf(go);
        if(b.size!=Vector3.zero) go.transform.position+=Vector3.up*(p.y-b.min.y);
    }

    static void ScaleHeight(GameObject go,float h)
    {
        Bounds b=BoundsOf(go); if(b.size.y>.001f)go.transform.localScale*=h/b.size.y;
    }

    static GameObject Instance(string path,Transform parent,float height)
    {
        var p=AssetDatabase.LoadAssetAtPath<GameObject>(path); if(!p)return null;
        var go=(GameObject)PrefabUtility.InstantiatePrefab(p); go.transform.SetParent(parent,false);
        if(height>0)ScaleHeight(go,height); return go;
    }

    static void RemoveColliders(GameObject go)
    {
        foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
    }
    static Material Mat(string key,Color color,string tex=null)
    {
        string dir="Assets/Materials/Source3D"; Directory.CreateDirectory(dir); string path=$"{dir}/{key}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path); var sh=Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        m.color=color; if(!string.IsNullOrEmpty(tex)&&m.HasProperty("_MainTex"))m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.08f); EditorUtility.SetDirty(m); return m;
    }

    static Material Wood()=>Mat("Wood",Color.white,"Assets/Environment/PolyHaven/Textures/wood_planks/wood_planks_diff_1k.jpg");
    static Material Stone()=>Mat("Stone",new Color(.72f,.70f,.66f),"Assets/Environment/PolyHaven/Textures/rocky_terrain_02/rocky_terrain_02_diff_1k.jpg");
    static Material Metal()=>Mat("Metal",new Color(.72f,.74f,.76f),"Assets/Environment/PolyHaven/Textures/metal_plate/metal_plate_diff_1k.jpg");
    static Material Green()=>Mat("Green",new Color(.25f,.48f,.20f),"Assets/Environment/PolyHaven/Textures/fabric_pattern_05/fabric_pattern_05_col_01_1k.jpg");
    static Material Red()=>Mat("Red",new Color(.60f,.12f,.08f),"Assets/Environment/PolyHaven/Textures/fabric_pattern_05/fabric_pattern_05_col_01_1k.jpg");
    static Material Fire()=>Metal();

    static GameObject Prim(PrimitiveType type,string name,Transform parent,Vector3 pos,Vector3 scale,Material mat)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;
        var r=g.GetComponent<Renderer>();if(r)r.sharedMaterial=mat;var c=g.GetComponent<Collider>();if(c)UnityEngine.Object.DestroyImmediate(c);return g;
    }

    static GameObject Group(string name,Transform parent)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);return g;
    }    static GameObject Create3D(string n,int seed,Transform parent)
    {
        n=n.ToLowerInvariant();
        if(n.Contains("barel")||n.Contains("barrel"))
        {
            var g=Instance("Assets/Environment/PolyHaven/Models/wine_barrel_01/wine_barrel_01_1k.fbx",parent,1.05f);
            if(g) return g;
            var p=Group("Barrel",parent); Prim(PrimitiveType.Cylinder,"Body",p.transform,Vector3.zero,new Vector3(.52f,.52f,.52f),Wood()); return p;
        }
        if(n.Contains("stump"))
        {
            var g=Instance("Assets/Art/Environment/Vegetation/Trees/Generic_Stump/Stump_04/Stump_04.prefab",parent,.8f);
            if(g) return g;
            var p=Group("Stump",parent); Prim(PrimitiveType.Cylinder,"Wood",p.transform,Vector3.zero,new Vector3(.55f,.35f,.55f),Wood()); return p;
        }
        if(n.Contains("ckfyr")||n.Contains("cmpfyr")) return MakeCampfire(parent);
        if(n.Contains("foodbowl")) return MakeBowl(parent);
        if(n.Contains("bouy")||n.Contains("buoy")) return MakeBuoy(parent);
        if(n.Contains("swrdstn")) return MakeSwordStone(parent);
        if(n.Contains("ped06")) return MakePedestal(parent);
        if(n.StartsWith("fla")) return MakeFlag(parent,seed);
        return null;
    }
    static GameObject MakeCampfire(Transform parent)
    {
        var g=Group("Campfire",parent);
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI*2f/8f;
            Prim(PrimitiveType.Sphere,"Stone",g.transform,new Vector3(Mathf.Cos(a)*.46f,.12f,Mathf.Sin(a)*.46f),new Vector3(.24f,.16f,.24f),Stone());
        }
        for(int i=0;i<3;i++)
        {
            var log=Prim(PrimitiveType.Cylinder,"Log",g.transform,new Vector3(0,.22f,0),new Vector3(.11f,.55f,.11f),Wood());
            log.transform.localRotation=Quaternion.Euler(90f,i*60f,0);
        }
        Prim(PrimitiveType.Sphere,"Ember",g.transform,new Vector3(0,.28f,0),new Vector3(.26f,.18f,.26f),Fire());
        return g;
    }

    static GameObject MakeBowl(Transform parent)
    {
        var g=Group("FoodBowl",parent);
        Prim(PrimitiveType.Cylinder,"Bowl",g.transform,new Vector3(0,.11f,0),new Vector3(.34f,.09f,.34f),Wood());
        Prim(PrimitiveType.Sphere,"Food",g.transform,new Vector3(0,.23f,0),new Vector3(.25f,.12f,.25f),Green());
        return g;
    }
    static GameObject MakeBuoy(Transform parent)
    {
        var g=Group("Buoy",parent);
        Prim(PrimitiveType.Cylinder,"Float",g.transform,new Vector3(0,.34f,0),new Vector3(.28f,.42f,.28f),Mat("BuoyRed",new Color(.65f,.08f,.05f),"Assets/Environment/PolyHaven/Textures/metal_plate/metal_plate_diff_1k.jpg"));
        Prim(PrimitiveType.Sphere,"Top",g.transform,new Vector3(0,.78f,0),new Vector3(.18f,.18f,.18f),Metal());
        return g;
    }

    static GameObject MakePedestal(Transform parent)
    {
        var g=Group("Pedestal",parent);
        Prim(PrimitiveType.Cube,"Base",g.transform,new Vector3(0,.18f,0),new Vector3(.8f,.36f,.8f),Stone());
        Prim(PrimitiveType.Cube,"Column",g.transform,new Vector3(0,.72f,0),new Vector3(.46f,.72f,.46f),Stone());
        return g;
    }

    static GameObject MakeSwordStone(Transform parent)
    {
        var g=MakePedestal(parent); g.name="SwordStone";
        Prim(PrimitiveType.Cube,"Blade",g.transform,new Vector3(0,1.58f,0),new Vector3(.07f,.78f,.025f),Metal());
        Prim(PrimitiveType.Cube,"Guard",g.transform,new Vector3(0,1.18f,0),new Vector3(.46f,.06f,.08f),Metal());
        Prim(PrimitiveType.Cylinder,"Grip",g.transform,new Vector3(0,.95f,0),new Vector3(.06f,.22f,.06f),Wood());
        return g;
    }
    static GameObject MakeFlag(Transform parent,int seed)
    {
        var g=Group("Flag",parent);
        Prim(PrimitiveType.Cylinder,"Pole",g.transform,new Vector3(0,1.35f,0),new Vector3(.035f,1.35f,.035f),Wood());
        var cloth=Prim(PrimitiveType.Cube,"Cloth",g.transform,new Vector3(.34f,2.05f,0),new Vector3(.65f,.38f,.025f),(seed&1)==0?Red():Green());
        cloth.transform.localRotation=Quaternion.Euler(0,0,-4f);
        return g;
    }
}

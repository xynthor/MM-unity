using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class AuditTextureWorld
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
    static string TexPath(Material m)
    {
        if(!m) return "<null-material>";
        if(!m.HasProperty("_MainTex")) return "<no-maintex-property>";
        var t=m.mainTexture; return t?AssetDatabase.GetAssetPath(t):"<null-maintex>";
    }
    static string AssetPath(GameObject go)
    {
        var src=PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;
        return src?AssetDatabase.GetAssetPath(src):"";
    }
    static bool TextureExpected(Material m)
    {
        if(!m) return true;
        string n=m.name.ToLowerInvariant();
        if(n.Contains("water")||n.Contains("proxy")||n.Contains("solidcore")) return false;
        return m.HasProperty("_MainTex");
    }
    static void ScanRenderers(string zone,string kind,Transform root,List<string> details,
        ref int renderers,ref int slots,ref int nullMat,ref int nullTex,ref int badTrees)
    {
        if(!root)return;
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))
        {
            renderers++;
            string ap=AssetPath(r.gameObject).ToLowerInvariant();
            if(ap.Contains("treehigh001")||ap.Contains("treehigh002")||ap.Contains("treehigh003")||
               ap.Contains("banyantree")||ap.Contains("tree_small_02")||ap.Contains("dead_tree_trunk_02")) badTrees++;
            var ms=r.sharedMaterials;
            for(int i=0;i<ms.Length;i++)
            {
                slots++; var m=ms[i];
                if(!m){nullMat++;details.Add($"{zone},{kind},NULL_MATERIAL,{r.name},slot={i}");continue;}
                if(TextureExpected(m) && (!m.mainTexture))
                {
                    nullTex++;
                    details.Add($"{zone},{kind},NULL_TEXTURE,{r.name},mat={m.name},asset={AssetPath(r.gameObject)}");
                }
            }
        }
    }
    static void ScanZone(Z z,List<string> sum,List<string> details)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var terrain=root.GetComponentInChildren<Terrain>(true);
        int terrainLayers=0,terrainMissing=0;
        if(terrain)
        {
            foreach(var l in terrain.terrainData.terrainLayers)
            {
                terrainLayers++;
                if(!l||!l.diffuseTexture){terrainMissing++;details.Add($"{z.key},TERRAIN,NULL_TEXTURE,{(l?l.name:"<null-layer>")}");}
            }
        }
        int ar=0,aslots=0,am=0,at=0,abad=0;
        int vr=0,vslots=0,vm=0,vt=0,vbad=0;
        int sr=0,sslots=0,sm=0,st=0,sbad=0;
        ScanRenderers(z.key,"ARCH",root.transform.Find("Architecture - MM6 Original Layout Expanded"),details,ref ar,ref aslots,ref am,ref at,ref abad);
        ScanRenderers(z.key,"VEG",root.transform.Find("Vegetation - MM Anchors + Natural Groves"),details,ref vr,ref vslots,ref vm,ref vt,ref vbad);
        ScanRenderers(z.key,"SRCSPRITE",root.transform.Find("Source Sprites - OneToOne"),details,ref sr,ref sslots,ref sm,ref st,ref sbad);
        var biome=root.transform.Find("Biome Vegetation - Source Grid");
        if(biome) ScanRenderers(z.key,"BIOME",biome,details,ref vr,ref vslots,ref vm,ref vt,ref vbad);
        sum.Add($"{z.key},terrainLayers={terrainLayers},terrainMissing={terrainMissing},archRenderers={ar},archSlots={aslots},archNullMat={am},archNullTex={at},vegRenderers={vr},vegSlots={vslots},vegNullMat={vm},vegNullTex={vt},badTreeAssets={vbad},spriteRenderers={sr},spriteNullMat={sm},spriteNullTex={st}");
    }
    static void ScanCactusPrefab(List<string> details)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Environment/DesertVegetation/Cactus.fbx");
        if(!prefab){details.Add("GLOBAL,CACTUS,MISSING_PREFAB");return;}
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var ms=r.sharedMaterials;
            for(int i=0;i<ms.Length;i++)
                details.Add($"GLOBAL,CACTUS,RENDERER={r.name},slot={i},mat={(ms[i]?ms[i].name:"<null>")},tex={TexPath(ms[i])}");
        }
        UnityEngine.Object.DestroyImmediate(go);
    }
    [MenuItem("MMUnity/Audit Textures + Materials")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation");
        var sum=new List<string>(); var details=new List<string>();
        foreach(var z in Zones) ScanZone(z,sum,details);
        ScanCactusPrefab(details);
        File.WriteAllLines("Validation/Texture_Audit_Summary.csv",new[]{"zone,audit"}.Concat(sum));
        File.WriteAllLines("Validation/Texture_Audit_Details.csv",new[]{"zone,kind,detail"}.Concat(details));
        Debug.Log($"TEXTURE_AUDIT_DONE zones={Zones.Length} details={details.Count}");
        EditorSceneManager.OpenScene("Assets/Scenes/Enroth_Linked_OpenWorld.unity",OpenSceneMode.Single);
    }
}

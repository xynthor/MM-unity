using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMFinalizeDragonIsleCanonicalTerrain
{
    const string PREVIEW="Assets/TempDragonTemplate/DragonIsleReferenceTerrain_PREVIEW.asset";
    const string FINAL="Assets/World/DragonIsle/Generated/DragonIsleReferenceTerrain_Final.asset";
    const string STANDALONE="Assets/Scenes/DragonIsle_Reference.unity";
    const string LINKED="Assets/Scenes/Enroth_Linked_OpenWorld.unity";

    [MenuItem("MMUnity/Dragon Isle/Finalize Canonical Terrain Asset")]
    public static void Run(){
        Directory.CreateDirectory("Validation/CactusDragonAudit");
        var src=AssetDatabase.LoadAssetAtPath<TerrainData>(PREVIEW);
        if(!src)throw new Exception("Missing traced preview TerrainData; refusing promotion");
        var fin=AssetDatabase.LoadAssetAtPath<TerrainData>(FINAL);
        if(!fin){
            Directory.CreateDirectory(Path.GetDirectoryName(FINAL));
            if(!AssetDatabase.CopyAsset(PREVIEW,FINAL))
                throw new IOException("Failed to copy reference terrain into Dragon Isle canonical folder");
            AssetDatabase.ImportAsset(FINAL,ImportAssetOptions.ForceUpdate);
            fin=AssetDatabase.LoadAssetAtPath<TerrainData>(FINAL);
        }
        if(!fin || fin.heightmapResolution!=src.heightmapResolution ||
           fin.alphamapResolution!=src.alphamapResolution)
            throw new Exception("Canonical terrain resolution mismatch");
        var result=new List<string>{"Dragon Isle canonical terrain finalization","final="+FINAL};
        foreach(var item in new[]{("standalone",STANDALONE),("linked",LINKED)}){
            var sc=EditorSceneManager.OpenScene(item.Item2,OpenSceneMode.Single);
            Transform region;
            if(item.Item1=="standalone"){
                region=sc.GetRootGameObjects()
                    .FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true))?.transform;
            } else {
                region=sc.GetRootGameObjects()
                    .SelectMany(g=>g.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(t=>t.name.StartsWith("Dragon Isle - LINKED",StringComparison.OrdinalIgnoreCase));
            }
            if(!region)throw new Exception("Dragon Isle missing from "+item.Item2);
            var terr=region.GetComponentInChildren<Terrain>(true);
            if(!terr)throw new Exception("Dragon Isle Terrain missing from "+item.Item2);
            if(terr.terrainData!=src && terr.terrainData!=fin)
                throw new Exception("Unexpected preexisting Dragon Isle TerrainData in "+item.Item2);
            terr.terrainData=fin;
            var col=terr.GetComponent<TerrainCollider>();
            if(col)col.terrainData=fin;
            EditorSceneManager.MarkSceneDirty(sc);
            if(!EditorSceneManager.SaveScene(sc,item.Item2))throw new IOException("Save failed: "+item.Item2);
            result.Add(item.Item1+"="+AssetDatabase.GetAssetPath(terr.terrainData));
        }
        AssetDatabase.SaveAssets();
        File.WriteAllLines("Validation/CactusDragonAudit/dragon_canonical_terrain_final.txt",result);
        Debug.Log("DRAGON_ISLE_CANONICAL_TERRAIN_FINALIZED "+string.Join(";",result));
    }
}
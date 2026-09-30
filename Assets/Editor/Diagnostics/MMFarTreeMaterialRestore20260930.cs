using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMFarTreeMaterialRestore20260930
{
    static readonly Dictionary<string,string> Map=new Dictionary<string,string>
    {
        {"Assets/World/WorldExtensions/Generated/LinkedTreeImpostors256/1852c3ef7d28336498e5559ecf3c1c5f_4300000_1b2473d571.mat","Assets/World/WorldExtensions/Generated/LinkedTreeImpostors/1852c3ef7d28336498e5559ecf3c1c5f_4300000_d7d10591_top.mat"},
        {"Assets/World/WorldExtensions/Generated/LinkedTreeImpostors256/1852c3ef7d28336498e5559ecf3c1c5f_4300000_09e335042b.mat","Assets/World/WorldExtensions/Generated/LinkedTreeImpostors/1852c3ef7d28336498e5559ecf3c1c5f_4300000_2a04fc38_top.mat"},
        {"Assets/World/WorldExtensions/Generated/LinkedTreeImpostors256/1852c3ef7d28336498e5559ecf3c1c5f_4300000_f5a67d736d.mat","Assets/World/WorldExtensions/Generated/LinkedTreeImpostors/1852c3ef7d28336498e5559ecf3c1c5f_4300000_e9011dab_top.mat"},
        {"Assets/World/WorldExtensions/Generated/LinkedTreeImpostors256/ed685f268ccd6ae48a6511c98bb3bde5_4300000_b7c972d06f.mat","Assets/World/WorldExtensions/Generated/LinkedTreeImpostors/ed685f268ccd6ae48a6511c98bb3bde5_4300000_b4bd758d_top.mat"},
        {"Assets/World/WorldExtensions/Generated/LinkedTreeImpostors256/5b6bf80a26085b049830e90137c07b49_4300000_3ab443635b.mat","Assets/World/WorldExtensions/Generated/LinkedTreeImpostors/5b6bf80a26085b049830e90137c07b49_4300000_8ed98dd5_top.mat"},
        {"Assets/World/WorldExtensions/Generated/LinkedTreeImpostors256/2efae4d2062bc3a44a3a826ffcecce31_4300000_1046f539bb.mat","Assets/World/WorldExtensions/Generated/LinkedTreeImpostors/2efae4d2062bc3a44a3a826ffcecce31_4300000_294930af_top.mat"}
    };
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
        int changed=0;
        foreach(var r in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)))
        {
            if(r.transform.name!="Linked distance LOD 2" || !r.sharedMaterial) continue;
            string p=AssetDatabase.GetAssetPath(r.sharedMaterial);
            if(!Map.TryGetValue(p,out var oldPath)) continue;
            var old=AssetDatabase.LoadAssetAtPath<Material>(oldPath);
            if(!old) throw new Exception("Missing material "+oldPath);
            r.sharedMaterial=old;
            EditorUtility.SetDirty(r);
            changed++;
        }
        if(changed<700) throw new Exception("Unexpected restore count "+changed);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText("Validation/EdgeGrid20260923/far_tree_material_restore_20260930.txt","PASS changed="+changed+" terrainWrites=0 heightWrites=0 alphamapWrites=0 transformWrites=0\n");
    }
}

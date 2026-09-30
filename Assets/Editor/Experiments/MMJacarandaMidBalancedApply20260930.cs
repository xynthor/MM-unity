using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMJacarandaMidBalancedApply20260930
{
    const string Backup="Backups/BeforeJacarandaMidBalanced_20260930/Enroth.unity";
    const string Current="Assets/World/WorldExtensions/Generated/LinkedJacarandaRepairV2/JacarandaMid.asset";
    const string Next="Assets/World/WorldExtensions/Generated/LinkedJacarandaMidLean20260930/JacarandaMidBalanced.asset";
    public static void Apply()
    {
        if(!File.Exists(Backup))throw new Exception("Backup missing");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/World/Enroth.unity"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Saved linked Edit Mode scene required");
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(Current);
        var next=AssetDatabase.LoadAssetAtPath<Mesh>(Next);
        if(!old||!next)throw new Exception("Missing Jacaranda mid mesh");
        int changed=0;
        foreach(var root in scene.GetRootGameObjects())
        foreach(var f in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if(f.transform.name=="Linked distance LOD 1"&&f.sharedMesh==old)
            {
                f.sharedMesh=next;EditorUtility.SetDirty(f);changed++;
            }
        }
        if(changed!=150)throw new Exception("Expected 150 Jacaranda mids, got "+changed);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Validation/EdgeGrid20260923/jacaranda_mid_balanced_applied_20260930.txt",
            "PASS changed="+changed+" terrainWrites=0 transformWrites=0 materialWrites=0 canonicalWrites=0\n");
        Debug.Log("Jacaranda balanced mids applied="+changed);
    }
}
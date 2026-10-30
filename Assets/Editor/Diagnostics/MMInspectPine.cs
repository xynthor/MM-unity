using UnityEditor;
using UnityEngine;
public static class MMInspectPine
{
    [MenuItem("MMUnity/Inspect CC0 Pine Materials")]
    public static void Run()
    {
        string p="Assets/Environment/PolyHaven/Models/pine_sapling_small/pine_sapling_small_1k.fbx";
        var go=AssetDatabase.LoadAssetAtPath<GameObject>(p);
        if(!go){Debug.LogError("PINE_INSPECT missing "+p);return;}
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var a=r.sharedMaterials;
            for(int i=0;i<a.Length;i++)
            {
                var m=a[i];
                Debug.Log($"PINE_INSPECT renderer={r.name} slot={i} mat={(m?m.name:"<null>")} tex={(m&&m.mainTexture?AssetDatabase.GetAssetPath(m.mainTexture):"<null>")}");
            }
        }
    }
}

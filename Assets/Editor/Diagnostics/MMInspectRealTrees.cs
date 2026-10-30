using UnityEditor;
using UnityEngine;

public static class MMInspectRealTrees
{
    [MenuItem("MMUnity/Inspect Real CC0 Trees")]
    public static void Run()
    {
        Dump("fir","Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_1k.fbx");
        Dump("searsia","Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_1k.fbx");
    }
    static void Dump(string id,string path)
    {
        var p=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!p){Debug.LogError("TREE_INSPECT missing "+path);return;}
        var go=(GameObject)PrefabUtility.InstantiatePrefab(p);
        foreach(var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mf=r.GetComponent<MeshFilter>();int sm=mf&&mf.sharedMesh?mf.sharedMesh.subMeshCount:-1;
            for(int i=0;i<r.sharedMaterials.Length;i++)
            {
                var m=r.sharedMaterials[i];Debug.Log($"TREE_INSPECT {id} renderer={r.name} submeshes={sm} slot={i} mat={(m?m.name:"<null>")} tex={(m&&m.mainTexture?AssetDatabase.GetAssetPath(m.mainTexture):"<null>")}");
            }
        }
        Object.DestroyImmediate(go);
    }
}

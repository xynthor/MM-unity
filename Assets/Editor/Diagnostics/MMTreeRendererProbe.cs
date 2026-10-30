using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Text;
public static class MMTreeRendererProbe
{
    sealed class S{public string id,p;public S(string i,string x){id=i;p=x;}}
    static readonly S[] A={
        new S("searsia","Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_1k.fbx"),
        new S("fir","Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_1k.fbx"),
        new S("island1","Assets/Environment/PolyHaven/Downloaded/island_tree_01/island_tree_01_1k.fbx"),
        new S("island2","Assets/Environment/PolyHaven/Downloaded/island_tree_02/island_tree_02_1k.fbx"),
        new S("island3","Assets/Environment/PolyHaven/Downloaded/island_tree_03/island_tree_03_1k.fbx"),
        new S("jacaranda","Assets/Environment/PolyHaven/Downloaded/jacaranda_tree/jacaranda_tree_1k.fbx")};
    static GameObject L(string p){var g=AssetDatabase.LoadAssetAtPath<GameObject>(p);return g?g:AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();}
    [MenuItem("MMUnity/Debug/Tree Renderer Probe")]
    public static void Run(){
        var sb=new StringBuilder();
        foreach(var s in A){
            var src=L(s.p);if(!src){sb.AppendLine(s.id+",MISSING");continue;}
            var go=(GameObject)PrefabUtility.InstantiatePrefab(src);
            go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.identity;
            sb.AppendLine("=== "+s.id+" ===");
            foreach(var r in go.GetComponentsInChildren<Renderer>(true)){
                var b=r.bounds;string mats=string.Join("|",(r.sharedMaterials??new Material[0]).Select(m=>m?m.name:"NULL"));
                sb.AppendLine($"{r.gameObject.name},mats={mats},size={b.size.x:F3}|{b.size.y:F3}|{b.size.z:F3},center={b.center.x:F3}|{b.center.y:F3}|{b.center.z:F3}");
            }
            UnityEngine.Object.DestroyImmediate(go);
        }
        File.WriteAllText("C:/MMUnityPort/Validation/TreeRendererProbe.txt",sb.ToString());
        Debug.Log("TREE_RENDERER_PROBE_DONE");
    }
}
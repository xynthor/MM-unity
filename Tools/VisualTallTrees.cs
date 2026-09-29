using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{string P(Transform t)=>t.parent?P(t.parent)+"/"+t.name:t.name; public void Execute(ExecutionResult result){foreach(var r in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.size.y>2&&r.transform.parent&&true).GroupBy(r=>string.Join(";",r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m)))).Select(g=>g.First())){var f=r.GetComponent<MeshFilter>();result.Log(r.transform.parent.name+"/"+r.name+" bounds="+r.bounds.size+" mesh="+(f?AssetDatabase.GetAssetPath(f.sharedMesh):"")+" mats="+string.Join(";",r.sharedMaterials.Select(m=>AssetDatabase.GetAssetPath(m))));}}}




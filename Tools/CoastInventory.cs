using System; using System.IO; using System.Linq; using System.Collections.Generic; using UnityEngine; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine.SceneManagement;
internal class CommandScript : IRunCommand { public void Execute(ExecutionResult result) {
 for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene; refusing to switch");
 var rows=new List<string>();
 foreach(var p in Directory.GetFiles("Assets/Scenes","*.unity").Where(p=>!Path.GetFileName(p).StartsWith("_Recovery_")).OrderBy(p=>p)){
 var s=EditorSceneManager.OpenScene(p,OpenSceneMode.Single); rows.Add("SCENE "+p);
 foreach(var t in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)))rows.Add("TERRAIN "+t.name+" pos="+t.transform.position+" size="+t.terrainData.size+" asset="+AssetDatabase.GetAssetPath(t.terrainData));
 foreach(var r in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.name.ToLower().Contains("water")||r.name.ToLower().Contains("ocean")||r.name.ToLower().Contains("seabed")))rows.Add("WATER "+r.name+" active="+r.gameObject.activeInHierarchy+" enabled="+r.enabled+" bounds="+r.bounds+" mesh="+AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>()?.sharedMesh));
 }
 Directory.CreateDirectory("Validation/Coast17");File.WriteAllLines("Validation/Coast17/inventory.txt",rows);result.Log("Inventory saved");
 EditorSceneManager.OpenScene("Assets/Scenes/Enroth_Linked_OpenWorld.unity",OpenSceneMode.Single);
} }

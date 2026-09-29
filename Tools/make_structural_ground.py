from pathlib import Path
p=Path('Tools/StructuralCleanup.cs').read_text()
head=p[:p.index(' public void Execute')]
head=head.replace('string Snapshot(Scene s)', 'HashSet<string> allowed=new HashSet<string>();\n string Snapshot(Scene s)')
head=head.replace('t.localPosition.ToString("R")+"|"+t.position.ToString("R")','(allowed.Contains(Key(t))?new Vector3(t.localPosition.x,0,t.localPosition.z):t.localPosition).ToString("R")+"|"+new Vector2(t.position.x,t.position.z).ToString("R")')
body=r'''
 public void Execute(ExecutionResult result){
 for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Unsaved scene");
 var report=new List<string>();
 foreach(var path in Directory.GetFiles("Assets/Scenes","*.unity").Where(p=>!Path.GetFileName(p).StartsWith("_Recovery_")).OrderBy(p=>p.Contains("Enroth_Linked")?1:0).ThenBy(p=>p)){
 var s=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var ts=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var terrains=ts.Select(t=>t.GetComponent<Terrain>()).Where(t=>t).ToArray();var moves=new Dictionary<Transform,float>();
 foreach(var a in ts.Where(t=>t.parent&&(t.parent.name.StartsWith("Vegetation - Source Anchored")||t.parent.name=="Realistic Ecosystem Supplementary"||t.parent.name=="Trees"||t.parent.name=="Forest - Natural Sparse"))){if(!a.gameObject.activeInHierarchy)continue;var approved=a.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.StartsWith("ApprovedReplacement_"));var target=approved?approved:a;if(target.parent&&Vector3.Dot(target.parent.up,Vector3.up)<.99999f)continue;var rs=target.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(rs.Length==0)continue;var p=target.position;var terrain=terrains.FirstOrDefault(t=>p.x>=t.transform.position.x&&p.z>=t.transform.position.z&&p.x<=t.transform.position.x+t.terrainData.size.x&&p.z<=t.transform.position.z+t.terrainData.size.z);if(!terrain)continue;float ground=terrain.SampleHeight(p)+terrain.transform.position.y;float gap=rs.Min(r=>r.bounds.min.y)-ground;if(gap>.15f)moves[target]=gap;
 }
 allowed.Clear();foreach(var m in moves)allowed.Add(Key(m.Key));string before=Snapshot(s);var entries=new List<string>();foreach(var m in moves){entries.Add(PathOf(m.Key)+"|lowered="+m.Value+"|xz="+new Vector2(m.Key.position.x,m.Key.position.z));m.Key.position-=Vector3.up*m.Value;}
 if(Snapshot(s)!=before)throw new Exception("Protected state changed in "+s.name);
 if(moves.Count>0){string backup=Backup+"/"+Path.GetFileName(path);if(!File.Exists(backup))File.Copy(path,backup);EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");s=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);if(Snapshot(s)!=before)throw new Exception("Reload invariant failed");}
 File.WriteAllLines(Out+"/"+s.name+"_ground_corrections.txt",entries);report.Add(s.name+"|grounded="+moves.Count+"|protected_hash="+Hash(before)+"|preserved=PASS");File.WriteAllLines(Out+"/ground_corrections.txt",report);
 }
 result.Log(string.Join("\n",report));
 }
}
'''
Path('Tools/StructuralGroundFix.cs').write_text(head+body)

from pathlib import Path
s=Path('Tools/StructuralGroundFinalize.cs').read_text();a=s.index('foreach(string name');b=s.index('var mire=',a);s=s[:a]+s[b:];a=s.index('var lt=');b=s.index('if(targets.Count',a);s=s[:a]+'''var lt=linked.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>PathOf(t).ToLowerInvariant().Contains("mire")).ToArray();
var targets=new Dictionary<Transform,Vector3>();foreach(var t in lt){string p=PathOf(t);foreach(var item in positions)if(p.EndsWith(item.Key,StringComparison.Ordinal))targets[t]=item.Value;}
'''+s[b:];Path('Tools/StructuralLinkedGround.cs').write_text(s)

from pathlib import Path
p=Path(r"C:/MMUnityPort/Tools/VisualLinkedRebuildVerify.cs")
s=p.read_text()
s=s.replace(' HashSet<string> targets=new HashSet<string>();\n string Snapshot(Scene s)',
''' HashSet<string> targets=new HashSet<string>();
 string Canon(string root){if(root=="Frozen Highlands")return "White Cap / Frozen Highlands";if(root=="Hermit's Isle")return "Hermits Isle";return root;}
 string Snapshot(Scene s)''')
s=s.replace('var roots=desired.Keys.Select(k=>{int i=k.IndexOf(\'/\');return k.Substring(0,i).Split(new[]{" - "},StringSplitOptions.None)[0];}).Distinct().ToArray();',
'''var roots=desired.Keys.Select(k=>{int i=k.IndexOf('/');return Canon(k.Substring(0,i).Split(new[]{" - "},StringSplitOptions.None)[0]);}).Distinct().ToArray();''')
s=s.replace('string root=row.Key.Substring(0,i).Split(new[]{" - "},StringSplitOptions.None)[0];string rel=row.Key.Substring(i);string k=root+"|"+rel;',
'''string root=Canon(row.Key.Substring(0,i).Split(new[]{" - "},StringSplitOptions.None)[0]);string rel=row.Key.Substring(i);string k=root+"|"+rel;''')
p.write_text(s)

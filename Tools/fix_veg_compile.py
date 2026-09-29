from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMVegetationInstanceAudit.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('candidates=candidates.GroupBy(x=>x.Item1.GetInstanceID()).Select(g=>g.First()).ToList();','candidates=candidates.GroupBy(x=>x.Item1).Select(g=>g.First()).ToList();')
s=s.replace('var fail=new List<string>();if(!ori)fail.Add("ORIENTATION");if(!tex)fail.Add("TEXTURE");if(!ground)fail.Add(gap>.18f?"FLOATING":"BURIED");if(water)fail.Add("WATER");if(!biome)fail.Add("BIOME");',
'''var reasons=new List<string>();if(!ori)reasons.Add("ORIENTATION");if(!tex)reasons.Add("TEXTURE");if(!ground)reasons.Add(gap>.18f?"FLOATING":"BURIED");if(water)reasons.Add("WATER");if(!biome)reasons.Add("BIOME");''')
s=s.replace('status=fail.Count==0?"PASS":string.Join("|",fail)','status=reasons.Count==0?"PASS":string.Join("|",reasons)')
s=s.replace('int fail=data.Count(x=>x.status!="PASS"),trees=data.Count(x=>x.tree);','int failCount=data.Count(x=>x.status!="PASS"),trees=data.Count(x=>x.tree);')
s=s.replace('(data.Count-fail).ToString(),fail.ToString()','(data.Count-failCount).ToString(),failCount.ToString()')
s=s.replace('fail={fail}','fail={failCount}')
p.write_text(s,encoding="utf-8")
print("VEG_COMPILE_FIXED")
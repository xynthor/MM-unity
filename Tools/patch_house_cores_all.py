from pathlib import Path
root=Path(r'C:\MMUnityPort\Assets\Editor')
files=sorted(root.glob('Build*OpenWorld.cs'))
files=[p for p in files if p.name not in {'BuildEnrothLinkedOpenWorld.cs','BuildDragonIsleOpenWorld.cs'}]
for p in files:
    s=p.read_text(encoding='utf-8')
    old='''        n=n.ToLowerInvariant();
        return n.Contains("hse")||n.Contains("tav")||n.Contains("inn")||n.Contains("shop")||n.Contains("bank")||n.Contains("smith")||n.Contains("blacksm")||n.Contains("magic")||n.Contains("merc")||n.Contains("armory")||n.Contains("town")||n.Contains("training")||n.Contains("guild")||n.Contains("temple")||n.Contains("stable")||n.Contains("stbl")||n.Contains("stor")||n.Contains("luck")||n.Contains("keep")||n.Contains("d18")||n.Contains("thiev");
'''
    new='''        n=n.ToLowerInvariant();
        if(n.Contains("sign")||n.Contains("sgn")) return false;
        return n.Contains("house")||n.Contains("hse")||n.Contains("tav")||n.Contains("inn")||n.Contains("shop")||n.Contains("bank")||n.Contains("smith")||n.Contains("blacksm")||n.Contains("magic")||n.Contains("merc")||n.Contains("armory")||n.Contains("town")||n.Contains("training")||n.Contains("guild")||n.Contains("temple")||n.Contains("stable")||n.Contains("stbl")||n.Contains("stor")||n.Contains("luck")||n.Contains("keep")||n.Contains("d18")||n.Contains("thiev");
'''
    if old not in s:
        print('NO_ISBUILD',p.name); continue
    s=s.replace(old,new,1)
    s=s.replace('if(b.size.x<2.5f||b.size.z<2.5f||b.size.x>60f||b.size.z>60f||b.size.y<1.8f||b.size.y>45f) continue;',
                'if(b.size.x<1.8f||b.size.z<1.8f||b.size.x>60f||b.size.z>60f||b.size.y<1.8f||b.size.y>45f) continue;',1)
    p.write_text(s,encoding='utf-8')
    print('PATCHED',p.name)
print('COUNT',len(files))

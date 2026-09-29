from pathlib import Path
root=Path(r'C:\MMUnityPort\Assets\Editor')
for p in sorted(root.glob('Build*OpenWorld.cs')):
    if p.name in ('BuildEnrothLinkedOpenWorld.cs','BuildDragonIsleOpenWorld.cs'): continue
    s=p.read_text(encoding='utf-8-sig'); n=s.count('return new Vector2(x,z);'); s=s.replace('return new Vector2(x,z);','return new Vector2(x,-z);');
    if n:p.write_text(s,encoding='utf-8')
    print(p.name,'spawn',n)

from pathlib import Path
ROOT=Path(r'C:\MMUnityPort')
for zone in ['NewSorpigal','CastleIronfist']:
    p=ROOT/f'Assets/Editor/Build{zone}OpenWorld.cs'
    s=p.read_text(encoding='utf-8-sig')
    s=s.replace('const float WorldScale = 3f;','const float WorldScale = 1f;')
    s=s.replace('const float ArchitectureScale = 1.35f;','const float ArchitectureScale = 1f;')
    old='''            Bounds original=GetRendererBounds(go);\n            string key=CompositeStructureKey(go.name);\n            Vector3 pivot=key!=null?compositeBounds[key].center:original.center;\n            float objectScale=ArchitectureScale*ArchitectureScaleFactor(go.name,original);\n            go.transform.localScale=Vector3.one*objectScale;\n            go.transform.position+=new Vector3(pivot.x*(WorldScale-objectScale),0f,pivot.z*(WorldScale-objectScale));'''
    new='''            Bounds original=GetRendererBounds(go);\n            // Exact MM6 1:1 geometry. Never scale or move source architecture in X/Z.\n            go.transform.localScale=Vector3.one;'''
    if old not in s:
        print(zone,'WARNING building scale block not found')
    else:
        s=s.replace(old,new)
    if zone=='CastleIronfist':
        s=s.replace('        CompactHouseP128fMain(compositeMembers,compositeBounds);\n','')
    p.write_text(s,encoding='utf-8')
    print('1x1',zone)

p=ROOT/'Assets/Editor/BuildEnrothLinkedOpenWorld.cs'
s=p.read_text(encoding='utf-8-sig')
s=s.replace('const float RegionSize=1536f;','const float RegionSize=512f;')
s=s.replace('streamer.edge=735f;','streamer.edge=245f;')
s=s.replace('streamer.inset=58f;','streamer.inset=20f;')
p.write_text(s,encoding='utf-8')
print('linked 512m regions')

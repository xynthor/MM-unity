from pathlib import Path
root=Path(r'C:\MMUnityPort\Assets\Editor')
files=sorted(root.glob('Build*OpenWorld.cs'))
files=[p for p in files if p.name not in {'BuildNewSorpigalOpenWorld.cs','BuildCastleIronfistOpenWorld.cs','BuildEnrothLinkedOpenWorld.cs','BuildDragonIsleOpenWorld.cs'}]
old='''            if (placement!=null && b.size.y>0.15f && WaterAffiliated(go.name))
            {
                float surface=0.12f;
                float desiredBase=surface+placement.sourceBaseOffset*objectScale;
                go.transform.position+=Vector3.up*(desiredBase-b.min.y);
                grounded++;
                b=GetRendererBounds(go);
            }
'''
new='''            if (placement!=null && b.size.y>0.15f)
            {
                // Same 1:1 source placement rule as New Sorpigal / Castle Ironfist.
                // Preserve baked source X/Z exactly; only ground Y from the source base offset.
                float surface=WaterAffiliated(go.name)?0.12f:SampleTerrainY(terrain,b.center.x,b.center.z);
                float desiredBase=surface+placement.sourceBaseOffset*objectScale;
                go.transform.position+=Vector3.up*(desiredBase-b.min.y);
                grounded++;
                b=GetRendererBounds(go);
            }
'''
for p in files:
    s=p.read_text(encoding='utf-8')
    if old not in s:
        print('NO_GROUND_BLOCK',p.name); continue
    s=s.replace(old,new,1)
    s=s.replace('''        grounded += GroundArchitectureClusters(terrain, group.transform);\n        StampDryGroundUnderBuildings(terrain, group.transform);\n        AddArchitectureSolidCores(group.transform);''','''        // Do not cluster-shift buildings or stamp terrain. Both change the source map.\n        AddArchitectureSolidCores(group.transform);''',1)
    p.write_text(s,encoding='utf-8')
    print('PATCHED',p.name)
print('COUNT',len(files))

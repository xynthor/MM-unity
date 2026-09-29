from pathlib import Path
root=Path(r"C:\MMUnityPort")
files=[
root/"Assets/Editor/MMWitcherTerrainPass.cs",
root/"Assets/Editor/MMEcotoneSeamPass.cs",
root/"Assets/Editor/AuditWorldSourceExact.cs",
]
for p in files:
    s=p.read_text(encoding="utf-8-sig")
    bak=root/"Backups"/(p.stem+"_terrain_lookup_fix_20260918.cs")
    bak.write_text(s,encoding="utf-8")
    if p.name=="MMWitcherTerrainPass.cs":
        old='''        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)
                 ??sc.GetRootGameObjects().FirstOrDefault();
        if(!root)return 0;
        var t=root.GetComponentInChildren<Terrain>(true);if(!t)return 0;'''
        new='''        var t=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!t){Debug.LogError("WITCHER_TERRAIN_NO_TERRAIN "+z.key);return 0;}'''
        if old not in s: raise SystemExit("WTP lookup needle missing")
        s=s.replace(old,new,1)
    elif p.name=="MMEcotoneSeamPass.cs":
        old='''        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)
                 ??sc.GetRootGameObjects().First();
        var t=root.GetComponentInChildren<Terrain>(true);if(!t)throw new Exception(z.key+" terrain missing");'''
        new='''        var t=sc.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!t)throw new Exception(z.key+" terrain missing");'''
        if old not in s: raise SystemExit("Ecotone lookup needle missing")
        s=s.replace(old,new,1)
    else:
        old='''        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var terrain=root.GetComponentInChildren<Terrain>(true);if(!terrain)throw new Exception(z.key+" terrain missing");
        var arch=root.transform.Find("Architecture - MM6 Original Layout Expanded");if(!arch)throw new Exception(z.key+" architecture missing");'''
        new='''        var roots=sc.GetRootGameObjects();
        var terrain=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!terrain)throw new Exception(z.key+" terrain missing");
        Transform arch=null;
        foreach(var rr in roots)
        {
            arch=rr.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Architecture - MM6 Original Layout Expanded");
            if(arch)break;
        }
        if(!arch)throw new Exception(z.key+" architecture missing");'''
        if old not in s: raise SystemExit("Audit lookup needle missing")
        s=s.replace(old,new,1)
    p.write_text(s,encoding="utf-8")
    print("PATCHED",p.name)

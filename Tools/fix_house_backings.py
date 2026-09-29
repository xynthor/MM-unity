from pathlib import Path
root=Path(r'C:\MMUnityPort\Assets\Editor')
files=sorted(root.glob('Build*OpenWorld.cs'))
files=[p for p in files if p.name not in {'BuildEnrothOpenWorld.cs','BuildEnrothFullWorld.cs'} and 'AllMM6' not in p.name]
new=r'''    static void AddArchitectureSolidCores(Transform root)
    {
        var mat=ArchitectureCoreMaterial(); int ncore=0;
        foreach(Transform child in root)
        {
            string n=child.name.ToLowerInvariant();
            if(n.StartsWith("solidcore_")) continue;
            bool building=n.Contains("hse")||n.Contains("tav")||n.Contains("inn")||n.Contains("shop")||n.Contains("bank")||n.Contains("smith")||n.Contains("magic")||n.Contains("merc")||n.Contains("armory")||n.Contains("town")||n.Contains("training")||n.Contains("guild")||n.Contains("temple")||n.Contains("stable")||n.Contains("stor")||n.Contains("luck")||n.Contains("keep")||n.Contains("d18")||n.Contains("thiev");
            if(!building) continue;
            Bounds b=GetRendererBounds(child.gameObject);
            if(b.size.x<4f||b.size.z<4f||b.size.x>38f||b.size.z>38f||b.size.y<2f||b.size.y>24f) continue;
            var core=GameObject.CreatePrimitive(PrimitiveType.Cube); core.name="SolidCore_"+(ncore++); core.transform.SetParent(root,true);
            float sx=Mathf.Max(1.5f,b.size.x*.48f), sz=Mathf.Max(1.5f,b.size.z*.48f), sy=Mathf.Max(1.2f,b.size.y*.52f);
            core.transform.position=new Vector3(b.center.x,b.min.y+sy*.52f,b.center.z);
            core.transform.localScale=new Vector3(sx,sy,sz);
            core.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
        Debug.Log("ARCHITECTURE_INTERIOR_BACKINGS "+ncore);
    }
'''
def replace_method(s, sig, repl):
    i=s.index(sig); b=s.index('{',i); d=0; j=b
    while j<len(s):
        if s[j]=='{': d+=1
        elif s[j]=='}':
            d-=1
            if d==0: return s[:i]+repl+s[j+1:]
        j+=1
    raise RuntimeError(sig)

for p in files:
    s=p.read_text(encoding='utf-8')
    s=replace_method(s,'    static void AddArchitectureSolidCores(Transform root)',new)
    old='bool explicitSmall=n.Contains("house")||n.Contains("hse")||n.Contains("tav")||n.Contains("inn")||n.Contains("shop")||n.Contains("bank")||n.Contains("smith")||n.Contains("temple")||n.Contains("guild")||n.Contains("fountain")||n.Contains("well")||n.Contains("stable");'
    newscale='bool explicitSmall=n.Contains("house")||n.Contains("hse")||n.Contains("fountain")||n.Contains("well");'
    if old in s:s=s.replace(old,newscale)
    p.write_text(s,encoding='utf-8')
print('patched',len(files),'builders')

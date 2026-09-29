from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMReferenceEcosystemPass.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('static GameObject[] Load(params string[] paths)=>paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).Where(x=>x).ToArray();',
'''static GameObject[] Load(params string[] paths)
    {
        return paths.Select(p=>AssetDatabase.LoadAssetAtPath<GameObject>(p)).Where(x=>x!=null).ToArray();
    }''')
p.write_text(s,encoding="utf-8")
print("ECOSYSTEM_LOAD_FIXED")
from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticVegetationPass.cs")
s=p.read_text(encoding="utf-8-sig")
old='''    static GameObject Load(string p)=>AssetDatabase.LoadAssetAtPath<GameObject>(p);
    static GameObject[] LoadMany(params string[] p)=>p.Select(Load).Where(x=>x).ToArray();'''
new='''    static GameObject Load(string p)
    {
        var g=AssetDatabase.LoadAssetAtPath<GameObject>(p);
        if(g)return g;
        return AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();
    }
    static GameObject[] LoadMany(params string[] p)=>p.Select(Load).Where(x=>x).Distinct().ToArray();'''
if old not in s: raise SystemExit("Load block missing")
s=s.replace(old,new,1)
s=s.replace('''    public static void ApplyAll()
    {
        var green=GreenPool();''','''    public static void ApplyAll()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        var green=GreenPool();''',1)
p.write_text(s,encoding="utf-8")
print("VEG_LOAD_ROBUST_PATCHED")
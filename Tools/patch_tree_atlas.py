from pathlib import Path
root = Path(r"C:\MMUnityPort\Assets\Editor")
files = [
    root / "BuildNewSorpigalOpenWorld.cs",
    root / "BuildCastleIronfistOpenWorld.cs",
    root / "BuildMistyIslandsOpenWorld.cs",
]
for p in files:
    s = p.read_text(encoding="utf-8")
    s = s.replace(
        "public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves;",
        "public Material waterMaterial, grassMaterial, rockMaterial, treeBark, treeLeaves, treeAtlas;"
    )
    needle = 'e.treeLeaves = CreateSimpleMaterial("TreeLeaves", Shader.Find("Standard"), new Color(0.18f,0.48f,0.13f));'
    repl = needle + '\n        e.treeAtlas = CreateTexturedMaterial("TreeAtlas", "Assets/EnvironmentAssets/Gobkit/TreeAtlas.png", 0.03f);'
    s = s.replace(needle, repl)
    start = s.index("    static void AssignTreeMaterials(GameObject go, EnvAssets e)")
    end = s.index("    static byte[] tileCache;", start)
    new_method = '''    static void AssignTreeMaterials(GameObject go, EnvAssets e)\n    {\n        foreach (var r in go.GetComponentsInChildren<Renderer>(true))\n        {\n            var src = r.sharedMaterials;\n            int n = (src == null || src.Length == 0) ? 1 : src.Length;\n            var dst = new Material[n];\n            for (int i=0;i<n;i++) dst[i] = e.treeAtlas ? e.treeAtlas : e.treeLeaves;\n            r.sharedMaterials = dst;\n            r.shadowCastingMode = ShadowCastingMode.On;\n            r.receiveShadows = true;\n        }\n    }\n\n'''
    s = s[:start] + new_method + s[end:]
    p.write_text(s, encoding="utf-8")
    print("patched", p.name)

from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\ZZRestoreOldGoodTerrainOnly_20260919.cs")
s=p.read_text(encoding="utf-8-sig")
needle='''    [InitializeOnLoadMethod]
    static void Init(){EditorApplication.delayCall+=Go;}

    static void Go()'''
repl='''    [InitializeOnLoadMethod]
    static void Init(){EditorApplication.delayCall+=Go;}

    [MenuItem("MMUnity/Locked/Restore Old Good Terrain _F6")]
    public static void RunNow(){Go();}

    static void Go()'''
if needle not in s: raise SystemExit("needle")
p.write_text(s.replace(needle,repl,1),encoding="utf-8")
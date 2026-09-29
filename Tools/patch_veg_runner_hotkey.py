from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\ZZLockedVegetationOnly_20260919.cs")
s=p.read_text(encoding="utf-8-sig")
needle='''    static ZZLockedVegetationOnly_20260919(){EditorApplication.delayCall+=Go;}
    static void Go()'''
repl='''    static ZZLockedVegetationOnly_20260919(){EditorApplication.delayCall+=Go;}
    [MenuItem("MMUnity/Locked/Vegetation Only _F7")]
    public static void RunNow(){Go();}
    static void Go()'''
if needle not in s: raise SystemExit("needle missing")
p.write_text(s.replace(needle,repl,1),encoding="utf-8")
print("VEG_RUNNER_HOTKEY_ADDED")
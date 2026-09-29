from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\ZZLockedWaterSurfaceOnly_20260919.cs")
s=p.read_text(encoding="utf-8-sig")
if '[MenuItem("MMUnity/Locked/Water Surface Only _F8")]' not in s:
    s=s.replace('''    [InitializeOnLoadMethod]
    static void Init(){EditorApplication.delayCall+=Go;}

    static void Go()''',
'''    [InitializeOnLoadMethod]
    static void Init(){EditorApplication.delayCall+=Go;}

    [MenuItem("MMUnity/Locked/Water Surface Only _F8")]
    public static void RunNow(){Go();}

    static void Go()''')
p.write_text(s,encoding="utf-8")
print("WATER_HOTKEY_ADDED")
from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\ZZLockedSeamAuditOnly_20260919.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('''    static void Init(){EditorApplication.delayCall+=Go;}
    static void Go()''','''    static void Init(){EditorApplication.delayCall+=Go;}
    [MenuItem("MMUnity/Locked/Seam Audit Only _F6")]
    public static void RunNow(){Go();}
    static void Go()''')
p.write_text(s,encoding="utf-8")
print("SEAM_HOTKEY_ADDED")
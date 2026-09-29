from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMImmediateCommands.cs")
s=p.read_text(encoding="utf-8-sig")
start=s.index('    [MenuItem("MMUnity/Restore Old Good Terrain _F8")]')
new='''    [MenuItem("MMUnity/Restore Old Good Sorpigal Vegetation _F8")]
    public static void ReferenceWorldFix()
    {
        MMRestoreOldGoodSorpigalVegetation.Apply();
        Debug.Log("IMMEDIATE_OLD_GOOD_SORPIGAL_VEGETATION_DONE");
    }
}'''
s=s[:start]+new
p.write_text(s,encoding="utf-8")
print("F8_SORPIGAL_VEG_READY")
from pathlib import Path
import shutil
p=Path(r"C:\MMUnityPort\Assets\Editor\MMImmediateCommands.cs")
shutil.copy2(p,Path(r"C:\MMUnityPort\Backups\MMImmediateCommands_pre_oldgoodterrain_20260919.cs"))
s=p.read_text(encoding="utf-8-sig")
start=s.index('    [MenuItem("MMUnity/Locked Seam Audit _F8")]')
end=s.index('\n    }\n}',start)+6
new='''    [MenuItem("MMUnity/Restore Old Good Terrain _F8")]
    public static void ReferenceWorldFix()
    {
        MMWorldPositionFreezeAudit.Snapshot();
        MMRealisticTerrainBiomePass.ApplyAll();
        MMRealisticSeamBlendPass.ApplyAll();
        MMSixTerrainAudit.Run();
        MMWorldPositionFreezeAudit.Verify();
        CaptureAuditScreens.Run();
        Debug.Log("IMMEDIATE_OLD_GOOD_TERRAIN_DONE");
    }
}'''
s=s[:start]+new
p.write_text(s,encoding="utf-8")
print("F8_TARGETED_TERRAIN_READY")
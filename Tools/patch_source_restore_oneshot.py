from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\ZZLockedSourceDecorationRestore_20260921.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace("MMSourceDecorationPlacementAudit.Run();","MMSourceDecorationRestorePass.AuditAll();")
p.write_text(s,encoding="utf-8")
print("ONESHOT_PATCHED")

from pathlib import Path
import shutil
root=Path(r"C:\MMUnityPort")
for name in ("rebuild_textured_new_sorpigal_objects_v2","rebuild_textured_castle_ironfist_objects_v2"):
    shutil.copy2(root/"Backups"/(name+"_pre_xfix_20260918.py"),root/"Tools"/(name+".py"))
print("EXPORTERS_RESTORED_PRE_XFIX")

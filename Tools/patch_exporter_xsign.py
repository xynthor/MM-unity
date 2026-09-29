from pathlib import Path
import shutil
root=Path(r"C:\MMUnityPort")
for name in ["rebuild_textured_new_sorpigal_objects_v2.py","rebuild_textured_castle_ironfist_objects_v2.py"]:
 p=root/"Tools"/name
 shutil.copy2(p,root/"Backups"/(p.stem+"_pre_xfix_20260918.py"))
 s=p.read_text(encoding="utf-8")
 old='out.append(f"v {-x*SCALE:.6f} {z*SCALE:.6f} {y*SCALE:.6f}")'
 new='out.append(f"v {x*SCALE:.6f} {z*SCALE:.6f} {y*SCALE:.6f}")'
 if old not in s: raise SystemExit(f"needle missing {name}")
 p.write_text(s.replace(old,new,1),encoding="utf-8")
 print("PATCHED",name)

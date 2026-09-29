from pathlib import Path
files=[
 Path(r'C:\MMUnityPort\Tools\rebuild_textured_new_sorpigal_objects_v2.py'),
 Path(r'C:\MMUnityPort\Tools\rebuild_textured_castle_ironfist_objects_v2.py')]
old='out.append(f"v {x*SCALE:.6f} {z*SCALE:.6f} {y*SCALE:.6f}")'
new='out.append(f"v {-x*SCALE:.6f} {z*SCALE:.6f} {y*SCALE:.6f}")'
for p in files:
    s=p.read_text(encoding='utf-8-sig')
    if old not in s:
        raise SystemExit(f'pattern missing in {p}')
    p.write_text(s.replace(old,new),encoding='utf-8')
    print('patched',p)

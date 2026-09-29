from pathlib import Path

ci_exp=Path(r'C:\MMUnityPort\Tools\rebuild_textured_castle_ironfist_objects_v2.py')
s=ci_exp.read_text(encoding='utf-8-sig')
s=s.replace('out.append(f"v {-x*SCALE:.6f} {z*SCALE:.6f} {-y*SCALE:.6f}")','out.append(f"v {x*SCALE:.6f} {z*SCALE:.6f} {-y*SCALE:.6f}")')
ci_exp.write_text(s,encoding='utf-8')

ci=Path(r'C:\MMUnityPort\Assets\Editor\BuildCastleIronfistOpenWorld.cs')
s=ci.read_text(encoding='utf-8-sig')
s=s.replace('return new Vector2(origin+sx*step,origin+sy*step);','return new Vector2(origin+(N-1f-sx)*step,origin+sy*step);')
s=s.replace('float sx=(x-origin)/step;\n        float sy=(z-origin)/step;','float sx=(N-1f)-(x-origin)/step;\n        float sy=(z-origin)/step;')
s=s.replace('float sx=x/(float)hmMax*(N-1),sy=y/(float)hmMax*(N-1);','float sx=(N-1)-x/(float)hmMax*(N-1),sy=y/(float)hmMax*(N-1);')
s=s.replace('float sx=x/511f*(N-1),sy=y/511f*(N-1);','float sx=(N-1)-x/511f*(N-1),sy=y/511f*(N-1);')
ci.write_text(s,encoding='utf-8')
print('patched Castle Ironfist to canonical source orientation')
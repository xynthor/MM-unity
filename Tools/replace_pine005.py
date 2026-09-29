from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\MMSourceGridExactPostPass.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('Pine_005/Pine_005_01.FBX','Pine_004_new/pine_004_4.prefab')
s=s.replace('Pine_005/Pine_005_01.prefab','Pine_004_new/pine_004_5.prefab')
p.write_text(s,encoding='utf-8')
print('Pine_005 removed from exact postpass')
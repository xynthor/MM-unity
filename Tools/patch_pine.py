from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\MMSourceGridExactPostPass.cs')
s=p.read_text(encoding='utf-8')
old='p.Contains("treehigh003") ||'
new='p.Contains("treehigh003") || p.Contains("pine_005") ||'
assert old in s
p.write_text(s.replace(old,new,1),encoding='utf-8')
print('patched pine_005 global cleanup')

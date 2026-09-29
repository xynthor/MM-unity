from pathlib import Path
p=Path(r'C:\MMUnityPort\Tools\apply_biome_overhaul.py')
s=p.read_text(encoding='utf-8')
s=s.replace("'NewSorpigal':31.75}def tint","'NewSorpigal':31.75}\n\ndef tint")
s=s.replace("print('pine rgba generated')for region","print('pine rgba generated')\n\nfor region")
s=s.replace("print('basic constants and biome layers patched')for region","print('basic constants and biome layers patched')\n\nfor region")
s=s.replace("print('tree variety assets patched')for idx","print('tree variety assets patched')\n\nfor idx")
p.write_text(s,encoding='utf-8')
print('fixed')
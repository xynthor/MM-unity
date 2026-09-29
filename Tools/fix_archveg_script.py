from pathlib import Path
p=Path(r'C:\MMUnityPort\Tools\apply_arch_veg_fix.py')
s=p.read_text(encoding='utf-8')
s=s.replace("print('asset fields/headroom patched')for region","print('asset fields/headroom patched')\n\nfor region")
s=s.replace("print('architecture safety/two-sided patch applied')for region","print('architecture safety/two-sided patch applied')\n\nfor region")
s=s.replace("print('biome tree selection/density patched')for region","print('biome tree selection/density patched')\n\nfor region")
p.write_text(s,encoding='utf-8')
print('fixed append boundaries')
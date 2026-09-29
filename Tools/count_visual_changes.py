from pathlib import Path
from collections import Counter
rows={}
for p in Path('Validation/VisualRefinement').glob('*_material_changes.txt'):
 for line in p.read_text().splitlines():
  a,b=line.split('|');rows[a]=b
print('renderer changes',len(rows))
c=Counter(p.split('/')[0] for p in rows)
for n,v in sorted(c.items()): print(n,v)
print('regions',len(c))
print('tree roots',len({p.rsplit('/',1)[0] for p in rows}))

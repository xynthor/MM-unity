from pathlib import Path
import csv
root=Path(r'C:\MMUnityPort')
for csvf in sorted((root/'Validation'/'SourceExactAudit').glob('*_buildings.csv')):
    zone=csvf.stem.replace('_buildings','')
    rows=list(csv.DictReader(open(csvf,encoding='utf-8-sig')))
    bad=[r for r in rows if r.get('is_house')=='1' and r.get('has_core')=='0' and 'sign' not in r['name'].lower() and 'sgn' not in r['name'].lower()]
    if not bad: continue
    objdir=root/'Assets'/'World'/zone/'Objects'
    print('\n'+zone,len(bad))
    for r in bad:
        p=objdir/(r['name']+'.obj')
        if not p.exists():
            print(' NOOBJ',r['name']); continue
        xs=[];ys=[];zs=[]
        for line in open(p,errors='ignore'):
            if line.startswith('v '):
                q=line.split(); xs.append(float(q[1])); ys.append(float(q[2])); zs.append(float(q[3]))
        print(f" {r['name']}: sx={max(xs)-min(xs):.2f} sy={max(ys)-min(ys):.2f} sz={max(zs)-min(zs):.2f}")

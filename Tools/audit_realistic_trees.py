import csv,collections,math
p=r'C:\MMUnityPort\Validation\RealisticTreePlacementAudit.csv'
rows=list(csv.DictReader(open(p,encoding='utf-8')))
print('rows',len(rows))
for z in sorted(set(r['zone'] for r in rows)):
    rs=[r for r in rows if r['zone']==z]
    placed=[r for r in rs if r['prefab']!='NONE']
    fail=[r for r in rs if r['status'].startswith('FAILED')]
    supplements=[r for r in placed if r['kind']=='SUPPLEMENTARY']
    src=[r for r in placed if r['kind']=='SOURCE']
    uniq=len(set(r['prefab'] for r in placed))
    cnt=collections.Counter(r['prefab'] for r in placed)
    maxshare=(max(cnt.values())/len(placed)*100) if placed else 0
    maxdx=max([abs(float(r['dx'])) for r in placed] or [0])
    maxdz=max([abs(float(r['dz'])) for r in placed] or [0])
    maxrx=max([abs(float(r['rootRotX'])) for r in placed] or [0])
    maxrz=max([abs(float(r['rootRotZ'])) for r in placed] or [0])
    minratio=min([float(r['verticalRatio']) for r in placed] or [999])
    maxgap=max([abs(float(r['groundGap'])) for r in placed] or [0])
    conflicts=sum(1 for r in src if 'ROAD_OR_WATER' in r['status'])
    suppfail=sum(1 for r in supplements if r['status']!='PASS')
    print(z,'placed',len(placed),'src',len(src),'supp',len(supplements),'uniq',uniq,'maxshare',round(maxshare,1),
          'fail',len(fail),'suppfail',suppfail,'sourceConflicts',conflicts,'maxdx',maxdx,'maxdz',maxdz,
          'maxRotXZ',max(maxrx,maxrz),'minRatio',round(minratio,3),'maxGap',round(maxgap,4))
print('FAILED',sum(1 for r in rows if r['status'].startswith('FAILED')))
print('NONE',sum(1 for r in rows if r['prefab']=='NONE'))

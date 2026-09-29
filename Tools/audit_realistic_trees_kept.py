import csv,collections,math
rows=list(csv.DictReader(open(r'C:\MMUnityPort\Validation\RealisticTreePlacementAudit.csv',encoding='utf-8')))
for z in sorted(set(r['zone'] for r in rows)):
    rs=[r for r in rows if r['zone']==z]
    kept=[r for r in rs if not r['status'].startswith('FAILED') and r['prefab']!='NONE']
    supp=[r for r in kept if r['kind']=='SUPPLEMENTARY']
    src=[r for r in kept if r['kind']=='SOURCE']
    cnt=collections.Counter(r['prefab'] for r in kept)
    uniq=len(cnt);maxshare=(max(cnt.values())/len(kept)*100 if kept else 0)
    badrot=sum(1 for r in kept if abs(float(r['rootRotX']))>.1 or abs(float(r['rootRotZ']))>.1)
    badcoord=sum(1 for r in kept if abs(float(r['dx']))>.01 or abs(float(r['dz']))>.01)
    badratio=sum(1 for r in kept if (('shrub' not in r['prefab'].lower() and 'cactus' not in r['prefab'].lower() and 'quiver_tree' not in r['prefab'].lower()) and float(r['verticalRatio'])<.70))
    badground=sum(1 for r in kept if abs(float(r['groundGap']))>.08)
    print(z,'kept',len(kept),'src',len(src),'supp',len(supp),'uniq',uniq,'maxshare',round(maxshare,1),
          'badrot',badrot,'badcoord',badcoord,'badratio',badratio,'badground',badground,
          'sourceConflicts',sum(1 for r in src if 'ROAD_OR_WATER' in r['status']))
print('kept',sum(1 for r in rows if not r['status'].startswith('FAILED') and r['prefab']!='NONE'))

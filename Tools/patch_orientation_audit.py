from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\AuditOneToOneWorld.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('int missing=0,posBad=0,scaleBad=0,coreMissing=0;',
            'int missing=0,posBad=0,scaleBad=0,rotBad=0,coreMissing=0;')
old='''            if(t.localScale!=Vector3.one){scaleBad++;bad.Add($"{z.key},SCALE,{n},{t.localScale}");}'''
new=old+'''\n            if(Quaternion.Angle(t.localRotation,Quaternion.identity)>.01f){rotBad++;bad.Add($"{z.key},ROTATION,{n},{t.localRotation.eulerAngles}");}'''
s=s.replace(old,new)
s=s.replace('posBad={posBad},scaleBad={scaleBad},coreMissing={coreMissing}',
            'posBad={posBad},scaleBad={scaleBad},rotBad={rotBad},coreMissing={coreMissing}')
p.write_text(s,encoding='utf-8')
print('orientation audit patched')
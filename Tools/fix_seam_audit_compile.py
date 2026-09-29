from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticSeamAudit.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('''float ar=p.horizontal?aa[i,aw-1,road]:aa[ah-1,i,road];
                float br=p.horizontal?bb[i,0,road]:bb[0,i,road];
                if(ar>.45f||br>.45f){roadSamples++;continue;}''',
'''float aRoad=p.horizontal?aa[i,aw-1,road]:aa[ah-1,i,road];
                float bRoad=p.horizontal?bb[i,0,road]:bb[0,i,road];
                if(aRoad>.45f||bRoad>.45f){roadSamples++;continue;}''')
p.write_text(s,encoding="utf-8")
print("SEAM_AUDIT_COMPILE_FIXED")
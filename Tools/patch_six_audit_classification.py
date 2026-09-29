from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMSixTerrainAudit.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('''var summary=new List<string>{"zone,cells,green,light_green,desert,volcanic,snow,arid,road,unsupported,extra_layers"};''',
'''var summary=new List<string>{"zone,cells,green,light_green,desert,volcanic,snow,arid,road,low_support,ecotone,micropatch_smoothed,real_fail,extra_layers"};''')
s=s.replace('''int[] count=new int[7];int unsupported=0,extra=0;''',
'''int[] count=new int[7];int lowSupport=0,ecotone=0,micropatch=0,realFail=0,extra=0;''')
s=s.replace('''            bool bad=ew<.22f; if(bad)unsupported++;
            if(a.GetLength(2)!=7)extra++;
            rows.Add(string.Join(",",new[]{sx.ToString(),sy.ToString(),g.ToString(),f.ToString(),MMRealisticTerrainBiomePass.ClassName(exp),MMRealisticTerrainBiomePass.ClassName(dom),F(ew),
                F(a[ay,ax,0]),F(a[ay,ax,1]),F(a[ay,ax,2]),F(a[ay,ax,3]),F(a[ay,ax,4]),F(a[ay,ax,5]),F(a[ay,ax,6]),bad?"LOW_SUPPORT":"OK"}));''',
'''            bool low=ew<.22f;string status="OK";
            if(low)
            {
                lowSupport++;
                int border=Mathf.Min(Mathf.Min(sx,127-sx),Mathf.Min(sy,127-sy));
                if(border<=6){status="ECOTONE_TRANSITION";ecotone++;}
                else
                {
                    int same=0,total=0;
                    for(int yy=Mathf.Max(0,sy-2);yy<=Mathf.Min(127,sy+2);yy++)
                    for(int xx=Mathf.Max(0,sx-2);xx<=Mathf.Min(127,sx+2);xx++)
                    {
                        byte rr=tile[yy*N+xx],gg=grp[rr],ff=sem[rr];
                        if(MMRealisticTerrainBiomePass.BaseClassAt(z.key,gg,ff,yy)==exp)same++;
                        total++;
                    }
                    if(same<=7){status="MICROPATCH_SMOOTHED";micropatch++;}
                    else{status="REAL_FAIL";realFail++;}
                }
            }
            if(a.GetLength(2)!=7)extra++;
            rows.Add(string.Join(",",new[]{sx.ToString(),sy.ToString(),g.ToString(),f.ToString(),MMRealisticTerrainBiomePass.ClassName(exp),MMRealisticTerrainBiomePass.ClassName(dom),F(ew),
                F(a[ay,ax,0]),F(a[ay,ax,1]),F(a[ay,ax,2]),F(a[ay,ax,3]),F(a[ay,ax,4]),F(a[ay,ax,5]),F(a[ay,ax,6]),status}));''')
s=s.replace('''summary.Add($"{z.key},{N*N},{count[0]},{count[1]},{count[2]},{count[3]},{count[4]},{count[5]},{count[6]},{unsupported},{extra}");
        Debug.Log($"SIX_TERRAIN {z.key} unsupported={unsupported} layers={td.alphamapLayers}");''',
'''summary.Add($"{z.key},{N*N},{count[0]},{count[1]},{count[2]},{count[3]},{count[4]},{count[5]},{count[6]},{lowSupport},{ecotone},{micropatch},{realFail},{extra}");
        Debug.Log($"SIX_TERRAIN {z.key} low={lowSupport} ecotone={ecotone} micropatch={micropatch} realFail={realFail} layers={td.alphamapLayers}");''')
p.write_text(s,encoding="utf-8")
print("SIX_TERRAIN_AUDIT_CLASSIFICATION_PATCHED")

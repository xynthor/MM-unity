from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRealisticSeamAudit.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('''var rows=new List<string>{"pair,direction,materialMeanL1,materialMaxL1,heightMeanM,heightMaxM,innerGradientMean,status"};''',
'''var rows=new List<string>{"pair,direction,materialMeanL1_nonRoad,materialMaxL1_nonRoad,roadEndpointSamples,heightMeanM,heightMaxM,innerGradientMean_nonRoad,status"};''')
old='''            float meanL=0,maxL=0,grad=0;
            for(int i=0;i<n;i++)
            {
                float l=0,g=0;
                for(int k=0;k<Mathf.Min(a.alphamapLayers,b.alphamapLayers);k++)
                {
                    float av=p.horizontal?aa[i,aw-1,k]:aa[ah-1,i,k];
                    float bv=p.horizontal?bb[i,0,k]:bb[0,i,k];
                    l+=Mathf.Abs(av-bv);
                    float ai=p.horizontal?aa[i,Mathf.Max(0,aw-9),k]:aa[Mathf.Max(0,ah-9),i,k];
                    float bi=p.horizontal?bb[i,Mathf.Min(bw-1,8),k]:bb[Mathf.Min(bh-1,8),i,k];
                    g+=Mathf.Abs(ai-av)+Mathf.Abs(bi-bv);
                }
                meanL+=l;maxL=Mathf.Max(maxL,l);grad+=g*.5f;
            }
            meanL/=Mathf.Max(1,n);grad/=Mathf.Max(1,n);'''
new='''            float meanL=0,maxL=0,grad=0;int nonRoad=0,roadSamples=0;
            int road=MMRealisticTerrainBiomePass.RoadOverlay;
            for(int i=0;i<n;i++)
            {
                float ar=p.horizontal?aa[i,aw-1,road]:aa[ah-1,i,road];
                float br=p.horizontal?bb[i,0,road]:bb[0,i,road];
                if(ar>.45f||br>.45f){roadSamples++;continue;} // source road endpoints are not biome seams
                float l=0,g=0;
                for(int k=0;k<Mathf.Min(a.alphamapLayers,b.alphamapLayers);k++)
                {
                    if(k==road)continue;
                    float av=p.horizontal?aa[i,aw-1,k]:aa[ah-1,i,k];
                    float bv=p.horizontal?bb[i,0,k]:bb[0,i,k];
                    l+=Mathf.Abs(av-bv);
                    float ai=p.horizontal?aa[i,Mathf.Max(0,aw-9),k]:aa[Mathf.Max(0,ah-9),i,k];
                    float bi=p.horizontal?bb[i,Mathf.Min(bw-1,8),k]:bb[Mathf.Min(bh-1,8),i,k];
                    g+=Mathf.Abs(ai-av)+Mathf.Abs(bi-bv);
                }
                meanL+=l;maxL=Mathf.Max(maxL,l);grad+=g*.5f;nonRoad++;
            }
            meanL/=Mathf.Max(1,nonRoad);grad/=Mathf.Max(1,nonRoad);'''
if old not in s: raise SystemExit("material audit block missing")
s=s.replace(old,new,1)
s=s.replace('''            bool bad=meanL>.035f||maxL>.16f||hm>.08f||hx>.65f||grad>.32f;''',
'''            bool bad=meanL>.035f||maxL>.16f||hm>.08f||hx>.65f||grad>.32f;''')
s=s.replace('''rows.Add($"{p.a}|{p.b},{(p.horizontal?"E-W":"N-S")},{F(meanL)},{F(maxL)},{F(hm)},{F(hx)},{F(grad)},{(bad?"FAIL":"PASS")}");''',
'''rows.Add($"{p.a}|{p.b},{(p.horizontal?"E-W":"N-S")},{F(meanL)},{F(maxL)},{roadSamples},{F(hm)},{F(hx)},{F(grad)},{(bad?"FAIL":"PASS")}");''')
p.write_text(s,encoding="utf-8")
print("SEAM_AUDIT_ROAD_AWARE_PATCHED")

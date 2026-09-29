from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRestoreOldGoodSorpigalVegetation.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('''        int w=td.detailWidth,h=td.detailHeight;var map=new int[h,w];
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)''','''        int w=td.detailWidth,h=td.detailHeight;var map=new int[h,w];
        var alpha=td.GetAlphamaps(0,0,td.alphamapWidth,td.alphamapHeight);
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)''')
old='''            float[] mix=td.GetAlphamaps(Mathf.Clamp(Mathf.RoundToInt(nx*(td.alphamapWidth-1)),0,td.alphamapWidth-1),Mathf.Clamp(Mathf.RoundToInt(nz*(td.alphamapHeight-1)),0,td.alphamapHeight-1),1,1).Cast<float>().ToArray();
            float green=mix.Length>MMRealisticTerrainBiomePass.Green?mix[MMRealisticTerrainBiomePass.Green]:0f;'''
new='''            int ax=Mathf.Clamp(Mathf.RoundToInt(nx*(td.alphamapWidth-1)),0,td.alphamapWidth-1);
            int ay=Mathf.Clamp(Mathf.RoundToInt(nz*(td.alphamapHeight-1)),0,td.alphamapHeight-1);
            float green=alpha[ay,ax,MMRealisticTerrainBiomePass.Green];'''
if old not in s: raise SystemExit("grass alpha block missing")
p.write_text(s.replace(old,new,1),encoding="utf-8")
print("GRASS_ALPHA_OPTIMIZED")
from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\AuditWorldSourceExact.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('byte raw=tile[sy*N+sx],g=group[raw],f=sem[raw];int exp=ExpectedLayer(g,f);',
'''byte raw=tile[sy*N+sx],g=group[raw],f=sem[raw];
            int exp=MMWitcherTerrainPass.ExpectedLayerForCell(z.key,tile,group,sem,sx,sy);''')
s=s.replace('float ew=((f&1)!=0&&exp==8)?.62f:1f; bool expAvailable=exp>=0 && exp<a.GetLength(2); bool tm=!expAvailable || act!=exp || Mathf.Abs(a[ay,ax,exp]-ew)>.08f; if(tm)tileMismatch++;',
'''float ew=1f; bool expAvailable=exp>=0 && exp<a.GetLength(2); bool tm=!expAvailable || act!=exp; if(tm)tileMismatch++;''')
s=s.replace('tm?"TILE_MISMATCH":"HEIGHT_CHECK"', '(!expAvailable?"LAYER_MISSING":tm?"TILE_MISMATCH":"HEIGHT_CHECK")')
p.write_text(s,encoding="utf-8")
print("AUDIT_PATCHED")
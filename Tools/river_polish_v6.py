from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')
def rep(old,new):
    global s
    c=s.count(old)
    if c!=1: raise SystemExit(f'expected 1 got {c}: {old[:90]!r}')
    s=s.replace(old,new,1)
rep('static readonly float[] RiverX={89f,91.1f,89.9f,93.0f,95.5f,98.4f,100.4f,103.5f,101.9f,104.4f,103.0f,104.7f,104f,103.8f,103.5f,103.2f,103f};','static readonly float[] RiverX={89f,92.3f,89.3f,93.7f,95.5f,98.5f,100.2f,103.5f,101.9f,104.4f,103.0f,104.7f,104f,103.8f,103.5f,103.2f,103f};')
rep('var mat=CreateTexturedMaterial("RiverBankSoil","Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png",.025f);mat.color=new Color(.42f,.38f,.25f,1f);EditorUtility.SetDirty(mat);','var mat=CreateTexturedMaterial("RiverBankSoil","Assets/EnvironmentAssets/UnitySamples/dry_soil_CH.png",.025f);mat.color=new Color(.30f,.28f,.16f,1f);if(mat.HasProperty("_Cull"))mat.SetInt("_Cull",0);EditorUtility.SetDirty(mat);')
p.write_text(s,encoding='utf-8')
print('RIVER_POLISH_V6_PATCHED')

from pathlib import Path
import shutil
root=Path(r"C:\MMUnityPort")
files=[
root/"Assets/Editor/MMGlobalOceanPass.cs",
root/"Assets/Editor/MMInternalCoastPass.cs",
root/"Assets/Editor/MMSourceGridExactPostPass.cs",
root/"Assets/Editor/MMCanonicalFinalPipeline.cs"]
for q in files:
    b=root/"Backups"/(q.stem+"_pre_witcher_water_20260918.cs")
    shutil.copy2(q,b)
    print("BACKUP",b.name)

p0=files[0]
s=p0.read_text(encoding="utf-8-sig")
s=s.replace("const float InnerX=1280f, InnerZ=768f;","const float InnerX=1344f, InnerZ=832f;")
start=s.index("    static Material OceanMaterial()")
end=s.index("    static Material SeabedMaterial()",start)
s=s[:start]+"    static Material OceanMaterial()\n    {\n        return MMUnifiedWorldWaterPass.SharedWaterMaterial();\n    }\n\n"+s[end:]
p0.write_text(s,encoding="utf-8")
p1=files[1]
s=p1.read_text(encoding="utf-8-sig")
start=s.index("    static Material WaterMaterial()")
end=s.index("    static void Emit(",start)
s=s[:start]+"    static Material WaterMaterial()\n    {\n        return MMUnifiedWorldWaterPass.SharedWaterMaterial();\n    }\n\n"+s[end:]
p1.write_text(s,encoding="utf-8")

p2=files[2]
s=p2.read_text(encoding="utf-8-sig")
needle='''    static Material SurfaceMaterial(string key,string texturePath,bool water=false)
    {
        string path=$"{LayerFolder}/{key}.mat";'''
repl='''    static Material SurfaceMaterial(string key,string texturePath,bool water=false)
    {
        if(water)return MMUnifiedWorldWaterPass.SharedWaterMaterial();
        string path=$"{LayerFolder}/{key}.mat";'''
if needle not in s:
    raise SystemExit("surface needle missing")
s=s.replace(needle,repl,1)
p2.write_text(s,encoding="utf-8")
p3=files[3]
s=p3.read_text(encoding="utf-8-sig")
needle='''        Debug.Log("FINAL_PIPELINE stage=NEIGHBOR_HEIGHT_TEXTURE_STITCH");
        MMNeighborSeamPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=ARCHITECTURE_FOOTPRINT_GROUNDING");'''
repl='''        Debug.Log("FINAL_PIPELINE stage=NEIGHBOR_HEIGHT_TEXTURE_STITCH");
        MMNeighborSeamPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=SEAM_ROAD_GRADE_REPAIR");
        MMRoadSeamGradePass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=ARCHITECTURE_FOOTPRINT_GROUNDING");'''
if needle not in s:
    raise SystemExit("canonical road needle missing")
s=s.replace(needle,repl,1)
needle2='''        Debug.Log("FINAL_PIPELINE stage=VISIBLE_TEXTURE_GATE");
        MMVisibleTextureGate.ApplyAll();

        RunAuditsOnly();'''
repl2='''        Debug.Log("FINAL_PIPELINE stage=WITCHER_UNIFIED_WATER");
        MMUnifiedWorldWaterPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=VISIBLE_TEXTURE_GATE");
        MMVisibleTextureGate.ApplyAll();

        RunAuditsOnly();'''
if needle2 not in s:
    raise SystemExit("canonical water needle missing")
s=s.replace(needle2,repl2,1)
p3.write_text(s,encoding="utf-8")
print("PATCHED_WATER_AND_ROAD_PIPELINE")

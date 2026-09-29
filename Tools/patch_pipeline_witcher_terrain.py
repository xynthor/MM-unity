from pathlib import Path
import shutil
p=Path(r"C:\MMUnityPort\Assets\Editor\MMCanonicalFinalPipeline.cs")
s=p.read_text(encoding="utf-8-sig")
shutil.copy2(p,Path(r"C:\MMUnityPort\Backups\MMCanonicalFinalPipeline_pre_witcher_terrain_20260918.cs"))
needle='''        Debug.Log("FINAL_PIPELINE stage=INTERNAL_COASTS_ROAD_PRESERVATION");
        MMInternalCoastPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=NEIGHBOR_HEIGHT_TEXTURE_STITCH");
        MMNeighborSeamPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=SEAM_ROAD_GRADE_REPAIR");'''
repl='''        Debug.Log("FINAL_PIPELINE stage=INTERNAL_COASTS_ROAD_PRESERVATION");
        MMInternalCoastPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=WITCHER_TERRAIN_MATERIALS");
        MMWitcherTerrainPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=NEIGHBOR_HEIGHT_TEXTURE_STITCH");
        MMNeighborSeamPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=WITCHER_ECOTONE_SEAMS");
        MMEcotoneSeamPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=SEAM_ROAD_GRADE_REPAIR");'''
if needle not in s: raise SystemExit("pipeline insertion point missing")
s=s.replace(needle,repl,1)
p.write_text(s,encoding="utf-8")
print("PIPELINE_WITCHER_TERRAIN_ECOTONE_ADDED")
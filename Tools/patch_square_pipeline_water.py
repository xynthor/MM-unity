from pathlib import Path
import shutil
root=Path(r"C:\MMUnityPort")
# pipeline
p=root/"Assets/Editor/MMCanonicalFinalPipeline.cs"
s=p.read_text(encoding="utf-8-sig"); shutil.copy2(p,root/"Backups/MMCanonicalFinalPipeline_pre_exact_square_20260918.cs")
s=s.replace('''        Debug.Log("FINAL_PIPELINE stage=CANONICAL_SOURCE_BIOME_TERRAIN");
        MMWitcherTerrainPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=NEIGHBOR_HEIGHT_TEXTURE_STITCH");
        MMNeighborSeamPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=SOURCE_BIOME_ECOTONE_SEAMS");
        MMEcotoneSeamPass.ApplyAll();
''','''        Debug.Log("FINAL_PIPELINE stage=NEIGHBOR_HEIGHT_TEXTURE_STITCH");
        MMNeighborSeamPass.ApplyAll();
''')
needle='''        Debug.Log("FINAL_PIPELINE stage=FINAL_ALL_ROAD_GRADE_REPAIR");
        MMRoadSeamGradePass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=WITCHER_UNIFIED_WATER");'''
repl='''        Debug.Log("FINAL_PIPELINE stage=FINAL_ALL_ROAD_GRADE_REPAIR");
        MMRoadSeamGradePass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=EXACT_SQUARE_BY_SQUARE_BIOMES");
        MMWitcherTerrainPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=WITCHER_UNIFIED_WATER");'''
if needle not in s: raise SystemExit("pipeline needle missing")
s=s.replace(needle,repl,1); p.write_text(s,encoding="utf-8")

# global ocean ring, never across linked land footprint
p=root/"Assets/Editor/MMGlobalOceanPass.cs"
s=p.read_text(encoding="utf-8-sig"); shutil.copy2(p,root/"Backups/MMGlobalOceanPass_pre_inner_hole_20260918.cs")
needle='''            float cx=(x0+x1)*.5f, cz=(z0+z1)*.5f;
            int q=v.Count;'''
repl='''            float cx=(x0+x1)*.5f, cz=(z0+z1)*.5f;
            // Internal zone water handles the linked world. The global ocean must never flood land.
            if(Mathf.Abs(cx)<InnerX && Mathf.Abs(cz)<InnerZ) continue;
            int q=v.Count;'''
if needle not in s: raise SystemExit("ocean loop needle missing")
s=s.replace(needle,repl,1)
s=s.replace('GLOBAL_OCEAN_DONE continuous=true depth=0.65..36m step=64m strips=false outer=5000m noInnerSquare=true',
            'GLOBAL_OCEAN_DONE continuousRing=true depth=0.65..36m step=64m strips=false outer=5000m innerLandHole=true')
p.write_text(s,encoding="utf-8")

# exact internal water level and safer coast contour
p=root/"Assets/Editor/MMInternalCoastPass.cs"
s=p.read_text(encoding="utf-8-sig"); shutil.copy2(p,root/"Backups/MMInternalCoastPass_pre_exact_water_20260918.cs")
s=s.replace('const int N=128; const float WaterY=.12f;','const int N=128; const float WaterY=.10f;')
s=s.replace('const int grid=256;const float step=2f,origin=-256f,th=.43f;',
            'const int grid=256;const float step=2f,origin=-256f,th=.56f;')
p.write_text(s,encoding="utf-8")
print("PIPELINE_OCEAN_WATER_PATCHED")
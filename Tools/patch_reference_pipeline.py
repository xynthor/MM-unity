from pathlib import Path
import shutil
p=Path(r"C:\MMUnityPort\Assets\Editor\MMCanonicalFinalPipeline.cs")
s=p.read_text(encoding="utf-8-sig")
shutil.copy2(p,Path(r"C:\MMUnityPort\Backups\MMCanonicalFinalPipeline_pre_reference_ecosystem_20260918.cs"))
needle='''        Debug.Log("FINAL_PIPELINE stage=EXACT_SQUARE_BY_SQUARE_BIOMES");
        MMWitcherTerrainPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=WITCHER_UNIFIED_WATER");'''
repl='''        Debug.Log("FINAL_PIPELINE stage=REFERENCE_MAP_BIOME_LOCK");
        MMWitcherTerrainPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=REFERENCE_MAP_ECOTONE_BLEND");
        MMEcotoneSeamPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=REFERENCE_MAP_ECOSYSTEMS");
        MMReferenceEcosystemPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=HARD_VERTICAL_ALL_TREES");
        MMTreeVerticalHardPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=WITCHER_UNIFIED_WATER");'''
if needle not in s: raise SystemExit("pipeline reference insertion missing")
s=s.replace(needle,repl,1)
p.write_text(s,encoding="utf-8")
print("PIPELINE_REFERENCE_ECOSYSTEM_INTEGRATED")

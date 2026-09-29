from pathlib import Path
import shutil
root=Path(r"C:\MMUnityPort")
p=root/"Assets/Editor/MMCanonicalFinalPipeline.cs"
shutil.copy2(p,root/"Backups/MMCanonicalFinalPipeline_pre_canonical_biomes_20260918.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('''        Debug.Log("FINAL_PIPELINE stage=SOURCE_BIOME_TERRAIN_MATERIALS_LOCKED");
        // Do not replace region terrain with a universal terrain material pass.

        Debug.Log("FINAL_PIPELINE stage=NEIGHBOR_HEIGHT_TEXTURE_STITCH");
        MMNeighborSeamPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=SOURCE_BIOME_SEAMS_LOCKED");
        // Do not average incompatible biome materials across region borders.
''','''        Debug.Log("FINAL_PIPELINE stage=CANONICAL_SOURCE_BIOME_TERRAIN");
        MMWitcherTerrainPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=NEIGHBOR_HEIGHT_TEXTURE_STITCH");
        MMNeighborSeamPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=SOURCE_BIOME_ECOTONE_SEAMS");
        MMEcotoneSeamPass.ApplyAll();
''')
s=s.replace('''        Debug.Log("FINAL_PIPELINE stage=SOURCE_TERRAIN_TRANSITION_POLISH");
        MMTerrainTransitionPolish.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=BIOME_IDENTITY_ECOTONES");
        MMBiomeIdentityPass.ApplyAll();

''','')
s=s.replace('''        AuditWorldSourceExact.Run();
        Debug.Log("FINAL_PIPELINE stage=LINKED_WORLD");''','''        AuditWorldSourceExact.Run();
        MMBiomeCellAudit.Run();
        MMVegetationInstanceAudit.Run();
        Debug.Log("FINAL_PIPELINE stage=LINKED_WORLD");''')
p.write_text(s,encoding="utf-8")

# fix audit cell center mapping to exact center of each 4px source cell
p=root/"Assets/Editor/MMBiomeCellAudit.cs"
s=p.read_text(encoding="utf-8-sig")
s=s.replace('''            int ax=Mathf.Clamp(Mathf.RoundToInt((sx+.5f)/N*(td.alphamapWidth-1)),0,td.alphamapWidth-1);
            int ay=Mathf.Clamp(Mathf.RoundToInt((N-1-sy+.5f)/N*(td.alphamapHeight-1)),0,td.alphamapHeight-1);''',
'''            int ax=Mathf.Clamp(sx*td.alphamapWidth/N+td.alphamapWidth/(N*2),0,td.alphamapWidth-1);
            int ay=Mathf.Clamp((N-1-sy)*td.alphamapHeight/N+td.alphamapHeight/(N*2),0,td.alphamapHeight-1);''')
p.write_text(s,encoding="utf-8")
print("PIPELINE_AND_CELL_AUDIT_PATCHED")
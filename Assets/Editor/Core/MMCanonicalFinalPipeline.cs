using UnityEditor;
using UnityEngine;
using System;

public static class MMCanonicalFinalPipeline
{
    [MenuItem("MMUnity/FINAL - Rebuild 15 Regions + Audit")]
    public static void RebuildEverything()
    {
        Debug.Log("FINAL_PIPELINE_BEGIN stage=BASE_REBUILD");
        BuildNewSorpigalOpenWorld.Build();
        BuildCastleIronfistOpenWorld.Build();
        BuildMireOfTheDamnedOpenWorld.Build();
        BuildDragonsandOpenWorld.Build();
        BuildHermitsIsleOpenWorld.Build();
        BuildMistyIslandsOpenWorld.Build();
        BuildBootlegBayOpenWorld.Build();
        BuildFreeHavenOpenWorld.Build();
        BuildBlackshireOpenWorld.Build();
        BuildParadiseValleyOpenWorld.Build();
        BuildEelInfestedWatersOpenWorld.Build();
        BuildSilverCoveOpenWorld.Build();
        BuildFrozenHighlandsOpenWorld.Build();
        BuildKriegspireOpenWorld.Build();
        BuildSweetWaterOpenWorld.Build();
        RunFinalPasses();
    }
    [MenuItem("MMUnity/FINAL - Apply Locked Passes + Audit")]
    public static void RunFinalPasses()
    {
        Debug.Log("FINAL_PIPELINE stage=REALISM_TERRAIN_VEGETATION");
        MMFinalRealismPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=INTERNAL_COASTS_ROAD_PRESERVATION");
        MMInternalCoastPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=NEIGHBOR_HEIGHT_TEXTURE_STITCH");
        MMNeighborSeamPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=ARCHITECTURE_FOOTPRINT_GROUNDING");
        MMGroundingWaterCleanup.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=SOURCE_PROP_3D_REPLACEMENT");
        MMSourceObject3DPass.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=OUTER_CURVED_SHORES");
        MMSourceGridExactPostPass.RebuildCurvedOuterShoresOnly();

        Debug.Log("FINAL_PIPELINE stage=FINAL_GROUNDING_IDEMPOTENCE");
        MMGroundingWaterCleanup.ApplyAll();

        Debug.Log("FINAL_PIPELINE stage=VISIBLE_TEXTURE_GATE");
        MMVisibleTextureGate.ApplyAll();

        RunAuditsOnly();
    }
    [MenuItem("MMUnity/FINAL - Audit Only")]
    public static void RunAuditsOnly()
    {
        Debug.Log("FINAL_PIPELINE stage=AUDIT_EVERYTHING");
        MMWorldComprehensiveAudit.Run();
        AuditTextureWorld.Run();
        AuditOneToOneWorld.Run();
        CaptureAuditScreens.Run();
        Debug.Log("FINAL_PIPELINE stage=LINKED_WORLD");
        BuildEnrothLinkedOpenWorld.Build();
        MMGlobalOceanPass.Apply();
        Debug.Log("FINAL_PIPELINE_DONE");
    }
}

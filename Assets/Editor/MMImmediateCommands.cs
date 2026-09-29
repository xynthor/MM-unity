using UnityEditor;
using UnityEngine;

public static class MMImmediateCommands
{
    [MenuItem("MMUnity/Immediate Road Seam Fix %#j")]
    public static void RoadFix()
    {
        MMRoadSeamGradePass.ApplyAll();
        MMRoadGradeAudit.Run();
        MMBridgeSupportAudit.Run();
        Debug.Log("IMMEDIATE_ROAD_FIX_DONE");
    }

    [MenuItem("MMUnity/Immediate Unified Water _F9")]
    public static void UnifiedWater()
    {
        MMUnifiedWorldWaterPass.ApplyAll();
        MMGlobalOceanPass.Apply();
        MMUnifiedWorldWaterPass.ApplyAll();
        Debug.Log("IMMEDIATE_UNIFIED_WATER_DONE");
    }

    [MenuItem("MMUnity/Sorpigal Tree Upgrade %#g")]
    public static void ReferenceWorldFix()
    {
        MMSorpigalTreeUpgrade.Run();
        Debug.Log("IMMEDIATE_SORPIGAL_TREE_UPGRADE_DONE");
    }
} 

using UnityEditor;
using UnityEngine;
using System;

public static class RebuildExactPlacement13
{
    public static void Run()
    {
        try
        {
            Debug.Log("REBUILD_EXACT_PLACEMENT_13 START");
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
            MMSourceGridExactPostPass.ApplyAll();
            BuildEnrothLinkedOpenWorld.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("REBUILD_EXACT_PLACEMENT_13 SUCCESS");
        }
        catch(Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("REBUILD_EXACT_PLACEMENT_13 FAILED");
            throw;
        }
    }
}

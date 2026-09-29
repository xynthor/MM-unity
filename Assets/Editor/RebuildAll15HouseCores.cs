using UnityEditor;
using UnityEngine;
using System;

public static class RebuildAll15HouseCores
{
    public static void Run()
    {
        try
        {
            Debug.Log("REBUILD_ALL15_HOUSE_CORES START");
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
            MMSourceGridExactPostPass.ApplyAll();
            BuildDragonIsleOpenWorld.Build();
            BuildEnrothLinkedOpenWorld.Build();
            AssetDatabase.SaveAssets();
            Debug.Log("REBUILD_ALL15_HOUSE_CORES SUCCESS");
        }
        catch(Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("REBUILD_ALL15_HOUSE_CORES FAILED");
            throw;
        }
    }
}

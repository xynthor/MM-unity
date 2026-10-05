using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MMWorldStateBatchQAAutoRun20261005
{
    const string Trigger =
        "C:/MMUnityPort/Validation/WorldState20261005/run.flag";

    static MMWorldStateBatchQAAutoRun20261005()
    {
        EditorApplication.delayCall += RunIfRequested;
    }

    static void RunIfRequested()
    {
        if (!File.Exists(Trigger) ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        File.Delete(Trigger);
        MMWorldStateBatchQA20261005.Run();
    }
}

public static class MMWorldStateBatchQA20261005
{
    const string Root = "C:/MMUnityPort/Validation/WorldState20261005";
    const string Result = Root + "/results.txt";
    const string Error = Root + "/error.txt";
    const string SavePath = Root + "/collected-save.json";

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(Result)) File.Delete(Result);
        if (File.Exists(Error)) File.Delete(Error);
        if (File.Exists(SavePath)) File.Delete(SavePath);

        GameObject player = null;
        GameObject pickupGo = null;

        try
        {
            player = new GameObject("MM_WorldStateQA_Player");

            MMInventory inventory =
                player.AddComponent<MMInventory>();
            inventory.showInventory = false;
            inventory.EditorClearForQa();

            MMQuestManager quests =
                player.AddComponent<MMQuestManager>();
            quests.showTracker = false;
            quests.showJournal = false;
            quests.EditorClearForQa();

            MMWorldState world =
                player.AddComponent<MMWorldState>();
            world.EditorClearForQa();

            MMSaveSystem save =
                player.AddComponent<MMSaveSystem>();
            save.savePlayerTransform = false;

            bool flagApiPass =
                world.SetFlag("door.qa.open", true) &&
                world.HasFlag("door.qa.open") &&
                !world.SetFlag("door.qa.open", true) &&
                world.SetFlag("door.qa.open", false) &&
                !world.HasFlag("door.qa.open") &&
                world.FlagCount == 0;

            pickupGo = new GameObject(
                "MM_WorldStateQA_Pickup");
            pickupGo.AddComponent<SphereCollider>();

            MMItemPickup pickup =
                pickupGo.AddComponent<MMItemPickup>();
            pickup.itemId = "qa.relic";
            pickup.displayName = "Persistent Relic";
            pickup.quantity = 1;
            pickup.collectOnce = true;
            pickup.deactivateOnCollect = true;
            pickup.persistCollected = true;
            pickup.persistentId = "relic.001";

            // Batch mode does not invoke normal runtime Awake on these
            // temporary objects, so apply the empty persistent state once
            // to capture the authored active state before collection.
            pickup.ApplyPersistentState(world);

            MMSaveData before = save.Capture();

            bool beforePass =
                before.worldFlags.Count == 0 &&
                inventory.GetCount("qa.relic") == 0 &&
                pickupGo.activeSelf &&
                pickup.CanInteract(player);

            pickup.Interact(player);

            string flagId = pickup.PersistentFlagId;
            bool collectedPass =
                flagId == "pickup.relic.001" &&
                world.HasFlag(flagId) &&
                inventory.GetCount("qa.relic") == 1 &&
                pickup.CollectedCount == 1 &&
                !pickupGo.activeSelf;

            MMSaveData collected = save.Capture();
            bool capturedFlagPass =
                collected.worldFlags.Count == 1 &&
                collected.worldFlags.Contains(flagId) &&
                collected.inventory.Count == 1 &&
                collected.inventory[0].id == "qa.relic" &&
                collected.inventory[0].quantity == 1;

            save.Restore(before);

            bool earlierRestorePass =
                !world.HasFlag(flagId) &&
                inventory.GetCount("qa.relic") == 0 &&
                pickupGo.activeSelf &&
                pickup.CollectedCount == 0 &&
                pickup.CanInteract(player);

            save.Restore(collected);

            bool collectedRestorePass =
                world.HasFlag(flagId) &&
                inventory.GetCount("qa.relic") == 1 &&
                !pickupGo.activeSelf &&
                pickup.CollectedCount == 1 &&
                !pickup.CanInteract(player);

            bool saveFilePass =
                save.SaveToPath(SavePath) &&
                File.Exists(SavePath);

            // Mutate back to pre-collection state, then prove file load
            // reapplies both logical inventory and world object state.
            world.ReplaceFlags(null);
            inventory.RemoveItem("qa.relic", 1);
            pickup.ApplyPersistentState(world);

            bool mutationPass =
                !world.HasFlag(flagId) &&
                inventory.GetCount("qa.relic") == 0 &&
                pickupGo.activeSelf;

            bool fileLoadReturned =
                save.LoadFromPath(SavePath);

            bool fileLoadPass =
                fileLoadReturned &&
                world.HasFlag(flagId) &&
                inventory.GetCount("qa.relic") == 1 &&
                !pickupGo.activeSelf &&
                pickup.CollectedCount == 1;

            world.SetFlag("gate.qa.unlocked", true);
            MMSaveData generic = save.Capture();
            bool genericFlagPass =
                generic.worldFlags.Contains(flagId) &&
                generic.worldFlags.Contains("gate.qa.unlocked") &&
                generic.worldFlags.Count == 2;

            bool pass =
                flagApiPass &&
                beforePass &&
                collectedPass &&
                capturedFlagPass &&
                earlierRestorePass &&
                collectedRestorePass &&
                saveFilePass &&
                mutationPass &&
                fileLoadPass &&
                genericFlagPass;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "flag_api|pass=" + flagApiPass,
                    "pre_collection|pass=" + beforePass +
                    "|flags=0|relic=0|active=True",
                    "collection|pass=" + collectedPass +
                    "|flag=" + flagId +
                    "|relic=1|active=False",
                    "capture_flag|pass=" + capturedFlagPass +
                    "|flags=" + collected.worldFlags.Count,
                    "restore_earlier_save|pass=" +
                    earlierRestorePass +
                    "|relic=0|active=True|collectedCount=0",
                    "restore_collected_state|pass=" +
                    collectedRestorePass +
                    "|relic=1|active=False|collectedCount=1",
                    "save_file|pass=" + saveFilePass,
                    "mutation_to_uncollected|pass=" + mutationPass,
                    "file_load_collected|pass=" + fileLoadPass,
                    "generic_flag_persistence|pass=" +
                    genericFlagPass +
                    "|flags=" + generic.worldFlags.Count,
                    "SUMMARY|pass=" + pass
                });

            if (!pass)
                throw new Exception(
                    "World-state batch QA failed; see " + Result);
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
            throw;
        }
        finally
        {
            if (pickupGo)
                UnityEngine.Object.DestroyImmediate(pickupGo);
            if (player)
                UnityEngine.Object.DestroyImmediate(player);
        }
    }
}

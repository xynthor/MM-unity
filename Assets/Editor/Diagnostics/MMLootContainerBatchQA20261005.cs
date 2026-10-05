using System;
using System.IO;
using UnityEngine;

public static class MMLootContainerBatchQA20261005
{
    const string Root = "C:/MMUnityPort/Validation/LootContainer20261005";
    const string Result = Root + "/results.txt";
    const string Error = Root + "/error.txt";
    const string SavePath = Root + "/looted-save.json";

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(Result)) File.Delete(Result);
        if (File.Exists(Error)) File.Delete(Error);
        if (File.Exists(SavePath)) File.Delete(SavePath);

        GameObject player = null;
        GameObject chestGo = null;

        try
        {
            player = new GameObject("MM_LootQA_Player");

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

            chestGo = new GameObject("MM_LootQA_Chest");
            chestGo.transform.localPosition =
                new Vector3(4f, 2f, 1f);

            MMLootContainer chest =
                chestGo.AddComponent<MMLootContainer>();
            chest.persistentId = "tomb.001";
            chest.persistLooted = true;
            chest.requiredWorldFlag = "tomb.access";
            chest.requiredQuestId = "qa.tomb";
            chest.requiredItemId = "qa.tomb.key";
            chest.requiredItemQuantity = 1;
            chest.consumeRequiredItem = true;
            chest.lootedLocalEulerOffset =
                new Vector3(-65f, 0f, 0f);
            chest.loot.Add(
                new MMLootEntry
                {
                    itemId = "loot.gem",
                    displayName = "Ancient Gem",
                    quantity = 2
                });
            chest.loot.Add(
                new MMLootEntry
                {
                    itemId = "loot.potion",
                    displayName = "Potion",
                    quantity = 1
                });

            chest.ApplyPersistentState(world);
            Quaternion closedRotation =
                chestGo.transform.localRotation;

            bool initialBlocked =
                !chest.CanInteract(player) &&
                !chest.IsLooted;

            world.SetFlag("tomb.access", true);
            quests.StartQuest(
                "qa.tomb",
                "Open the Tomb",
                "Recover the relic",
                1,
                true);
            inventory.AddItem(
                "qa.tomb.key",
                1,
                "Tomb Key");

            bool activeQuestBlocked =
                !chest.CanInteract(player);

            quests.CompleteQuest("qa.tomb");

            bool readyPass =
                chest.CanInteract(player) &&
                inventory.GetCount("qa.tomb.key") == 1;

            MMSaveData beforeLoot =
                save.Capture();

            chest.Interact(player);

            string lootFlag =
                chest.PersistentFlagId;

            bool lootPass =
                lootFlag == "container.tomb.001.looted" &&
                world.HasFlag(lootFlag) &&
                chest.IsLooted &&
                chest.LootCount == 1 &&
                inventory.GetCount("qa.tomb.key") == 0 &&
                inventory.GetCount("loot.gem") == 2 &&
                inventory.GetCount("loot.potion") == 1 &&
                !chest.CanInteract(player) &&
                Quaternion.Angle(
                    chestGo.transform.localRotation,
                    closedRotation *
                    Quaternion.Euler(
                        chest.lootedLocalEulerOffset)) < 0.01f;

            chest.Interact(player);

            bool singleLootPass =
                chest.LootCount == 1 &&
                inventory.GetCount("loot.gem") == 2 &&
                inventory.GetCount("loot.potion") == 1;

            MMSaveData looted =
                save.Capture();

            bool capturePass =
                looted.worldFlags.Contains("tomb.access") &&
                looted.worldFlags.Contains(lootFlag) &&
                inventory.GetCount("qa.tomb.key") == 0;

            save.Restore(beforeLoot);

            bool rollbackPass =
                world.HasFlag("tomb.access") &&
                !world.HasFlag(lootFlag) &&
                !chest.IsLooted &&
                chest.LootCount == 0 &&
                inventory.GetCount("qa.tomb.key") == 1 &&
                inventory.GetCount("loot.gem") == 0 &&
                inventory.GetCount("loot.potion") == 0 &&
                Quaternion.Angle(
                    chestGo.transform.localRotation,
                    closedRotation) < 0.01f &&
                chest.CanInteract(player);

            save.Restore(looted);

            bool restoreLootedPass =
                world.HasFlag(lootFlag) &&
                chest.IsLooted &&
                chest.LootCount == 1 &&
                inventory.GetCount("qa.tomb.key") == 0 &&
                inventory.GetCount("loot.gem") == 2 &&
                inventory.GetCount("loot.potion") == 1 &&
                !chest.CanInteract(player);

            bool saveFilePass =
                save.SaveToPath(SavePath) &&
                File.Exists(SavePath);

            save.Restore(beforeLoot);

            bool mutationPass =
                !chest.IsLooted &&
                inventory.GetCount("qa.tomb.key") == 1 &&
                inventory.GetCount("loot.gem") == 0;

            bool fileLoadReturned =
                save.LoadFromPath(SavePath);

            bool fileLoadPass =
                fileLoadReturned &&
                chest.IsLooted &&
                world.HasFlag(lootFlag) &&
                inventory.GetCount("qa.tomb.key") == 0 &&
                inventory.GetCount("loot.gem") == 2 &&
                inventory.GetCount("loot.potion") == 1;

            bool pass =
                initialBlocked &&
                activeQuestBlocked &&
                readyPass &&
                lootPass &&
                singleLootPass &&
                capturePass &&
                rollbackPass &&
                restoreLootedPass &&
                saveFilePass &&
                mutationPass &&
                fileLoadPass;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "initial_blocked|pass=" + initialBlocked,
                    "active_quest_blocked|pass=" + activeQuestBlocked,
                    "requirements_met|pass=" + readyPass,
                    "loot_once|pass=" + lootPass +
                    "|key=0|gem=2|potion=1",
                    "second_interaction_no_duplicate|pass=" +
                    singleLootPass,
                    "capture_looted_state|pass=" + capturePass,
                    "restore_pre_loot|pass=" + rollbackPass +
                    "|key=1|gem=0|open=False",
                    "restore_looted|pass=" + restoreLootedPass +
                    "|key=0|gem=2|open=True",
                    "save_file|pass=" + saveFilePass,
                    "mutate_pre_loot|pass=" + mutationPass,
                    "file_load_looted|pass=" + fileLoadPass,
                    "SUMMARY|pass=" + pass
                });

            if (!pass)
                throw new Exception(
                    "Loot container QA failed; see " + Result);
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
            throw;
        }
        finally
        {
            if (chestGo)
                UnityEngine.Object.DestroyImmediate(chestGo);
            if (player)
                UnityEngine.Object.DestroyImmediate(player);
        }
    }
}

using System;
using System.IO;
using UnityEngine;

public static class MMSaveSystemBatchQA20261005
{
    const string Root = "C:/MMUnityPort/Validation/SaveSystem20261005";
    const string Result = Root + "/results.txt";
    const string Error = Root + "/error.txt";
    const string SavePath = Root + "/qa-save.json";
    const string InvalidVersionPath = Root + "/qa-invalid-version.json";

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(Result)) File.Delete(Result);
        if (File.Exists(Error)) File.Delete(Error);
        if (File.Exists(SavePath)) File.Delete(SavePath);
        if (File.Exists(InvalidVersionPath)) File.Delete(InvalidVersionPath);

        GameObject player = null;

        try
        {
            player = new GameObject("MM_SaveQA_Player");
            MMInventory inventory = player.AddComponent<MMInventory>();
            inventory.showInventory = false;
            inventory.EditorClearForQa();

            MMQuestManager quests = player.AddComponent<MMQuestManager>();
            quests.showTracker = false;
            quests.showJournal = false;
            quests.EditorClearForQa();

            MMSaveSystem save = player.AddComponent<MMSaveSystem>();
            save.savePlayerTransform = true;

            inventory.AddItem("qa.relic", 3, "Ancient Relic");
            inventory.AddItem("qa.coin", 7, "Coin");

            quests.StartQuest(
                "qa.active",
                "Active Quest",
                "Reach three progress.",
                3,
                true);
            quests.AddProgress("qa.active", 2);

            quests.StartQuest(
                "qa.done",
                "Completed Quest",
                "Finish this quest.",
                2,
                false);
            quests.CompleteQuest("qa.done");
            quests.TrackQuest("qa.active");

            player.transform.position = new Vector3(12.5f, 4.25f, -8.75f);
            player.transform.rotation = Quaternion.Euler(0f, 33f, 0f);

            bool savePass =
                save.SaveToPath(SavePath) &&
                File.Exists(SavePath) &&
                new FileInfo(SavePath).Length > 20;

            MMSaveData captured = save.Capture();
            bool capturePass =
                captured.version == MMSaveSystem.CurrentVersion &&
                captured.inventory.Count == 2 &&
                captured.quests.Count == 2 &&
                captured.trackedQuestId == "qa.active" &&
                Mathf.Approximately(captured.playerX, 12.5f) &&
                Mathf.Approximately(captured.playerY, 4.25f) &&
                Mathf.Approximately(captured.playerZ, -8.75f) &&
                Mathf.Abs(Mathf.DeltaAngle(captured.playerYaw, 33f)) < 0.05f;

            inventory.RemoveItem("qa.relic", 3);
            inventory.RemoveItem("qa.coin", 7);
            inventory.AddItem("qa.junk", 9, "Junk");

            quests.RemoveQuest("qa.active");
            quests.RemoveQuest("qa.done");
            quests.StartQuest(
                "qa.mutated",
                "Mutated Quest",
                "Should disappear after load.",
                9,
                true);

            player.transform.position = new Vector3(-99f, 17f, 44f);
            player.transform.rotation = Quaternion.Euler(0f, 170f, 0f);

            bool loadReturned = save.LoadFromPath(SavePath);

            MMQuestRecord active = quests.GetQuest("qa.active");
            MMQuestRecord done = quests.GetQuest("qa.done");

            bool inventoryRestorePass =
                loadReturned &&
                inventory.UniqueItemCount == 2 &&
                inventory.GetCount("qa.relic") == 3 &&
                inventory.GetCount("qa.coin") == 7 &&
                inventory.GetCount("qa.junk") == 0;

            bool questRestorePass =
                quests.QuestCount == 2 &&
                active != null &&
                active.title == "Active Quest" &&
                active.objective == "Reach three progress." &&
                active.goal == 3 &&
                active.progress == 2 &&
                active.status == MMQuestStatus.Active &&
                done != null &&
                done.title == "Completed Quest" &&
                done.goal == 2 &&
                done.progress == 2 &&
                done.status == MMQuestStatus.Completed &&
                quests.TrackedQuest == active &&
                quests.GetQuest("qa.mutated") == null;

            Vector3 restoredPosition = player.transform.position;
            float restoredYaw = player.transform.eulerAngles.y;
            bool transformRestorePass =
                Vector3.Distance(
                    restoredPosition,
                    new Vector3(12.5f, 4.25f, -8.75f)) < 0.001f &&
                Mathf.Abs(Mathf.DeltaAngle(restoredYaw, 33f)) < 0.05f;

            int beforeMissingRelics = inventory.GetCount("qa.relic");
            int beforeMissingQuestCount = quests.QuestCount;
            Vector3 beforeMissingPosition = player.transform.position;

            bool missingFilePass =
                !save.LoadFromPath(Root + "/does-not-exist.json") &&
                inventory.GetCount("qa.relic") == beforeMissingRelics &&
                quests.QuestCount == beforeMissingQuestCount &&
                player.transform.position == beforeMissingPosition;

            File.WriteAllText(
                InvalidVersionPath,
                "{\"version\":99}");

            int beforeVersionCoins = inventory.GetCount("qa.coin");
            string beforeTracked =
                quests.TrackedQuest != null
                    ? quests.TrackedQuest.id
                    : string.Empty;

            bool versionPass =
                !save.LoadFromPath(InvalidVersionPath) &&
                inventory.GetCount("qa.coin") == beforeVersionCoins &&
                quests.TrackedQuest != null &&
                quests.TrackedQuest.id == beforeTracked;

            inventory.AddItem("qa.extra", 1, "Extra");
            bool overwritePass =
                save.SaveToPath(SavePath) &&
                File.Exists(SavePath) &&
                new FileInfo(SavePath).Length > 20;

            bool pass =
                savePass &&
                capturePass &&
                inventoryRestorePass &&
                questRestorePass &&
                transformRestorePass &&
                missingFilePass &&
                versionPass &&
                overwritePass;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "save_file|pass=" + savePass,
                    "capture|pass=" + capturePass +
                    "|items=" + captured.inventory.Count +
                    "|quests=" + captured.quests.Count +
                    "|tracked=" + captured.trackedQuestId,
                    "inventory_restore|pass=" + inventoryRestorePass +
                    "|relic=3|coin=7|junk=0",
                    "quest_restore|pass=" + questRestorePass +
                    "|active=2/3|completed=2/2|tracked=qa.active",
                    "transform_restore|pass=" + transformRestorePass +
                    "|position=" + restoredPosition.ToString("F2") +
                    "|yaw=" + restoredYaw.ToString("F2"),
                    "missing_file_safe|pass=" + missingFilePass,
                    "version_rejection_safe|pass=" + versionPass,
                    "overwrite_save|pass=" + overwritePass,
                    "SUMMARY|pass=" + pass
                });

            if (!pass)
                throw new Exception(
                    "Save-system batch QA failed; see " + Result);
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
            throw;
        }
        finally
        {
            if (player)
                UnityEngine.Object.DestroyImmediate(player);
        }
    }
}

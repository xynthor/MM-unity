using System;
using System.IO;
using UnityEngine;

public static class MMPersistentGateBatchQA20261005
{
    const string Root = "C:/MMUnityPort/Validation/PersistentGate20261005";
    const string Result = Root + "/results.txt";
    const string Error = Root + "/error.txt";
    const string SavePath = Root + "/opened-save.json";

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(Result)) File.Delete(Result);
        if (File.Exists(Error)) File.Delete(Error);
        if (File.Exists(SavePath)) File.Delete(SavePath);

        GameObject player = null;
        GameObject gateGo = null;

        try
        {
            player = new GameObject("MM_GateQA_Player");

            MMInventory inventory = player.AddComponent<MMInventory>();
            inventory.showInventory = false;
            inventory.EditorClearForQa();

            MMQuestManager quests = player.AddComponent<MMQuestManager>();
            quests.showTracker = false;
            quests.showJournal = false;
            quests.EditorClearForQa();

            MMWorldState world = player.AddComponent<MMWorldState>();
            world.EditorClearForQa();

            MMSaveSystem save = player.AddComponent<MMSaveSystem>();
            save.savePlayerTransform = false;

            gateGo = new GameObject("MM_GateQA_Gate");
            gateGo.transform.localPosition = new Vector3(3f, 1f, -2f);
            BoxCollider collider = gateGo.AddComponent<BoxCollider>();

            MMPersistentGate gate = gateGo.AddComponent<MMPersistentGate>();
            gate.persistentId = "vault.001";
            gate.persistOpen = true;
            gate.requiredWorldFlag = "vault.authorized";
            gate.requiredQuestId = "qa.vault";
            gate.requiredItemId = "qa.vault.key";
            gate.requiredItemQuantity = 1;
            gate.consumeRequiredItem = true;
            gate.openDuration = 0f;
            gate.openLocalPositionOffset = new Vector3(0f, 2f, 0f);
            gate.disableCollidersWhenOpen = true;

            gate.ApplyPersistentState(world);
            Vector3 closedPosition = gateGo.transform.localPosition;

            bool initialBlocked =
                !gate.CanInteract(player) &&
                !gate.IsOpen &&
                collider.enabled;

            world.SetFlag("vault.authorized", true);
            bool flagOnlyBlocked =
                !gate.CanInteract(player);

            quests.StartQuest(
                "qa.vault",
                "Vault Access",
                "Unlock the vault",
                1,
                true);
            inventory.AddItem(
                "qa.vault.key",
                1,
                "Vault Key");

            bool activeQuestBlocked =
                !gate.CanInteract(player);

            quests.CompleteQuest("qa.vault");

            bool readyPass =
                gate.CanInteract(player) &&
                inventory.GetCount("qa.vault.key") == 1 &&
                quests.GetQuest("qa.vault").status ==
                    MMQuestStatus.Completed;

            MMSaveData readyClosed = save.Capture();

            gate.Interact(player);

            string gateFlag = gate.PersistentFlagId;
            Vector3 expectedOpen =
                closedPosition + gate.openLocalPositionOffset;

            bool openPass =
                gateFlag == "gate.vault.001.open" &&
                world.HasFlag(gateFlag) &&
                gate.IsOpen &&
                gate.OpenCount == 1 &&
                inventory.GetCount("qa.vault.key") == 0 &&
                !collider.enabled &&
                Vector3.Distance(
                    gateGo.transform.localPosition,
                    expectedOpen) < 0.001f &&
                !gate.CanInteract(player);

            MMSaveData opened = save.Capture();

            bool capturedOpenPass =
                opened.worldFlags.Contains("vault.authorized") &&
                opened.worldFlags.Contains(gateFlag) &&
                opened.inventory.Count == 0 &&
                opened.quests.Count == 1 &&
                opened.quests[0].status ==
                    MMQuestStatus.Completed;

            save.Restore(readyClosed);

            bool restoreClosedPass =
                !world.HasFlag(gateFlag) &&
                world.HasFlag("vault.authorized") &&
                !gate.IsOpen &&
                gate.OpenCount == 0 &&
                collider.enabled &&
                inventory.GetCount("qa.vault.key") == 1 &&
                Vector3.Distance(
                    gateGo.transform.localPosition,
                    closedPosition) < 0.001f &&
                gate.CanInteract(player);

            save.Restore(opened);

            bool restoreOpenPass =
                world.HasFlag(gateFlag) &&
                gate.IsOpen &&
                gate.OpenCount == 1 &&
                !collider.enabled &&
                inventory.GetCount("qa.vault.key") == 0 &&
                Vector3.Distance(
                    gateGo.transform.localPosition,
                    expectedOpen) < 0.001f &&
                !gate.CanInteract(player);

            bool saveFilePass =
                save.SaveToPath(SavePath) &&
                File.Exists(SavePath);

            save.Restore(readyClosed);

            bool mutationPass =
                !gate.IsOpen &&
                collider.enabled &&
                inventory.GetCount("qa.vault.key") == 1;

            bool fileLoadReturned =
                save.LoadFromPath(SavePath);

            bool fileLoadPass =
                fileLoadReturned &&
                gate.IsOpen &&
                world.HasFlag(gateFlag) &&
                !collider.enabled &&
                inventory.GetCount("qa.vault.key") == 0;

            bool pass =
                initialBlocked &&
                flagOnlyBlocked &&
                activeQuestBlocked &&
                readyPass &&
                openPass &&
                capturedOpenPass &&
                restoreClosedPass &&
                restoreOpenPass &&
                saveFilePass &&
                mutationPass &&
                fileLoadPass;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "initial_requirements|pass=" + initialBlocked,
                    "world_flag_only_blocked|pass=" + flagOnlyBlocked,
                    "active_quest_blocked|pass=" + activeQuestBlocked,
                    "requirements_met|pass=" + readyPass,
                    "open_and_consume_key|pass=" + openPass +
                    "|flag=" + gateFlag +
                    "|colliderEnabled=" + collider.enabled,
                    "capture_open_state|pass=" + capturedOpenPass +
                    "|flags=" + opened.worldFlags.Count,
                    "restore_closed_save|pass=" + restoreClosedPass +
                    "|key=1|open=False|collider=True",
                    "restore_open_save|pass=" + restoreOpenPass +
                    "|key=0|open=True|collider=False",
                    "save_file|pass=" + saveFilePass,
                    "mutate_back_closed|pass=" + mutationPass,
                    "file_load_open_state|pass=" + fileLoadPass,
                    "SUMMARY|pass=" + pass
                });

            if (!pass)
                throw new Exception(
                    "Persistent gate QA failed; see " + Result);
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
            throw;
        }
        finally
        {
            if (gateGo)
                UnityEngine.Object.DestroyImmediate(gateGo);
            if (player)
                UnityEngine.Object.DestroyImmediate(player);
        }
    }
}

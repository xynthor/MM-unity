using System;
using System.IO;
using UnityEngine;

public static class MMWorldFlagSwitchBatchQA20261005
{
    const string Root = "C:/MMUnityPort/Validation/WorldFlagSwitch20261005";
    const string Result = Root + "/results.txt";
    const string Error = Root + "/error.txt";

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(Result)) File.Delete(Result);
        if (File.Exists(Error)) File.Delete(Error);

        GameObject player = null;
        GameObject switchGo = null;
        GameObject gateGo = null;
        GameObject toggleGo = null;

        try
        {
            player = new GameObject("MM_SwitchQA_Player");
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

            switchGo = new GameObject("MM_SwitchQA_Lever");
            switchGo.transform.localPosition = new Vector3(2f, 1f, 3f);
            MMWorldFlagSwitch lever =
                switchGo.AddComponent<MMWorldFlagSwitch>();
            lever.flagId = "mechanism.bridge.enabled";
            lever.requiredQuestId = "qa.mechanism";
            lever.requiredItemId = "qa.crank";
            lever.requiredItemQuantity = 1;
            lever.consumeRequiredItem = false;
            lever.toggleFlag = false;
            lever.setValue = true;
            lever.disableInteractionWhenSet = true;
            lever.transitionDuration = 0f;
            lever.activeLocalEulerOffset = new Vector3(0f, 0f, -40f);
            lever.ApplyPersistentState(world);
            Quaternion inactiveRotation = switchGo.transform.localRotation;

            gateGo = new GameObject("MM_SwitchQA_Gate");
            BoxCollider gateCollider = gateGo.AddComponent<BoxCollider>();
            MMPersistentGate gate =
                gateGo.AddComponent<MMPersistentGate>();
            gate.persistentId = "bridge.001";
            gate.requiredWorldFlag = "mechanism.bridge.enabled";
            gate.openDuration = 0f;
            gate.openLocalPositionOffset = new Vector3(0f, 2f, 0f);
            gate.ApplyPersistentState(world);
            Vector3 gateClosedPosition = gateGo.transform.localPosition;

            bool initialPass =
                !lever.CanInteract(player) &&
                !gate.CanInteract(player) &&
                !world.HasFlag("mechanism.bridge.enabled");

            quests.StartQuest(
                "qa.mechanism",
                "Repair Mechanism",
                "Repair the bridge controls",
                1,
                true);
            inventory.AddItem("qa.crank", 1, "Crank");

            bool activeQuestBlocked =
                !lever.CanInteract(player);

            quests.CompleteQuest("qa.mechanism");

            bool readyPass =
                lever.CanInteract(player) &&
                !gate.CanInteract(player);

            MMSaveData beforeActivation = save.Capture();

            lever.Interact(player);

            bool leverPass =
                world.HasFlag("mechanism.bridge.enabled") &&
                lever.VisualActive &&
                lever.InteractionCount == 1 &&
                inventory.GetCount("qa.crank") == 1 &&
                !lever.CanInteract(player) &&
                Quaternion.Angle(
                    switchGo.transform.localRotation,
                    inactiveRotation * Quaternion.Euler(
                        lever.activeLocalEulerOffset)) < 0.01f &&
                gate.CanInteract(player);

            gate.Interact(player);

            bool gatePass =
                gate.IsOpen &&
                world.HasFlag(gate.PersistentFlagId) &&
                !gateCollider.enabled &&
                Vector3.Distance(
                    gateGo.transform.localPosition,
                    gateClosedPosition + gate.openLocalPositionOffset) < 0.001f;

            MMSaveData activated = save.Capture();

            save.Restore(beforeActivation);

            bool rollbackPass =
                !world.HasFlag("mechanism.bridge.enabled") &&
                !world.HasFlag(gate.PersistentFlagId) &&
                !lever.VisualActive &&
                Quaternion.Angle(
                    switchGo.transform.localRotation,
                    inactiveRotation) < 0.01f &&
                !gate.IsOpen &&
                gateCollider.enabled &&
                inventory.GetCount("qa.crank") == 1 &&
                lever.CanInteract(player) &&
                !gate.CanInteract(player);

            save.Restore(activated);

            bool restorePass =
                world.HasFlag("mechanism.bridge.enabled") &&
                world.HasFlag(gate.PersistentFlagId) &&
                lever.VisualActive &&
                gate.IsOpen &&
                !gateCollider.enabled &&
                inventory.GetCount("qa.crank") == 1;

            toggleGo = new GameObject("MM_SwitchQA_Toggle");
            MMWorldFlagSwitch toggle =
                toggleGo.AddComponent<MMWorldFlagSwitch>();
            toggle.flagId = "torch.qa.lit";
            toggle.toggleFlag = true;
            toggle.transitionDuration = 0f;
            toggle.ApplyPersistentState(world);

            bool toggleReady =
                toggle.CanInteract(player) &&
                !world.HasFlag("torch.qa.lit") &&
                !toggle.VisualActive;

            toggle.Interact(player);
            bool toggleOn =
                world.HasFlag("torch.qa.lit") &&
                toggle.VisualActive &&
                toggle.InteractionCount == 1 &&
                toggle.CanInteract(player);

            toggle.Interact(player);
            bool toggleOff =
                !world.HasFlag("torch.qa.lit") &&
                !toggle.VisualActive &&
                toggle.InteractionCount == 2 &&
                toggle.CanInteract(player);

            bool togglePass =
                toggleReady && toggleOn && toggleOff;

            bool pass =
                initialPass &&
                activeQuestBlocked &&
                readyPass &&
                leverPass &&
                gatePass &&
                rollbackPass &&
                restorePass &&
                togglePass;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "initial_blocked|pass=" + initialPass,
                    "active_quest_blocked|pass=" + activeQuestBlocked,
                    "requirements_met|pass=" + readyPass,
                    "lever_sets_flag|pass=" + leverPass +
                    "|flag=True|crank=1|gateEligible=True",
                    "gate_unlocked_by_flag|pass=" + gatePass +
                    "|open=True|collider=False",
                    "restore_pre_activation|pass=" + rollbackPass +
                    "|lever=False|gate=False",
                    "restore_activated|pass=" + restorePass +
                    "|lever=True|gate=True",
                    "toggle_switch|pass=" + togglePass +
                    "|onThenOff=True|interactions=2",
                    "SUMMARY|pass=" + pass
                });

            if (!pass)
                throw new Exception(
                    "World-flag switch QA failed; see " + Result);
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
            throw;
        }
        finally
        {
            if (toggleGo)
                UnityEngine.Object.DestroyImmediate(toggleGo);
            if (gateGo)
                UnityEngine.Object.DestroyImmediate(gateGo);
            if (switchGo)
                UnityEngine.Object.DestroyImmediate(switchGo);
            if (player)
                UnityEngine.Object.DestroyImmediate(player);
        }
    }
}

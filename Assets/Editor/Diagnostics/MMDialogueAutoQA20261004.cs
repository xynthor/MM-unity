using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MMDialogueAutoQA20261004
{
    const string Root = "C:/MMUnityPort/Validation/Dialogue20261004";
    const string Trigger = Root + "/run.flag";
    const string Result = Root + "/runtime.txt";
    const string Error = Root + "/error.txt";
    const string SessionKey = "MMDialogueAutoQA20261004";

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        Directory.CreateDirectory(Root);
        EditorApplication.playModeStateChanged -= ModeChanged;
        EditorApplication.playModeStateChanged += ModeChanged;
        EditorApplication.delayCall += MaybeStart;
    }

    static void MaybeStart()
    {
        if (!File.Exists(Trigger) ||
            EditorApplication.isPlaying ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        try
        {
            if (File.Exists(Result)) File.Delete(Result);
            if (File.Exists(Error)) File.Delete(Error);
            File.Delete(Trigger);
            SessionState.SetBool(SessionKey, true);
            EditorApplication.isPlaying = true;
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
        }
    }

    static void ModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false))
            return;

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(SessionKey, false);
            return;
        }

        if (state != PlayModeStateChange.EnteredPlayMode)
            return;

        GameObject target = null;

        try
        {
            Application.runInBackground = true;

            MMThirdPersonController player =
                UnityEngine.Object.FindObjectsByType<MMThirdPersonController>(
                        FindObjectsInactive.Exclude)
                    .Where(p => p.gameObject.activeInHierarchy)
                    .OrderByDescending(p =>
                        p.transform.parent &&
                        p.transform.parent.name.Contains("Persistent Player"))
                    .First();

            MMInteractionController interaction =
                player.GetComponent<MMInteractionController>();
            MMDialogueController dialogue =
                player.GetComponent<MMDialogueController>();
            MMHeroCombatController combat =
                player.GetComponent<MMHeroCombatController>();

            if (!interaction || !dialogue || !combat || !player.playerCamera)
                throw new InvalidOperationException(
                    "Dialogue/interaction/player components missing.");

            // Fast Enter Play Mode can preserve runtime component state between
            // diagnostic sessions. Reset only the QA player's transient state.
            MMHealth vitals = player.GetComponent<MMHealth>();
            if (vitals)
                vitals.ResetHealth();
            player.enabled = true;
            combat.enabled = true;
            combat.ResetCombat();

            Vector3 forward = player.playerCamera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = player.transform.forward;
            forward.Normalize();

            target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "MM_DialogueQA_Target";
            target.transform.localScale = new Vector3(0.55f, 0.80f, 0.55f);
            target.transform.position =
                player.transform.position +
                Vector3.up * interaction.interactionHeight +
                forward * 1.60f;

            MMDialogueInteractable talk =
                target.AddComponent<MMDialogueInteractable>();
            talk.speaker = "QA Speaker";
            talk.lines = new[] { "Line one", "Line two" };
            talk.repeatable = false;

            Physics.SyncTransforms();

            MMInteractable acquired = interaction.EditorScanNowForQa();
            bool acquirePass =
                acquired == talk &&
                acquired.Prompt == "Talk";

            bool movementBefore = player.enabled;
            bool combatBefore = combat.enabled;

            bool interacted = interaction.EditorInteractNowForQa();

            bool startActive = dialogue.IsActive;
            string startSpeaker = dialogue.CurrentSpeaker;
            string startLine = dialogue.CurrentLine;
            int startIndex = dialogue.CurrentLineIndex;
            bool startMovementEnabled = player.enabled;
            bool startCombatEnabled = combat.enabled;
            bool startInteractionNull = interaction.CurrentInteractable == null;

            bool startPass =
                interacted &&
                startActive &&
                startSpeaker == "QA Speaker" &&
                startLine == "Line one" &&
                startIndex == 0 &&
                !startMovementEnabled &&
                !startCombatEnabled &&
                startInteractionNull;

            bool advanceResult = dialogue.EditorAdvanceForQa();
            bool advanceActive = dialogue.IsActive;
            string advanceLine = dialogue.CurrentLine;
            int advanceIndex = dialogue.CurrentLineIndex;
            bool advanceMovementEnabled = player.enabled;
            bool advanceCombatEnabled = combat.enabled;

            bool advancePass =
                advanceResult &&
                advanceActive &&
                advanceLine == "Line two" &&
                advanceIndex == 1 &&
                !advanceMovementEnabled &&
                !advanceCombatEnabled;

            bool finalAdvanceResult = dialogue.EditorAdvanceForQa();
            bool completePass =
                !finalAdvanceResult &&
                !dialogue.IsActive &&
                talk.CompletedCount == 1 &&
                player.enabled == movementBefore &&
                combat.enabled == combatBefore &&
                interaction.EditorScanNowForQa() == null &&
                !talk.CanInteract(player.gameObject);

            talk.repeatable = true;
            bool restartPass =
                talk.CanInteract(player.gameObject) &&
                dialogue.StartDialogue(talk) &&
                dialogue.IsActive &&
                dialogue.CurrentLine == "Line one";

            dialogue.EditorCloseForQa();

            bool cancelPass =
                restartPass &&
                !dialogue.IsActive &&
                talk.CompletedCount == 1 &&
                player.enabled == movementBefore &&
                combat.enabled == combatBefore;

            bool pass =
                acquirePass &&
                startPass &&
                advancePass &&
                completePass &&
                cancelPass;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "acquire|pass=" + acquirePass +
                    "|prompt=" + (acquired ? acquired.Prompt : "NULL"),
                    "start|pass=" + startPass +
                    "|interacted=" + interacted +
                    "|active=" + startActive +
                    "|speaker=" + startSpeaker +
                    "|line=" + startLine +
                    "|index=" + startIndex +
                    "|movementEnabled=" + startMovementEnabled +
                    "|combatEnabled=" + startCombatEnabled +
                    "|interactionNull=" + startInteractionNull,
                    "advance|pass=" + advancePass +
                    "|result=" + advanceResult +
                    "|active=" + advanceActive +
                    "|line=" + advanceLine +
                    "|index=" + advanceIndex +
                    "|movementEnabled=" + advanceMovementEnabled +
                    "|combatEnabled=" + advanceCombatEnabled,
                    "complete_once|pass=" + completePass +
                    "|completedCount=" + talk.CompletedCount +
                    "|controlsRestored=" +
                    (player.enabled == movementBefore && combat.enabled == combatBefore),
                    "cancel_restore|pass=" + cancelPass +
                    "|completedCount=" + talk.CompletedCount,
                    "SUMMARY|pass=" + pass
                });
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
        }
        finally
        {
            if (target)
                UnityEngine.Object.Destroy(target);

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.isPlaying = false;
            };
        }
    }
}

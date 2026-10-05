using System;
using System.IO;
using UnityEngine;

public static class MMDialogueQuestBatchQA20261005
{
    const string Root = "C:/MMUnityPort/Validation/DialogueQuest20261005";
    const string Result = Root + "/results.txt";
    const string Error = Root + "/error.txt";

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(Result)) File.Delete(Result);
        if (File.Exists(Error)) File.Delete(Error);

        GameObject player = null;
        GameObject source = null;
        GameObject directSource = null;

        try
        {
            player = new GameObject("MM_DialogueQuestQA_Player");
            MMQuestManager quests = player.AddComponent<MMQuestManager>();
            quests.showTracker = false;
            quests.showJournal = false;
            quests.EditorClearForQa();

            MMDialogueController controller =
                player.AddComponent<MMDialogueController>();
            controller.showPanel = false;

            source = new GameObject("MM_DialogueQuestQA_Source");
            source.AddComponent<SphereCollider>();

            MMDialogueInteractable dialogue =
                source.AddComponent<MMDialogueInteractable>();
            dialogue.speaker = "Quest Giver";
            dialogue.lines = new[]
            {
                "I need help.",
                "Return when the task is done."
            };
            dialogue.repeatable = true;

            MMDialogueQuestHook hook =
                source.AddComponent<MMDialogueQuestHook>();
            hook.questId = "qa.dialogue.progress";
            hook.questTitle = "Dialogue Progress";
            hook.objective = "Finish the conversation twice.";
            hook.goal = 2;
            hook.startQuestOnDialogueStart = true;
            hook.progressOnDialogueComplete = 1;
            hook.completeQuestOnDialogueComplete = false;
            hook.EditorBindForQa();

            bool initialPass =
                quests.QuestCount == 0 &&
                dialogue.CanInteract(player);

            dialogue.Interact(player);
            MMQuestRecord started =
                quests.GetQuest("qa.dialogue.progress");

            bool startPass =
                controller.IsActive &&
                started != null &&
                started.status == MMQuestStatus.Active &&
                started.progress == 0 &&
                started.goal == 2 &&
                quests.TrackedQuest == started &&
                dialogue.CompletedCount == 0;

            controller.EditorCloseForQa();
            int closeProgress = started.progress;
            MMQuestStatus closeStatus = started.status;
            int closeCompletedCount = dialogue.CompletedCount;
            bool closePass =
                !controller.IsActive &&
                closeCompletedCount == 0 &&
                closeProgress == 0 &&
                closeStatus == MMQuestStatus.Active;

            dialogue.Interact(player);
            bool firstAdvancePass =
                controller.EditorAdvanceForQa() &&
                controller.IsActive &&
                controller.CurrentLineIndex == 1 &&
                started.progress == 0;

            bool firstCompleteReturn =
                controller.EditorAdvanceForQa();

            bool firstCompletePass =
                !firstCompleteReturn &&
                !controller.IsActive &&
                dialogue.CompletedCount == 1 &&
                started.progress == 1 &&
                started.status == MMQuestStatus.Active;

            dialogue.Interact(player);
            controller.EditorAdvanceForQa();
            controller.EditorAdvanceForQa();

            bool secondCompletePass =
                dialogue.CompletedCount == 2 &&
                started.progress == 2 &&
                started.status == MMQuestStatus.Completed &&
                Mathf.Approximately(started.NormalizedProgress, 1f);

            directSource = new GameObject(
                "MM_DialogueQuestQA_DirectComplete");
            directSource.AddComponent<SphereCollider>();

            MMDialogueInteractable directDialogue =
                directSource.AddComponent<MMDialogueInteractable>();
            directDialogue.speaker = "Captain";
            directDialogue.lines = new[] { "Your work is complete." };
            directDialogue.repeatable = false;

            MMDialogueQuestHook directHook =
                directSource.AddComponent<MMDialogueQuestHook>();
            directHook.questId = "qa.dialogue.direct";
            directHook.startQuestOnDialogueStart = false;
            directHook.progressOnDialogueComplete = 0;
            directHook.completeQuestOnDialogueComplete = true;
            directHook.EditorBindForQa();

            bool manualStart =
                quests.StartQuest(
                    "qa.dialogue.direct",
                    "Direct Completion",
                    "Report to the captain.",
                    5,
                    false);

            MMQuestRecord direct =
                quests.GetQuest("qa.dialogue.direct");

            directDialogue.Interact(player);
            controller.EditorAdvanceForQa();

            bool directPass =
                manualStart &&
                direct != null &&
                direct.status == MMQuestStatus.Completed &&
                direct.progress == 5 &&
                directDialogue.CompletedCount == 1 &&
                !directDialogue.CanInteract(player);

            bool pass =
                initialPass &&
                startPass &&
                closePass &&
                firstAdvancePass &&
                firstCompletePass &&
                secondCompletePass &&
                directPass;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "initial|pass=" + initialPass,
                    "start_on_dialogue|pass=" + startPass +
                    "|progress=0|goal=2|active=True",
                    "close_without_progress|pass=" + closePass +
                    "|progress=" + closeProgress +
                    "|status=" + closeStatus +
                    "|completedCount=" + closeCompletedCount,
                    "advance_without_completion|pass=" +
                    firstAdvancePass,
                    "first_completion|pass=" + firstCompletePass +
                    "|completedCount=1|progress=1",
                    "second_completion|pass=" +
                    secondCompletePass +
                    "|completedCount=" + dialogue.CompletedCount +
                    "|progress=" + started.progress +
                    "|status=" + started.status,
                    "direct_completion|pass=" + directPass +
                    "|progress=" + direct.progress +
                    "|status=" + direct.status,
                    "SUMMARY|pass=" + pass
                });

            if (!pass)
                throw new Exception(
                    "Dialogue quest QA failed; see " + Result);
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
            throw;
        }
        finally
        {
            if (directSource)
                UnityEngine.Object.DestroyImmediate(directSource);
            if (source)
                UnityEngine.Object.DestroyImmediate(source);
            if (player)
                UnityEngine.Object.DestroyImmediate(player);
        }
    }
}

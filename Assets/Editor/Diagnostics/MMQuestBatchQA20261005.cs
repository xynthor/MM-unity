using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MMQuestBatchQA20261005
{
    const string Root = "C:/MMUnityPort/Validation/Quest20261005";
    const string Result = Root + "/batch_results.txt";
    const string Error = Root + "/batch_error.txt";

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(Result)) File.Delete(Result);
        if (File.Exists(Error)) File.Delete(Error);

        GameObject player = null;
        GameObject source = null;

        try
        {
            player = new GameObject("MM_QuestBatch_Player");
            MMQuestManager manager = player.AddComponent<MMQuestManager>();
            manager.showTracker = false;
            manager.showJournal = false;
            manager.EditorClearForQa();

            source = new GameObject("MM_QuestBatch_Interactable");
            source.AddComponent<SphereCollider>();
            MMQuestInteractable q = source.AddComponent<MMQuestInteractable>();
            q.questId = "qa.relic";
            q.questTitle = "Recover the Relic";
            q.objective = "Inspect the relic twice.";
            q.goal = 2;
            q.progressPerInteraction = 1;
            q.autoStart = true;
            q.interactOnce = false;
            q.disableWhenQuestCompleted = true;

            bool initialPass =
                manager.QuestCount == 0 &&
                q.CanInteract(player);

            q.Interact(player);
            MMQuestRecord first = manager.GetQuest("qa.relic");
            int firstProgress = first != null ? first.progress : -1;
            int firstGoal = first != null ? first.goal : -1;
            MMQuestStatus firstStatus =
                first != null ? first.status : MMQuestStatus.Completed;
            int firstInteractionCount = q.InteractionCount;
            bool firstCanInteract = q.CanInteract(player);

            bool firstPass =
                first != null &&
                firstStatus == MMQuestStatus.Active &&
                firstProgress == 1 &&
                firstGoal == 2 &&
                manager.TrackedQuest == first &&
                firstInteractionCount == 1 &&
                firstCanInteract;

            q.Interact(player);
            MMQuestRecord second = manager.GetQuest("qa.relic");

            bool completionPass =
                second != null &&
                second.status == MMQuestStatus.Completed &&
                second.progress == 2 &&
                Mathf.Approximately(second.NormalizedProgress, 1f) &&
                q.InteractionCount == 2 &&
                !q.CanInteract(player);

            bool completedStable =
                !manager.AddProgress("qa.relic", 1) &&
                !manager.StartQuest(
                    "qa.relic",
                    "Duplicate",
                    "Duplicate must not replace existing quest.",
                    1,
                    true) &&
                !manager.CompleteQuest("qa.relic");

            bool manualStart =
                manager.StartQuest(
                    "qa.manual",
                    "Manual Quest",
                    "Reach three progress.",
                    3,
                    false);

            MMQuestRecord manual = manager.GetQuest("qa.manual");
            bool manualPass =
                manualStart &&
                manual != null &&
                manager.AddProgress("qa.manual", 2) &&
                manual.progress == 2 &&
                manual.status == MMQuestStatus.Active &&
                manager.CompleteQuest("qa.manual") &&
                manual.progress == 3 &&
                manual.status == MMQuestStatus.Completed;

            bool trackingPass =
                manager.TrackQuest("qa.manual") &&
                manager.TrackedQuest == manual &&
                !manager.TrackQuest("qa.missing");

            bool removePass =
                manager.RemoveQuest("qa.manual") &&
                manager.GetQuest("qa.manual") == null &&
                manager.QuestCount == 1 &&
                !manager.RemoveQuest("qa.manual");

            bool pass =
                initialPass &&
                firstPass &&
                completionPass &&
                completedStable &&
                manualPass &&
                trackingPass &&
                removePass;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "initial|pass=" + initialPass +
                    "|questCount=0|canInteract=True",
                    "first_interaction|pass=" + firstPass +
                    "|progress=" + firstProgress +
                    "|goal=" + firstGoal +
                    "|status=" + firstStatus +
                    "|interactionCount=" + firstInteractionCount +
                    "|canInteract=" + firstCanInteract,
                    "completion|pass=" + completionPass +
                    "|progress=" + second.progress +
                    "|status=" + second.status +
                    "|canInteractAfterComplete=" + q.CanInteract(player),
                    "completed_stability|pass=" + completedStable,
                    "manual_lifecycle|pass=" + manualPass,
                    "tracking|pass=" + trackingPass,
                    "remove|pass=" + removePass +
                    "|questCount=" + manager.QuestCount,
                    "SUMMARY|pass=" + pass
                });

            if (!pass)
                throw new Exception("Quest batch QA failed; see " + Result);
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
            throw;
        }
        finally
        {
            if (source) UnityEngine.Object.DestroyImmediate(source);
            if (player) UnityEngine.Object.DestroyImmediate(player);
        }
    }
}

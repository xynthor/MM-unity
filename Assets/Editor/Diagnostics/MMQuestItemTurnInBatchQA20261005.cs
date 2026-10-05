using System;
using System.IO;
using UnityEngine;

public static class MMQuestItemTurnInBatchQA20261005
{
    const string Root = "C:/MMUnityPort/Validation/QuestItemTurnIn20261005";
    const string Result = Root + "/results.txt";
    const string Error = Root + "/error.txt";

    public static void Run()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(Result)) File.Delete(Result);
        if (File.Exists(Error)) File.Delete(Error);

        GameObject player = null;
        GameObject turnInGo = null;
        GameObject repeatGo = null;

        try
        {
            player = new GameObject("MM_TurnInQA_Player");
            MMQuestManager quests = player.AddComponent<MMQuestManager>();
            quests.showTracker = false;
            quests.showJournal = false;
            quests.EditorClearForQa();

            MMInventory inventory = player.AddComponent<MMInventory>();
            inventory.showInventory = false;
            inventory.EditorClearForQa();

            bool questStarted = quests.StartQuest(
                "qa.fetch.relic",
                "Return the Relics",
                "Bring two relics.",
                1,
                true);

            turnInGo = new GameObject("MM_TurnInQA_Object");
            turnInGo.AddComponent<SphereCollider>();
            MMQuestItemTurnIn turnIn =
                turnInGo.AddComponent<MMQuestItemTurnIn>();
            turnIn.questId = "qa.fetch.relic";
            turnIn.requiredItemId = "qa.relic";
            turnIn.requiredItemDisplayName = "Ancient Relic";
            turnIn.requiredQuantity = 2;
            turnIn.consumeRequiredItems = true;
            turnIn.completeQuestOnTurnIn = true;
            turnIn.rewardItemId = "qa.gold";
            turnIn.rewardItemDisplayName = "Gold";
            turnIn.rewardQuantity = 5;
            turnIn.interactOnce = true;

            bool missingPass =
                questStarted &&
                !turnIn.CanInteract(player) &&
                turnIn.Prompt == "Turn in Ancient Relic";

            inventory.AddItem("qa.relic", 1, "Ancient Relic");
            bool insufficientPass =
                !turnIn.CanInteract(player) &&
                inventory.GetCount("qa.relic") == 1;

            inventory.AddItem("qa.relic", 1);
            bool readyPass =
                turnIn.CanInteract(player) &&
                inventory.GetCount("qa.relic") == 2;

            turnIn.Interact(player);
            MMQuestRecord completed =
                quests.GetQuest("qa.fetch.relic");

            bool completionPass =
                completed != null &&
                completed.status == MMQuestStatus.Completed &&
                completed.progress == completed.goal &&
                inventory.GetCount("qa.relic") == 0 &&
                inventory.GetCount("qa.gold") == 5 &&
                turnIn.TurnInCount == 1 &&
                !turnIn.CanInteract(player);

            turnIn.Interact(player);
            bool repeatBlockedPass =
                inventory.GetCount("qa.gold") == 5 &&
                turnIn.TurnInCount == 1;

            bool progressQuestStarted = quests.StartQuest(
                "qa.fetch.tokens",
                "Token Delivery",
                "Turn in three tokens.",
                3,
                false);

            repeatGo = new GameObject("MM_TurnInQA_Repeat");
            repeatGo.AddComponent<SphereCollider>();
            MMQuestItemTurnIn repeat =
                repeatGo.AddComponent<MMQuestItemTurnIn>();
            repeat.questId = "qa.fetch.tokens";
            repeat.requiredItemId = "qa.token";
            repeat.requiredItemDisplayName = "Token";
            repeat.requiredQuantity = 1;
            repeat.consumeRequiredItems = true;
            repeat.completeQuestOnTurnIn = false;
            repeat.progressAmount = 1;
            repeat.interactOnce = false;

            inventory.AddItem("qa.token", 2, "Token");
            repeat.Interact(player);
            repeat.Interact(player);

            MMQuestRecord tokens =
                quests.GetQuest("qa.fetch.tokens");

            bool progressPass =
                progressQuestStarted &&
                tokens != null &&
                tokens.status == MMQuestStatus.Active &&
                tokens.progress == 2 &&
                tokens.goal == 3 &&
                inventory.GetCount("qa.token") == 0 &&
                repeat.TurnInCount == 2 &&
                !repeat.CanInteract(player);

            inventory.AddItem("qa.token", 1, "Token");
            repeat.Interact(player);

            bool progressCompletionPass =
                tokens.status == MMQuestStatus.Completed &&
                tokens.progress == 3 &&
                inventory.GetCount("qa.token") == 0 &&
                repeat.TurnInCount == 3 &&
                !repeat.CanInteract(player);

            bool pass =
                missingPass &&
                insufficientPass &&
                readyPass &&
                completionPass &&
                repeatBlockedPass &&
                progressPass &&
                progressCompletionPass;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "missing_item|pass=" + missingPass,
                    "insufficient_item|pass=" + insufficientPass +
                    "|relicCount=1",
                    "ready|pass=" + readyPass +
                    "|relicCount=2",
                    "complete_turnin|pass=" + completionPass +
                    "|questStatus=" + completed.status +
                    "|relicCount=" + inventory.GetCount("qa.relic") +
                    "|goldCount=" + inventory.GetCount("qa.gold") +
                    "|turnIns=" + turnIn.TurnInCount,
                    "repeat_blocked|pass=" + repeatBlockedPass +
                    "|goldCount=" + inventory.GetCount("qa.gold"),
                    "progress_turnins|pass=" + progressPass +
                    "|progress=2|goal=3|turnIns=2",
                    "progress_completion|pass=" +
                    progressCompletionPass +
                    "|progress=" + tokens.progress +
                    "|status=" + tokens.status +
                    "|turnIns=" + repeat.TurnInCount,
                    "SUMMARY|pass=" + pass
                });

            if (!pass)
                throw new Exception(
                    "Quest item turn-in QA failed; see " + Result);
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
            throw;
        }
        finally
        {
            if (repeatGo)
                UnityEngine.Object.DestroyImmediate(repeatGo);
            if (turnInGo)
                UnityEngine.Object.DestroyImmediate(turnInGo);
            if (player)
                UnityEngine.Object.DestroyImmediate(player);
        }
    }
}

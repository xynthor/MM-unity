using UnityEngine;

[DisallowMultipleComponent]
public class MMQuestInteractable : MMInteractable
{
    [Header("Quest")]
    public string questId;
    public string questTitle;
    [TextArea(2, 4)] public string objective;
    [Min(1)] public int goal = 1;
    [Min(1)] public int progressPerInteraction = 1;
    public bool autoStart = true;
    public bool interactOnce;
    public bool disableWhenQuestCompleted = true;

    public int InteractionCount { get; private set; }

    public override bool CanInteract(GameObject interactor)
    {
        if (!base.CanInteract(interactor) ||
            !interactor ||
            string.IsNullOrWhiteSpace(questId) ||
            (interactOnce && InteractionCount > 0))
            return false;

        MMQuestManager quests = interactor.GetComponent<MMQuestManager>();
        if (!quests)
            return false;

        MMQuestRecord record = quests.GetQuest(questId);

        if (record == null)
            return autoStart;

        if (disableWhenQuestCompleted &&
            record.status == MMQuestStatus.Completed)
            return false;

        return record.status == MMQuestStatus.Active;
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
            return;

        MMQuestManager quests = interactor.GetComponent<MMQuestManager>();
        MMQuestRecord record = quests.GetQuest(questId);

        if (record == null)
        {
            if (!autoStart ||
                !quests.StartQuest(
                    questId,
                    questTitle,
                    objective,
                    goal,
                    true))
                return;

            record = quests.GetQuest(questId);
        }

        if (record == null || record.status != MMQuestStatus.Active)
            return;

        if (!quests.AddProgress(
                questId,
                Mathf.Max(1, progressPerInteraction)))
            return;

        InteractionCount++;
    }
}

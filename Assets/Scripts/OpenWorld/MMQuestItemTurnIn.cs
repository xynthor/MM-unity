using UnityEngine;

[DisallowMultipleComponent]
public class MMQuestItemTurnIn : MMInteractable
{
    [Header("Quest")]
    public string questId;
    [Min(1)] public int progressAmount = 1;
    public bool completeQuestOnTurnIn = true;

    [Header("Required Item")]
    public string requiredItemId;
    public string requiredItemDisplayName;
    [Min(1)] public int requiredQuantity = 1;
    public bool consumeRequiredItems = true;

    [Header("Reward")]
    public string rewardItemId;
    public string rewardItemDisplayName;
    [Min(1)] public int rewardQuantity = 1;

    [Header("Interaction")]
    public bool interactOnce = true;

    public int TurnInCount { get; private set; }

    public override string Prompt
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(prompt) &&
                prompt != "Interact")
                return prompt;

            string label =
                string.IsNullOrWhiteSpace(requiredItemDisplayName)
                    ? requiredItemId
                    : requiredItemDisplayName;

            return string.IsNullOrWhiteSpace(label)
                ? "Turn in"
                : "Turn in " + label;
        }
    }

    public override bool CanInteract(GameObject interactor)
    {
        if (!base.CanInteract(interactor) ||
            !interactor ||
            string.IsNullOrWhiteSpace(questId) ||
            string.IsNullOrWhiteSpace(requiredItemId) ||
            requiredQuantity <= 0 ||
            (interactOnce && TurnInCount > 0))
            return false;

        MMQuestManager quests =
            interactor.GetComponent<MMQuestManager>();
        MMInventory inventory =
            interactor.GetComponent<MMInventory>();

        if (!quests || !inventory)
            return false;

        MMQuestRecord quest = quests.GetQuest(questId);
        if (quest == null ||
            quest.status != MMQuestStatus.Active)
            return false;

        return inventory.HasItem(
            requiredItemId,
            Mathf.Max(1, requiredQuantity));
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
            return;

        MMQuestManager quests =
            interactor.GetComponent<MMQuestManager>();
        MMInventory inventory =
            interactor.GetComponent<MMInventory>();
        MMQuestRecord quest = quests.GetQuest(questId);

        if (quest == null ||
            quest.status != MMQuestStatus.Active)
            return;

        int quantity = Mathf.Max(1, requiredQuantity);

        if (consumeRequiredItems &&
            !inventory.RemoveItem(requiredItemId, quantity))
            return;

        bool changed = completeQuestOnTurnIn
            ? quests.CompleteQuest(questId)
            : quests.AddProgress(
                questId,
                Mathf.Max(1, progressAmount));

        if (!changed)
        {
            if (consumeRequiredItems)
                inventory.AddItem(
                    requiredItemId,
                    quantity,
                    requiredItemDisplayName);
            return;
        }

        TurnInCount++;

        if (!string.IsNullOrWhiteSpace(rewardItemId) &&
            rewardQuantity > 0)
        {
            inventory.AddItem(
                rewardItemId,
                Mathf.Max(1, rewardQuantity),
                rewardItemDisplayName);
        }
    }
}

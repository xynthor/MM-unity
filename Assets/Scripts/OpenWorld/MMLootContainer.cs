using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class MMLootEntry
{
    public string itemId;
    public string displayName;
    [Min(1)] public int quantity = 1;
}

[DisallowMultipleComponent]
public class MMLootContainer : MMInteractable
{
    [Header("Persistence")]
    public string persistentId;
    public bool persistLooted = true;

    [Header("Requirements")]
    public string requiredWorldFlag;
    public string requiredQuestId;
    public string requiredItemId;
    [Min(1)] public int requiredItemQuantity = 1;
    public bool consumeRequiredItem;

    [Header("Loot")]
    public List<MMLootEntry> loot =
        new List<MMLootEntry>();

    [Header("Visual State")]
    public Transform movingPart;
    public Vector3 lootedLocalPositionOffset;
    public Vector3 lootedLocalEulerOffset =
        new Vector3(-70f, 0f, 0f);

    Vector3 closedLocalPosition;
    Quaternion closedLocalRotation;
    bool initialStateCaptured;

    public bool IsLooted { get; private set; }
    public int LootCount { get; private set; }

    public string PersistentFlagId =>
        string.IsNullOrWhiteSpace(persistentId)
            ? null
            : "container." + persistentId.Trim() + ".looted";

    public override string Prompt =>
        IsLooted
            ? "Empty"
            : string.IsNullOrWhiteSpace(prompt) || prompt == "Interact"
                ? "Open"
                : prompt;

    void Awake()
    {
        CaptureInitialState();
    }

    public override bool CanInteract(GameObject interactor)
    {
        if (!base.CanInteract(interactor) ||
            !interactor ||
            IsLooted ||
            !HasValidLoot())
            return false;

        MMInventory inventory =
            interactor.GetComponent<MMInventory>();
        MMWorldState world =
            interactor.GetComponent<MMWorldState>();

        if (!inventory)
            return false;

        if (persistLooted &&
            world &&
            !string.IsNullOrEmpty(PersistentFlagId) &&
            world.HasFlag(PersistentFlagId))
        {
            SetLootedState(true);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(requiredWorldFlag) &&
            (!world || !world.HasFlag(requiredWorldFlag)))
            return false;

        if (!string.IsNullOrWhiteSpace(requiredQuestId))
        {
            MMQuestManager quests =
                interactor.GetComponent<MMQuestManager>();
            MMQuestRecord quest =
                quests ? quests.GetQuest(requiredQuestId) : null;

            if (quest == null ||
                quest.status != MMQuestStatus.Completed)
                return false;
        }

        if (!string.IsNullOrWhiteSpace(requiredItemId) &&
            !inventory.HasItem(
                requiredItemId,
                Mathf.Max(1, requiredItemQuantity)))
            return false;

        return true;
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
            return;

        MMInventory inventory =
            interactor.GetComponent<MMInventory>();
        if (!inventory)
            return;

        int requiredQuantity =
            Mathf.Max(1, requiredItemQuantity);

        if (consumeRequiredItem &&
            !string.IsNullOrWhiteSpace(requiredItemId) &&
            !inventory.RemoveItem(
                requiredItemId,
                requiredQuantity))
            return;

        List<MMLootEntry> validLoot =
            loot.Where(IsValidLoot).ToList();

        foreach (MMLootEntry entry in validLoot)
        {
            inventory.AddItem(
                entry.itemId,
                Mathf.Max(1, entry.quantity),
                entry.displayName);
        }

        MMWorldState world =
            interactor.GetComponent<MMWorldState>();

        if (persistLooted &&
            world &&
            !string.IsNullOrEmpty(PersistentFlagId))
            world.SetFlag(PersistentFlagId, true);

        IsLooted = true;
        LootCount++;
        SetLootedState(true);
    }

    public void ApplyPersistentState(MMWorldState worldState)
    {
        if (!persistLooted ||
            !worldState ||
            string.IsNullOrEmpty(PersistentFlagId))
            return;

        bool looted =
            worldState.HasFlag(PersistentFlagId);

        SetLootedState(looted);
        LootCount = looted ? Mathf.Max(1, LootCount) : 0;
    }

    public void SetLootedState(bool looted)
    {
        CaptureInitialState();

        Transform part = movingPart ? movingPart : transform;
        part.localPosition = looted
            ? closedLocalPosition + lootedLocalPositionOffset
            : closedLocalPosition;
        part.localRotation = looted
            ? closedLocalRotation *
              Quaternion.Euler(lootedLocalEulerOffset)
            : closedLocalRotation;

        IsLooted = looted;
    }

    bool HasValidLoot()
    {
        return loot != null &&
               loot.Any(IsValidLoot);
    }

    static bool IsValidLoot(MMLootEntry entry)
    {
        return entry != null &&
               !string.IsNullOrWhiteSpace(entry.itemId) &&
               entry.quantity > 0;
    }

    void CaptureInitialState()
    {
        if (initialStateCaptured)
            return;

        if (!movingPart)
            movingPart = transform;

        closedLocalPosition = movingPart.localPosition;
        closedLocalRotation = movingPart.localRotation;
        initialStateCaptured = true;
    }
}

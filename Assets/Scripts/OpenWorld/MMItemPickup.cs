using UnityEngine;

[DisallowMultipleComponent]
public class MMItemPickup : MMInteractable
{
    [Header("Item")]
    public string itemId;
    public string displayName;
    [Min(1)] public int quantity = 1;

    [Header("Collection")]
    public bool collectOnce = true;
    public bool deactivateOnCollect = true;

    [Header("Persistence")]
    public bool persistCollected = true;
    public string persistentId;

    bool initialStateCaptured;
    bool initialActiveSelf;

    public int CollectedCount { get; private set; }
    public string PersistentFlagId =>
        string.IsNullOrWhiteSpace(persistentId)
            ? null
            : "pickup." + persistentId.Trim();

    void Awake()
    {
        CaptureInitialState();
    }

    public override string Prompt
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(prompt) &&
                prompt != "Interact")
                return prompt;

            string label = string.IsNullOrWhiteSpace(displayName)
                ? itemId
                : displayName;

            return string.IsNullOrWhiteSpace(label)
                ? "Pick up"
                : "Pick up " + label;
        }
    }

    public override bool CanInteract(GameObject interactor)
    {
        if (!base.CanInteract(interactor) ||
            !interactor ||
            string.IsNullOrWhiteSpace(itemId) ||
            quantity <= 0 ||
            (collectOnce && CollectedCount > 0))
            return false;

        MMInventory inventory =
            interactor.GetComponent<MMInventory>();
        if (!inventory)
            return false;

        if (collectOnce &&
            persistCollected &&
            !string.IsNullOrEmpty(PersistentFlagId))
        {
            MMWorldState worldState =
                interactor.GetComponent<MMWorldState>();

            if (worldState &&
                worldState.HasFlag(PersistentFlagId))
                return false;
        }

        return true;
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
            return;

        MMInventory inventory = interactor.GetComponent<MMInventory>();
        if (!inventory.AddItem(
                itemId,
                Mathf.Max(1, quantity),
                displayName))
            return;

        CollectedCount++;

        if (collectOnce &&
            persistCollected &&
            !string.IsNullOrEmpty(PersistentFlagId))
        {
            MMWorldState worldState =
                interactor.GetComponent<MMWorldState>();

            if (worldState)
                worldState.SetFlag(PersistentFlagId, true);
        }

        if (collectOnce && deactivateOnCollect)
            gameObject.SetActive(false);
    }

    public void ApplyPersistentState(MMWorldState worldState)
    {
        if (!collectOnce ||
            !persistCollected ||
            !worldState ||
            string.IsNullOrEmpty(PersistentFlagId))
            return;

        CaptureInitialState();

        bool collected =
            worldState.HasFlag(PersistentFlagId);

        if (collected)
        {
            CollectedCount = Mathf.Max(1, CollectedCount);

            if (deactivateOnCollect)
                gameObject.SetActive(false);
        }
        else
        {
            CollectedCount = 0;

            if (deactivateOnCollect &&
                gameObject.activeSelf != initialActiveSelf)
                gameObject.SetActive(initialActiveSelf);
        }
    }

    void CaptureInitialState()
    {
        if (initialStateCaptured)
            return;

        initialActiveSelf = gameObject.activeSelf;
        initialStateCaptured = true;
    }
}

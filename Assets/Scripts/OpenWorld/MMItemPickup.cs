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

    public int CollectedCount { get; private set; }

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

        return interactor.GetComponent<MMInventory>();
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

        if (collectOnce && deactivateOnCollect)
            gameObject.SetActive(false);
    }
}

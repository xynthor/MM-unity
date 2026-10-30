using UnityEngine;
using UnityEngine.Events;

public class MMSimpleInteractable : MMInteractable
{
    public bool interactOnce;
    public UnityEvent onInteract = new UnityEvent();

    public int InteractionCount { get; private set; }

    public override bool CanInteract(GameObject interactor)
        => base.CanInteract(interactor) && (!interactOnce || InteractionCount == 0);

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
            return;

        InteractionCount++;
        onInteract?.Invoke();
    }
}

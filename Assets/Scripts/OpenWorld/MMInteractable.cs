using UnityEngine;

public abstract class MMInteractable : MonoBehaviour
{
    public string prompt = "Interact";
    public Transform interactionPoint;

    public virtual string Prompt => string.IsNullOrWhiteSpace(prompt) ? "Interact" : prompt;

    public virtual Vector3 InteractionPosition =>
        interactionPoint ? interactionPoint.position : transform.position;

    public virtual bool CanInteract(GameObject interactor)
        => enabled && gameObject.activeInHierarchy;

    public abstract void Interact(GameObject interactor);
}

using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MMWorldFlagSwitch : MMInteractable
{
    [Header("World Flag")]
    public string flagId;
    public bool toggleFlag;
    public bool setValue = true;
    public bool disableInteractionWhenSet = true;

    [Header("Requirements")]
    public string requiredWorldFlag;
    public string requiredQuestId;
    public string requiredItemId;
    [Min(1)] public int requiredItemQuantity = 1;
    public bool consumeRequiredItem;

    [Header("Visual State")]
    public Transform movingPart;
    public Vector3 activeLocalPositionOffset;
    public Vector3 activeLocalEulerOffset = new Vector3(0f, 0f, -35f);
    [Min(0f)] public float transitionDuration = 0.30f;

    Vector3 inactiveLocalPosition;
    Quaternion inactiveLocalRotation;
    bool initialStateCaptured;
    bool visualActive;
    Coroutine transitionRoutine;

    public bool VisualActive => visualActive;
    public int InteractionCount { get; private set; }

    public override string Prompt =>
        string.IsNullOrWhiteSpace(prompt) || prompt == "Interact"
            ? (visualActive && toggleFlag ? "Deactivate" : "Activate")
            : prompt;

    void Awake()
    {
        CaptureInitialState();
    }

    public override bool CanInteract(GameObject interactor)
    {
        if (!base.CanInteract(interactor) ||
            !interactor ||
            string.IsNullOrWhiteSpace(flagId))
            return false;

        MMWorldState world =
            interactor.GetComponent<MMWorldState>();

        if (!world)
            return false;

        bool current = world.HasFlag(flagId);

        if (!toggleFlag &&
            disableInteractionWhenSet &&
            current == setValue)
            return false;

        if (!string.IsNullOrWhiteSpace(requiredWorldFlag) &&
            !world.HasFlag(requiredWorldFlag))
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

        if (!string.IsNullOrWhiteSpace(requiredItemId))
        {
            MMInventory inventory =
                interactor.GetComponent<MMInventory>();

            if (!inventory ||
                !inventory.HasItem(
                    requiredItemId,
                    Mathf.Max(1, requiredItemQuantity)))
                return false;
        }

        return true;
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
            return;

        MMWorldState world =
            interactor.GetComponent<MMWorldState>();
        if (!world)
            return;

        MMInventory inventory =
            interactor.GetComponent<MMInventory>();

        int quantity = Mathf.Max(1, requiredItemQuantity);
        if (consumeRequiredItem &&
            !string.IsNullOrWhiteSpace(requiredItemId) &&
            (!inventory ||
             !inventory.RemoveItem(requiredItemId, quantity)))
            return;

        bool next = toggleFlag
            ? !world.HasFlag(flagId)
            : setValue;

        world.SetFlag(flagId, next);
        InteractionCount++;
        SetVisualState(next);
    }

    public void ApplyPersistentState(MMWorldState worldState)
    {
        if (!worldState ||
            string.IsNullOrWhiteSpace(flagId))
            return;

        SetVisualStateImmediate(
            worldState.HasFlag(flagId));
    }

    public void SetVisualState(bool active)
    {
        CaptureInitialState();

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (!Application.isPlaying ||
            transitionDuration <= 0.001f)
        {
            SetVisualStateImmediate(active);
            return;
        }

        transitionRoutine =
            StartCoroutine(AnimateVisual(active));
    }

    public void SetVisualStateImmediate(bool active)
    {
        CaptureInitialState();

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        Transform part = movingPart ? movingPart : transform;
        part.localPosition = active
            ? inactiveLocalPosition + activeLocalPositionOffset
            : inactiveLocalPosition;
        part.localRotation = active
            ? inactiveLocalRotation *
              Quaternion.Euler(activeLocalEulerOffset)
            : inactiveLocalRotation;

        visualActive = active;
    }

    IEnumerator AnimateVisual(bool active)
    {
        Transform part = movingPart ? movingPart : transform;

        Vector3 fromPos = part.localPosition;
        Quaternion fromRot = part.localRotation;
        Vector3 toPos = active
            ? inactiveLocalPosition + activeLocalPositionOffset
            : inactiveLocalPosition;
        Quaternion toRot = active
            ? inactiveLocalRotation *
              Quaternion.Euler(activeLocalEulerOffset)
            : inactiveLocalRotation;

        float duration = Mathf.Max(0.01f, transitionDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration));

            part.localPosition =
                Vector3.LerpUnclamped(fromPos, toPos, t);
            part.localRotation =
                Quaternion.SlerpUnclamped(fromRot, toRot, t);

            yield return null;
        }

        part.localPosition = toPos;
        part.localRotation = toRot;
        visualActive = active;
        transitionRoutine = null;
    }

    void CaptureInitialState()
    {
        if (initialStateCaptured)
            return;

        if (!movingPart)
            movingPart = transform;

        inactiveLocalPosition = movingPart.localPosition;
        inactiveLocalRotation = movingPart.localRotation;
        initialStateCaptured = true;
    }
}

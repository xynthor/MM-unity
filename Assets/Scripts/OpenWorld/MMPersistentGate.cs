using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MMPersistentGate : MMInteractable
{
    [Header("Persistence")]
    public string persistentId;
    public bool persistOpen = true;

    [Header("Requirements")]
    public string requiredWorldFlag;
    public string requiredQuestId;
    public string requiredItemId;
    [Min(1)] public int requiredItemQuantity = 1;
    public bool consumeRequiredItem = true;

    [Header("Gate Motion")]
    public Transform movingPart;
    public Vector3 openLocalPositionOffset = new Vector3(0f, 2.5f, 0f);
    public Vector3 openLocalEulerOffset;
    [Min(0f)] public float openDuration = 0.55f;
    public bool disableCollidersWhenOpen = true;

    Vector3 closedLocalPosition;
    Quaternion closedLocalRotation;
    bool initialStateCaptured;
    Coroutine motionRoutine;
    Collider[] gateColliders;

    public bool IsOpen { get; private set; }
    public int OpenCount { get; private set; }

    public string PersistentFlagId =>
        string.IsNullOrWhiteSpace(persistentId)
            ? null
            : "gate." + persistentId.Trim() + ".open";

    public override string Prompt =>
        IsOpen
            ? "Open"
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
            IsOpen)
            return false;

        MMWorldState world =
            interactor.GetComponent<MMWorldState>();

        if (persistOpen &&
            world &&
            !string.IsNullOrEmpty(PersistentFlagId) &&
            world.HasFlag(PersistentFlagId))
        {
            SetOpenImmediate(true);
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

        MMInventory inventory =
            interactor.GetComponent<MMInventory>();

        int quantity = Mathf.Max(1, requiredItemQuantity);

        if (consumeRequiredItem &&
            !string.IsNullOrWhiteSpace(requiredItemId) &&
            (!inventory ||
             !inventory.RemoveItem(requiredItemId, quantity)))
            return;

        MMWorldState world =
            interactor.GetComponent<MMWorldState>();

        if (persistOpen &&
            world &&
            !string.IsNullOrEmpty(PersistentFlagId))
            world.SetFlag(PersistentFlagId, true);

        OpenCount++;
        SetOpen(true);
    }

    public void ApplyPersistentState(MMWorldState worldState)
    {
        if (!persistOpen ||
            !worldState ||
            string.IsNullOrEmpty(PersistentFlagId))
            return;

        SetOpenImmediate(
            worldState.HasFlag(PersistentFlagId));

        OpenCount = IsOpen ? Mathf.Max(1, OpenCount) : 0;
    }

    public void SetOpen(bool open)
    {
        CaptureInitialState();

        if (motionRoutine != null)
        {
            StopCoroutine(motionRoutine);
            motionRoutine = null;
        }

        if (!Application.isPlaying ||
            openDuration <= 0.001f)
        {
            SetOpenImmediate(open);
            return;
        }

        motionRoutine = StartCoroutine(
            AnimateState(open));
    }

    public void SetOpenImmediate(bool open)
    {
        CaptureInitialState();

        if (motionRoutine != null)
        {
            StopCoroutine(motionRoutine);
            motionRoutine = null;
        }

        Transform part = movingPart ? movingPart : transform;
        part.localPosition = open
            ? closedLocalPosition + openLocalPositionOffset
            : closedLocalPosition;
        part.localRotation = open
            ? closedLocalRotation * Quaternion.Euler(openLocalEulerOffset)
            : closedLocalRotation;

        IsOpen = open;
        UpdateColliders(open);
    }

    IEnumerator AnimateState(bool open)
    {
        Transform part = movingPart ? movingPart : transform;
        Vector3 fromPos = part.localPosition;
        Quaternion fromRot = part.localRotation;

        Vector3 toPos = open
            ? closedLocalPosition + openLocalPositionOffset
            : closedLocalPosition;
        Quaternion toRot = open
            ? closedLocalRotation * Quaternion.Euler(openLocalEulerOffset)
            : closedLocalRotation;

        float duration = Mathf.Max(0.01f, openDuration);
        float elapsed = 0f;

        if (open)
            UpdateColliders(true);

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
        IsOpen = open;

        if (!open)
            UpdateColliders(false);

        motionRoutine = null;
    }

    void CaptureInitialState()
    {
        if (initialStateCaptured)
            return;

        if (!movingPart)
            movingPart = transform;

        closedLocalPosition = movingPart.localPosition;
        closedLocalRotation = movingPart.localRotation;
        gateColliders =
            GetComponentsInChildren<Collider>(true);
        initialStateCaptured = true;
    }

    void UpdateColliders(bool open)
    {
        if (!disableCollidersWhenOpen ||
            gateColliders == null)
            return;

        foreach (Collider collider in gateColliders)
        {
            if (collider)
                collider.enabled = !open;
        }
    }
}

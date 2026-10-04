using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class MMInteractionController : MonoBehaviour
{
    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;

    [Header("Detection")]
    public Camera playerCamera;
    public float interactionRange = 2.5f;
    public float interactionHeight = 0.72f;
    public float interactionRadius = 0.28f;
    public LayerMask interactionMask = ~0;

    [Header("Prompt")]
    public bool showPrompt = true;
    public float promptBottomOffset = 82f;

    MMInteractable current;
    MMDialogueController dialogue;
    GUIStyle promptStyle;

    public MMInteractable CurrentInteractable => current;
    public string CurrentPrompt => current ? current.Prompt : string.Empty;

    void Awake()
    {
        if (!playerCamera)
        {
            MMThirdPersonController movement =
                GetComponent<MMThirdPersonController>();
            if (movement)
                playerCamera = movement.playerCamera;
        }

        if (!playerCamera)
            playerCamera = Camera.main;

        dialogue = GetComponent<MMDialogueController>();
    }

    void Update()
    {
        if (!dialogue)
            dialogue = GetComponent<MMDialogueController>();

        if (dialogue && dialogue.IsActive)
        {
            current = null;
            return;
        }

        current = Scan();

        if (current &&
            Input.GetKeyDown(interactKey) &&
            current.CanInteract(gameObject))
        {
            current.Interact(gameObject);
            current = Scan();
        }
    }

    MMInteractable Scan()
    {
        if (!playerCamera)
            return null;

        Vector3 direction = playerCamera.transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        direction.Normalize();

        Vector3 origin =
            transform.position +
            Vector3.up * interactionHeight;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            Mathf.Max(0.05f, interactionRadius),
            direction,
            interactionRange,
            interactionMask,
            QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits.OrderBy(h => h.distance))
        {
            if (!hit.collider)
                continue;

            Transform hitTransform = hit.collider.transform;
            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform))
                continue;

            MMInteractable interactable =
                hit.collider.GetComponentInParent<MMInteractable>();

            // The first solid non-player collider is an occluder. It either
            // is a valid interactable in player range, or it blocks anything
            // farther forward from the player.
            if (!interactable)
                return null;

            float playerDistance = Vector3.Distance(
                transform.position,
                interactable.InteractionPosition);

            if (playerDistance > interactionRange ||
                !interactable.CanInteract(gameObject))
                return null;

            return interactable;
        }

        return null;
    }

    void OnGUI()
    {
        if (!showPrompt || !current)
            return;

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
        }

        string text = "[" + interactKey + "] " + current.Prompt;
        float width = Mathf.Min(360f, Screen.width - 24f);
        Rect rect = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height - promptBottomOffset,
            width,
            38f);

        GUI.Box(rect, text, promptStyle);
    }

#if UNITY_EDITOR
    public MMInteractable EditorScanNowForQa()
    {
        current = Scan();
        return current;
    }

    public bool EditorInteractNowForQa()
    {
        current = Scan();
        if (!current || !current.CanInteract(gameObject))
            return false;

        current.Interact(gameObject);
        current = Scan();
        return true;
    }
#endif
}

public static class MMInteractionBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureController()
    {
        MMThirdPersonController player = null;
        MMThirdPersonController fallback = null;

        foreach (MMThirdPersonController candidate in
                 Object.FindObjectsByType<MMThirdPersonController>(
                     FindObjectsInactive.Exclude))
        {
            if (!candidate || !candidate.gameObject.activeInHierarchy)
                continue;

            if (!fallback)
                fallback = candidate;

            if (candidate.transform.parent &&
                candidate.transform.parent.name.Contains("Persistent Player"))
            {
                player = candidate;
                break;
            }
        }

        if (!player)
            player = fallback;

        if (!player)
            return;

        MMInteractionController interaction =
            player.GetComponent<MMInteractionController>();

        if (!interaction)
            interaction =
                player.gameObject.AddComponent<MMInteractionController>();

        interaction.playerCamera = player.playerCamera;
    }
}

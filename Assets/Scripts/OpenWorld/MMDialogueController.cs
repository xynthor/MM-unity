using UnityEngine;

[DisallowMultipleComponent]
public class MMDialogueInteractable : MMInteractable
{
    public string speaker;
    [TextArea(2, 5)] public string[] lines = new string[0];
    public bool repeatable = true;

    public int CompletedCount { get; private set; }

    public override string Prompt =>
        string.IsNullOrWhiteSpace(prompt) || prompt == "Interact"
            ? "Talk"
            : prompt;

    public override bool CanInteract(GameObject interactor)
    {
        if (!base.CanInteract(interactor) ||
            lines == null ||
            lines.Length == 0 ||
            (!repeatable && CompletedCount > 0))
            return false;

        MMDialogueController dialogue =
            interactor ? interactor.GetComponent<MMDialogueController>() : null;

        return dialogue && !dialogue.IsActive;
    }

    public override void Interact(GameObject interactor)
    {
        if (!CanInteract(interactor))
            return;

        MMDialogueController dialogue =
            interactor.GetComponent<MMDialogueController>();

        dialogue.StartDialogue(this);
    }

    internal void NotifyCompleted()
    {
        CompletedCount++;
    }
}

[DisallowMultipleComponent]
public class MMDialogueController : MonoBehaviour
{
    [Header("Input")]
    public KeyCode advanceKey = KeyCode.E;
    public KeyCode alternateAdvanceKey = KeyCode.Space;
    public KeyCode closeKey = KeyCode.Escape;

    [Header("Presentation")]
    public bool showPanel = true;
    public float panelBottomOffset = 34f;
    public float panelWidth = 760f;

    MMDialogueInteractable source;
    MMThirdPersonController movement;
    MMHeroCombatController combat;
    int lineIndex;
    int startedFrame;
    bool restoreMovement;
    bool restoreCombat;
    GUIStyle speakerStyle;
    GUIStyle lineStyle;
    GUIStyle hintStyle;

    public bool IsActive => source != null;
    public string CurrentSpeaker =>
        source ? (string.IsNullOrWhiteSpace(source.speaker) ? string.Empty : source.speaker) : string.Empty;
    public string CurrentLine =>
        source && source.lines != null && lineIndex >= 0 && lineIndex < source.lines.Length
            ? source.lines[lineIndex]
            : string.Empty;
    public int CurrentLineIndex => IsActive ? lineIndex : -1;

    void Awake()
    {
        movement = GetComponent<MMThirdPersonController>();
        combat = GetComponent<MMHeroCombatController>();
    }

    void Update()
    {
        if (!IsActive || Time.frameCount <= startedFrame)
            return;

        if (Input.GetKeyDown(closeKey))
        {
            EndDialogue(false);
            return;
        }

        if (Input.GetKeyDown(advanceKey) ||
            Input.GetKeyDown(alternateAdvanceKey))
            Advance();
    }

    public bool StartDialogue(MMDialogueInteractable dialogueSource)
    {
        if (!dialogueSource ||
            dialogueSource.lines == null ||
            dialogueSource.lines.Length == 0)
            return false;

        MMHealth health = GetComponent<MMHealth>();
        if (health && health.IsDead)
            return false;

        if (IsActive)
            EndDialogue(false);

        source = dialogueSource;
        lineIndex = 0;
        startedFrame = Time.frameCount;

        if (!movement)
            movement = GetComponent<MMThirdPersonController>();
        if (!combat)
            combat = GetComponent<MMHeroCombatController>();

        restoreMovement = movement && movement.enabled;
        restoreCombat = combat && combat.enabled;

        if (combat && combat.enabled)
        {
            combat.ResetCombat();
            combat.enabled = false;
        }

        if (movement && movement.enabled)
            movement.enabled = false;

        return true;
    }

    public bool Advance()
    {
        if (!IsActive)
            return false;

        lineIndex++;

        if (lineIndex >= source.lines.Length)
        {
            MMDialogueInteractable completed = source;
            EndDialogue(false);
            completed.NotifyCompleted();
            return false;
        }

        return true;
    }

    public void Close()
    {
        EndDialogue(false);
    }

    void EndDialogue(bool completed)
    {
        source = null;
        lineIndex = 0;
        RestoreControls();
    }

    void RestoreControls()
    {
        MMHealth health = GetComponent<MMHealth>();
        bool dead = health && health.IsDead;

        if (!dead)
        {
            if (movement)
                movement.enabled = restoreMovement;
            if (combat)
                combat.enabled = restoreCombat;
        }

        restoreMovement = false;
        restoreCombat = false;
    }

    void OnDisable()
    {
        if (IsActive)
        {
            source = null;
            RestoreControls();
        }
    }

    void OnGUI()
    {
        if (!showPanel || !IsActive)
            return;

        EnsureStyles();

        float width = Mathf.Min(panelWidth, Screen.width - 32f);
        float height = 150f;
        Rect panel = new Rect(
            (Screen.width - width) * 0.5f,
            Screen.height - panelBottomOffset - height,
            width,
            height);

        GUI.Box(panel, GUIContent.none);

        float x = panel.x + 18f;
        float innerWidth = panel.width - 36f;

        if (!string.IsNullOrWhiteSpace(CurrentSpeaker))
            GUI.Label(
                new Rect(x, panel.y + 12f, innerWidth, 28f),
                CurrentSpeaker,
                speakerStyle);

        GUI.Label(
            new Rect(x, panel.y + 43f, innerWidth, 70f),
            CurrentLine,
            lineStyle);

        string hint =
            "[" + advanceKey + "/" + alternateAdvanceKey + "] Continue    [" + closeKey + "] Close";

        GUI.Label(
            new Rect(x, panel.y + 116f, innerWidth, 24f),
            hint,
            hintStyle);
    }

    void EnsureStyles()
    {
        if (speakerStyle != null)
            return;

        speakerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold
        };

        lineStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleRight
        };
    }

#if UNITY_EDITOR
    public bool EditorAdvanceForQa() => Advance();
    public void EditorCloseForQa() => Close();
#endif
}

public static class MMDialogueBootstrap
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

        if (!player.GetComponent<MMDialogueController>())
            player.gameObject.AddComponent<MMDialogueController>();
    }
}

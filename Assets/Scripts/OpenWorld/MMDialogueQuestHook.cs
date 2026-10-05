using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MMDialogueInteractable))]
public class MMDialogueQuestHook : MonoBehaviour
{
    [Header("Quest")]
    public string questId;
    public string questTitle;
    [TextArea(2, 4)] public string objective;
    [Min(1)] public int goal = 1;

    [Header("Dialogue Hooks")]
    public bool startQuestOnDialogueStart = true;
    [Min(0)] public int progressOnDialogueComplete = 0;
    public bool completeQuestOnDialogueComplete;

    MMDialogueInteractable dialogue;
    bool bound;

    void Awake()
    {
        dialogue = GetComponent<MMDialogueInteractable>();
    }

    void OnEnable()
    {
        Bind();
    }

    void OnDisable()
    {
        Unbind();
    }

    void Bind()
    {
        if (bound)
            return;

        if (!dialogue)
            dialogue = GetComponent<MMDialogueInteractable>();

        if (!dialogue)
            return;

        dialogue.DialogueStarted += OnDialogueStarted;
        dialogue.DialogueCompleted += OnDialogueCompleted;
        bound = true;
    }

    void Unbind()
    {
        if (!bound || !dialogue)
            return;

        dialogue.DialogueStarted -= OnDialogueStarted;
        dialogue.DialogueCompleted -= OnDialogueCompleted;
        bound = false;
    }

#if UNITY_EDITOR
    public void EditorBindForQa()
    {
        Bind();
    }
#endif

    void OnDialogueStarted(GameObject interactor)
    {
        if (!startQuestOnDialogueStart)
            return;

        MMQuestManager quests = GetManager(interactor);
        if (!quests ||
            string.IsNullOrWhiteSpace(questId) ||
            quests.GetQuest(questId) != null)
            return;

        quests.StartQuest(
            questId,
            questTitle,
            objective,
            Mathf.Max(1, goal),
            true);
    }

    void OnDialogueCompleted(GameObject interactor)
    {
        MMQuestManager quests = GetManager(interactor);
        if (!quests || string.IsNullOrWhiteSpace(questId))
            return;

        MMQuestRecord record = quests.GetQuest(questId);
        if (record == null && startQuestOnDialogueStart)
        {
            quests.StartQuest(
                questId,
                questTitle,
                objective,
                Mathf.Max(1, goal),
                true);
            record = quests.GetQuest(questId);
        }

        if (record == null || record.status != MMQuestStatus.Active)
            return;

        if (progressOnDialogueComplete > 0)
            quests.AddProgress(
                questId,
                Mathf.Max(1, progressOnDialogueComplete));

        record = quests.GetQuest(questId);
        if (completeQuestOnDialogueComplete &&
            record != null &&
            record.status == MMQuestStatus.Active)
            quests.CompleteQuest(questId);
    }

    static MMQuestManager GetManager(GameObject interactor)
    {
        return interactor
            ? interactor.GetComponent<MMQuestManager>()
            : null;
    }
}

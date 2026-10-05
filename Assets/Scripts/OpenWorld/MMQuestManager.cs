using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum MMQuestStatus
{
    Active,
    Completed
}

[Serializable]
public class MMQuestRecord
{
    public string id;
    public string title;
    public string objective;
    public int progress;
    public int goal;
    public MMQuestStatus status;

    public float NormalizedProgress =>
        goal > 0 ? Mathf.Clamp01(progress / (float)goal) : 1f;
}

[DisallowMultipleComponent]
public class MMQuestManager : MonoBehaviour
{
    [Header("UI")]
    public KeyCode journalKey = KeyCode.J;
    public bool showTracker = true;
    public bool showJournal;
    public float trackerWidth = 360f;

    readonly Dictionary<string, MMQuestRecord> quests =
        new Dictionary<string, MMQuestRecord>(StringComparer.Ordinal);

    string trackedQuestId;
    GUIStyle titleStyle;
    GUIStyle objectiveStyle;
    GUIStyle hintStyle;

    public event Action<MMQuestRecord> QuestChanged;

    public IEnumerable<MMQuestRecord> Quests => quests.Values;
    public int QuestCount => quests.Count;

    public MMQuestRecord TrackedQuest =>
        !string.IsNullOrEmpty(trackedQuestId) &&
        quests.TryGetValue(trackedQuestId, out MMQuestRecord record)
            ? record
            : null;

    void Update()
    {
        if (Input.GetKeyDown(journalKey))
            showJournal = !showJournal;
    }

    public MMQuestRecord GetQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return null;

        quests.TryGetValue(questId.Trim(), out MMQuestRecord record);
        return record;
    }

    public bool StartQuest(
        string questId,
        string title,
        string objective,
        int goal = 1,
        bool track = true)
    {
        string id = NormalizeId(questId);
        if (id == null || quests.ContainsKey(id))
            return false;

        var record = new MMQuestRecord
        {
            id = id,
            title = string.IsNullOrWhiteSpace(title) ? id : title.Trim(),
            objective = objective == null ? string.Empty : objective.Trim(),
            progress = 0,
            goal = Mathf.Max(1, goal),
            status = MMQuestStatus.Active
        };

        quests.Add(id, record);

        if (track || TrackedQuest == null)
            trackedQuestId = id;

        QuestChanged?.Invoke(record);
        return true;
    }

    public bool AddProgress(string questId, int amount = 1)
    {
        MMQuestRecord record = GetQuest(questId);
        if (record == null ||
            record.status != MMQuestStatus.Active ||
            amount <= 0)
            return false;

        record.progress = Mathf.Clamp(
            record.progress + amount,
            0,
            record.goal);

        if (record.progress >= record.goal)
            record.status = MMQuestStatus.Completed;

        QuestChanged?.Invoke(record);
        return true;
    }

    public bool CompleteQuest(string questId)
    {
        MMQuestRecord record = GetQuest(questId);
        if (record == null ||
            record.status == MMQuestStatus.Completed)
            return false;

        record.progress = record.goal;
        record.status = MMQuestStatus.Completed;
        QuestChanged?.Invoke(record);
        return true;
    }

    public bool TrackQuest(string questId)
    {
        MMQuestRecord record = GetQuest(questId);
        if (record == null)
            return false;

        trackedQuestId = record.id;
        return true;
    }

    public bool RemoveQuest(string questId)
    {
        MMQuestRecord record = GetQuest(questId);
        if (record == null)
            return false;

        bool removed = quests.Remove(record.id);
        if (!removed)
            return false;

        if (trackedQuestId == record.id)
            trackedQuestId = quests.Values
                .FirstOrDefault(q => q.status == MMQuestStatus.Active)?.id;

        return true;
    }

#if UNITY_EDITOR
    public void EditorClearForQa()
    {
        quests.Clear();
        trackedQuestId = null;
        showJournal = false;
    }
#endif

    void OnGUI()
    {
        EnsureStyles();

        if (showTracker && TrackedQuest != null)
            DrawTracker(TrackedQuest);

        if (showJournal)
            DrawJournal();
    }

    void DrawTracker(MMQuestRecord quest)
    {
        float width = Mathf.Min(trackerWidth, Screen.width - 24f);
        float height = 92f;
        Rect box = new Rect(
            Screen.width - width - 18f,
            18f,
            width,
            height);

        GUI.Box(box, GUIContent.none);

        string state = quest.status == MMQuestStatus.Completed
            ? "Completed"
            : quest.title;

        GUI.Label(
            new Rect(box.x + 14f, box.y + 9f, box.width - 28f, 24f),
            state,
            titleStyle);

        string objective = quest.objective;
        if (quest.goal > 1)
            objective += "  " + quest.progress + "/" + quest.goal;

        GUI.Label(
            new Rect(box.x + 14f, box.y + 36f, box.width - 28f, 34f),
            objective,
            objectiveStyle);

        GUI.Label(
            new Rect(box.x + 14f, box.y + 68f, box.width - 28f, 18f),
            "[" + journalKey + "] Journal",
            hintStyle);
    }

    void DrawJournal()
    {
        float width = Mathf.Min(620f, Screen.width - 40f);
        float height = Mathf.Min(520f, Screen.height - 60f);
        Rect box = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height);

        GUI.Box(box, GUIContent.none);
        GUI.Label(
            new Rect(box.x + 18f, box.y + 14f, box.width - 36f, 30f),
            "Quest Journal",
            titleStyle);

        float y = box.y + 54f;
        foreach (MMQuestRecord quest in quests.Values)
        {
            if (y > box.yMax - 58f)
                break;

            string prefix =
                quest.status == MMQuestStatus.Completed ? "✓ " : "• ";
            string progress =
                quest.goal > 1
                    ? " (" + quest.progress + "/" + quest.goal + ")"
                    : string.Empty;

            GUI.Label(
                new Rect(box.x + 20f, y, box.width - 40f, 24f),
                prefix + quest.title + progress,
                objectiveStyle);

            GUI.Label(
                new Rect(box.x + 38f, y + 24f, box.width - 58f, 30f),
                quest.objective,
                hintStyle);

            y += 62f;
        }

        if (quests.Count == 0)
            GUI.Label(
                new Rect(box.x + 20f, y, box.width - 40f, 30f),
                "No quests.",
                objectiveStyle);
    }

    void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        objectiveStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            wordWrap = true
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleRight,
            wordWrap = true
        };
    }

    static string NormalizeId(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return null;

        return questId.Trim();
    }
}

public static class MMQuestBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureManager()
    {
        MMThirdPersonController player = null;
        MMThirdPersonController fallback = null;

        foreach (MMThirdPersonController candidate in
                 UnityEngine.Object.FindObjectsByType<MMThirdPersonController>(
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

        if (!player.GetComponent<MMQuestManager>())
            player.gameObject.AddComponent<MMQuestManager>();
    }
}

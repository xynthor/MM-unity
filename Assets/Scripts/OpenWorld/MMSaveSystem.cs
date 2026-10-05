using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

[Serializable]
public class MMSavedInventoryItem
{
    public string id;
    public string displayName;
    public int quantity;
}

[Serializable]
public class MMSavedQuest
{
    public string id;
    public string title;
    public string objective;
    public int progress;
    public int goal;
    public MMQuestStatus status;
}

[Serializable]
public class MMSaveData
{
    public int version = 1;
    public List<MMSavedInventoryItem> inventory =
        new List<MMSavedInventoryItem>();
    public List<MMSavedQuest> quests =
        new List<MMSavedQuest>();
    public List<string> worldFlags =
        new List<string>();
    public string trackedQuestId;
    public float playerX;
    public float playerY;
    public float playerZ;
    public float playerYaw;
}

[DisallowMultipleComponent]
public class MMSaveSystem : MonoBehaviour
{
    public const int CurrentVersion = 1;

    [Header("File")]
    public string saveFileName = "mmunity-save.json";

    [Header("Input")]
    public KeyCode saveKey = KeyCode.F5;
    public KeyCode loadKey = KeyCode.F9;

    [Header("State")]
    public bool savePlayerTransform = true;

    MMInventory inventory;
    MMQuestManager quests;
    MMWorldState worldState;

    public string LastMessage { get; private set; }
    public string DefaultSavePath =>
        Path.Combine(
            Application.persistentDataPath,
            string.IsNullOrWhiteSpace(saveFileName)
                ? "mmunity-save.json"
                : saveFileName.Trim());

    void Awake()
    {
        ResolveDependencies();
    }

    void Update()
    {
        if (Input.GetKeyDown(saveKey))
        {
            bool ok = SaveToPath(DefaultSavePath);
            LastMessage = ok ? "Game saved" : "Save failed";
        }

        if (Input.GetKeyDown(loadKey))
        {
            bool ok = LoadFromPath(DefaultSavePath);
            LastMessage = ok ? "Game loaded" : "Load failed";
        }
    }

    public MMSaveData Capture()
    {
        ResolveDependencies();

        var data = new MMSaveData
        {
            version = CurrentVersion
        };

        if (inventory)
        {
            foreach (MMInventoryEntry entry in inventory.Items)
            {
                data.inventory.Add(
                    new MMSavedInventoryItem
                    {
                        id = entry.id,
                        displayName = entry.displayName,
                        quantity = entry.quantity
                    });
            }
        }

        if (quests)
        {
            foreach (MMQuestRecord quest in quests.Quests)
            {
                data.quests.Add(
                    new MMSavedQuest
                    {
                        id = quest.id,
                        title = quest.title,
                        objective = quest.objective,
                        progress = quest.progress,
                        goal = quest.goal,
                        status = quest.status
                    });
            }

            if (quests.TrackedQuest != null)
                data.trackedQuestId = quests.TrackedQuest.id;
        }

        if (worldState)
            data.worldFlags.AddRange(worldState.Flags);

        if (savePlayerTransform)
        {
            Vector3 p = transform.position;
            data.playerX = p.x;
            data.playerY = p.y;
            data.playerZ = p.z;
            data.playerYaw = transform.eulerAngles.y;
        }

        return data;
    }

    public bool SaveToPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            MMSaveData data = Capture();
            string json = JsonUtility.ToJson(data, true);

            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string temporary = fullPath + ".tmp";
            File.WriteAllText(temporary, json);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            File.Move(temporary, fullPath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("MM save failed: " + e.Message);
            return false;
        }
    }

    public bool LoadFromPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);
            MMSaveData data = JsonUtility.FromJson<MMSaveData>(json);

            if (data == null ||
                data.version != CurrentVersion)
                return false;

            Restore(data);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("MM load failed: " + e.Message);
            return false;
        }
    }

    public void Restore(MMSaveData data)
    {
        if (data == null ||
            data.version != CurrentVersion)
            throw new ArgumentException(
                "Unsupported or missing save data.",
                nameof(data));

        ResolveDependencies();

        if (inventory)
            RestoreInventory(data.inventory);

        if (quests)
            RestoreQuests(data.quests, data.trackedQuestId);

        if (worldState)
        {
            worldState.ReplaceFlags(data.worldFlags);
            ApplyPersistentWorldObjects();
        }

        if (savePlayerTransform)
            RestoreTransform(data);
    }

    void RestoreInventory(List<MMSavedInventoryItem> saved)
    {
        foreach (MMInventoryEntry entry in inventory.Items.ToArray())
            inventory.RemoveItem(entry.id, entry.quantity);

        if (saved == null)
            return;

        foreach (MMSavedInventoryItem item in saved)
        {
            if (item == null ||
                string.IsNullOrWhiteSpace(item.id) ||
                item.quantity <= 0)
                continue;

            inventory.AddItem(
                item.id,
                item.quantity,
                item.displayName);
        }
    }

    void RestoreQuests(
        List<MMSavedQuest> saved,
        string trackedQuestId)
    {
        foreach (MMQuestRecord quest in quests.Quests.ToArray())
            quests.RemoveQuest(quest.id);

        if (saved != null)
        {
            foreach (MMSavedQuest item in saved)
            {
                if (item == null ||
                    string.IsNullOrWhiteSpace(item.id) ||
                    item.goal <= 0)
                    continue;

                if (!quests.StartQuest(
                        item.id,
                        item.title,
                        item.objective,
                        item.goal,
                        false))
                    continue;

                MMQuestRecord restored =
                    quests.GetQuest(item.id);

                if (restored == null)
                    continue;

                if (item.status == MMQuestStatus.Completed)
                {
                    quests.CompleteQuest(item.id);
                }
                else if (item.progress > 0)
                {
                    int progress = Mathf.Clamp(
                        item.progress,
                        0,
                        Mathf.Max(0, item.goal - 1));

                    if (progress > 0)
                        quests.AddProgress(item.id, progress);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(trackedQuestId))
            quests.TrackQuest(trackedQuestId);
    }

    void ApplyPersistentWorldObjects()
    {
        foreach (MMItemPickup pickup in
                 UnityEngine.Object.FindObjectsByType<MMItemPickup>(
                     FindObjectsInactive.Include))
        {
            if (pickup)
                pickup.ApplyPersistentState(worldState);
        }
    }

    void RestoreTransform(MMSaveData data)
    {
        CharacterController cc =
            GetComponent<CharacterController>();
        bool restoreController =
            cc && cc.enabled;

        if (restoreController)
            cc.enabled = false;

        transform.position = new Vector3(
            data.playerX,
            data.playerY,
            data.playerZ);
        transform.rotation =
            Quaternion.Euler(0f, data.playerYaw, 0f);

        if (restoreController)
            cc.enabled = true;
    }

    void ResolveDependencies()
    {
        if (!inventory)
            inventory = GetComponent<MMInventory>();
        if (!quests)
            quests = GetComponent<MMQuestManager>();
        if (!worldState)
            worldState = GetComponent<MMWorldState>();
    }
}

public static class MMSaveSystemBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureSaveSystem()
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

        GameObject go = player.gameObject;

        if (!go.GetComponent<MMInventory>())
            go.AddComponent<MMInventory>();
        if (!go.GetComponent<MMQuestManager>())
            go.AddComponent<MMQuestManager>();
        if (!go.GetComponent<MMWorldState>())
            go.AddComponent<MMWorldState>();
        if (!go.GetComponent<MMSaveSystem>())
            go.AddComponent<MMSaveSystem>();
    }
}

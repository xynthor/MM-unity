using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class MMInventoryEntry
{
    public string id;
    public string displayName;
    public int quantity;
}

[DisallowMultipleComponent]
public class MMInventory : MonoBehaviour
{
    [Header("UI")]
    public KeyCode inventoryKey = KeyCode.I;
    public bool showInventory;
    public float panelWidth = 420f;

    readonly Dictionary<string, MMInventoryEntry> items =
        new Dictionary<string, MMInventoryEntry>(StringComparer.Ordinal);

    GUIStyle titleStyle;
    GUIStyle itemStyle;
    GUIStyle hintStyle;

    public event Action<MMInventoryEntry> ItemChanged;

    public int UniqueItemCount => items.Count;
    public IEnumerable<MMInventoryEntry> Items =>
        items.Values.OrderBy(x => x.displayName ?? x.id);

    void Update()
    {
        if (Input.GetKeyDown(inventoryKey))
            showInventory = !showInventory;
    }

    public int GetCount(string itemId)
    {
        string id = NormalizeId(itemId);
        return id != null && items.TryGetValue(id, out MMInventoryEntry entry)
            ? entry.quantity
            : 0;
    }

    public bool HasItem(string itemId, int quantity = 1)
    {
        return quantity > 0 && GetCount(itemId) >= quantity;
    }

    public bool AddItem(
        string itemId,
        int quantity = 1,
        string displayName = null)
    {
        string id = NormalizeId(itemId);
        if (id == null || quantity <= 0)
            return false;

        if (!items.TryGetValue(id, out MMInventoryEntry entry))
        {
            entry = new MMInventoryEntry
            {
                id = id,
                displayName = NormalizeDisplayName(displayName, id),
                quantity = 0
            };
            items.Add(id, entry);
        }
        else if (!string.IsNullOrWhiteSpace(displayName))
        {
            entry.displayName = displayName.Trim();
        }

        checked
        {
            entry.quantity += quantity;
        }

        ItemChanged?.Invoke(entry);
        return true;
    }

    public bool RemoveItem(string itemId, int quantity = 1)
    {
        string id = NormalizeId(itemId);
        if (id == null ||
            quantity <= 0 ||
            !items.TryGetValue(id, out MMInventoryEntry entry) ||
            entry.quantity < quantity)
            return false;

        entry.quantity -= quantity;

        if (entry.quantity == 0)
        {
            items.Remove(id);
            ItemChanged?.Invoke(
                new MMInventoryEntry
                {
                    id = entry.id,
                    displayName = entry.displayName,
                    quantity = 0
                });
        }
        else
        {
            ItemChanged?.Invoke(entry);
        }

        return true;
    }

    public bool SetDisplayName(string itemId, string displayName)
    {
        string id = NormalizeId(itemId);
        if (id == null ||
            string.IsNullOrWhiteSpace(displayName) ||
            !items.TryGetValue(id, out MMInventoryEntry entry))
            return false;

        entry.displayName = displayName.Trim();
        ItemChanged?.Invoke(entry);
        return true;
    }

#if UNITY_EDITOR
    public void EditorClearForQa()
    {
        items.Clear();
        showInventory = false;
    }
#endif

    void OnGUI()
    {
        if (!showInventory)
            return;

        EnsureStyles();

        float width = Mathf.Min(panelWidth, Screen.width - 40f);
        float rowHeight = 28f;
        float contentHeight = Mathf.Max(1, items.Count) * rowHeight;
        float height = Mathf.Min(
            Screen.height - 60f,
            78f + contentHeight);

        Rect box = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height);

        GUI.Box(box, GUIContent.none);
        GUI.Label(
            new Rect(box.x + 18f, box.y + 14f, box.width - 36f, 28f),
            "Inventory",
            titleStyle);

        float y = box.y + 52f;

        if (items.Count == 0)
        {
            GUI.Label(
                new Rect(box.x + 20f, y, box.width - 40f, rowHeight),
                "Empty",
                itemStyle);
        }
        else
        {
            foreach (MMInventoryEntry entry in Items)
            {
                if (y > box.yMax - 44f)
                    break;

                GUI.Label(
                    new Rect(box.x + 20f, y, box.width - 40f, rowHeight),
                    entry.displayName + "  x" + entry.quantity,
                    itemStyle);

                y += rowHeight;
            }
        }

        GUI.Label(
            new Rect(box.x + 18f, box.yMax - 28f, box.width - 36f, 20f),
            "[" + inventoryKey + "] Close",
            hintStyle);
    }

    void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };

        itemStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleRight
        };
    }

    static string NormalizeId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        return itemId.Trim();
    }

    static string NormalizeDisplayName(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Trim();
    }
}

public static class MMInventoryBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInventory()
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

        if (!player.GetComponent<MMInventory>())
            player.gameObject.AddComponent<MMInventory>();
    }
}

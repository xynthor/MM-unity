using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class MMWorldState : MonoBehaviour
{
    readonly HashSet<string> flags =
        new HashSet<string>(StringComparer.Ordinal);

    public event Action<string, bool> FlagChanged;

    public int FlagCount => flags.Count;
    public IEnumerable<string> Flags =>
        flags.OrderBy(x => x, StringComparer.Ordinal);

    public bool HasFlag(string flagId)
    {
        string id = Normalize(flagId);
        return id != null && flags.Contains(id);
    }

    public bool SetFlag(string flagId, bool value = true)
    {
        string id = Normalize(flagId);
        if (id == null)
            return false;

        bool changed = value
            ? flags.Add(id)
            : flags.Remove(id);

        if (changed)
            FlagChanged?.Invoke(id, value);

        return changed;
    }

    public void ReplaceFlags(IEnumerable<string> values)
    {
        string[] previous = flags.ToArray();
        flags.Clear();

        if (values != null)
        {
            foreach (string value in values)
            {
                string id = Normalize(value);
                if (id != null)
                    flags.Add(id);
            }
        }

        foreach (string id in previous)
        {
            if (!flags.Contains(id))
                FlagChanged?.Invoke(id, false);
        }

        foreach (string id in flags)
        {
            if (!previous.Contains(id))
                FlagChanged?.Invoke(id, true);
        }
    }

#if UNITY_EDITOR
    public void EditorClearForQa()
    {
        ReplaceFlags(null);
    }
#endif

    static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

public static class MMWorldStateBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureWorldState()
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

        if (!player.GetComponent<MMWorldState>())
            player.gameObject.AddComponent<MMWorldState>();
    }
}

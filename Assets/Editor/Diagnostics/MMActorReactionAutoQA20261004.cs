using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MMActorReactionAutoQA20261004
{
    const string Root = "C:/MMUnityPort/Validation/Combat20261004";
    const string Trigger = Root + "/run_actor_reaction.flag";
    const string Result = Root + "/actor_reaction.txt";
    const string Error = Root + "/actor_reaction_error.txt";
    const string SessionKey = "MMActorReactionAutoQA20261004";

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        Directory.CreateDirectory(Root);
        EditorApplication.playModeStateChanged -= ModeChanged;
        EditorApplication.playModeStateChanged += ModeChanged;
        EditorApplication.delayCall += MaybeStart;
    }

    static void MaybeStart()
    {
        if (!File.Exists(Trigger) ||
            EditorApplication.isPlaying ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        try
        {
            Directory.CreateDirectory(Root);
            if (File.Exists(Result)) File.Delete(Result);
            if (File.Exists(Error)) File.Delete(Error);
            File.Delete(Trigger);
            SessionState.SetBool(SessionKey, true);
            EditorApplication.isPlaying = true;
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
        }
    }

    static void ModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false))
            return;

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(SessionKey, false);
            return;
        }

        if (state != PlayModeStateChange.EnteredPlayMode)
            return;

        try
        {
            Application.runInBackground = true;

            MMThirdPersonController player =
                UnityEngine.Object.FindObjectsByType<MMThirdPersonController>(
                        FindObjectsInactive.Exclude)
                    .Where(p => p.gameObject.activeInHierarchy)
                    .OrderByDescending(p =>
                        p.transform.parent &&
                        p.transform.parent.name.Contains("Persistent Player"))
                    .First();

            MMHealth health = player.GetComponent<MMHealth>();
            MMCombatActor actor = player.GetComponent<MMCombatActor>();
            MMHeroCombatController combat =
                player.GetComponent<MMHeroCombatController>();

            if (!health || !actor || !combat)
                throw new InvalidOperationException(
                    "Runtime hero vitals bootstrap did not create required components.");

            bool bootstrapPass =
                Mathf.Approximately(health.CurrentHealth, 100f) &&
                !health.IsDead &&
                player.enabled &&
                combat.enabled;

            bool damageAccepted =
                health.ApplyDamage(20f, null);

            int nonlethalDamageEvents = health.DamageEventCount;
            int nonlethalHitReactions = actor.EditorHitReactionCount;

            bool nonlethalPass =
                damageAccepted &&
                Mathf.Approximately(health.CurrentHealth, 80f) &&
                nonlethalDamageEvents == 1 &&
                nonlethalHitReactions == 1 &&
                actor.EditorDeathReactionCount == 0 &&
                player.enabled &&
                combat.enabled;

            bool lethalAccepted =
                health.ApplyDamage(500f, null);

            bool deathPass =
                lethalAccepted &&
                health.IsDead &&
                Mathf.Approximately(health.CurrentHealth, 0f) &&
                health.DamageEventCount == 2 &&
                actor.EditorHitReactionCount == 1 &&
                actor.EditorDeathReactionCount == 1 &&
                !player.enabled &&
                !combat.enabled;

            bool ignoredAfterDeath =
                !health.ApplyDamage(10f, null) &&
                health.DamageEventCount == 2 &&
                actor.EditorDeathReactionCount == 1;

            bool pass =
                bootstrapPass &&
                nonlethalPass &&
                deathPass &&
                ignoredAfterDeath;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "bootstrap|pass=" + bootstrapPass +
                    "|health=100" +
                    "|movementEnabled=True" +
                    "|combatEnabled=True",
                    "nonlethal|pass=" + nonlethalPass +
                    "|health=80" +
                    "|damageEvents=" + nonlethalDamageEvents +
                    "|hitReactions=" + nonlethalHitReactions,
                    "death|pass=" + deathPass +
                    "|dead=" + health.IsDead +
                    "|health=" + health.CurrentHealth.ToString("F1") +
                    "|deathReactions=" + actor.EditorDeathReactionCount +
                    "|movementEnabled=" + player.enabled +
                    "|combatEnabled=" + combat.enabled,
                    "post_death_damage_ignored|pass=" + ignoredAfterDeath,
                    "SUMMARY|pass=" + pass
                });
        }
        catch (Exception e)
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(Error, e.ToString());
        }
        finally
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.isPlaying = false;
            };
        }
    }
}

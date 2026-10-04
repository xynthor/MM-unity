using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MMCombatDefenseAutoQA20261004
{
    const string Root = "C:/MMUnityPort/Validation/Combat20261004";
    const string Trigger = Root + "/run_defense.flag";
    const string Result = Root + "/defense.txt";
    const string Error = Root + "/defense_error.txt";
    const string SessionKey = "MMCombatDefenseAutoQA20261004";

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

        GameObject attacker = null;
        GameObject target = null;

        try
        {
            Application.runInBackground = true;

            attacker = new GameObject("MM_DefenseQA_Attacker");
            attacker.transform.position = Vector3.zero;
            attacker.transform.rotation = Quaternion.identity;
            MMHeroCombatController attack =
                attacker.AddComponent<MMHeroCombatController>();

            target = new GameObject("MM_DefenseQA_Target");
            target.transform.position = new Vector3(0f, 0f, 1.15f);
            target.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            BoxCollider box = target.AddComponent<BoxCollider>();
            box.size = new Vector3(0.45f, 1.4f, 0.45f);
            SphereCollider sphere = target.AddComponent<SphereCollider>();
            sphere.radius = 0.34f;

            MMHealth health = target.AddComponent<MMHealth>();
            health.maxHealth = 100f;
            health.ResetHealth();

            MMHeroCombatController defense =
                target.AddComponent<MMHeroCombatController>();
            MMCombatActor actor = target.AddComponent<MMCombatActor>();
            actor.health = health;
            actor.blockDamageReduction = 0.70f;
            actor.blockArc = 120f;

            defense.EditorSetBlockingForQa(true);

            int frontalHits =
                attack.EditorApplyMeleeHitForQa(40f, 1.15f, 0.55f);

            float afterFrontal = health.CurrentHealth;
            bool frontalPass =
                frontalHits == 1 &&
                Mathf.Approximately(afterFrontal, 88f) &&
                health.DamageEventCount == 1 &&
                actor.EditorBlockedHitCount == 1 &&
                actor.EditorHitReactionCount == 0;

            attacker.transform.position =
                target.transform.position - target.transform.forward * 1.15f;
            attacker.transform.rotation =
                Quaternion.LookRotation(target.transform.position - attacker.transform.position);

            int rearHits =
                attack.EditorApplyMeleeHitForQa(20f, 1.15f, 0.55f);

            float afterRear = health.CurrentHealth;
            bool rearPass =
                rearHits == 1 &&
                Mathf.Approximately(afterRear, 68f) &&
                health.DamageEventCount == 2 &&
                actor.EditorBlockedHitCount == 1 &&
                actor.EditorHitReactionCount == 1;

            defense.EditorSetBlockingForQa(false);
            attacker.transform.position =
                target.transform.position + target.transform.forward * 1.15f;
            attacker.transform.rotation =
                Quaternion.LookRotation(target.transform.position - attacker.transform.position);

            int unblockedHits =
                attack.EditorApplyMeleeHitForQa(10f, 1.15f, 0.55f);

            float afterUnblocked = health.CurrentHealth;
            bool unblockedPass =
                unblockedHits == 1 &&
                Mathf.Approximately(afterUnblocked, 58f) &&
                health.DamageEventCount == 3 &&
                actor.EditorBlockedHitCount == 1 &&
                actor.EditorHitReactionCount == 2;

            bool pass = frontalPass && rearPass && unblockedPass;

            File.WriteAllLines(
                Result,
                new[]
                {
                    "frontal_block|pass=" + frontalPass +
                    "|health=" + afterFrontal.ToString("F1") +
                    "|blockedHits=" + actor.EditorBlockedHitCount +
                    "|hitReactionsAfterBlock=0",
                    "rear_bypass|pass=" + rearPass +
                    "|health=" + afterRear.ToString("F1") +
                    "|fullDamage=20" +
                    "|hitReactionsAfterRear=1",
                    "unblocked_front|pass=" + unblockedPass +
                    "|health=" + afterUnblocked.ToString("F1") +
                    "|fullDamage=10" +
                    "|hitReactionsFinal=" + actor.EditorHitReactionCount,
                    "SUMMARY|pass=" + pass
                });
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
        }
        finally
        {
            if (attacker) UnityEngine.Object.Destroy(attacker);
            if (target) UnityEngine.Object.Destroy(target);

            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.isPlaying = false;
            };
        }
    }
}

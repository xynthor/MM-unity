using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MMCombatRuntimeQA20261004
{
    const string Dir = "C:/MMUnityPort/Validation/Combat20261004";
    const string NearName = "MM_CombatQA_Near";
    const string FarName = "MM_CombatQA_Far";

    public static void Begin()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Combat QA Begin requires Play Mode.");

        Directory.CreateDirectory(Dir);
        Cleanup();

        Application.runInBackground = true;
        MMHeroCombatController combat = FindCombat();
        combat.EditorResetMeleeQaCounters();

        Vector3 origin = combat.hitOrigin
            ? combat.hitOrigin.position
            : combat.transform.position + Vector3.up * combat.hitHeight;

        GameObject near = CreateTarget(
            NearName,
            origin + combat.transform.forward * combat.lightReach,
            60f,
            true);

        GameObject far = CreateTarget(
            FarName,
            origin + combat.transform.forward * 3.50f,
            60f,
            false);

        combat.EditorTriggerLightAttackForQa();

        File.WriteAllText(
            Dir + "/runtime.txt",
            "BEGIN" +
            "|near=" + near.transform.position.ToString("F3") +
            "|far=" + far.transform.position.ToString("F3") +
            "|lightDelay=" + combat.lightHitDelay.ToString("F3") +
            Environment.NewLine);
    }

    public static bool Finish()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Combat QA Finish requires Play Mode.");

        MMHeroCombatController combat = FindCombat();
        MMHealth near = GameObject.Find(NearName).GetComponent<MMHealth>();
        MMHealth far = GameObject.Find(FarName).GetComponent<MMHealth>();

        float lightNearHealth = near.CurrentHealth;
        int lightNearEvents = near.DamageEventCount;
        float lightFarHealth = far.CurrentHealth;
        int lightFarEvents = far.DamageEventCount;
        int lightSwingCount = combat.EditorMeleeSwingCount;
        int lightTargetHitCount = combat.EditorMeleeTargetHitCount;

        bool delayedLightPass =
            Mathf.Approximately(lightNearHealth, 60f - combat.lightDamage) &&
            lightNearEvents == 1 &&
            Mathf.Approximately(lightFarHealth, 60f) &&
            lightFarEvents == 0 &&
            lightSwingCount == 1 &&
            lightTargetHitCount == 1;

        int lethalHits = combat.EditorApplyMeleeHitForQa(
            100f,
            combat.lightReach,
            combat.lightRadius);

        bool lethalPass =
            lethalHits == 1 &&
            near.IsDead &&
            near.DamageEventCount == 2 &&
            Mathf.Approximately(near.CurrentHealth, 0f) &&
            Mathf.Approximately(far.CurrentHealth, 60f);

        int outOfRangeHits = combat.EditorApplyMeleeHitForQa(
            20f,
            0.10f,
            0.10f);

        bool rangePass =
            outOfRangeHits == 0 &&
            Mathf.Approximately(far.CurrentHealth, 60f) &&
            far.DamageEventCount == 0;

        bool pass = delayedLightPass && lethalPass && rangePass;

        string[] rows =
        {
            "delayed_light|pass=" + delayedLightPass +
            "|nearHealth=" + lightNearHealth.ToString("F1") +
            "|nearEvents=" + lightNearEvents +
            "|farHealth=" + lightFarHealth.ToString("F1") +
            "|farEvents=" + lightFarEvents +
            "|swingCount=" + lightSwingCount +
            "|targetHitCount=" + lightTargetHitCount,
            "lethal_followup|pass=" + lethalPass +
            "|lethalHits=" + lethalHits +
            "|dead=" + near.IsDead +
            "|nearEvents=" + near.DamageEventCount,
            "out_of_range|pass=" + rangePass +
            "|hits=" + outOfRangeHits +
            "|farHealth=" + far.CurrentHealth.ToString("F1") +
            "|farEvents=" + far.DamageEventCount,
            "SUMMARY|pass=" + pass
        };

        File.AppendAllLines(Dir + "/runtime.txt", rows);
        Cleanup();
        return pass;
    }

    static MMHeroCombatController FindCombat()
    {
        MMHeroCombatController[] all =
            UnityEngine.Object.FindObjectsByType<MMHeroCombatController>(
                FindObjectsInactive.Exclude);

        MMHeroCombatController persistent = all
            .Where(c => c.gameObject.activeInHierarchy)
            .FirstOrDefault(c =>
                c.transform.parent &&
                c.transform.parent.name.Contains("Persistent Player"));

        if (persistent)
            return persistent;

        MMHeroCombatController active =
            all.FirstOrDefault(c => c.gameObject.activeInHierarchy);

        if (!active)
            throw new InvalidOperationException("Active hero combat controller missing.");

        return active;
    }

    static GameObject CreateTarget(
        string name,
        Vector3 position,
        float healthValue,
        bool duplicateCollider)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;

        BoxCollider box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(0.42f, 0.82f, 0.42f);

        if (duplicateCollider)
        {
            SphereCollider sphere = go.AddComponent<SphereCollider>();
            sphere.radius = 0.32f;
        }

        MMHealth health = go.AddComponent<MMHealth>();
        health.maxHealth = healthValue;
        health.ResetHealth();
        return go;
    }

    static void Cleanup()
    {
        foreach (string n in new[] { NearName, FarName })
        {
            GameObject go = GameObject.Find(n);
            if (go)
                UnityEngine.Object.Destroy(go);
        }
    }
}

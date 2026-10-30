using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MMInteractionRuntimeQA20261004
{
    const string Dir = "C:/MMUnityPort/Validation/Interaction20261004";

    public static bool Run()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Interaction QA requires Play Mode.");

        Directory.CreateDirectory(Dir);
        Application.runInBackground = true;

        MMThirdPersonController player =
            UnityEngine.Object.FindObjectsByType<MMThirdPersonController>(
                    FindObjectsInactive.Exclude)
                .First(p => p.gameObject.activeInHierarchy);

        MMInteractionController interaction =
            player.GetComponent<MMInteractionController>();

        MMHeroCombatController combat =
            player.GetComponent<MMHeroCombatController>();

        if (!interaction || !combat || !player.playerCamera)
            throw new InvalidOperationException(
                "Player interaction/combat/camera missing.");

        Vector3 forward = player.playerCamera.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = player.transform.forward;
        forward.Normalize();

        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "MM_InteractionQA_Target";
        target.transform.localScale = new Vector3(0.55f, 0.70f, 0.55f);

        MMSimpleInteractable simple =
            target.AddComponent<MMSimpleInteractable>();
        simple.prompt = "Inspect";
        simple.interactOnce = true;

        target.transform.position =
            player.transform.position +
            Vector3.up * interaction.interactionHeight +
            forward * 1.60f;

        Physics.SyncTransforms();

        MMInteractable acquired =
            interaction.EditorScanNowForQa();

        float nearPlayerDistance = Vector3.Distance(
            player.transform.position,
            target.transform.position);

        bool acquirePass =
            acquired == simple &&
            interaction.CurrentPrompt == "Inspect" &&
            nearPlayerDistance <= interaction.interactionRange;

        bool firstInteract =
            interaction.EditorInteractNowForQa();

        bool oneShotPass =
            firstInteract &&
            simple.InteractionCount == 1 &&
            interaction.EditorScanNowForQa() == null &&
            !interaction.EditorInteractNowForQa() &&
            simple.InteractionCount == 1;

        simple.interactOnce = false;
        target.transform.position =
            player.transform.position +
            Vector3.up * interaction.interactionHeight +
            forward * (interaction.interactionRange + 1.15f);

        Physics.SyncTransforms();

        float farPlayerDistance = Vector3.Distance(
            player.transform.position,
            target.transform.position);

        bool rangePass =
            farPlayerDistance > interaction.interactionRange &&
            interaction.EditorScanNowForQa() == null;

        target.transform.position =
            player.transform.position +
            Vector3.up * interaction.interactionHeight +
            forward * 1.60f;

        GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name = "MM_InteractionQA_Blocker";
        blocker.transform.localScale = new Vector3(0.85f, 0.90f, 0.24f);
        blocker.transform.position =
            player.transform.position +
            Vector3.up * interaction.interactionHeight +
            forward * 0.82f;

        Physics.SyncTransforms();

        bool occlusionPass =
            interaction.EditorScanNowForQa() == null;

        bool inputPass =
            interaction.interactKey == KeyCode.E &&
            combat.kickKey == KeyCode.C &&
            interaction.interactKey != combat.kickKey;

        bool pass =
            acquirePass &&
            oneShotPass &&
            rangePass &&
            occlusionPass &&
            inputPass;

        string[] rows =
        {
            "acquire|pass=" + acquirePass +
            "|prompt=" + (acquired ? acquired.Prompt : "NULL") +
            "|distance=" + nearPlayerDistance.ToString("F2") +
            "|limit=" + interaction.interactionRange.ToString("F2"),
            "one_shot|pass=" + oneShotPass +
            "|count=" + simple.InteractionCount,
            "range|pass=" + rangePass +
            "|distance=" + farPlayerDistance.ToString("F2") +
            "|limit=" + interaction.interactionRange.ToString("F2"),
            "occlusion|pass=" + occlusionPass,
            "input_separation|pass=" + inputPass +
            "|interact=" + interaction.interactKey +
            "|kick=" + combat.kickKey,
            "SUMMARY|pass=" + pass
        };

        File.WriteAllLines(Dir + "/runtime.txt", rows);

        UnityEngine.Object.Destroy(target);
        UnityEngine.Object.Destroy(blocker);
        return pass;
    }
}

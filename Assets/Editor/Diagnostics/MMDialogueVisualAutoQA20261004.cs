using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MMDialogueVisualAutoQA20261004
{
    const string Root = "C:/MMUnityPort/Validation/Dialogue20261004";
    const string Trigger = Root + "/visual.flag";
    const string Shot = Root + "/dialogue_panel.png";
    const string Error = Root + "/visual_error.txt";
    const string SessionKey = "MMDialogueVisualAutoQA20261004";

    static GameObject target;
    static int startFrame;
    static bool requested;

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        Directory.CreateDirectory(Root);
        EditorApplication.playModeStateChanged -= ModeChanged;
        EditorApplication.playModeStateChanged += ModeChanged;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
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
            if (File.Exists(Shot)) File.Delete(Shot);
            if (File.Exists(Error)) File.Delete(Error);
            File.Delete(Trigger);
            requested = false;
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

            MMHealth vitals = player.GetComponent<MMHealth>();
            if (vitals) vitals.ResetHealth();
            player.enabled = true;

            MMHeroCombatController combat =
                player.GetComponent<MMHeroCombatController>();
            if (combat) combat.enabled = true;

            MMInteractionController interaction =
                player.GetComponent<MMInteractionController>();
            MMDialogueController dialogue =
                player.GetComponent<MMDialogueController>();

            if (dialogue.IsActive)
                dialogue.EditorCloseForQa();

            Vector3 forward = player.playerCamera.transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.001f
                ? forward.normalized
                : player.transform.forward;

            target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "MM_DialogueVisualQA";
            target.transform.localScale = new Vector3(0.55f, 0.80f, 0.55f);
            target.transform.position =
                player.transform.position +
                Vector3.up * interaction.interactionHeight +
                forward * 1.60f;

            MMDialogueInteractable talk =
                target.AddComponent<MMDialogueInteractable>();
            talk.speaker = "Loremaster";
            talk.lines = new[]
            {
                "The road ahead is open, but the old ruins beyond the ridge are dangerous."
            };

            Physics.SyncTransforms();

            if (!dialogue.StartDialogue(talk) || !dialogue.IsActive)
                throw new InvalidOperationException("Could not start dialogue visual QA.");

            startFrame = Time.frameCount;
        }
        catch (Exception e)
        {
            File.WriteAllText(Error, e.ToString());
            EditorApplication.isPlaying = false;
        }
    }

    static void Tick()
    {
        if (!SessionState.GetBool(SessionKey, false) ||
            !EditorApplication.isPlaying ||
            startFrame <= 0)
            return;

        if (!requested && Time.frameCount >= startFrame + 4)
        {
            ScreenCapture.CaptureScreenshot(Shot);
            requested = true;
            return;
        }

        if (requested &&
            Time.frameCount >= startFrame + 8 &&
            File.Exists(Shot))
        {
            if (target) UnityEngine.Object.Destroy(target);
            startFrame = 0;
            requested = false;
            EditorApplication.isPlaying = false;
        }
    }
}

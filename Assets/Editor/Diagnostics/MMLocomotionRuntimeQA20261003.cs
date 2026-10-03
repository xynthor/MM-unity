using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MMLocomotionRuntimeQA20261003
{
    const string SessionKey = "MMLocomotionRuntimeQA20261003";
    const string Dir = "C:/MMUnityPort/Validation/Hero/Locomotion20261003";
    const string Report = Dir + "/deterministic_runtime.txt";
    const string Error = Dir + "/deterministic_error.txt";

    static MMThirdPersonController player;
    static Animator animator;
    static MMHeroFootIK footIK;
    static Vector3 startPosition;
    static double nextAt;
    static int step;
    static bool sawAir;
    static bool sawUngrounded;
    static float bestLateral;
    static readonly List<string> rows = new List<string>();

    static MMLocomotionRuntimeQA20261003()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += PlayModeChanged;
    }

    public static void Start()
    {
        Directory.CreateDirectory(Dir);
        if (File.Exists(Report)) File.Delete(Report);
        if (File.Exists(Error)) File.Delete(Error);

        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Locomotion QA must start from Edit Mode.");

        SessionState.SetBool(SessionKey, true);
        EditorApplication.isPlaying = true;
    }

    static void PlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            try
            {
                Application.runInBackground = true;
                player = UnityEngine.Object
                    .FindObjectsByType<MMThirdPersonController>(FindObjectsInactive.Exclude)
                    .Single(p => p.gameObject.activeInHierarchy);

                animator = player.animator;
                if (!animator || !animator.runtimeAnimatorController)
                    throw new InvalidOperationException("Player Animator/controller is missing.");

                footIK = animator.GetComponent<MMHeroFootIK>();
                if (!footIK)
                    throw new InvalidOperationException("MMHeroFootIK was not attached at runtime.");

                player.EditorClearTestInput();
                player.rotationSmoothTime = 0.085f;
                startPosition = player.transform.position;
                rows.Clear();
                step = 0;
                sawAir = false;
                sawUngrounded = false;
                bestLateral = 0f;
                Record("baseline");
                nextAt = Time.realtimeSinceStartupAsDouble + 0.20;
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(SessionKey, false);
        }
    }

    static void Tick()
    {
        if (!SessionState.GetBool(SessionKey, false) ||
            !EditorApplication.isPlaying ||
            !player ||
            Time.realtimeSinceStartupAsDouble < nextAt)
            return;

        try
        {
            bool grounded = player.GetComponent<CharacterController>().isGrounded;
            if (!grounded) sawUngrounded = true;
            if (animator.GetCurrentAnimatorStateInfo(0).IsName("Air")) sawAir = true;
            bestLateral = Mathf.Max(bestLateral, Mathf.Abs(animator.GetFloat("MoveX")));

            switch (step++)
            {
                case 0:
                    player.EditorSetTestInput(new Vector2(0f, 1f), false, false, false);
                    nextAt += 0.70;
                    break;

                case 1:
                    Record("forward");
                    MMSequentialEvidence.Render(
                        player.playerCamera,
                        Dir + "/Deterministic_Forward.png",
                        1280, 720);
                    player.rotationSmoothTime = 0.80f;
                    player.EditorSetTestInput(new Vector2(1f, 0f), false, false, false);
                    nextAt = Time.realtimeSinceStartupAsDouble + 0.18;
                    break;

                case 2:
                    Record("lateral");
                    MMSequentialEvidence.Render(
                        player.playerCamera,
                        Dir + "/Deterministic_Lateral.png",
                        1280, 720);
                    player.rotationSmoothTime = 0.085f;
                    player.EditorSetTestInput(new Vector2(0f, 1f), true, false, false);
                    nextAt = Time.realtimeSinceStartupAsDouble + 0.65;
                    break;

                case 3:
                    Record("sprint");
                    player.EditorSetTestInput(Vector2.zero, false, false, true);
                    nextAt = Time.realtimeSinceStartupAsDouble + 0.10;
                    break;

                case 4:
                    Record("jump_rise");
                    MMSequentialEvidence.Render(
                        player.playerCamera,
                        Dir + "/Deterministic_JumpRise.png",
                        1280, 720);
                    nextAt = Time.realtimeSinceStartupAsDouble + 0.30;
                    break;

                case 5:
                    Record("jump_mid");
                    nextAt = Time.realtimeSinceStartupAsDouble + 0.25;
                    break;

                default:
                    grounded = player.GetComponent<CharacterController>().isGrounded;
                    if (sawUngrounded && grounded)
                    {
                        Record("landed");
                        Finish();
                    }
                    else if (step > 20)
                    {
                        throw new TimeoutException("Player did not complete jump/landing cycle.");
                    }
                    else
                    {
                        nextAt = Time.realtimeSinceStartupAsDouble + 0.15;
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Fail(e);
        }
    }

    static string StateName()
    {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion")) return "Locomotion";
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Air")) return "Air";
        return "Other";
    }

    static void Record(string label)
    {
        bool grounded = player.GetComponent<CharacterController>().isGrounded;
        if (!grounded) sawUngrounded = true;
        if (StateName() == "Air") sawAir = true;

        float moveX = animator.GetFloat("MoveX");
        bestLateral = Mathf.Max(bestLateral, Mathf.Abs(moveX));

        rows.Add(
            label +
            "|time=" + Time.time.ToString("F3") +
            "|position=" + player.transform.position.ToString("F3") +
            "|distance=" + Vector3.Distance(startPosition, player.transform.position).ToString("F3") +
            "|state=" + StateName() +
            "|speed=" + animator.GetFloat("Speed").ToString("F3") +
            "|moveX=" + moveX.ToString("F3") +
            "|moveY=" + animator.GetFloat("MoveY").ToString("F3") +
            "|vertical=" + player.EditorVerticalVelocity.ToString("F3") +
            "|verticalParam=" + animator.GetFloat("VerticalSpeed").ToString("F3") +
            "|grounded=" + grounded +
            "|ikL=" + footIK.LastLeftHit +
            "|ikR=" + footIK.LastRightHit +
            "|ikWL=" + footIK.LeftWeight.ToString("F2") +
            "|ikWR=" + footIK.RightWeight.ToString("F2"));

        File.WriteAllLines(Report, rows);
    }

    static void Finish()
    {
        player.EditorClearTestInput();

        float distance = Vector3.Distance(startPosition, player.transform.position);
        bool ikPass = footIK.LeftWeight > 0f || footIK.RightWeight > 0f;
        bool pass =
            distance > 2f &&
            bestLateral > 0.15f &&
            sawAir &&
            sawUngrounded &&
            ikPass &&
            animator.runtimeAnimatorController != null;

        rows.Add(
            "SUMMARY|pass=" + pass +
            "|distance=" + distance.ToString("F3") +
            "|bestLateral=" + bestLateral.ToString("F3") +
            "|sawAir=" + sawAir +
            "|sawUngrounded=" + sawUngrounded +
            "|ikPass=" + ikPass +
            "|controller=" + animator.runtimeAnimatorController.name);

        File.WriteAllLines(Report, rows);
        SessionState.SetBool(SessionKey, false);
        EditorApplication.isPlaying = false;
    }

    static void Fail(Exception e)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(Error, e.ToString());
        SessionState.SetBool(SessionKey, false);
        if (EditorApplication.isPlaying)
            EditorApplication.isPlaying = false;
    }
}

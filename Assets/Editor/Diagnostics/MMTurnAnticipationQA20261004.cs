using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MMTurnAnticipationQA20261004
{
    const string SessionKey = "MMTurnAnticipationQA20261004";
    const string Dir = "C:/MMUnityPort/Validation/Hero/TurnAnticipation20261004";
    const string Report = Dir + "/runtime.txt";
    const string Error = Dir + "/error.txt";

    static MMThirdPersonController player;
    static Animator animator;
    static double nextAt;
    static int step;
    static bool sawLeft;
    static bool sawRight;
    static readonly List<string> rows = new List<string>();

    static MMTurnAnticipationQA20261004()
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
            throw new InvalidOperationException("Turn QA must start from Edit Mode.");

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
                    throw new InvalidOperationException("Active hero Animator/controller missing.");

                player.EditorClearTestInput();
                rows.Clear();
                sawLeft = false;
                sawRight = false;
                step = 0;
                Record("baseline");
                nextAt = Time.realtimeSinceStartupAsDouble + 1.00;
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
            Observe();

            switch (step++)
            {
                case 0:
                    if (!player.GetComponent<CharacterController>().isGrounded)
                    {
                        step--;
                        nextAt = Time.realtimeSinceStartupAsDouble + 0.10;
                        break;
                    }
                    PrimeForTurn(90f);
                    player.EditorSetTestInput(new Vector2(0f, 1f), false, false, false);
                    nextAt = Time.realtimeSinceStartupAsDouble + 0.12;
                    break;

                case 1:
                    Observe();
                    Record("right_early");
                    MMSequentialEvidence.Render(player.playerCamera, Dir + "/TurnRight_Early.png", 1280, 720);
                    nextAt = Time.realtimeSinceStartupAsDouble + 0.34;
                    break;

                case 2:
                    Observe();
                    Record("right_mid");
                    MMSequentialEvidence.Render(player.playerCamera, Dir + "/TurnRight_Mid.png", 1280, 720);
                    player.EditorSetTestInput(Vector2.zero);
                    nextAt = Time.realtimeSinceStartupAsDouble + 0.70;
                    break;

                case 3:
                    if (!player.GetComponent<CharacterController>().isGrounded ||
                        player.EditorPlanarVelocity.magnitude > player.turnAnticipationMaxSpeed)
                    {
                        step--;
                        nextAt = Time.realtimeSinceStartupAsDouble + 0.10;
                        break;
                    }
                    PrimeForTurn(-90f);
                    player.EditorSetTestInput(new Vector2(0f, 1f), false, false, false);
                    nextAt = Time.realtimeSinceStartupAsDouble + 0.12;
                    break;

                case 4:
                    Observe();
                    Record("left_early");
                    MMSequentialEvidence.Render(player.playerCamera, Dir + "/TurnLeft_Early.png", 1280, 720);
                    nextAt = Time.realtimeSinceStartupAsDouble + 0.34;
                    break;

                case 5:
                    Observe();
                    Record("left_mid");
                    MMSequentialEvidence.Render(player.playerCamera, Dir + "/TurnLeft_Mid.png", 1280, 720);
                    player.EditorClearTestInput();
                    nextAt = Time.realtimeSinceStartupAsDouble + 0.55;
                    break;

                default:
                    Observe();
                    Record("finish");
                    Finish();
                    break;
            }
        }
        catch (Exception e)
        {
            Fail(e);
        }
    }

    static void PrimeForTurn(float offsetDegrees)
    {
        player.EditorSetTestInput(Vector2.zero);
        Vector3 forward = player.playerCamera ? player.playerCamera.transform.forward : Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        float cameraYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        player.transform.rotation = Quaternion.Euler(0f, cameraYaw - offsetDegrees, 0f);
    }

    static void Observe()
    {
        var state = animator.GetCurrentAnimatorStateInfo(0);
        sawLeft |= state.IsName("Turn Left");
        sawRight |= state.IsName("Turn Right");
    }

    static string StateName()
    {
        var s = animator.GetCurrentAnimatorStateInfo(0);
        if (s.IsName("Turn Left")) return "Turn Left";
        if (s.IsName("Turn Right")) return "Turn Right";
        if (s.IsName("Locomotion")) return "Locomotion";
        if (s.IsName("Air")) return "Air";
        return "Other";
    }

    static void Record(string label)
    {
        rows.Add(
            label +
            "|time=" + Time.time.ToString("F3") +
            "|state=" + StateName() +
            "|yaw=" + player.transform.eulerAngles.y.ToString("F1") +
            "|speed=" + animator.GetFloat("Speed").ToString("F3") +
            "|moveX=" + animator.GetFloat("MoveX").ToString("F3") +
            "|moveY=" + animator.GetFloat("MoveY").ToString("F3"));
        File.WriteAllLines(Report, rows);
    }

    static void Finish()
    {
        bool pass = sawLeft && sawRight;
        rows.Add("SUMMARY|pass=" + pass + "|sawLeft=" + sawLeft + "|sawRight=" + sawRight);
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

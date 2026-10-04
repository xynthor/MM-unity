using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MMTraversalRuntimeQA20261004
{
    const string Key = "MMTraversalRuntimeQA20261004";
    const string Dir = "C:/MMUnityPort/Validation/Traversal20261004/Runtime";
    const float WaterY = 0.10f;

    static MMThirdPersonController player;
    static CharacterController cc;
    static Camera cam;
    static int phase;
    static int lastFrame = -1;
    static float elapsed;
    static Vector3 start;
    static Vector3 direction;
    static float minY;
    static float maxY;
    static int groundedFrames;
    static int sampleFrames;
    static Bounds activeBounds;
    static readonly List<string> rows = new List<string>();

    static MMTraversalRuntimeQA20261004()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += Mode;
    }

    public static void Start()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Traversal QA must start in Edit Mode.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Saved scene required.");

        Directory.CreateDirectory(Dir);
        foreach (string f in Directory.GetFiles(Dir))
            File.Delete(f);

        rows.Clear();
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    static void Mode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            try
            {
                Application.runInBackground = true;
                player = UnityEngine.Object.FindObjectsByType<MMThirdPersonController>(FindObjectsInactive.Exclude)
                    .Where(p => p.gameObject.activeInHierarchy)
                    .OrderByDescending(p => p.transform.parent && p.transform.parent.name.Contains("Persistent Player"))
                    .First();
                cc = player.GetComponent<CharacterController>();
                cam = player.playerCamera;
                if (!cc || !cam) throw new InvalidOperationException("Player controller/camera missing.");

                phase = 0;
                lastFrame = -1;
                BeginWalkableSlope();
            }
            catch (Exception e) { Fail(e); }
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Key, false);
        }
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || !player) return;
        if (lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;

        try
        {
            elapsed += Time.deltaTime;
            minY = Mathf.Min(minY, player.transform.position.y);
            maxY = Mathf.Max(maxY, player.transform.position.y);
            if (cc.isGrounded) groundedFrames++;
            sampleFrames++;

            switch (phase)
            {
                case 0:
                    if (elapsed >= 0.75f) EndWalkableSlope();
                    break;
                case 1:
                    if (elapsed >= 0.55f) EndSteepSlope();
                    break;
                case 2:
                    if (elapsed >= 2.0f) EndBridge();
                    break;
                case 3:
                    {
                        Vector3 stairDelta = player.transform.position - start;
                        float stairForward = Vector3.Dot(
                            new Vector3(stairDelta.x, 0f, stairDelta.z),
                            direction);
                        float stairRise = maxY - start.y;
                        if ((elapsed >= 0.45f && stairForward > 1.65f && stairRise > 0.55f) ||
                            elapsed >= 1.25f)
                            EndStairs();
                    }
                    break;
                case 4:
                    if (elapsed >= 0.85f) EndHouseCollision();
                    break;
                case 5:
                    if (elapsed >= 0.35f) EndCameraCollision();
                    break;
                case 6:
                    if (elapsed >= 1.15f) EndCoast();
                    break;
            }
        }
        catch (Exception e) { Fail(e); }
    }

    static void ResetTracking(Vector3 s, Vector3 dir)
    {
        start = s;
        direction = dir.normalized;
        elapsed = 0f;
        minY = maxY = s.y;
        groundedFrames = 0;
        sampleFrames = 0;
    }

    static void Teleport(Vector3 pos)
    {
        player.EditorClearTestInput();
        cc.enabled = false;
        player.transform.position = pos;
        cc.enabled = true;
        player.EditorResetMovementQaCounters();
    }

    static Vector2 InputForWorld(Vector3 worldDir)
    {
        worldDir.y = 0f;
        worldDir.Normalize();
        Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
        Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
        return Vector2.ClampMagnitude(new Vector2(Vector3.Dot(worldDir, r), Vector3.Dot(worldDir, f)), 1f);
    }

    static float GroundY(float x, float z, float fallback)
    {
        if (Physics.Raycast(new Vector3(x, 500f, z), Vector3.down, out RaycastHit hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return fallback;
    }

    static void Capture(string name)
    {
        MMSequentialEvidence.Render(cam, Dir + "/" + name + ".png", 1280, 720);
    }

    static Terrain TerrainByName(string name)
        => Terrain.activeTerrains.Single(t => t.terrainData && t.terrainData.name == name);

    static Vector3 UphillAt(Terrain t, Vector3 probe, out float slope)
    {
        TerrainData d = t.terrainData;
        float u = Mathf.InverseLerp(t.transform.position.x, t.transform.position.x + d.size.x, probe.x);
        float v = Mathf.InverseLerp(t.transform.position.z, t.transform.position.z + d.size.z, probe.z);
        Vector3 n = d.GetInterpolatedNormal(u, v);
        slope = d.GetSteepness(u, v);
        return new Vector3(-n.x / Mathf.Max(0.001f, n.y), 0f, -n.z / Mathf.Max(0.001f, n.y)).normalized;
    }

    static void BeginWalkableSlope()
    {
        phase = 0;
        Terrain t = TerrainByName("FrozenHighlands_LinkedNorthProfile");
        Vector3 probe = new Vector3(160f, 0f, 384f);
        Vector3 up = UphillAt(t, probe, out float slope);
        Vector3 s = probe - up * 3f;
        s.y = t.SampleHeight(s) + t.transform.position.y + 0.06f;
        Teleport(s);
        ResetTracking(s, up);
        player.EditorSetTestInput(InputForWorld(up), false, false, false);
        rows.Add("walkable_slope_begin|slope=" + slope.ToString("F2") + "|start=" + s.ToString("F3"));
    }

    static void EndWalkableSlope()
    {
        player.EditorClearTestInput();
        Vector3 d = player.transform.position - start;
        float forward = Vector3.Dot(new Vector3(d.x, 0f, d.z), direction);
        float dy = d.y;
        bool pass = forward > 1.5f && dy > 0.5f && GroundedFraction() > 0.65f;
        rows.Add($"walkable_slope|pass={pass}|forward={forward:F3}|dy={dy:F3}|groundedFrac={GroundedFraction():F2}|end={player.transform.position:F3}");
        Capture("01_WalkableSlope");
        BeginSteepSlope();
    }

    static void BeginSteepSlope()
    {
        phase = 1;
        Terrain best = null;
        Vector3 probe = Vector3.zero;
        float bestSlope = -1f;

        foreach (Terrain t in Terrain.activeTerrains)
        {
            TerrainData d = t.terrainData;
            for (int iz = 2; iz < 31; iz++)
            for (int ix = 2; ix < 31; ix++)
            {
                float u = ix / 32f, v = iz / 32f;
                float s = d.GetSteepness(u, v);
                if (s < 60f || s > 72f) continue;
                Vector3 p = t.transform.position + new Vector3(u * d.size.x, d.GetInterpolatedHeight(u, v), v * d.size.z);
                if (p.y < 3f) continue;
                if (s > bestSlope) { bestSlope = s; best = t; probe = p; }
            }
        }

        if (!best) throw new InvalidOperationException("No stable >60 degree slope candidate.");

        Vector3 up = UphillAt(best, probe, out float slope);
        Vector3 s0 = probe - up * 1.5f;
        s0.y = best.SampleHeight(s0) + best.transform.position.y + 0.06f;
        Teleport(s0);
        ResetTracking(s0, up);
        player.EditorSetTestInput(InputForWorld(up), false, false, false);
        rows.Add("steep_slope_begin|terrain=" + best.terrainData.name + "|slope=" + slope.ToString("F2") + "|start=" + s0.ToString("F3"));
    }

    static void EndSteepSlope()
    {
        player.EditorClearTestInput();
        Vector3 d = player.transform.position - start;
        float forward = Vector3.Dot(new Vector3(d.x, 0f, d.z), direction);
        float dy = d.y;
        bool pass = dy < 0.9f || forward < 1.2f;
        rows.Add($"steep_slope|pass={pass}|forward={forward:F3}|dy={dy:F3}|minY={minY:F3}|maxY={maxY:F3}|end={player.transform.position:F3}");
        Capture("02_SteepSlope");
        BeginBridge();
    }

    static Transform FindSemantic(string exactName)
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .First(t => t.gameObject.activeInHierarchy && t.name == exactName);
    }

    static void BeginBridge()
    {
        phase = 2;
        Transform t = FindSemantic("088_M087_bridge2ns");
        Collider col = t.GetComponentInChildren<Collider>(true);
        activeBounds = col.bounds;
        Vector3 dir = Vector3.forward;
        Vector3 s = activeBounds.center - dir * (activeBounds.extents.z - 1.4f);
        s.y = activeBounds.max.y + 0.06f;
        Teleport(s);
        ResetTracking(s, dir);
        player.EditorSetTestInput(InputForWorld(dir), false, false, false);
        rows.Add("bridge_begin|bounds=" + activeBounds + "|start=" + s.ToString("F3"));
    }

    static void EndBridge()
    {
        player.EditorClearTestInput();
        Vector3 d = player.transform.position - start;
        float forward = Vector3.Dot(new Vector3(d.x, 0f, d.z), direction);
        bool pass = forward > 8f && minY > WaterY + 0.45f && GroundedFraction() > 0.55f;
        rows.Add($"bridge|pass={pass}|forward={forward:F3}|minY={minY:F3}|groundedFrac={GroundedFraction():F2}|end={player.transform.position:F3}");
        Capture("03_FreeHavenBridge");
        BeginStairs();
    }

    static void BeginStairs()
    {
        phase = 3;
        Transform t = FindSemantic("041_M040_T2stairsS");
        Renderer renderer = t.GetComponentInChildren<Renderer>(true);
        activeBounds = renderer.bounds;
        Vector3 dir = Vector3.forward;
        // The tower's south stair object contains two side flights around a
        // blocked center doorway. Use the left flight, not the object center.
        float stairX = activeBounds.min.x + 1.25f;
        Vector3 s = new Vector3(stairX, 0f, activeBounds.min.z - 0.55f);
        s.y = GroundY(s.x, s.z, activeBounds.min.y) + 0.06f;
        Teleport(s);
        ResetTracking(s, dir);
        player.EditorSetTestInput(InputForWorld(dir), false, false, false);
        rows.Add("stairs_begin|bounds=" + activeBounds + "|start=" + s.ToString("F3"));
    }

    static void EndStairs()
    {
        player.EditorClearTestInput();
        Vector3 d = player.transform.position - start;
        float forward = Vector3.Dot(new Vector3(d.x, 0f, d.z), direction);
        float rise = maxY - start.y;
        bool pass = forward > 2.20f && rise > 0.55f;
        var renderContains = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
            .Where(r => r.enabled && r.gameObject.activeInHierarchy && r.bounds.Contains(cam.transform.position))
            .Select(r => r.name)
            .Distinct()
            .Take(8)
            .ToArray();
        var overlapNames = Physics.OverlapSphere(cam.transform.position, 0.05f, ~0, QueryTriggerInteraction.Ignore)
            .Select(c => c.name)
            .Distinct()
            .Take(8)
            .ToArray();
        rows.Add($"stairs|pass={pass}|forward={forward:F3}|rise={rise:F3}|groundedFrac={GroundedFraction():F2}|end={player.transform.position:F3}|camera={cam.transform.position:F3}|cameraDistance={Vector3.Distance(cam.transform.position,player.transform.position+Vector3.up*player.cameraHeight):F3}|renderContains={string.Join(";",renderContains)}|colliderOverlaps={string.Join(";",overlapNames)}");
        Capture("04_BootlegStairs");
        BeginHouseCollision();
    }

    static Transform FindHouse()
        => FindSemantic("SolidCore_089_M088_6_rich_houseS");

    static void BeginHouseCollision()
    {
        phase = 4;
        Transform t = FindHouse();
        Collider col = t.GetComponentInChildren<Collider>(true);
        activeBounds = col.bounds;
        Vector3 dir = Vector3.forward;
        Vector3 s = new Vector3(activeBounds.center.x, 0f, activeBounds.min.z - 1.1f);
        s.y = GroundY(s.x, s.z, activeBounds.min.y) + 0.06f;
        Teleport(s);
        ResetTracking(s, dir);
        player.EditorSetTestInput(InputForWorld(dir), false, false, false);
        rows.Add("house_collision_begin|bounds=" + activeBounds + "|start=" + s.ToString("F3"));
    }

    static void EndHouseCollision()
    {
        player.EditorClearTestInput();
        float allowedZ = activeBounds.min.z - cc.radius + 0.12f;
        bool notInside = player.transform.position.z <= allowedZ;
        bool pass = notInside;
        rows.Add($"house_collision|pass={pass}|endZ={player.transform.position.z:F3}|allowedZ={allowedZ:F3}|end={player.transform.position:F3}");
        Capture("05_FreeHavenHouseCollision");
        BeginCameraCollision();
    }

    static void BeginCameraCollision()
    {
        phase = 5;
        Transform t = FindHouse();
        Collider col = t.GetComponentInChildren<Collider>(true);
        activeBounds = col.bounds;

        Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
        float ext = Mathf.Abs(f.x) * activeBounds.extents.x + Mathf.Abs(f.z) * activeBounds.extents.z;
        Vector3 s = activeBounds.center + f * (ext + 0.75f);
        s.y = GroundY(s.x, s.z, activeBounds.min.y) + 0.06f;
        Teleport(s);
        ResetTracking(s, Vector3.zero);
        player.EditorClearTestInput();
        rows.Add("camera_collision_begin|bounds=" + activeBounds + "|start=" + s.ToString("F3") + "|forward=" + f.ToString("F3"));
    }

    static void EndCameraCollision()
    {
        Vector3 focus = player.transform.position + Vector3.up * player.cameraHeight;
        float dist = Vector3.Distance(cam.transform.position, focus);
        bool inside = activeBounds.Contains(cam.transform.position);
        bool pass = !inside && dist < player.cameraDistance - 0.25f;
        rows.Add($"camera_collision|pass={pass}|distance={dist:F3}|baseDistance={player.cameraDistance:F3}|cameraInsideWall={inside}|camera={cam.transform.position:F3}");
        Capture("06_CameraCollision");
        BeginCoast();
    }

    static void BeginCoast()
    {
        phase = 6;
        Terrain t = TerrainByName("BootlegBayTerrain");
        Vector3 probe = new Vector3(544f, 0f, 144f);
        Vector3 up = UphillAt(t, probe, out float slope);
        Vector3 down = -up;
        Vector3 s = probe + up * 3.0f;
        s.y = t.SampleHeight(s) + t.transform.position.y + 0.06f;
        Teleport(s);
        ResetTracking(s, down);
        player.EditorSetTestInput(InputForWorld(down), false, false, false);
        rows.Add($"coast_begin|slope={slope:F2}|waterY={WaterY:F3}|start={s:F3}|downhill={down:F3}");
    }

    static void EndCoast()
    {
        player.EditorClearTestInput();
        Vector3 d = player.transform.position - start;
        float forward = Vector3.Dot(new Vector3(d.x, 0f, d.z), direction);
        float submerge = WaterY - minY;
        bool pass = minY >= WaterY - 0.15f;
        rows.Add($"coast|pass={pass}|forward={forward:F3}|minRootY={minY:F3}|waterY={WaterY:F3}|submerge={submerge:F3}|end={player.transform.position:F3}");
        Capture("07_BootlegCoast");

        bool allPass = rows.Where(x => x.Contains("|pass=")).All(x => x.Contains("|pass=True"));
        rows.Add("SUMMARY|pass=" + allPass + "|cases=" + rows.Count(x => x.Contains("|pass=")));
        File.WriteAllLines(Dir + "/results.txt", rows);
        player.EditorClearTestInput();
        SessionState.SetBool(Key, false);
        EditorApplication.isPlaying = false;
    }

    static float GroundedFraction()
        => sampleFrames > 0 ? groundedFrames / (float)sampleFrames : 0f;

    static void Fail(Exception e)
    {
        Directory.CreateDirectory(Dir);
        File.WriteAllText(Dir + "/error.txt", e.ToString());
        if (player) player.EditorClearTestInput();
        SessionState.SetBool(Key, false);
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
    }
}

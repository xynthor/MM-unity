using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only, read-only visual review of the accepted Frozen route.
// Run in Enroth Play Mode. Does not modify scenes, terrain, colliders or assets.
public static class MMFrozenRouteCaptureQA
{
    const string TerrainName = "Terrain_FrozenHighlands_North";
    const int Width = 1280;
    const int Height = 720;

    struct Site
    {
        public readonly string Name;
        public readonly Vector2 Position;
        public readonly Vector2 LookAt;
        public readonly float MinimumEyeY;

        public Site(string name, float x, float z, float tx, float tz, float minimumEyeY = 0f)
        {
            Name = name;
            Position = new Vector2(x, z);
            LookAt = new Vector2(tx, tz);
            MinimumEyeY = minimumEyeY;
        }
    }

    static readonly Site[] Route =
    {
        new Site("01_forest", -121f, 1109f, -85f, 1125f),
        new Site("02_clearing", -55f, 1127f, -93f, 1162f),
        new Site("03_ridge", -92f, 1165f, -96f, 1207f),
        // Verified dry-shore source location for the previously accepted ice preview.
        new Site("04_ice_coast", -100f, 1228f, -93f, 1270f, 41.7f),
        new Site("05_reverse_downhill", -87f, 1162f, -57f, 1110f)
    };

    [MenuItem("MM Unity/Frozen Route QA/Capture accepted baseline")]
    public static void CaptureBaseline() { Capture("baseline"); }

    [MenuItem("MM Unity/Frozen Route QA/Capture candidate")]
    public static void CaptureCandidate() { Capture("candidate"); }

    static void Capture(string label)
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Enroth")
        {
            Debug.LogError("Frozen route capture requires Enroth in Play Mode.");
            return;
        }

        var terrain = Terrain.activeTerrains.FirstOrDefault(t => t && t.terrainData && t.name == TerrainName);
        var camera = Camera.main;
        if (!terrain || !camera)
        {
            Debug.LogError("Frozen route capture requires the active Frozen North terrain and main gameplay camera.");
            return;
        }

        string root = Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Validation/NorthStyleRevamp20261013/FrozenRouteAudit/" + label));
        Directory.CreateDirectory(root);

        var scene = SceneManager.GetActiveScene();
        bool wasDirty = scene.isDirty;
        var notes = new List<string>
        {
            "Frozen Highlands route camera audit; label=" + label,
            "Scene=" + scene.name + "  sceneDirtyBefore=" + wasDirty,
            "Terrain=" + terrain.name + "  runtimeLayers=" + terrain.terrainData.terrainLayers.Length,
            "RidgePines=" + MMFrozenRidgeTreeLine.AddedPines +
            "  GrovePines=" + MMFrozenGroveAgeLayer.AddedPines,
            "Five exact camera stations; same coordinates and orientation for both captures.",
            "Measurements are inspection leads, not automatic visual approval."
        };

        Vector3 savedPosition = camera.transform.position;
        Quaternion savedRotation = camera.transform.rotation;
        RenderTexture savedTarget = camera.targetTexture;
        RenderTexture savedActive = RenderTexture.active;
        RenderTexture target = null;
        Texture2D texture = null;
        try
        {
            target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);

            foreach (Site site in Route)
            {
                float eyeY = Mathf.Max(Ground(terrain, site.Position) + 2.2f, site.MinimumEyeY);
                float lookY = Ground(terrain, site.LookAt) + 1.2f;
                Vector3 eye = new Vector3(site.Position.x, eyeY, site.Position.y);
                Vector3 look = new Vector3(site.LookAt.x, lookY, site.LookAt.y);
                Vector3 direction = (look - eye).normalized;
                if (direction.sqrMagnitude < .5f) continue;

                camera.transform.SetPositionAndRotation(
                    eye, Quaternion.LookRotation(direction, Vector3.up));
                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
                texture.Apply(false, false);

                string file = Path.Combine(root, site.Name + ".png");
                File.WriteAllBytes(file, texture.EncodeToPNG());
                notes.Add(site.Name + "  eye=" + eye.ToString("F2") +
                    "  look=" + look.ToString("F2") + "  image=" + Path.GetFileName(file));
            }

            AppendContactAudit(terrain, notes);
        }
        catch (Exception e)
        {
            notes.Add("CAPTURE_ERROR=" + e);
            Debug.LogException(e);
        }
        finally
        {
            camera.targetTexture = savedTarget;
            camera.transform.SetPositionAndRotation(savedPosition, savedRotation);
            RenderTexture.active = savedActive;
            if (target)
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
            if (texture) UnityEngine.Object.DestroyImmediate(texture);
            notes.Add("sceneDirtyAfter=" + SceneManager.GetActiveScene().isDirty);
            File.WriteAllLines(Path.Combine(root, "manifest.txt"), notes);
        }

        Debug.Log("Frozen route evidence saved: " + root);
    }

    static float Ground(Terrain terrain, Vector2 p)
    {
        return terrain.SampleHeight(new Vector3(p.x, 0f, p.y)) + terrain.transform.position.y;
    }

    static void AppendContactAudit(Terrain terrain, List<string> notes)
    {
        var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude);
        var flags = new List<string>();
        int pines = 0, rocks = 0;

        foreach (Transform tr in transforms)
        {
            if (!tr || tr.position.x < -180f || tr.position.x > 40f ||
                tr.position.z < 1060f || tr.position.z > 1240f)
                continue;

            if (tr.name.StartsWith("AuthoredPine_", StringComparison.Ordinal))
            {
                Renderer[] lod0 = tr.GetComponentsInChildren<Renderer>(true)
                    .Where(r => r && r.name.Contains("LOD0")).ToArray();
                if (lod0.Length == 0) continue;
                pines++;
                float difference = lod0.Min(r => r.bounds.min.y) -
                                   Ground(terrain, new Vector2(tr.position.x, tr.position.z));
                if (difference > .20f)
                    flags.Add("PINE possible floating base: " + tr.name +
                        "  clearance=" + difference.ToString("F3") + "m");
            }
            else if (tr.name.StartsWith("AuthoredBoulder_", StringComparison.Ordinal))
            {
                MeshFilter filter = tr.GetComponentsInChildren<MeshFilter>(true)
                    .FirstOrDefault(f => f && f.sharedMesh);
                if (!filter || !tr.GetComponentsInChildren<Renderer>(true).Any(r => r && r.enabled))
                    continue;
                rocks++;
                float minimum = float.PositiveInfinity;
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                {
                    Vector3 world = filter.transform.TransformPoint(vertex);
                    minimum = Mathf.Min(minimum, world.y -
                        Ground(terrain, new Vector2(world.x, world.z)));
                    if (minimum < -.10f) break;
                }
                if (minimum > .30f)
                    flags.Add("ROCK fully detached candidate: " + tr.name +
                        "  minimumClearance=" + minimum.ToString("F3") + "m");
            }
        }

        notes.Add("contactAudit: pineRoots=" + pines + " rockMeshes=" + rocks +
                  " raisedCandidates=" + flags.Count);
        notes.AddRange(flags.OrderBy(x => x));
    }
}

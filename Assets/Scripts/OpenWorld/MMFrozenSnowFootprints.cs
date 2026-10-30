using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// Visual-only, bounded winter-clearing pilot: compacted boot prints on snowy
// Frozen Highlands terrain. Never edits TerrainData, colliders, scene assets,
// player locomotion, or audio. A single pooled mesh renders the recent trail.
[DisallowMultipleComponent]
public sealed class MMFrozenSnowFootprints : MonoBehaviour
{
    const string RootName = "MM_FrozenSnowFootprints";
    const string TerrainName = "Terrain_FrozenHighlands_North";
    const int Capacity = 88;
    const float GroundOffset = .045f;

    struct Print
    {
        public Vector3 position;
        public Vector3 forward;
        public bool left;
    }

    public static MMFrozenSnowFootprints Instance { get; private set; }
    public int PrintCount => prints.Count;
    public bool Ready => player && terrain && cc && mesh && material;

    readonly List<Print> prints = new List<Print>(Capacity);
    Terrain terrain;
    MMThirdPersonController player;
    CharacterController cc;
    UnityEngine.Mesh mesh;
    Texture2D texture;
    Material material;
    MeshFilter filter;
    MeshRenderer display;
    Vector3 previous;
    float distanceAccumulated;
    bool leftFoot = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (SceneManager.GetActiveScene().name != "Enroth" ||
            GameObject.Find(RootName)) return;

        var root = new GameObject(RootName);
        root.AddComponent<MMFrozenSnowFootprints>();
    }

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        var shader = Shader.Find("Unlit/Transparent");
        if (!shader)
        {
            Debug.LogWarning("Frozen snow footprints skipped: transparent shader missing");
            enabled = false;
            return;
        }

        texture = BuildSole();
        material = new Material(shader) { name = "Frozen compacted-footprint material (runtime)" };
        material.mainTexture = texture;
        material.hideFlags = HideFlags.DontSave;
        mesh = new UnityEngine.Mesh { name = "Frozen compacted-footprint pool (runtime)" };
        mesh.MarkDynamic();
        mesh.hideFlags = HideFlags.DontSave;
        filter = gameObject.AddComponent<MeshFilter>();
        display = gameObject.AddComponent<MeshRenderer>();
        filter.sharedMesh = mesh;
        display.sharedMaterial = material;
        display.shadowCastingMode = ShadowCastingMode.Off;
        display.receiveShadows = false;
        display.lightProbeUsage = LightProbeUsage.Off;
        display.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    static Texture2D BuildSole()
    {
        const int width = 128, height = 256;
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
        {
            name = "Frozen boot impression mask (runtime)",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        var colors = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float u = (x + .5f) / width * 2f - 1f;
                float v = (y + .5f) / height * 2f - 1f;
                // Toe and heel are distinct: compacted snow, not opaque ovals.
                float widthAtY = v > -.28f ? .87f : .61f;
                float asymmetry = .07f * Mathf.Sin(v * 4f + 1.8f);
                float dx = (u - asymmetry) / widthAtY;
                float radius = dx * dx + (v * v) / (.98f * .98f);
                float falloff = Mathf.Clamp01((1.02f - radius) * 7f);
                float grains = Mathf.PerlinNoise(x * .29f + 17f, y * .26f + 4f);
                float tread = (y / 14) % 4 == 1 ? .58f : 1f;
                float arch = v > -.35f && v < .04f ? .45f : 1f;
                float opacity = .24f * falloff * (.48f + .52f * grains) *
                    tread * arch;
                if (v < -.84f) opacity *= .70f;
                colors[y * width + x] =
                    new Color(.58f, .64f, .67f, opacity);
            }

        result.SetPixels(colors);
        result.Apply(false, true);
        return result;
    }

    void Update()
    {
        if (!Ready)
        {
            if (!terrain)
                foreach (var candidate in Terrain.activeTerrains)
                    if (candidate && candidate.name == TerrainName)
                    {
                        terrain = candidate;
                        break;
                    }

            if (!player)
            {
                foreach (var candidate in FindObjectsByType<MMThirdPersonController>(
                    FindObjectsInactive.Exclude))
                    if (candidate && candidate.gameObject.activeInHierarchy)
                    {
                        player = candidate;
                        cc = candidate.GetComponent<CharacterController>();
                        previous = candidate.transform.position;
                        break;
                    }
            }
            return;
        }

        var current = player.transform.position;
        Vector3 motion = current - previous;
        previous = current;
        motion.y = 0f;
        float length = motion.magnitude;

        // Teleports, idle and editor-camera repositioning create no marks.
        if (length > 3f || Time.deltaTime <= 0f)
        {
            distanceAccumulated = 0f;
            return;
        }
        if (length < .002f || !cc.isGrounded || !player.enabled)
            return;

        float pace = Mathf.Clamp(length / Time.deltaTime, 0f, 12f);
        distanceAccumulated += length;
        float interval = pace > 5f ? .76f : .53f;
        if (distanceAccumulated < interval)
            return;
        distanceAccumulated = Mathf.Min(distanceAccumulated - interval, interval);

        Vector3 direction = motion.normalized;
        Vector3 side = new Vector3(direction.z, 0f, -direction.x);
        Vector3 target = current - direction * .18f + side * (leftFoot ? -.19f : .19f);
        if (!Eligible(target))
            return;

        target.y = terrain.SampleHeight(target) + terrain.transform.position.y;
        prints.Add(new Print { position = target, forward = direction, left = leftFoot });
        leftFoot = !leftFoot;

        if (prints.Count > Capacity) prints.RemoveAt(0);
        RebuildMesh();
    }

    bool Eligible(Vector3 point)
    {
        if (point.x < -170f || point.x > 40f ||
            point.z < 1060f || point.z > 1180f)
            return false;

        float u = (point.x - terrain.transform.position.x) / terrain.terrainData.size.x;
        float v = (point.z - terrain.transform.position.z) / terrain.terrainData.size.z;
        if (u <= 0f || u >= 1f || v <= 0f || v >= 1f ||
            terrain.terrainData.GetSteepness(u, v) > 35f)
            return false;

        float surface = terrain.SampleHeight(point) + terrain.transform.position.y;
        if (surface < 25f || Mathf.Abs(player.transform.position.y - surface) > 1.7f)
            return false;

        int x = Mathf.Clamp(Mathf.RoundToInt(u * (terrain.terrainData.alphamapWidth - 1)),
            0, terrain.terrainData.alphamapWidth - 1);
        int z = Mathf.Clamp(Mathf.RoundToInt(v * (terrain.terrainData.alphamapHeight - 1)),
            0, terrain.terrainData.alphamapHeight - 1);
        // The approved packed-snow pass moves snow from original layer 18
        // into runtime layer 19. Count both as snow, not only the source.
        var weights = terrain.terrainData.GetAlphamaps(x, z, 1, 1);
        float snowWeight = weights[0, 0, 18];
        if (weights.GetLength(2) > 19) snowWeight += weights[0, 0, 19];
        return snowWeight >= .78f;
    }

    void RebuildMesh()
    {
        var vertices = new Vector3[prints.Count * 4];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[prints.Count * 6];

        for (int i = 0; i < prints.Count; i++)
        {
            var mark = prints[i];
            Vector3 right = new Vector3(mark.forward.z, 0f, -mark.forward.x);
            float width = .086f, length = .175f;
            for (int corner = 0; corner < 4; corner++)
            {
                float horizontal = (corner % 2 == 0 ? -1f : 1f) * width;
                float longitudinal = (corner < 2 ? -1f : 1f) * length;
                Vector3 point = mark.position + right * horizontal +
                    mark.forward * longitudinal;
                point.y = terrain.SampleHeight(point) +
                    terrain.transform.position.y + GroundOffset;
                vertices[i * 4 + corner] = point;
                uv[i * 4 + corner] = new Vector2(corner % 2, corner / 2);
            }

            int p = i * 4, t = i * 6;
            triangles[t] = p;
            triangles[t + 1] = p + 2;
            triangles[t + 2] = p + 1;
            triangles[t + 3] = p + 1;
            triangles[t + 4] = p + 2;
            triangles[t + 5] = p + 3;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Release(mesh);
        Release(material);
        Release(texture);
    }

    static void Release(UnityEngine.Object item)
    {
        if (!item) return;
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}

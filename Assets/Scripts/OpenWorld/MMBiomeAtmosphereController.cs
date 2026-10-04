using System;
using System.Linq;
using UnityEngine;

public class MMBiomeAtmosphereController : MonoBehaviour
{
    [Serializable]
    public struct AtmosphereProfile
    {
        public string id;
        public Color fogColor;
        public float fogStart;
        public float fogEnd;
        public Color ambientSky;
        public Color ambientEquator;
        public Color ambientGround;
        public float ambientIntensity;
        public Color sunColor;
        public float sunIntensity;
    }

    [Header("Target")]
    public Transform target;

    [Header("Transition")]
    [Min(0.1f)] public float transitionSeconds = 3.0f;
    public bool affectSun = true;

    Light masterSun;
    AtmosphereProfile current;
    bool initialized;
    string currentTerrainName = "";

    void Awake()
    {
        if (!target)
        {
            var player = FindFirstObjectByType<MMThirdPersonController>();
            if (player) target = player.transform;
        }

        masterSun = FindObjectsByType<Light>(FindObjectsInactive.Exclude)
            .FirstOrDefault(l => l.enabled && l.type == LightType.Directional);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;

        AtmosphereProfile p = ResolveProfile(target ? target.position : Vector3.zero, out currentTerrainName);
        current = p;
        Apply(current);
        initialized = true;
    }

    void Update()
    {
        if (!target) return;

        AtmosphereProfile targetProfile = ResolveProfile(target.position, out string terrainName);
        currentTerrainName = terrainName;

        if (!initialized)
        {
            current = targetProfile;
            initialized = true;
        }

        float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.1f, transitionSeconds));
        current = Lerp(current, targetProfile, k);
        Apply(current);
    }

    public string CurrentTerrainName => currentTerrainName;
    public string CurrentProfileId => current.id;

    AtmosphereProfile ResolveProfile(Vector3 worldPosition, out string terrainName)
    {
        Terrain terrain = Terrain.activeTerrains.FirstOrDefault(t =>
        {
            if (!t || !t.terrainData) return false;
            Vector3 p = t.transform.position;
            Vector3 s = t.terrainData.size;
            return worldPosition.x >= p.x && worldPosition.x <= p.x + s.x &&
                   worldPosition.z >= p.z && worldPosition.z <= p.z + s.z;
        });

        terrainName = terrain && terrain.terrainData ? terrain.terrainData.name : "None";
        return ProfileForTerrain(terrainName);
    }

    public static AtmosphereProfile ProfileForTerrain(string terrainName)
    {
        string n = (terrainName ?? "").ToLowerInvariant();

        if (n.Contains("dragonisle") || n.Contains("dragonsand") ||
            n.Contains("hermits") || n.Contains("paradisevalley"))
            return Desert();

        if (n.Contains("frozenhighlands"))
            return Frozen();

        if (n.Contains("kriegspire"))
            return ColdHighland();

        if (n.Contains("sweetwater"))
            return ColdHighland();

        if (n.Contains("mireofthedamned"))
            return Mire();

        if (n.Contains("eelinfested") || n.Contains("mistyislands") ||
            n.Contains("bootlegbay") || n.Contains("archipelago"))
            return Maritime();

        if (n.Contains("silvercove"))
            return CoolCoast();

        if (n.Contains("blackshire"))
            return HighlandGreen();

        return Temperate();
    }

    public static AtmosphereProfile Temperate()
    {
        return NewProfile(
            "Temperate",
            new Color(0.62f, 0.72f, 0.78f), 900f, 2200f,
            new Color(0.56f, 0.69f, 0.82f),
            new Color(0.42f, 0.52f, 0.43f),
            new Color(0.20f, 0.23f, 0.17f), 1.00f,
            new Color(1.00f, 0.96f, 0.88f), 1.05f);
    }

    public static AtmosphereProfile Desert()
    {
        return NewProfile(
            "Desert",
            new Color(0.78f, 0.72f, 0.61f), 1200f, 2700f,
            new Color(0.72f, 0.71f, 0.66f),
            new Color(0.56f, 0.47f, 0.34f),
            new Color(0.28f, 0.23f, 0.16f), 1.06f,
            new Color(1.00f, 0.89f, 0.70f), 1.10f);
    }

    public static AtmosphereProfile Frozen()
    {
        return NewProfile(
            "Frozen",
            new Color(0.72f, 0.80f, 0.86f), 650f, 1850f,
            new Color(0.64f, 0.75f, 0.88f),
            new Color(0.47f, 0.56f, 0.64f),
            new Color(0.24f, 0.28f, 0.31f), 1.02f,
            new Color(0.86f, 0.92f, 1.00f), 1.00f);
    }

    public static AtmosphereProfile ColdHighland()
    {
        return NewProfile(
            "ColdHighland",
            new Color(0.58f, 0.65f, 0.69f), 700f, 1900f,
            new Color(0.52f, 0.63f, 0.74f),
            new Color(0.38f, 0.43f, 0.42f),
            new Color(0.18f, 0.20f, 0.20f), 0.96f,
            new Color(0.91f, 0.94f, 1.00f), 0.98f);
    }

    public static AtmosphereProfile Mire()
    {
        return NewProfile(
            "Mire",
            new Color(0.53f, 0.60f, 0.54f), 650f, 1700f,
            new Color(0.49f, 0.59f, 0.60f),
            new Color(0.36f, 0.43f, 0.34f),
            new Color(0.15f, 0.18f, 0.13f), 0.95f,
            new Color(0.90f, 0.93f, 0.84f), 0.96f);
    }

    public static AtmosphereProfile Maritime()
    {
        return NewProfile(
            "Maritime",
            new Color(0.61f, 0.75f, 0.80f), 650f, 1800f,
            new Color(0.56f, 0.73f, 0.84f),
            new Color(0.40f, 0.55f, 0.55f),
            new Color(0.18f, 0.24f, 0.22f), 1.00f,
            new Color(0.96f, 0.97f, 0.94f), 1.02f);
    }

    public static AtmosphereProfile CoolCoast()
    {
        return NewProfile(
            "CoolCoast",
            new Color(0.60f, 0.71f, 0.76f), 750f, 2000f,
            new Color(0.54f, 0.67f, 0.79f),
            new Color(0.39f, 0.49f, 0.46f),
            new Color(0.18f, 0.21f, 0.18f), 0.99f,
            new Color(0.95f, 0.96f, 0.93f), 1.01f);
    }

    public static AtmosphereProfile HighlandGreen()
    {
        return NewProfile(
            "HighlandGreen",
            new Color(0.59f, 0.68f, 0.66f), 750f, 1950f,
            new Color(0.53f, 0.65f, 0.73f),
            new Color(0.38f, 0.49f, 0.38f),
            new Color(0.17f, 0.20f, 0.14f), 0.98f,
            new Color(0.95f, 0.95f, 0.87f), 1.02f);
    }

    static AtmosphereProfile NewProfile(
        string id,
        Color fogColor, float fogStart, float fogEnd,
        Color sky, Color equator, Color ground, float ambientIntensity,
        Color sunColor, float sunIntensity)
    {
        return new AtmosphereProfile
        {
            id = id,
            fogColor = fogColor,
            fogStart = fogStart,
            fogEnd = fogEnd,
            ambientSky = sky,
            ambientEquator = equator,
            ambientGround = ground,
            ambientIntensity = ambientIntensity,
            sunColor = sunColor,
            sunIntensity = sunIntensity
        };
    }

    static AtmosphereProfile Lerp(AtmosphereProfile a, AtmosphereProfile b, float t)
    {
        return new AtmosphereProfile
        {
            id = b.id,
            fogColor = Color.Lerp(a.fogColor, b.fogColor, t),
            fogStart = Mathf.Lerp(a.fogStart, b.fogStart, t),
            fogEnd = Mathf.Lerp(a.fogEnd, b.fogEnd, t),
            ambientSky = Color.Lerp(a.ambientSky, b.ambientSky, t),
            ambientEquator = Color.Lerp(a.ambientEquator, b.ambientEquator, t),
            ambientGround = Color.Lerp(a.ambientGround, b.ambientGround, t),
            ambientIntensity = Mathf.Lerp(a.ambientIntensity, b.ambientIntensity, t),
            sunColor = Color.Lerp(a.sunColor, b.sunColor, t),
            sunIntensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t)
        };
    }

    public static void ApplyRenderSettings(AtmosphereProfile p)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = p.fogColor;
        RenderSettings.fogStartDistance = p.fogStart;
        RenderSettings.fogEndDistance = p.fogEnd;
        RenderSettings.ambientSkyColor = p.ambientSky;
        RenderSettings.ambientEquatorColor = p.ambientEquator;
        RenderSettings.ambientGroundColor = p.ambientGround;
        RenderSettings.ambientIntensity = p.ambientIntensity;
    }

#if UNITY_EDITOR
    public void EditorTestTick(Vector3 worldPosition, float deltaSeconds)
    {
        AtmosphereProfile targetProfile = ResolveProfile(worldPosition, out string terrainName);
        currentTerrainName = terrainName;

        if (!initialized)
        {
            current = targetProfile;
            initialized = true;
        }

        float k = 1f - Mathf.Exp(-Mathf.Max(0f, deltaSeconds) / Mathf.Max(0.1f, transitionSeconds));
        current = Lerp(current, targetProfile, k);
        Apply(current);
    }
#endif

    void Apply(AtmosphereProfile p)
    {
        ApplyRenderSettings(p);

        if (affectSun && masterSun)
        {
            masterSun.color = p.sunColor;
            masterSun.intensity = p.sunIntensity;
        }
    }
}


public static class MMBiomeAtmosphereBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureController()
    {
        var player = UnityEngine.Object.FindFirstObjectByType<MMThirdPersonController>();
        if (!player) return;

        var controller = player.GetComponent<MMBiomeAtmosphereController>();
        if (!controller)
            controller = player.gameObject.AddComponent<MMBiomeAtmosphereController>();

        controller.target = player.transform;
    }
}

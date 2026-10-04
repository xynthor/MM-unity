using System;
using UnityEngine;

[DisallowMultipleComponent]
public class MMBiomeWeatherController : MonoBehaviour
{
    [Serializable]
    public struct WeatherProfile
    {
        public string id;
        public float emissionRate;
        public Color color;
        public Vector2 size;
        public float lifetime;
        public Vector3 boxSize;
        public float emitterHeight;
        public Vector3 velocity;
        public float noiseStrength;
        public float noiseFrequency;
    }

    [Header("Target")]
    public Transform target;

    [Header("Transition")]
    [Min(0.1f)] public float transitionSeconds = 2.5f;

    ParticleSystem weather;
    ParticleSystemRenderer weatherRenderer;
    Material runtimeMaterial;
    Texture2D runtimeTexture;
    MMBiomeAtmosphereController atmosphere;
    WeatherProfile current;
    bool initialized;

    public string CurrentWeatherId => current.id;
    public float CurrentEmissionRate => current.emissionRate;
    public int AliveParticleCount => weather ? weather.particleCount : 0;
    public string RuntimeShaderName =>
        runtimeMaterial && runtimeMaterial.shader ? runtimeMaterial.shader.name : "None";

    void Awake()
    {
        if (!target)
        {
            var player = FindAnyObjectByType<MMThirdPersonController>();
            if (player) target = player.transform;
        }

        atmosphere = GetComponent<MMBiomeAtmosphereController>();
        if (!atmosphere)
            atmosphere = gameObject.AddComponent<MMBiomeAtmosphereController>();

        if (atmosphere && !atmosphere.target)
            atmosphere.target = target;

        CreateWeatherSystem();

        string profileId = atmosphere ? atmosphere.CurrentProfileId : "Temperate";
        current = ProfileForAtmosphere(profileId);
        Apply(current);
        initialized = true;
    }

    void Update()
    {
        if (!target || !weather) return;

        transform.position = target.position;

        string profileId = atmosphere ? atmosphere.CurrentProfileId : "Temperate";
        WeatherProfile desired = ProfileForAtmosphere(profileId);

        if (!initialized)
        {
            current = desired;
            initialized = true;
        }

        float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.1f, transitionSeconds));
        current = Lerp(current, desired, k);
        Apply(current);
    }

    void CreateWeatherSystem()
    {
        var go = new GameObject("MM_BiomeWeather");
        go.transform.SetParent(transform, false);

        weather = go.AddComponent<ParticleSystem>();
        weatherRenderer = go.GetComponent<ParticleSystemRenderer>();

        var main = weather.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 900;
        main.gravityModifier = 0f;
        main.startSpeed = 0f;

        var emission = weather.emission;
        emission.rateOverTime = 0f;

        var shape = weather.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(24f, 6f, 24f);

        var velocity = weather.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;

        var noise = weather.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.scrollSpeed = 0.18f;

        var colorOverLifetime = weather.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(1f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        runtimeTexture = BuildSoftParticleTexture(64);
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (!shader) shader = Shader.Find("Particles/Standard Unlit");
        if (!shader) shader = Shader.Find("Sprites/Default");

        runtimeMaterial = new Material(shader)
        {
            name = "MM Runtime Biome Weather"
        };
        if (runtimeMaterial.HasProperty("_MainTex"))
            runtimeMaterial.SetTexture("_MainTex", runtimeTexture);

        weatherRenderer.sharedMaterial = runtimeMaterial;
        weatherRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        weatherRenderer.sortMode = ParticleSystemSortMode.Distance;
        weatherRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        weatherRenderer.receiveShadows = false;

        weather.Play();
    }

    void Apply(WeatherProfile p)
    {
        if (!weather) return;

        weather.transform.localPosition = new Vector3(0f, p.emitterHeight, 0f);

        var main = weather.main;
        main.startLifetime = p.lifetime;
        main.startSize = new ParticleSystem.MinMaxCurve(p.size.x, p.size.y);
        main.startColor = p.color;
        main.maxParticles = 900;

        var emission = weather.emission;
        emission.rateOverTime = Mathf.Max(0f, p.emissionRate);

        var shape = weather.shape;
        shape.scale = p.boxSize;

        var velocity = weather.velocityOverLifetime;
        velocity.x = new ParticleSystem.MinMaxCurve(p.velocity.x * 0.75f, p.velocity.x * 1.25f);
        velocity.y = new ParticleSystem.MinMaxCurve(p.velocity.y * 0.85f, p.velocity.y * 1.15f);
        velocity.z = new ParticleSystem.MinMaxCurve(p.velocity.z * 0.75f, p.velocity.z * 1.25f);

        var noise = weather.noise;
        noise.strength = p.noiseStrength;
        noise.frequency = p.noiseFrequency;
    }

    public static WeatherProfile ProfileForAtmosphere(string atmosphereId)
    {
        switch ((atmosphereId ?? "").Trim())
        {
            case "Frozen":
                return NewProfile(
                    "Snow",
                    92f,
                    new Color(0.92f, 0.96f, 1.00f, 0.78f),
                    0.055f, 0.15f, 6.2f,
                    new Vector3(30f, 5f, 30f), 10f,
                    new Vector3(0.45f, -2.35f, 0.18f),
                    0.38f, 0.20f);

            case "ColdHighland":
                return NewProfile(
                    "Flurries",
                    24f,
                    new Color(0.88f, 0.93f, 0.98f, 0.48f),
                    0.045f, 0.11f, 5.4f,
                    new Vector3(28f, 5f, 28f), 9f,
                    new Vector3(0.62f, -1.35f, 0.25f),
                    0.48f, 0.24f);

            case "Desert":
                return NewProfile(
                    "Dust",
                    34f,
                    new Color(0.82f, 0.72f, 0.56f, 0.34f),
                    0.07f, 0.20f, 4.5f,
                    new Vector3(28f, 7f, 28f), 3.5f,
                    new Vector3(0.88f, 0.12f, 0.30f),
                    0.72f, 0.18f);

            case "Mire":
                return NewProfile(
                    "MireMotes",
                    18f,
                    new Color(0.66f, 0.74f, 0.66f, 0.22f),
                    0.14f, 0.42f, 5.5f,
                    new Vector3(24f, 4f, 24f), 2.2f,
                    new Vector3(0.10f, 0.10f, 0.05f),
                    0.42f, 0.12f);

            case "Maritime":
                return NewProfile(
                    "SeaMist",
                    11f,
                    new Color(0.82f, 0.91f, 0.94f, 0.18f),
                    0.09f, 0.26f, 4.8f,
                    new Vector3(26f, 4f, 26f), 2.8f,
                    new Vector3(0.38f, 0.04f, 0.14f),
                    0.35f, 0.10f);

            case "CoolCoast":
                return NewProfile(
                    "CoastalMist",
                    8f,
                    new Color(0.82f, 0.89f, 0.92f, 0.15f),
                    0.08f, 0.22f, 4.6f,
                    new Vector3(24f, 4f, 24f), 2.6f,
                    new Vector3(0.28f, 0.03f, 0.10f),
                    0.28f, 0.09f);

            case "HighlandGreen":
                return NewProfile(
                    "Pollen",
                    4f,
                    new Color(0.82f, 0.84f, 0.62f, 0.16f),
                    0.045f, 0.095f, 5.2f,
                    new Vector3(22f, 4f, 22f), 2.8f,
                    new Vector3(0.16f, 0.05f, 0.06f),
                    0.36f, 0.11f);

            default:
                return NewProfile(
                    "Clear",
                    0f,
                    new Color(0.90f, 0.95f, 1.00f, 0f),
                    0.04f, 0.08f, 4f,
                    new Vector3(20f, 4f, 20f), 3f,
                    Vector3.zero,
                    0f, 0.1f);
        }
    }

    static WeatherProfile NewProfile(
        string id,
        float emission,
        Color color,
        float minSize,
        float maxSize,
        float lifetime,
        Vector3 boxSize,
        float emitterHeight,
        Vector3 velocity,
        float noiseStrength,
        float noiseFrequency)
    {
        return new WeatherProfile
        {
            id = id,
            emissionRate = emission,
            color = color,
            size = new Vector2(minSize, maxSize),
            lifetime = lifetime,
            boxSize = boxSize,
            emitterHeight = emitterHeight,
            velocity = velocity,
            noiseStrength = noiseStrength,
            noiseFrequency = noiseFrequency
        };
    }

    static WeatherProfile Lerp(WeatherProfile a, WeatherProfile b, float t)
    {
        return new WeatherProfile
        {
            id = b.id,
            emissionRate = Mathf.Lerp(a.emissionRate, b.emissionRate, t),
            color = Color.Lerp(a.color, b.color, t),
            size = Vector2.Lerp(a.size, b.size, t),
            lifetime = Mathf.Lerp(a.lifetime, b.lifetime, t),
            boxSize = Vector3.Lerp(a.boxSize, b.boxSize, t),
            emitterHeight = Mathf.Lerp(a.emitterHeight, b.emitterHeight, t),
            velocity = Vector3.Lerp(a.velocity, b.velocity, t),
            noiseStrength = Mathf.Lerp(a.noiseStrength, b.noiseStrength, t),
            noiseFrequency = Mathf.Lerp(a.noiseFrequency, b.noiseFrequency, t)
        };
    }

    static Texture2D BuildSoftParticleTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "MM Runtime Soft Particle",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };

        var pixels = new Color32[size * size];
        float center = (size - 1) * 0.5f;
        float radius = Mathf.Max(1f, center);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / radius;
                float dy = (y - center) / radius;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - d);
                alpha = alpha * alpha * (3f - 2f * alpha);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

#if UNITY_EDITOR
    public void EditorTestProfile(string atmosphereId, float deltaSeconds)
    {
        WeatherProfile desired = ProfileForAtmosphere(atmosphereId);
        if (!initialized)
        {
            current = desired;
            initialized = true;
        }

        float k = 1f - Mathf.Exp(-Mathf.Max(0f, deltaSeconds) / Mathf.Max(0.1f, transitionSeconds));
        current = Lerp(current, desired, k);
        Apply(current);
    }
#endif

    void OnDestroy()
    {
        if (runtimeMaterial) Destroy(runtimeMaterial);
        if (runtimeTexture) Destroy(runtimeTexture);
    }
}

public static class MMBiomeWeatherBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureController()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<MMThirdPersonController>();
        if (!player) return;

        var weather = player.GetComponent<MMBiomeWeatherController>();
        if (!weather)
            weather = player.gameObject.AddComponent<MMBiomeWeatherController>();

        weather.target = player.transform;
    }
}

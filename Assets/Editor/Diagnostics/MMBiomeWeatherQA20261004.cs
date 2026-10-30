using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MMBiomeWeatherQA20261004
{
    const string SessionKey = "MMBiomeWeatherQA20261004";
    const string Dir = "C:/MMUnityPort/Validation/Weather20261004";
    const string Report = Dir + "/runtime.txt";
    const string Error = Dir + "/error.txt";

    static MMThirdPersonController player;
    static MMBiomeAtmosphereController atmosphere;
    static MMBiomeWeatherController weather;
    static ParticleSystem particles;
    static double nextAt;
    static int step;
    static readonly List<string> rows = new List<string>();

    static readonly string[] TerrainNames =
    {
        "FrozenHighlands_LinkedNorthProfile",
        "ParadiseValley_Terrain",
        "MireOfTheDamnedTerrain",
        "BootlegBayTerrain"
    };

    static readonly string[] ExpectedAtmosphere =
    {
        "Frozen",
        "Desert",
        "Mire",
        "Maritime"
    };

    static readonly string[] ExpectedWeather =
    {
        "Snow",
        "Dust",
        "MireMotes",
        "SeaMist"
    };

    static MMBiomeWeatherQA20261004()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += ModeChanged;
    }

    public static void Start()
    {
        Directory.CreateDirectory(Dir);
        if (File.Exists(Report)) File.Delete(Report);
        if (File.Exists(Error)) File.Delete(Error);

        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Weather QA must start from Edit Mode.");

        SessionState.SetBool(SessionKey, true);
        EditorApplication.isPlaying = true;
    }

    static void ModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            try
            {
                Application.runInBackground = true;
                player = UnityEngine.Object.FindObjectsByType<MMThirdPersonController>(
                        FindObjectsInactive.Exclude)
                    .Single(p => p.gameObject.activeInHierarchy);

                atmosphere = player.GetComponent<MMBiomeAtmosphereController>();
                weather = player.GetComponent<MMBiomeWeatherController>();

                if (!atmosphere || !weather)
                    throw new InvalidOperationException("Biome atmosphere/weather bootstrap missing.");

                particles = weather.GetComponentInChildren<ParticleSystem>(true);
                if (!particles)
                    throw new InvalidOperationException("Runtime weather ParticleSystem missing.");

                atmosphere.transitionSeconds = 0.7f;
                weather.transitionSeconds = 0.6f;

                rows.Clear();
                step = 0;
                MoveTo(0);
                nextAt = Time.realtimeSinceStartupAsDouble + 2.4;
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
            int i = step;
            Record(i);
            MMSequentialEvidence.Render(
                player.playerCamera,
                Dir + "/" + (i + 1).ToString("00") + "_" + ExpectedWeather[i] + ".png",
                1280,
                720);

            step++;
            if (step >= TerrainNames.Length)
            {
                bool pass = true;
                for (int n = 0; n < TerrainNames.Length; n++)
                {
                    pass &= rows[n].Contains("atmosphere=" + ExpectedAtmosphere[n]);
                    pass &= rows[n].Contains("weather=" + ExpectedWeather[n]);
                }

                pass &= weather.RuntimeShaderName != "Hidden/InternalErrorShader";
                pass &= weather.RuntimeShaderName != "None";

                rows.Add(
                    "SUMMARY|pass=" + pass +
                    "|shader=" + weather.RuntimeShaderName +
                    "|maxParticles=" + particles.main.maxParticles);
                File.WriteAllLines(Report, rows);

                SessionState.SetBool(SessionKey, false);
                EditorApplication.isPlaying = false;
                return;
            }

            MoveTo(step);
            nextAt = Time.realtimeSinceStartupAsDouble + 2.4;
        }
        catch (Exception e)
        {
            Fail(e);
        }
    }

    static void MoveTo(int index)
    {
        Terrain terrain = Terrain.activeTerrains
            .Single(t => t.terrainData && t.terrainData.name == TerrainNames[index]);

        Vector3 p = terrain.transform.position;
        Vector3 s = terrain.terrainData.size;
        float x = p.x + s.x * 0.5f;
        float z = p.z + s.z * 0.5f;
        float y = terrain.SampleHeight(new Vector3(x, 0f, z)) + p.y + 1.6f;

        player.GetComponent<CharacterController>().enabled = false;
        player.transform.position = new Vector3(x, y, z);
        player.GetComponent<CharacterController>().enabled = true;

        particles.Clear(true);
    }

    static void Record(int index)
    {
        rows.Add(
            TerrainNames[index] +
            "|atmosphere=" + atmosphere.CurrentProfileId +
            "|weather=" + weather.CurrentWeatherId +
            "|emission=" + weather.CurrentEmissionRate.ToString("F2") +
            "|alive=" + weather.AliveParticleCount +
            "|shader=" + weather.RuntimeShaderName);
        File.WriteAllLines(Report, rows);
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

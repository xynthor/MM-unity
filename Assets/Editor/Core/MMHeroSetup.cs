using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MMHeroSetup
{
    const string HeroPath = "Assets/Player/Hero/Heraklios.fbx";
    const string AnimRoot = "Assets/Player/Hero/Animations";
    const string ControllerPath = "Assets/Player/Hero/MMHero.controller";

    [MenuItem("MMUnity/Player/Setup Hero Animation System")]
    public static void Setup()
    {
        ConfigureHero();
        ConfigureAnimations();
        AssetDatabase.Refresh();
        BuildController();
        Validate();
        Debug.Log("MM_HERO_SETUP_DONE");
    }

    [MenuItem("MMUnity/Player/Build Hero Controller")]
    public static void BuildOnly()
    {
        AssetDatabase.Refresh();
        BuildController();
        Validate();
        Debug.Log("MM_HERO_BUILD_DONE");
    }

    [MenuItem("MMUnity/Player/Build Hero Controller Fast")]
    public static void BuildFast()
    {
        BuildController();
        Debug.Log("MM_HERO_BUILD_FAST_DONE");
    }

    [MenuItem("MMUnity/Player/Log Hero Import Enums")]
    public static void LogImportEnums()
    {
        Debug.Log("MM_HERO_ENUM animHuman=" + (int)ModelImporterAnimationType.Human +
                  " avatarCreate=" + (int)ModelImporterAvatarSetup.CreateFromThisModel +
                  " avatarCopy=" + (int)ModelImporterAvatarSetup.CopyFromOther +
                  " avatarNone=" + (int)ModelImporterAvatarSetup.NoAvatar);
    }

    [MenuItem("MMUnity/Player/Validate Hero Avatars")]
    public static void ValidateOnly()
    {
        Validate();
        Debug.Log("MM_HERO_VALIDATE_DONE");
    }

    [MenuItem("MMUnity/Player/Reimport Hero Test Pair")]
    public static void ReimportTestPair()
    {
        AssetDatabase.ImportAsset(HeroPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(AnimRoot + "/Locomotion Pack/idle.fbx", ImportAssetOptions.ForceUpdate);
        Debug.Log("MM_HERO_TEST_REIMPORT_DONE");
    }

    [MenuItem("MMUnity/Player/Validate Hero Samples Fast")]
    public static void ValidateSamplesFast()
    {
        string[] paths = {
            HeroPath,
            AnimRoot + "/Locomotion Pack/idle.fbx",
            AnimRoot + "/Great Sword Pack/great sword attack.fbx",
            AnimRoot + "/Pro Melee Axe Pack/standing melee attack horizontal.fbx",
            AnimRoot + "/Pro Longbow Pack/standing dodge forward.fbx",
            AnimRoot + "/Male Injured Pack/injured idle.fbx",
            AnimRoot + "/Magic Spell Pack/Standing 2H Magic Attack 01.fbx",
            AnimRoot + "/Action Adventure Pack/falling idle.fbx",
            AnimRoot + "/Sword and Shield Pack/sword and shield slash.fbx"
        };
        foreach (string p in paths)
        {
            Avatar av = AssetDatabase.LoadAllAssetsAtPath(p).OfType<Avatar>().FirstOrDefault();
            Debug.Log("MM_HERO_SAMPLE path=" + p + " avatar=" + (av != null) +
                      " valid=" + (av != null && av.isValid) +
                      " human=" + (av != null && av.isHuman));
        }
        Debug.Log("MM_HERO_SAMPLE_DONE");
    }

    [MenuItem("MMUnity/Player/Audit Open Scene Players")]
    public static void AuditOpenPlayers()
    {
        var players = UnityEngine.Object.FindObjectsByType<MMThirdPersonController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Debug.Log("MM_HERO_PLAYER_AUDIT count=" + players.Length);
        foreach (var p in players)
            Debug.Log("MM_HERO_PLAYER name=" + p.name + " scene=" + p.gameObject.scene.name +
                      " active=" + p.gameObject.activeInHierarchy +
                      " visual=" + (p.visualRoot ? p.visualRoot.name : "null"));
    }

    [MenuItem("MMUnity/Player/Apply Hero To Active Open Player")]
    public static void ApplyHeroToActiveOpenPlayer()
    {
        var players = UnityEngine.Object.FindObjectsByType<MMThirdPersonController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        var active = players.Where(p => p.gameObject.activeInHierarchy).ToArray();
        if (active.Length != 1)
            throw new Exception("Expected exactly one active player, found " + active.Length);
        ReplacePlayerVisual(active[0]);
        EditorSceneManager.MarkSceneDirty(active[0].gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("MM_HERO_ACTIVE_PLAYER_APPLIED name=" + active[0].name);
    }

    public static void ReplacePlayerVisual(MMThirdPersonController ctrl)
    {
        var player = ctrl.gameObject;
        Vector3 pos = player.transform.position;
        Quaternion rot = player.transform.rotation;
        Vector3 scale = player.transform.localScale;
        var hero = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath);
        var ac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(HeroPath).OfType<Avatar>()
            .FirstOrDefault(a => a.isValid && a.isHuman);
        if (!hero || !ac || !avatar) throw new Exception("Hero asset/controller/avatar missing.");

        if (ctrl.visualRoot && ctrl.visualRoot.gameObject != player)
            UnityEngine.Object.DestroyImmediate(ctrl.visualRoot.gameObject);

        var visual = (GameObject)PrefabUtility.InstantiatePrefab(hero, player.transform);
        visual.name = "HERAKLIOS Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;

        var cc = player.GetComponent<CharacterController>();
        float targetHeight = cc ? cc.height * Mathf.Abs(player.transform.lossyScale.y) : 1.08f;
        ScaleVisualToHeight(visual, targetHeight);
        Bounds b = RendererBounds(visual);
        float capsuleBottom = cc ? player.transform.TransformPoint(cc.center - Vector3.up * cc.height * 0.5f).y : player.transform.position.y;
        visual.transform.position += Vector3.up * (capsuleBottom - b.min.y);

        var animator = visual.GetComponentInChildren<Animator>();
        if (!animator) animator = visual.AddComponent<Animator>();
        animator.avatar = avatar;
        animator.runtimeAnimatorController = ac;
        animator.applyRootMotion = false;

        ctrl.visualRoot = visual.transform;
        ctrl.animator = animator;
        var combat = player.GetComponent<MMHeroCombatController>();
        if (!combat) combat = player.AddComponent<MMHeroCombatController>();
        combat.animator = animator;

        if (player.transform.position != pos || player.transform.rotation != rot || player.transform.localScale != scale)
            throw new Exception("Player root transform changed during hero swap.");
    }

    static void ScaleVisualToHeight(GameObject go, float targetHeight)
    {
        Bounds b = RendererBounds(go);
        if (b.size.y > 0.0001f)
            go.transform.localScale *= targetHeight / b.size.y;
    }

    static Bounds RendererBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    [MenuItem("MMUnity/Player/Build Heraklios Materials And Apply")]
    public static void BuildHerakliosMaterialsAndApply()
    {
        const string texDir = "Assets/Player/Hero/Textures";
        const string matDir = "Assets/Player/Hero/Materials";
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets/Player/Hero", "Materials");

        string normalPath = texDir + "/Heraklios_Normal.png";
        var ni = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (ni && ni.textureType != TextureImporterType.NormalMap)
        {
            ni.textureType = TextureImporterType.NormalMap;
            ni.SaveAndReimport();
        }

        var bodyTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/Heraklios_Body_Diffuse.png");
        var outfitTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/Heraklios_Outfit_Diffuse.png");
        var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        var specTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texDir + "/Heraklios_Specular.png");
        if (!bodyTex || !outfitTex || !normalTex || !specTex)
            throw new Exception("Heraklios textures are not imported yet.");

        Shader sh = Shader.Find("Standard (Specular setup)");
        if (!sh) sh = Shader.Find("Standard");
        if (!sh) throw new Exception("No compatible Standard shader found.");

        Material body = CreateOrUpdateMaterial(matDir + "/Heraklios_Body.mat", sh, bodyTex, normalTex, specTex);
        Material outfit = CreateOrUpdateMaterial(matDir + "/Heraklios_Outfit.mat", sh, outfitTex, normalTex, specTex);

        var mi = AssetImporter.GetAtPath(HeroPath) as ModelImporter;
        if (!mi) throw new Exception("Heraklios importer missing.");
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "BattalionLeader_MAT"), body);
        mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "phong1"), outfit);
        mi.SaveAndReimport();

        var players = UnityEngine.Object.FindObjectsByType<MMThirdPersonController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        var active = players.FirstOrDefault(p => p.gameObject.activeInHierarchy);
        if (active)
        {
            ReplacePlayerVisual(active);
            EditorSceneManager.MarkSceneDirty(active.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        Debug.Log("MM_HERAKLIOS_MATERIALS_DONE body=" + body.name + " outfit=" + outfit.name);
    }

    static Material CreateOrUpdateMaterial(string path, Shader shader, Texture2D diffuse, Texture2D normal, Texture2D spec)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else m.shader = shader;

        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", diffuse);
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", diffuse);
        if (m.HasProperty("_BumpMap"))
        {
            m.SetTexture("_BumpMap", normal);
            m.EnableKeyword("_NORMALMAP");
        }
        if (m.HasProperty("_SpecGlossMap"))
        {
            m.SetTexture("_SpecGlossMap", spec);
            m.EnableKeyword("_SPECGLOSSMAP");
        }
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.42f);
        EditorUtility.SetDirty(m);
        return m;
    }

    [MenuItem("MMUnity/Player/Audit And Capture Active Hero")]
    public static void AuditAndCaptureActiveHero()
    {
        var players = UnityEngine.Object.FindObjectsByType<MMThirdPersonController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        var ctrl = players.FirstOrDefault(p => p.gameObject.activeInHierarchy);
        if (!ctrl || !ctrl.visualRoot) throw new Exception("Active player/visual missing.");

        var rs = ctrl.visualRoot.GetComponentsInChildren<Renderer>(true);
        Bounds b = RendererBounds(ctrl.visualRoot.gameObject);
        var mats = rs.SelectMany(r => r.sharedMaterials).Where(m => m).Select(m => m.name).Distinct().ToArray();
        var smr = ctrl.visualRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
        Debug.Log("MM_HERO_ACTIVE visual=" + ctrl.visualRoot.name +
                  " mesh=" + (smr && smr.sharedMesh ? smr.sharedMesh.name : "none") +
                  " renderers=" + rs.Length +
                  " height=" + b.size.y.ToString("F3") +
                  " mats=" + string.Join(",", mats));

        string dir = "C:/MMUnityPort/Validation/Hero";
        Directory.CreateDirectory(dir);
        string path = dir + "/Heraklios_active_preview.png";

        var go = new GameObject("TEMP Hero Preview Camera");
        var cam = go.AddComponent<Camera>();
        if (ctrl.playerCamera) cam.CopyFrom(ctrl.playerCamera);
        cam.enabled = false;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 38f;
        cam.nearClipPlane = 0.02f;
        cam.farClipPlane = 5000f;

        Vector3 focus = b.center + Vector3.up * (b.size.y * 0.03f);
        float dist = Mathf.Max(b.size.y * 2.05f, 2.0f);
        Vector3 front = ctrl.transform.forward;
        if (front.sqrMagnitude < 0.1f) front = Vector3.forward;
        go.transform.position = focus + front.normalized * dist + Vector3.up * (b.size.y * 0.08f);
        go.transform.rotation = Quaternion.LookRotation(focus - go.transform.position, Vector3.up);

        var rt = new RenderTexture(900, 900, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(900, 900, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 900, 900), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(go);
        Debug.Log("MM_HERO_PREVIEW " + path);
    }

    static void ConfigureHero()
    {
        var mi = AssetImporter.GetAtPath(HeroPath) as ModelImporter;
        if (!mi) throw new Exception("Hero ModelImporter missing: " + HeroPath);
        bool dirty = mi.animationType != ModelImporterAnimationType.Human ||
                     mi.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;
        mi.animationType = ModelImporterAnimationType.Human;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        mi.importAnimation = true;
        if (dirty) mi.SaveAndReimport();
    }    static void ConfigureAnimations()
    {
        string full = Path.Combine(Directory.GetCurrentDirectory(), AnimRoot);
        string[] files = Directory.GetFiles(full, "*.fbx", SearchOption.AllDirectories);
        int changed = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string f in files)
            {
                string assetPath = f.Replace('\\','/').Substring(Directory.GetCurrentDirectory().Replace('\\','/').Length + 1);
                var mi = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (!mi) continue;
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.importAnimation = true;
                mi.importCameras = false;
                mi.importLights = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;

                var clips = mi.defaultClipAnimations;
                if (clips != null && clips.Length > 0)
                {
                    bool loop = IsLooping(Path.GetFileNameWithoutExtension(f));
                    foreach (var c in clips)
                    {
                        c.loopTime = loop;
                        c.loopPose = loop;
                    }
                    mi.clipAnimations = clips;
                }
                mi.SaveAndReimport();
                changed++;
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        Debug.Log("MM_HERO_IMPORT configured=" + changed);
    }

    static bool IsLooping(string n)
    {
        n = n.ToLowerInvariant();
        return n.Contains("idle") || n.Contains("walk") || n.Contains("run") ||
               n.Contains("strafe") || n.Contains("sneak") || n.Contains("fall a loop");
    }    static AnimationClip Clip(string pack, string file)
    {
        string p = AnimRoot + "/" + pack + "/" + file + ".fbx";
        return AssetDatabase.LoadAllAssetsAtPath(p)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
    }

    static void AddTrigger(AnimatorController ac, string n)
        => ac.AddParameter(n, AnimatorControllerParameterType.Trigger);

    static AnimatorState AddAction(AnimatorStateMachine sm, string name, AnimationClip clip, string trigger, float exit = 0.92f)
    {
        if (!clip) throw new Exception("Missing clip for state: " + name);
        var st = sm.AddState(name);
        st.motion = clip;
        var enter = sm.AddAnyStateTransition(st);
        enter.hasExitTime = false;
        enter.duration = 0.06f;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        var back = st.AddTransition(sm.defaultState);
        back.hasExitTime = true;
        back.exitTime = exit;
        back.duration = 0.08f;
        return st;
    }

    static void BuildController()
    {
        var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (!ac)
        {
            ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }
        else
        {
            // Rebuild in place so open-scene Animator references never drop while
            // iterating the generated controller.
            var existingRoot = ac.layers[0].stateMachine;
            existingRoot.states = Array.Empty<ChildAnimatorState>();
            existingRoot.stateMachines = Array.Empty<ChildAnimatorStateMachine>();
            existingRoot.anyStateTransitions = Array.Empty<AnimatorStateTransition>();
            existingRoot.entryTransitions = Array.Empty<AnimatorTransition>();
            existingRoot.defaultState = null;

            foreach (var objectToRemove in AssetDatabase.LoadAllAssetsAtPath(ControllerPath)
                         .Where(o => o is AnimatorState || o is BlendTree)
                         .ToArray())
                UnityEngine.Object.DestroyImmediate(objectToRemove, true);

            ac.parameters = Array.Empty<AnimatorControllerParameter>();
        }

        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("MoveX", AnimatorControllerParameterType.Float);
        ac.AddParameter("MoveY", AnimatorControllerParameterType.Float);
        ac.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        ac.AddParameter("Block", AnimatorControllerParameterType.Bool);
        foreach (string p in new[]{"Attack1","Attack2","Attack3","HeavyAttack","Kick",
                                   "DodgeForward","DodgeBackward","DodgeLeft","DodgeRight","Hit","Death","Cast",
                                   "TurnLeft","TurnRight"})
            AddTrigger(ac,p);

        var sm = ac.layers[0].stateMachine;
        var locomotion = sm.AddState("Locomotion");
        sm.defaultState = locomotion;

        var blend = new BlendTree {
            name="HeroLocomotion", blendType=BlendTreeType.Simple1D,
            blendParameter="Speed", useAutomaticThresholds=false
        };
        AssetDatabase.AddObjectToAsset(blend, ac);

        var walkDirectional = new BlendTree {
            name="HeroWalkDirectional",
            blendType=BlendTreeType.SimpleDirectional2D,
            blendParameter="MoveX",
            blendParameterY="MoveY",
            useAutomaticThresholds=false
        };
        AssetDatabase.AddObjectToAsset(walkDirectional, ac);
        walkDirectional.AddChild(Clip("Locomotion Pack","walking"), new Vector2(0f,1f));
        walkDirectional.AddChild(Clip("Locomotion Pack","left strafe walking"), new Vector2(-1f,0f));
        walkDirectional.AddChild(Clip("Locomotion Pack","right strafe walking"), new Vector2(1f,0f));
        walkDirectional.AddChild(Clip("Pro Melee Axe Pack","standing walk back"), new Vector2(0f,-1f));

        var runDirectional = new BlendTree {
            name="HeroRunDirectional",
            blendType=BlendTreeType.SimpleDirectional2D,
            blendParameter="MoveX",
            blendParameterY="MoveY",
            useAutomaticThresholds=false
        };
        AssetDatabase.AddObjectToAsset(runDirectional, ac);
        runDirectional.AddChild(Clip("Locomotion Pack","running"), new Vector2(0f,1f));
        runDirectional.AddChild(Clip("Locomotion Pack","left strafe"), new Vector2(-1f,0f));
        runDirectional.AddChild(Clip("Locomotion Pack","right strafe"), new Vector2(1f,0f));
        runDirectional.AddChild(Clip("Pro Melee Axe Pack","standing run back"), new Vector2(0f,-1f));

        blend.AddChild(Clip("Locomotion Pack","idle"),0f);
        blend.AddChild(walkDirectional,0.42f);
        blend.AddChild(runDirectional,0.78f);
        locomotion.motion = blend;

        var air = sm.AddState("Air");
        var airBlend = new BlendTree {
            name="HeroAir",
            blendType=BlendTreeType.Simple1D,
            blendParameter="VerticalSpeed",
            useAutomaticThresholds=false
        };
        AssetDatabase.AddObjectToAsset(airBlend, ac);
        airBlend.AddChild(Clip("Action Adventure Pack","falling idle"),-4f);
        airBlend.AddChild(Clip("Locomotion Pack","jump"),4f);
        air.motion = airBlend;

        var toAir = locomotion.AddTransition(air);
        toAir.hasExitTime = false;
        toAir.duration = 0.03f;
        toAir.AddCondition(AnimatorConditionMode.IfNot,0f,"Grounded");

        var fromAir = air.AddTransition(locomotion);
        fromAir.hasExitTime = false;
        fromAir.duration = 0.10f;
        fromAir.AddCondition(AnimatorConditionMode.If,0f,"Grounded");

        var turnLeft = sm.AddState("Turn Left");
        turnLeft.motion = Clip("Locomotion Pack","left turn 90");
        turnLeft.speed = 1.45f;
        var toTurnLeft = locomotion.AddTransition(turnLeft);
        toTurnLeft.hasExitTime = false;
        toTurnLeft.duration = 0.04f;
        toTurnLeft.AddCondition(AnimatorConditionMode.If,0f,"TurnLeft");
        var fromTurnLeft = turnLeft.AddTransition(locomotion);
        fromTurnLeft.hasExitTime = true;
        fromTurnLeft.exitTime = 0.72f;
        fromTurnLeft.duration = 0.08f;

        var turnRight = sm.AddState("Turn Right");
        turnRight.motion = Clip("Locomotion Pack","right turn 90");
        turnRight.speed = 1.45f;
        var toTurnRight = locomotion.AddTransition(turnRight);
        toTurnRight.hasExitTime = false;
        toTurnRight.duration = 0.04f;
        toTurnRight.AddCondition(AnimatorConditionMode.If,0f,"TurnRight");
        var fromTurnRight = turnRight.AddTransition(locomotion);
        fromTurnRight.hasExitTime = true;
        fromTurnRight.exitTime = 0.72f;
        fromTurnRight.duration = 0.08f;

        var controllerLayers = ac.layers;
        controllerLayers[0].iKPass = true;
        ac.layers = controllerLayers;
        AddAction(sm,"Attack 1",Clip("Pro Melee Axe Pack","standing melee attack horizontal"),"Attack1");
        AddAction(sm,"Attack 2",Clip("Pro Melee Axe Pack","standing melee attack backhand"),"Attack2");
        AddAction(sm,"Attack 3",Clip("Pro Melee Axe Pack","standing melee attack downward"),"Attack3");
        AddAction(sm,"Heavy Attack",Clip("Pro Melee Axe Pack","standing melee attack 360 high"),"HeavyAttack");
        AddAction(sm,"Kick",Clip("Pro Melee Axe Pack","standing melee attack kick ver. 1"),"Kick");
        AddAction(sm,"Dodge Forward",Clip("Pro Longbow Pack","standing dodge forward"),"DodgeForward",0.86f);
        AddAction(sm,"Dodge Backward",Clip("Pro Longbow Pack","standing dodge backward"),"DodgeBackward",0.86f);
        AddAction(sm,"Dodge Left",Clip("Pro Longbow Pack","standing dodge left"),"DodgeLeft",0.86f);
        AddAction(sm,"Dodge Right",Clip("Pro Longbow Pack","standing dodge right"),"DodgeRight",0.86f);
        AddAction(sm,"Hit",Clip("Pro Melee Axe Pack","standing react large gut"),"Hit",0.90f);
        AddAction(sm,"Cast",Clip("Magic Spell Pack","Standing 2H Magic Attack 01"),"Cast",0.93f);

        var block = sm.AddState("Block");
        block.motion = Clip("Pro Melee Axe Pack","standing block idle");
        var toBlock = sm.AddAnyStateTransition(block);
        toBlock.hasExitTime = false; toBlock.duration = 0.05f; toBlock.canTransitionToSelf = false;
        toBlock.AddCondition(AnimatorConditionMode.If,0f,"Block");
        var fromBlock = block.AddTransition(locomotion);
        fromBlock.hasExitTime = false; fromBlock.duration = 0.08f;
        fromBlock.AddCondition(AnimatorConditionMode.IfNot,0f,"Block");

        var death = sm.AddState("Death");
        death.motion = Clip("Great Sword Pack","two handed sword death");
        var toDeath = sm.AddAnyStateTransition(death);
        toDeath.hasExitTime = false; toDeath.duration = 0.05f; toDeath.canTransitionToSelf = false;
        toDeath.AddCondition(AnimatorConditionMode.If,0f,"Death");

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        Debug.Log("MM_HERO_CONTROLLER built=" + ControllerPath);
    }    static void Validate()
    {
        var hero = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath);
        if (!hero) throw new Exception("Hero asset missing after import.");
        var av = hero.GetComponent<Animator>() ? hero.GetComponent<Animator>().avatar : null;
        if (!av)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(HeroPath).OfType<Avatar>().ToArray();
            av = all.FirstOrDefault();
        }
        Debug.Log("MM_HERO_AVATAR valid=" + (av && av.isValid) + " human=" + (av && av.isHuman));

        int valid = 0, invalid = 0;
        string full = Path.Combine(Directory.GetCurrentDirectory(), AnimRoot);
        foreach (string f in Directory.GetFiles(full, "*.fbx", SearchOption.AllDirectories))
        {
            string ap = f.Replace('\\','/').Substring(Directory.GetCurrentDirectory().Replace('\\','/').Length + 1);
            var avs = AssetDatabase.LoadAllAssetsAtPath(ap).OfType<Avatar>().ToArray();
            if (avs.Any(a => a.isValid && a.isHuman)) valid++; else invalid++;
        }
        Debug.Log("MM_HERO_ANIM_AVATARS valid=" + valid + " invalid=" + invalid);
    }
}

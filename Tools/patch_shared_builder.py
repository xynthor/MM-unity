from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('const float ArchitectureScale = 1.35f;', 'const float ArchitectureScale = 1.50f;')
s=s.replace('static readonly string TilePath = "Assets/World/NewSorpigal/Data/tilemap_u8.bin";',
'''static readonly string TilePath = "Assets/World/NewSorpigal/Data/tilemap_u8.bin";
    static readonly string SemPath = "Assets/World/NewSorpigal/Data/tile_semantics_u8.bin";''')
s=s.replace('e.treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/UnitySamples/Trees/BanyanTree.fbx");',
'''e.treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/TreeHigh001.fbx");''')
s=s.replace('e.rockA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/UnitySamples/Rock_A_01.fbx");',
'''e.rockA = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/Rock001.fbx");''')
s=s.replace('e.rockB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/UnitySamples/Rock_A_02.fbx");',
'''e.rockB = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EnvironmentAssets/Gobkit/Rock002.fbx");''')
old='''    static bool IsWater(byte tile) => tile >= 126 && tile <= 161;
    static bool IsDirt(byte tile) => tile >= 162 && tile <= 197;
    static bool IsRoad(byte tile) => tile >= 198 && tile <= 233;'''
new='''    static byte[] semanticCache;
    static byte Sem(byte tile)
    {
        if (semanticCache == null) semanticCache = File.ReadAllBytes(SemPath);
        return semanticCache[tile];
    }
    static bool IsWater(byte tile) => (Sem(tile) & 1) != 0;
    static bool IsShore(byte tile) => (Sem(tile) & 2) != 0;
    static bool IsRoad(byte tile) => (Sem(tile) & 8) != 0;
    static bool IsDirt(byte tile) => (Sem(tile) & 16) != 0 && !IsWater(tile) && !IsRoad(tile);'''
if old not in s: raise SystemExit('semantic block not found')
s=s.replace(old,new)
p.write_text(s,encoding='utf-8')
print('phase1 patched')
s=p.read_text(encoding='utf-8')
s=s.replace('var baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Player/Runner/Runner_Base.fbx");',
'''var baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Player/Warrior/MM_Warrior.fbx");''')
s=s.replace('visual.name = "Runner Visual";', 'visual.name = "Warrior Visual";')
s=s.replace('string path="Assets/Player/Runner/MMPlayer.controller";', 'string path="Assets/Player/Warrior/MMPlayer.controller";')
start=s.index('    static RuntimeAnimatorController CreatePlayerAnimatorController()')
end=s.index('    static void SetupLighting(Transform parent)', start)
replacement='''    static RuntimeAnimatorController CreatePlayerAnimatorController()
    {
        string path="Assets/Player/Warrior/MMPlayer.controller";
        AssetDatabase.DeleteAsset(path);
        var ac=AnimatorController.CreateAnimatorControllerAtPath(path);
        ac.AddParameter("Speed",AnimatorControllerParameterType.Float);
        ac.AddParameter("Grounded",AnimatorControllerParameterType.Bool);
        var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Player/Warrior/MM_Warrior.fbx")
            .OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        Func<string,AnimationClip> get=n=>clips.FirstOrDefault(c=>c.name.IndexOf(n,StringComparison.OrdinalIgnoreCase)>=0);
        var idle=get("Idle") ?? clips.FirstOrDefault();
        var walk=get("Walk") ?? idle;
        var run=get("Run") ?? walk;
        var sm=ac.layers[0].stateMachine;
        var locomotion=sm.AddState("Locomotion");
        var blend=new BlendTree { name="LocomotionBlend", blendType=BlendTreeType.Simple1D,
            blendParameter="Speed", useAutomaticThresholds=false };
        AssetDatabase.AddObjectToAsset(blend,ac);
        if (idle) blend.AddChild(idle,0f);
        if (walk) blend.AddChild(walk,0.35f);
        if (run) blend.AddChild(run,1f);
        locomotion.motion=blend;
        sm.defaultState=locomotion;
        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        return ac;
    }

'''
s=s[:start]+replacement+s[end:]
p.write_text(s,encoding='utf-8')
print('phase2 player patched')
s=p.read_text(encoding='utf-8')
s=s.replace('ScaleToHeight(go,Mathf.Lerp(6.5f,10.5f,Deterministic01(i*19+3)));',
'''ScaleToHeight(go,Mathf.Lerp(7.5f,11.5f,Deterministic01(i*19+3)));''')
s=s.replace('ScaleToHeight(extra,Mathf.Lerp(6f,9.5f,Deterministic01(i*47)));',
'''ScaleToHeight(extra,Mathf.Lerp(7.0f,10.5f,Deterministic01(i*47)));''')
s=s.replace('ScaleToHeight(go,Mathf.Lerp(1.8f,3.8f,Deterministic01(i*53)));',
'''float rockTarget = (i % 11 == 0)
                    ? Mathf.Lerp(1.5f,2.6f,Deterministic01(i*53))
                    : Mathf.Lerp(0.45f,1.35f,Deterministic01(i*53));
                ScaleToHeight(go,rockTarget);''')
s=s.replace('cc.height = 1.78f;', 'cc.height = 1.80f;')
s=s.replace('cc.center = new Vector3(0,0.89f,0);', 'cc.center = new Vector3(0,0.90f,0);')
p.write_text(s,encoding='utf-8')
print('phase3 scale patched')

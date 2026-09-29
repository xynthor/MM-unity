from pathlib import Path
import shutil
p=Path(r"C:\MMUnityPort\Assets\Editor\MMInternalCoastPass.cs")
s=p.read_text(encoding="utf-8-sig")
shutil.copy2(p,Path(r"C:\MMUnityPort\Backups\MMInternalCoastPass_pre_realistic_safe_20260919.cs"))
s=s.replace('''    static readonly Z[] Zones={
        new Z("CastleIronfist","Assets/Scenes/CastleIronfist_SourceGrid.unity"),''',
'''    static readonly Z[] Zones={
        new Z("NewSorpigal","Assets/Scenes/NewSorpigal_OpenWorld.unity"),
        new Z("CastleIronfist","Assets/Scenes/CastleIronfist_SourceGrid.unity"),''')
s=s.replace('const int N=128; const float WaterY=.12f;','const int N=128; const float WaterY=.10f;')
s=s.replace('''        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0)??sc.GetRootGameObjects().First();
        var terrain=root.GetComponentInChildren<Terrain>(true);if(!terrain)throw new Exception(z.key+" terrain missing");''',
'''        var roots=sc.GetRootGameObjects();
        var terrain=roots.SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).FirstOrDefault();
        if(!terrain)throw new Exception(z.key+" terrain missing");
        var root=roots.FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true)==terrain)??roots.First();''')
s=s.replace('int tris=BuildWater(z,root.transform,tile,sem,mat);','int tris=BuildWater(z,root.transform,tile,sem,mat,terrain);')
s=s.replace('''    static Material WaterMaterial()
    {
        string p="Assets/Materials/SourceGridTerrain/InternalWaterSmooth.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        var sh=Shader.Find("MMUnity/DepthWater")??Shader.Find("Standard");if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,p);}m.shader=sh;
        m.color=new Color(.05f,.32f,.43f,.82f);if(m.HasProperty("_ShallowColor"))m.SetColor("_ShallowColor",new Color(.09f,.57f,.66f,.52f));
        if(m.HasProperty("_DeepColor"))m.SetColor("_DeepColor",new Color(.015f,.14f,.28f,.88f));if(m.HasProperty("_DepthRange"))m.SetFloat("_DepthRange",10f);
        m.renderQueue=3000;EditorUtility.SetDirty(m);return m;
    }''',
'''    static Material WaterMaterial()
    {
        string p="Assets/Materials/SourceGridTerrain/GlobalOcean.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        var sh=Shader.Find("MMUnity/EnrothMasterWater")??Shader.Find("MMUnity/DepthWater")??Shader.Find("Standard");
        if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,p);}else m.shader=sh;
        if(m.HasProperty("_DepthRange"))m.SetFloat("_DepthRange",18f);
        if(m.HasProperty("_FoamDepth"))m.SetFloat("_FoamDepth",1.0f);
        if(m.HasProperty("_NormalTex"))m.SetTexture("_NormalTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TerrainDemoScene_HDRP/Prefabs/Water/Textures/Water_Normal.png"));
        if(m.HasProperty("_NormalStrength"))m.SetFloat("_NormalStrength",.45f);
        m.renderQueue=3000;EditorUtility.SetDirty(m);return m;
    }''')
s=s.replace('static int BuildWater(Z z,Transform root,byte[] tile,byte[] sem,Material mat)',
            'static int BuildWater(Z z,Transform root,byte[] tile,byte[] sem,Material mat,Terrain terrain)')
old='''            for(int i=0;i<4;i++){w[i]=SoftWorld(tile,sem,p[i].x,p[i].y);if(w[i]>=th)bits|=1<<i;}'''
new='''            for(int i=0;i<4;i++)
            {
                w[i]=SoftWorld(tile,sem,p[i].x,p[i].y);
                float gy=terrain.SampleHeight(new Vector3(p[i].x,0f,p[i].y))+terrain.transform.position.y;
                if(gy>WaterY+.08f)w[i]=0f;
                if(w[i]>=th)bits|=1<<i;
            }'''
if old not in s: raise SystemExit("water corner needle missing")
s=s.replace(old,new,1)
p.write_text(s,encoding="utf-8")
print("INTERNAL_WATER_SAFE_PATCHED")
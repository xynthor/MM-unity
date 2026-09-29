from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothFullWorld.cs')
s=p.read_text(encoding='utf-8')
needle='    static void BuildMasterOcean(Transform parent,Terrain mainland,Terrain dragon)\n'
helper=r'''    static Material MasterWaterMaterial()
    {
        string path="Assets/World/Enroth/Generated/EnrothMasterWater.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("MMUnity/DepthWater"));AssetDatabase.CreateAsset(m,path);}
        m.shader=Shader.Find("MMUnity/DepthWater");
        m.SetColor("_ShallowColor",new Color(.10f,.43f,.52f,.72f));
        m.SetColor("_DeepColor",new Color(.012f,.075f,.17f,.97f));
        m.SetColor("_FoamColor",new Color(.80f,.94f,.90f,.82f));
        m.SetFloat("_DepthRange",12f); m.SetFloat("_FoamDepth",1.1f);
        m.SetFloat("_WaveScale",.10f); m.SetVector("_WaveSpeed",new Vector4(.028f,.018f,0,0));
        EditorUtility.SetDirty(m); return m;
    }

'''
if helper not in s:s=s.replace(needle,helper+needle,1)
s=s.replace('r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/NewSorpigal/Environment/WaterDepth.mat");','r.sharedMaterial=MasterWaterMaterial();')
p.write_text(s,encoding='utf-8')
print('master water patched')
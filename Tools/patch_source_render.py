from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\RenderNewSorpigalRiverCheck.cs')
s=p.read_text(encoding='utf-8')
old='''        string outPath=Path.GetFullPath("Preview/NewSorpigal_RiverCheck.png");Directory.CreateDirectory(Path.GetDirectoryName(outPath));File.WriteAllBytes(outPath,tex.EncodeToPNG());
        RenderTexture.active=null;cam.targetTexture=null;RenderSettings.fog=fog;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(go);
        Debug.Log($"NS_RIVER_CHECK bounds={b} output={outPath}");'''
new='''        string outPath=Path.GetFullPath("Preview/NewSorpigal_RiverCheck.png");Directory.CreateDirectory(Path.GetDirectoryName(outPath));File.WriteAllBytes(outPath,tex.EncodeToPNG());
        Vector3 source=new Vector3((64f-103f)*12f,61.5f,(103f-64f)*12f);
        cam.fieldOfView=46f;go.transform.position=source+new Vector3(42f,34f,-48f);go.transform.LookAt(source+new Vector3(0f,-5f,0f));
        cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
        string sourcePath=Path.GetFullPath("Preview/NewSorpigal_RiverSourceCheck.png");File.WriteAllBytes(sourcePath,tex.EncodeToPNG());
        RenderTexture.active=null;cam.targetTexture=null;RenderSettings.fog=fog;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(go);
        Debug.Log($"NS_RIVER_CHECK bounds={b} output={outPath} source={sourcePath}");'''
if s.count(old)!=1: raise SystemExit('match '+str(s.count(old)))
s=s.replace(old,new,1)
p.write_text(s,encoding='utf-8')
print('SOURCE_RENDER_PATCHED')

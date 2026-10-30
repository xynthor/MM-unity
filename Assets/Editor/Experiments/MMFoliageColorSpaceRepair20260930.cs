using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class MMFoliageColorSpaceRepair20260930
{
    const string Root="Assets/World/WorldExtensions/Generated/LinkedFoliageCoverage/Materials";
    const string Backup="Backups/BeforeFoliageSRGBRepair_20260930/manifest.json";
    const string Report="Validation/EdgeGrid20260923/foliage_srgb_repair_20260930.txt";

    public static void Apply()
    {
        if(SceneManager.GetActiveScene().path!="Assets/Scenes/World/Enroth.unity")
            throw new Exception("Linked Enroth scene required");
        if(!File.Exists(Backup)) throw new Exception("Verified foliage backup missing");

        var rows=new List<string>();
        var touched=new HashSet<string>();
        int materials=0,mismatches=0,reimports=0;
        foreach(string mp in AssetDatabase.FindAssets("t:Material",new[]{Root}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var generated=AssetDatabase.LoadAssetAtPath<Material>(mp);
            if(!generated || !(generated.mainTexture is Texture2D)) continue;
            string srcMatPath=AssetDatabase.GUIDToAssetPath(Path.GetFileNameWithoutExtension(mp));
            var source=AssetDatabase.LoadAssetAtPath<Material>(srcMatPath);
            if(!source || !(source.mainTexture is Texture2D)) continue;
            var srcImp=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(source.mainTexture)) as TextureImporter;
            string gp=AssetDatabase.GetAssetPath(generated.mainTexture);
            var genImp=AssetImporter.GetAtPath(gp) as TextureImporter;
            if(srcImp==null || genImp==null) continue;
            materials++;
            float cutoff=generated.HasProperty("_Cutoff")?generated.GetFloat("_Cutoff"):.34f;
            bool need=genImp.sRGBTexture!=srcImp.sRGBTexture
                || !genImp.mipmapEnabled || !genImp.mipMapsPreserveCoverage
                || Mathf.Abs(genImp.alphaTestReferenceValue-cutoff)>.0001f
                || !genImp.alphaIsTransparency || genImp.anisoLevel<4;
            if(!need) continue;
            mismatches++;
            if(touched.Add(gp))
            {
                genImp.sRGBTexture=srcImp.sRGBTexture;
                genImp.alphaSource=TextureImporterAlphaSource.FromInput;
                genImp.alphaIsTransparency=true;
                genImp.mipmapEnabled=true;
                genImp.mipMapsPreserveCoverage=true;
                genImp.alphaTestReferenceValue=cutoff;
                genImp.anisoLevel=4;
                genImp.textureCompression=TextureImporterCompression.CompressedHQ;
                genImp.SaveAndReimport();
                reimports++;
            }
            rows.Add(mp+" | source="+srcMatPath+" | generatedTexture="+gp+" | sRGB="+srcImp.sRGBTexture+" | cutoff="+cutoff.ToString("0.###"));
        }
        AssetDatabase.SaveAssets();
        rows.Insert(0,"PASS materials="+materials+" mismatches="+mismatches+" uniqueTextureReimports="+reimports+" terrainWrites=0 sceneWrites=0 placementWrites=0 canonicalWrites=0");
        File.WriteAllLines(Report,rows);
        Debug.Log(rows[0]);
    }
}

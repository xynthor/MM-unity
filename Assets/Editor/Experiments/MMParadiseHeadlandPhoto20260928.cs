using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Security.Cryptography;

// Explicit one-time edit of the verified eight-channel generated terrain.
// Called through Unity MCP. No editor-load callbacks or automatic flags.
public static class MMParadiseHeadlandPhoto20260928
{
    const string Root="Validation/EdgeGrid20260923/";
    const string Path="Assets/World/WorldExtensions/Generated/ParadiseValley_WestTerrain.asset";
    const string Expected="00eda311707d89a83a30cfa190b9406b3248e62460217adeaae53ad81deabb6c";
    static string Hash(string path) { using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant(); }
    public static void Apply()
    {
        if(!File.Exists("Backups/Takeover_20260927_235314/manifest.json")||Hash(Path)!=Expected)
            throw new Exception("Verified eight-channel headland baseline required; repeat application refused.");
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(Path);
        if(!td||td.alphamapLayers!=8||td.terrainLayers[4].name!="ReferenceWestForestInterior"||td.terrainLayers[7].name!="ReferenceWestOchreCape")
            throw new Exception("Unexpected Paradise West material layout.");
        var image=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        Color32[] mask;
        try {
            if(!image.LoadImage(File.ReadAllBytes(Root+"Takeover_ParadiseHeadlandMask.png"))||image.width!=513||image.height!=513)
                throw new Exception("Registered 513px headland mask required.");
            mask=image.GetPixels32();
        } finally { UnityEngine.Object.DestroyImmediate(image); }
        var old=td.GetAlphamaps(0,0,512,512);
        var a=(float[,,])old.Clone();
        int count=0; double moved=0;
        for(int z=1;z<511;z++) for(int x=1;x<511;x++) {
            float u=x/511f,v=z/511f;
            if(td.GetInterpolatedHeight(u,v)-24f<1.0f||old[z,x,4]>.025f||old[z,x,6]>.08f)continue;
            float confidence=mask[Mathf.RoundToInt(v*512)*513+Mathf.RoundToInt(u*512)].r/255f;
            float slope=td.GetSteepness(u,v);
            float w=confidence*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(28,48,slope)));
            if(w<.02f)continue;
            // Transfer soil and sand, plus a smaller exposed-rock fraction.
            // Forest, road and all height samples remain unchanged.
            float sand=a[z,x,2]*.92f*w;
            float soil=a[z,x,5]*.85f*w;
            float rock=a[z,x,3]*.45f*w;
            a[z,x,2]-=sand;a[z,x,5]-=soil;a[z,x,3]-=rock;
            a[z,x,7]+=sand+soil+rock;
            moved+=sand+soil+rock;count++;
        }
        if(count<3000||moved<1000)throw new Exception("Registered headland paint unexpectedly small: "+count+" / "+moved);
        for(int i=0;i<512;i++)for(int k=0;k<8;k++)
            if(a[0,i,k]!=old[0,i,k]||a[511,i,k]!=old[511,i,k]||a[i,0,k]!=old[i,0,k]||a[i,511,k]!=old[i,511,k])
                throw new Exception("Protected material border changed.");
        td.SetAlphamaps(0,0,a);td.SetBaseMapDirty();EditorUtility.SetDirty(td);AssetDatabase.SaveAssets();
        foreach(var t in Terrain.activeTerrains)if(t.terrainData==td)t.Flush();
        File.WriteAllText(Root+"Takeover_ParadiseHeadland_Result.txt","painted="+count+" transferredAlpha="+moved.ToString("F2")+" channels=8 heightWrites=0 borderWrites=0 forestChannelWrites=0 canonicalWrites=0\n");
        Debug.Log("HEADLAND_REFERENCE_PASS painted="+count+" transferredAlpha="+moved.ToString("F2"));
    }
}

using UnityEditor;
using UnityEngine;
using System;

public static class MMTargetedCornerHeightPass
{
    static TerrainData T(string z)=>AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/World/"+z+"/Generated/"+z+"Terrain.asset");

    static void BlendPair(TerrainData west,TerrainData east,bool fromSouth)
    {
        int r=west.heightmapResolution;
        if(east.heightmapResolution!=r)throw new Exception("height resolution mismatch");
        var a=west.GetHeights(0,0,r,r);var b=east.GetHeights(0,0,r,r);
        const int along=15;
        const int inward=9;
        for(int i=0;i<=along;i++)
        {
            int y=fromSouth?i:r-1-i;
            float aw=a[y,r-1]*west.size.y;
            float bw=b[y,0]*east.size.y;
            float target=(aw+bw)*.5f;
            float da=(target-aw)/west.size.y;
            float db=(target-bw)/east.size.y;
            for(int q=0;q<inward;q++)
            {
                float u=q/(float)(inward-1);
                float w=1f-Mathf.SmoothStep(0f,1f,u);
                a[y,r-1-q]+=da*w;
                b[y,q]+=db*w;
            }
        }
        west.SetHeights(0,0,a);east.SetHeights(0,0,b);
        EditorUtility.SetDirty(west);EditorUtility.SetDirty(east);
    }

    [MenuItem("MMUnity/Realistic World/Targeted Four-Way Corner Height Repair")]
    public static void Apply()
    {
        BlendPair(T("ParadiseValley"),T("Blackshire"),true);
        BlendPair(T("HermitsIsle"),T("Dragonsand"),false);
        AssetDatabase.SaveAssets();
        Debug.Log("TARGETED_CORNER_HEIGHT_REPAIR_DONE junction=Hermits_Dragonsand_Paradise_Blackshire");
    }
}
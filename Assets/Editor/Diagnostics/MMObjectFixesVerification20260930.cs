using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class MMObjectFixesVerification20260930
{
    const float X0=-1792f,Z0=-1280f,Step=2f;
    const string Mask="Validation/EdgeGrid20260923/WaterQA20260929/CurrentWaterCoverage.png";
    static bool Wet(Color32[] px,int w,int h,float x,float z){
        int ix=Mathf.FloorToInt((x-X0)/Step),iz=Mathf.FloorToInt((z-Z0)/Step);
        if(ix<0||iz<0||ix>=w||iz>=h)return true;
        var c=px[iz*w+ix];
        return (c.r==40&&c.g==100&&c.b==135)||(c.r==240&&c.g==75&&c.b==35);
    }
    public static void Run(){
        var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
        var tex=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        if(!tex.LoadImage(File.ReadAllBytes(Mask)))throw new Exception("Water mask load failed");
        var px=tex.GetPixels32();int mw=tex.width,mh=tex.height;UnityEngine.Object.DestroyImmediate(tex);

        var rooibos=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true))
            .Where(f=>f.sharedMesh&&AssetDatabase.GetAssetPath(f.sharedMesh).IndexOf("wild_rooibos_bush",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        int wetRooibos=rooibos.Count(f=>{var r=f.GetComponent<Renderer>();var p=r?r.bounds.center:f.transform.position;return Wet(px,mw,mh,p.x,p.z);});

        var granite=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true))
            .Where(f=>f.sharedMesh&&f.sharedMesh.name.IndexOf("Rock_Granite_rcCwC_LOD",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        int graniteBad=granite.Count(f=>{var r=f.GetComponent<MeshRenderer>();if(!r||!r.sharedMaterial||!r.sharedMaterial.shader||r.sharedMaterial.shader.name!="Standard")return true;
            var t=r.sharedMaterial.GetTexture("_MainTex");return !t||AssetDatabase.GetAssetPath(t).IndexOf("Rock_Granite_rcCwC_albedo",StringComparison.OrdinalIgnoreCase)<0;});

        var rockRoot=s.GetRootGameObjects().FirstOrDefault(g=>g.name=="Original Regions Rock Scatter 20260930");
        int rocks=rockRoot?rockRoot.GetComponentsInChildren<MeshRenderer>(true).Length:0;
        int wetRocks=rockRoot?rockRoot.GetComponentsInChildren<MeshRenderer>(true).Count(r=>Wet(px,mw,mh,r.bounds.center.x,r.bounds.center.z)):0;
        int badRockMat=rockRoot?rockRoot.GetComponentsInChildren<MeshRenderer>(true).Count(r=>!r.sharedMaterial||!r.sharedMaterial.shader||r.sharedMaterial.shader.name!="Standard"||!r.sharedMaterial.mainTexture):0;

        var ts=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>(true)).ToArray();
        var grid=new Terrain[6,5];int bad=0,dup=0,holes=0,seams=0;float worst=0;
        foreach(var a in ts){
            var p=a.transform.position;var d=a.terrainData;int col=Mathf.RoundToInt((p.x+d.size.x*.5f)/512f)+2;int row=Mathf.RoundToInt((p.z+d.size.z*.5f)/512f)+1;
            if(col< -1||col>4||row< -1||row>3||Mathf.Abs(d.size.x-512)>.01f||Mathf.Abs(d.size.z-512)>.01f){bad++;continue;}
            if(grid[col+1,row+1])dup++;else grid[col+1,row+1]=a;
        }
        for(int x=0;x<6;x++)for(int z=0;z<5;z++){
            var a=grid[x,z];if(!a){holes++;continue;}
            if(x<5&&grid[x+1,z]){var b=grid[x+1,z];var ah=a.terrainData.GetHeights(512,0,1,513);var bh=b.terrainData.GetHeights(0,0,1,513);for(int i=0;i<513;i++)worst=Mathf.Max(worst,Mathf.Abs((a.transform.position.y+ah[i,0]*a.terrainData.size.y)-(b.transform.position.y+bh[i,0]*b.terrainData.size.y)));seams++;}
            if(z<4&&grid[x,z+1]){var b=grid[x,z+1];var ah=a.terrainData.GetHeights(0,512,513,1);var bh=b.terrainData.GetHeights(0,0,513,1);for(int i=0;i<513;i++)worst=Mathf.Max(worst,Mathf.Abs((a.transform.position.y+ah[0,i]*a.terrainData.size.y)-(b.transform.position.y+bh[0,i]*b.terrainData.size.y)));seams++;}
        }
        string report=$"rooibosMeshes={rooibos.Length} wetRooibosMeshes={wetRooibos} graniteRenderers={granite.Length} graniteBad={graniteBad} originalRegionRocks={rocks} wetRocks={wetRocks} badRockMaterials={badRockMat} grid={ts.Length}/30 holes={holes} duplicate={dup} badTerrain={bad} seams={seams}/49 maxGapM={worst:F5}";
        File.WriteAllText("Validation/EdgeGrid20260923/object_fixes_verification_20260930.txt",report+"\n");
        Debug.Log(report);
        if(wetRooibos!=0||graniteBad!=0||rocks<55||wetRocks!=0||badRockMat!=0||ts.Length!=30||holes!=0||dup!=0||bad!=0||seams!=49||worst>.1f)throw new Exception("VERIFY FAIL "+report);
    }
}

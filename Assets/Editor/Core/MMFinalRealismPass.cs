using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMFinalRealismPass
{
    sealed class Z { public string key,display,scene; public Z(string k,string d,string s){key=k;display=d;scene=s;} }
    static readonly Z[] Zones={
        new Z("NewSorpigal","New Sorpigal","Assets/Scenes/Regions/NewSorpigal.unity"),
        new Z("CastleIronfist","Castle Ironfist","Assets/Scenes/Regions/CastleIronfist.unity"),
        new Z("MireOfTheDamned","Mire of the Damned","Assets/Scenes/Regions/MireOfTheDamned.unity"),
        new Z("Dragonsand","Dragonsand","Assets/Scenes/Regions/Dragonsand.unity"),
        new Z("HermitsIsle","Hermit's Isle","Assets/Scenes/Regions/HermitsIsle.unity"),
        new Z("MistyIslands","Misty Islands","Assets/Scenes/Regions/MistyIslands.unity"),
        new Z("BootlegBay","Bootleg Bay","Assets/Scenes/Regions/BootlegBay.unity"),
        new Z("FreeHaven","Free Haven","Assets/Scenes/Regions/FreeHaven.unity"),
        new Z("Blackshire","Blackshire","Assets/Scenes/Regions/Blackshire.unity"),
        new Z("ParadiseValley","Paradise Valley","Assets/Scenes/Regions/ParadiseValley.unity"),
        new Z("EelInfestedWaters","Eel Infested Waters","Assets/Scenes/Regions/EelInfestedWaters.unity"),        new Z("SilverCove","Silver Cove","Assets/Scenes/Regions/SilverCove.unity"),
        new Z("FrozenHighlands","White Cap / Frozen Highlands","Assets/Scenes/Regions/FrozenHighlands.unity"),
        new Z("Kriegspire","Kriegspire","Assets/Scenes/Regions/Kriegspire.unity"),
        new Z("SweetWater","Sweet Water","Assets/Scenes/Regions/SweetWater.unity")};
    const int N=128; const float Cell=4f;

    [MenuItem("MMUnity/Final Realism Pass")]
    public static void ApplyAll()
    {
        AssetDatabase.Refresh();
        foreach(var z in Zones) ApplyZone(z);
        AssetDatabase.SaveAssets();
        Debug.Log("FINAL_REALISM_ALL_DONE zones="+Zones.Length);
    }

    static void ApplyZone(Z z)
    {
        var sc=EditorSceneManager.OpenScene(z.scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.name.IndexOf("Open World",StringComparison.OrdinalIgnoreCase)>=0) ?? sc.GetRootGameObjects().First();
        var terrain=root.GetComponentInChildren<Terrain>(true); if(!terrain) throw new Exception(z.display+" terrain missing");
        string d=$"Assets/World/{z.key}/Data";
        byte[] h=File.ReadAllBytes(d+"/heightmap_u8.bin"), tile=File.ReadAllBytes(d+"/tilemap_u8.bin"), grp=File.ReadAllBytes(d+"/tile_groups_u8.bin"), sem=File.ReadAllBytes(d+"/tile_semantics_u8.bin");
        SmoothCoast(terrain,h,tile,sem);
        RefineOrganicRelief(z,root.transform,terrain,h,tile,grp,sem);
        SmoothSourceRoadGrades(terrain,h,tile,grp,sem);
        RepaintSourceCoverage(z,terrain,tile,grp,sem);
        // Repaint strictly from the authoritative source tile groups; X/Z coverage is not invented.
        int grounded=AlignArchitectureToFinalTerrain(root.transform,terrain);
        int hiddenCores=HideSolidCoreRenderers(root.transform);
        int veg=RebuildVegetation(z,root.transform,terrain,tile,grp,sem);
        GroundPreserved(z,root.transform,terrain);
        int badVeg=CullUntexturedVegetation(root.transform);
        int forms=BuildTerrainForms(z,root.transform,terrain,h,tile,grp,sem);
        // Preserved user additions are handled as whole placed objects by the grounding/water cleanup pass.
        EditorSceneManager.MarkSceneDirty(sc); EditorSceneManager.SaveScene(sc,z.scene);
        Debug.Log($"FINAL_REALISM {z.display} groundedArchitecture={grounded} hiddenCores={hiddenCores} vegetation={veg} removedUntexturedVeg={badVeg} terrainForms={forms}");
    }
    static float H(byte[] a,float sx,float sy)
    {
        sx=Mathf.Clamp(sx,0,N-1); sy=Mathf.Clamp(sy,0,N-1);
        int x0=Mathf.FloorToInt(sx),y0=Mathf.FloorToInt(sy),x1=Mathf.Min(N-1,x0+1),y1=Mathf.Min(N-1,y0+1);
        float tx=sx-x0,ty=sy-y0;
        return Mathf.Lerp(Mathf.Lerp(a[y0*N+x0],a[y0*N+x1],tx),Mathf.Lerp(a[y1*N+x0],a[y1*N+x1],tx),ty);
    }
    static float W(byte[] tile,byte[] sem,float sx,float sy)
    {
        float wx=(Mathf.PerlinNoise(sx*.071f+11.3f,sy*.071f+37.9f)-.5f)*2.2f;
        float wy=(Mathf.PerlinNoise(sx*.071f+73.1f,sy*.071f+5.7f)-.5f)*2.2f;
        sx=Mathf.Clamp(sx+wx,0,N-1); sy=Mathf.Clamp(sy+wy,0,N-1);
        int cx=Mathf.RoundToInt(sx),cy=Mathf.RoundToInt(sy); float sum=0f,wt=0f;
        for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++){
            int x=Mathf.Clamp(cx+dx,0,N-1),y=Mathf.Clamp(cy+dy,0,N-1);
            float d2=dx*dx+dy*dy,w=Mathf.Exp(-d2/4.5f); wt+=w;
            if((sem[tile[y*N+x]]&1)!=0)sum+=w;
        }
        return sum/Mathf.Max(.0001f,wt);
    }
    static Vector2 WS(float x,float z)=>new Vector2(Mathf.Clamp(x/Cell+64f,0,N-1),Mathf.Clamp(64f-z/Cell,0,N-1));
    static byte G(byte[] tile,byte[] grp,float x,float z)
    {
        Vector2 q=WS(x,z); int sx=Mathf.RoundToInt(q.x),sy=Mathf.RoundToInt(q.y); return grp[tile[sy*N+sx]];
    }
    static byte F(byte[] tile,byte[] sem,float x,float z)
    {
        Vector2 q=WS(x,z); int sx=Mathf.RoundToInt(q.x),sy=Mathf.RoundToInt(q.y); return sem[tile[sy*N+sx]];
    }
    static void SmoothCoast(Terrain t,byte[] src,byte[] tile,byte[] sem)
    {
        var td=t.terrainData; int r=td.heightmapResolution; var a=td.GetHeights(0,0,r,r); Vector3 p=t.transform.position,s=td.size;
        for(int y=0;y<r;y++) for(int x=0;x<r;x++)
        {
            float wx=p.x+x/(float)(r-1)*s.x, wz=p.z+y/(float)(r-1)*s.z; Vector2 q=WS(wx,wz);
            float wf=W(tile,sem,q.x,q.y); byte raw=tile[Mathf.Clamp(Mathf.RoundToInt(q.y),0,N-1)*N+Mathf.Clamp(Mathf.RoundToInt(q.x),0,N-1)]; bool sourceWater=(sem[raw]&1)!=0;
            float source=H(src,q.x,q.y)*.25f;
            if(sourceWater){
                float depth=Mathf.Lerp(.35f,5.5f,Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.52f,.98f,wf)));
                float n=(Mathf.PerlinNoise(wx*.018f+19.7f,wz*.018f+61.2f)-.5f)*.55f;
                float target=-depth+n*Mathf.InverseLerp(.55f,1f,wf); a[y,x]=Mathf.Clamp01((target-p.y)/s.y);
            } else if(wf>.015f){
                float land=Mathf.Max(.28f,source); float k=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.08f,.52f,wf));
                float shore=Mathf.Lerp(land,.18f,k); a[y,x]=Mathf.Clamp01((shore-p.y)/s.y);
            }
        }
        td.SetHeights(0,0,a); t.Flush(); EditorUtility.SetDirty(td);
    }
    static bool NearWaterCell(byte[] tile,byte[] sem,float wx,float wz)
    {
        Vector2 q=WS(wx,wz); int cx=Mathf.RoundToInt(q.x),cy=Mathf.RoundToInt(q.y);
        for(int dy=-2;dy<=2;dy++) for(int dx=-2;dx<=2;dx++)
        {
            int sx=Mathf.Clamp(cx+dx,0,N-1),sy=Mathf.Clamp(cy+dy,0,N-1);
            if((sem[tile[sy*N+sx]]&1)!=0)return true;
        }
        return false;
    }
    static void RefineOrganicRelief(Z z,Transform root,Terrain t,byte[] src,byte[] tile,byte[] grp,byte[] sem)
    {
        var td=t.terrainData; int r=td.heightmapResolution; var a=td.GetHeights(0,0,r,r);
        Vector3 p=t.transform.position,s=td.size; float dx=s.x/(r-1),dz=s.z/(r-1);
        for(int y=1;y<r-1;y++) for(int x=1;x<r-1;x++)
        {
            float wx=p.x+x*dx,wz=p.z+y*dz; byte f=F(tile,sem,wx,wz);
            if((f&1)!=0||Road(tile,grp,sem,wx,wz)||NearWaterCell(tile,sem,wx,wz))continue;
            if(wx<p.x+12f||wx>p.x+s.x-12f||wz<p.z+12f||wz>p.z+s.z-12f)continue;
            Vector2 q=WS(wx,wz); float sourceY=H(src,q.x,q.y)*.25f;
            float n1=(Mathf.PerlinNoise(wx*.021f+17.13f,wz*.021f+3.71f)-.5f)*2f;
            float n2=(Mathf.PerlinNoise(wx*.057f+51.7f,wz*.057f+29.4f)-.5f)*2f;
            float n3=(Mathf.PerlinNoise(wx*.173f+91.31f,wz*.173f+47.09f)-.5f)*2f;
            float amp=Mathf.Lerp(.16f,.52f,Mathf.InverseLerp(2f,22f,sourceY));
            float micro=sourceY>6f?n3*.11f:0f;
            float target=Mathf.Max(.24f,sourceY+(n1*.72f+n2*.28f)*amp+micro);
            a[y,x]=Mathf.Clamp01((target-p.y)/s.y);
        }
        td.SetHeights(0,0,a); t.Flush(); EditorUtility.SetDirty(td);
    }

    static void SetCellWeights(float[] w,byte g,byte f)
    {
        if((f&8)!=0||(g>=8&&g<255)){w[7]=1;return;} if((f&1)!=0){w[8]=.62f;w[2]=.38f;return;}
        if((f&2)!=0||g==5){w[2]=1;return;} if(g==0){w[0]=1;return;} if(g==1){w[1]=1;return;}
        if(g==2){w[2]=1;return;} if(g==3){w[3]=1;return;} if(g==4){w[4]=1;return;} if(g==6){w[5]=1;return;} if(g==7){w[6]=1;return;} w[4]=1;
    }
    static void AddCell(float[] dst,byte[] tile,byte[] grp,byte[] sem,int sx,int sy,float wt)
    {
        sx=Mathf.Clamp(sx,0,N-1); sy=Mathf.Clamp(sy,0,N-1); byte raw=tile[sy*N+sx]; float[] q=new float[dst.Length]; SetCellWeights(q,grp[raw],sem[raw]);
        for(int i=0;i<dst.Length;i++) dst[i]+=q[i]*wt;
    }
    static void RepaintTerrain(Terrain t,byte[] tile,byte[] grp,byte[] sem)
    {
        var td=t.terrainData; int r=td.alphamapResolution,l=td.terrainLayers.Length; if(l<9){Debug.Log("FINAL_REALISM_REPAINT_SKIP layers="+l+" terrain="+t.name);return;} var a=new float[r,r,l];
        for(int y=0;y<r;y++) for(int x=0;x<r;x++)
        {
            float sx=(x+.5f)/r*N-.5f, sy=N-1-((y+.5f)/r*N-.5f); int x0=Mathf.FloorToInt(sx),y0=Mathf.FloorToInt(sy); float tx=sx-x0,ty=sy-y0;
            float[] w=new float[l]; AddCell(w,tile,grp,sem,x0,y0,(1-tx)*(1-ty)); AddCell(w,tile,grp,sem,x0+1,y0,tx*(1-ty));
            AddCell(w,tile,grp,sem,x0,y0+1,(1-tx)*ty); AddCell(w,tile,grp,sem,x0+1,y0+1,tx*ty);
            int cx=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1),cy=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1); byte raw=tile[cy*N+cx],f=sem[raw],g=grp[raw];
            if((f&8)!=0||(g>=8&&g<255)){for(int k=0;k<l;k++)w[k]*=.15f; if(l>7)w[7]+=.85f;}
            float steep=td.GetSteepness(x/(float)Mathf.Max(1,r-1),y/(float)Mathf.Max(1,r-1));
            if((f&9)==0&&l>8){float rock=Mathf.Clamp01(Mathf.InverseLerp(35f,60f,steep))*.22f; for(int k=0;k<l;k++)w[k]*=(1-rock);w[8]+=rock;}
            float sum=w.Sum(); for(int k=0;k<l;k++)a[y,x,k]=w[k]/Mathf.Max(.0001f,sum);
        }
        td.SetAlphamaps(0,0,a); EditorUtility.SetDirty(td);
    }
    static void SmoothSourceRoadGrades(Terrain t,byte[] src,byte[] tile,byte[] grp,byte[] sem)
    {
        TerrainData td=t.terrainData;
        int r=td.heightmapResolution;
        float[,] a=td.GetHeights(0,0,r,r);
        float[,] b=(float[,])a.Clone();
        Vector3 p=t.transform.position,s=td.size;
        for(int y=1;y<r-1;y++)for(int x=1;x<r-1;x++){
            float wx=p.x+x/(float)(r-1)*s.x,wz=p.z+y/(float)(r-1)*s.z;if(!Road(tile,grp,sem,wx,wz))continue;
            Vector2 q=WS(wx,wz);float sourceY=H(src,q.x,q.y)*.25f,sourceN=Mathf.Clamp01((sourceY-p.y)/s.y);
            float sum=sourceN*6f,wt=6f;
            for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)if((ox!=0||oy!=0)&&Road(tile,grp,sem,wx+ox*s.x/(r-1),wz+oy*s.z/(r-1))){sum+=a[y+oy,x+ox];wt++;}
            b[y,x]=Mathf.Lerp(a[y,x],sum/wt,.90f);
        }
        td.SetHeights(0,0,b);t.Flush();EditorUtility.SetDirty(td);
    }
    static int SourceLayer(Z z,byte raw,byte[] grp,byte[] sem,int layers)
    {
        byte g=grp[raw],f=sem[raw]; if((f&8)!=0||(g>=8&&g<255))return Mathf.Min(2,layers-1);
        if((f&1)!=0){bool rocky=z.key=="HermitsIsle"||z.key=="SweetWater"||z.key=="FrozenHighlands"||z.key=="Kriegspire";return Mathf.Min(rocky?3:1,layers-1);}
        if(g==7)return layers>5?5:0; if(g==6)return layers>4?4:0;
        if(g==4||g==5)return Mathf.Min(3,layers-1); if(g==2||g==3)return z.key=="Dragonsand"?0:Mathf.Min(1,layers-1);
        return 0;
    }
    static void AddSourceLayer(float[] w,Z z,byte[] tile,byte[] grp,byte[] sem,int sx,int sy,float weight)
    {
        sx=Mathf.Clamp(sx,0,N-1);sy=Mathf.Clamp(sy,0,N-1);int k=SourceLayer(z,tile[sy*N+sx],grp,sem,w.Length);w[k]+=weight;
    }
    static void RepaintSourceCoverage(Z z,Terrain t,byte[] tile,byte[] grp,byte[] sem)
    {
        var td=t.terrainData;int r=td.alphamapResolution,l=td.alphamapLayers;if(l<4)return;var a=new float[r,r,l];
        for(int y=0;y<r;y++)for(int x=0;x<r;x++){
            float sx=(x+.5f)/r*N-.5f,sy=N-1-((y+.5f)/r*N-.5f);
            // Preserve source coverage exactly. Bilinear sampling softens the 4 m cell boundaries without moving them.
            int x0=Mathf.FloorToInt(sx),y0=Mathf.FloorToInt(sy);float tx=sx-x0,ty=sy-y0;var w=new float[l];
            AddSourceLayer(w,z,tile,grp,sem,x0,y0,(1-tx)*(1-ty));AddSourceLayer(w,z,tile,grp,sem,x0+1,y0,tx*(1-ty));
            AddSourceLayer(w,z,tile,grp,sem,x0,y0+1,(1-tx)*ty);AddSourceLayer(w,z,tile,grp,sem,x0+1,y0+1,tx*ty);
            int cx=Mathf.Clamp(Mathf.RoundToInt(sx),0,N-1),cy=Mathf.Clamp(Mathf.RoundToInt(sy),0,N-1);byte raw=tile[cy*N+cx],g=grp[raw],f=sem[raw];
            if((f&8)!=0||(g>=8&&g<255)){for(int k=0;k<l;k++)w[k]*=.08f;w[Mathf.Min(2,l-1)]+=.92f;}
            else {float steep=td.GetSteepness(x/(float)Mathf.Max(1,r-1),y/(float)Mathf.Max(1,r-1));float rk=Mathf.Clamp01(Mathf.InverseLerp(42f,68f,steep))*.28f;if(rk>0f){for(int k=0;k<l;k++)w[k]*=(1-rk);w[Mathf.Min(3,l-1)]+=rk;}}
            float sum=w.Sum();for(int k=0;k<l;k++)a[y,x,k]=w[k]/Mathf.Max(.0001f,sum);
        }
        td.SetAlphamaps(0,0,a);EditorUtility.SetDirty(td);
    }
    static bool VegName(string n)
    {
        n=n.ToLowerInvariant(); return n.StartsWith("6tree")||n.StartsWith("tree")||n.StartsWith("swptree")||n.StartsWith("snotre");
    }
    static bool P(string s,out float v)=>float.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out v);
    static int Hash(string a,int i){unchecked{return (a.GetHashCode()*397)^i;}}
    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray(); if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds; for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds); return b;
    }
    static void ScaleHeight(GameObject go,float h)
    {
        Bounds b=BoundsOf(go); if(b.size.y>.01f)go.transform.localScale*=h/b.size.y;
    }
    static void Ground(GameObject go,Terrain t,float sink=0f)
    {
        Bounds b=BoundsOf(go); float y=t.SampleHeight(new Vector3(go.transform.position.x,0,go.transform.position.z))+t.transform.position.y;
        if(b.size!=Vector3.zero)go.transform.position+=Vector3.up*(y-b.min.y-sink); else go.transform.position=new Vector3(go.transform.position.x,y-sink,go.transform.position.z);
    }
    static int HideSolidCoreRenderers(Transform root)
    {
        var arch=root.Find("Architecture - MM6 Original Layout Expanded"); if(!arch)return 0; int n=0;
        foreach(Transform c in arch.Cast<Transform>()) if(c.name.StartsWith("SolidCore_",StringComparison.OrdinalIgnoreCase))
        {
            var r=c.GetComponent<MeshRenderer>(); if(r&&r.enabled){r.enabled=false;n++;}
        }
        return n;
    }
    static bool SkipArchitectureGrounding(string n)
    {
        n=n.ToLowerInvariant();
        return n.Contains("bridge")||n.Contains("pier")||n.Contains("dock")||n.Contains("ship")||n.Contains("shp")||n.Contains("fountain")||n.Contains("well")||n.Contains("buoy")||n.Contains("bouy")||n.Contains("antifly");
    }
    static int AlignArchitectureToFinalTerrain(Transform root,Terrain t)
    {
        var arch=root.Find("Architecture - MM6 Original Layout Expanded"); if(!arch)return 0; int moved=0;
        foreach(Transform c in arch.Cast<Transform>().ToList())
        {
            if(!c || c.name.StartsWith("SolidCore_",StringComparison.OrdinalIgnoreCase) || SkipArchitectureGrounding(c.name))continue;
            Bounds b=BoundsOf(c.gameObject); if(b.size==Vector3.zero||b.size.y<.2f)continue;
            float y=t.SampleHeight(new Vector3(b.center.x,0f,b.center.z))+t.transform.position.y;
            float dy=y-b.min.y; if(Mathf.Abs(dy)>.03f){c.position+=Vector3.up*dy;moved++;}
        }
        return moved;
    }
    static bool NearArchitecture(Transform root,float x,float z,float margin)
    {
        var a=root.Find("Architecture - MM6 Original Layout Expanded"); if(!a)return false;
        foreach(var r in a.GetComponentsInChildren<Renderer>(true)){var b=r.bounds;if(x>=b.min.x-margin&&x<=b.max.x+margin&&z>=b.min.z-margin&&z<=b.max.z+margin)return true;}
        return false;
    }
    static GameObject Load(string p)=>AssetDatabase.LoadAssetAtPath<GameObject>(p);
    static GameObject Spawn(GameObject prefab,Transform parent,Vector3 pos,float yaw,float height,Terrain t)
    {
        if(!prefab)return null;
        var go=new GameObject("VegetationWrapper");go.transform.SetParent(parent,true);go.transform.position=pos;go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one;
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab);visual.transform.SetParent(go.transform,false);visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one;
        string pp=AssetDatabase.GetAssetPath(prefab).ToLowerInvariant();
        if(pp.Contains("fir_sapling"))ApplyCC0FirMaterials(visual);
        else if(pp.Contains("searsia_lucida"))ApplyCC0SearsiaMaterials(visual);
        else if(pp.Contains("dead_quiver_trunk"))ApplyCC0DeadTrunkMaterial(visual);
        else if(pp.Contains("pine_sapling_small"))ApplyCC0PineMaterials(visual);
        else if(pp.Contains("/shrub_02/"))ApplyCC0ShrubMaterial(visual);
        NormalizeVisibleUpright(go,t);
        if(height>0)ScaleHeight(go,height);Bounds vb=BoundsOf(go);if(vb.size!=Vector3.zero)go.transform.position+=new Vector3(pos.x-vb.center.x,0f,pos.z-vb.center.z);Ground(go,t);return go;
    }
    static void NormalizeVisibleUpright(GameObject go,Terrain t)
    {
        Bounds before=BoundsOf(go);if(before.size==Vector3.zero)return;
        Transform visual=go.transform.childCount==1?go.transform.GetChild(0):null;
        if(!visual){Ground(go,t);return;}
        Quaternion baseRot=visual.localRotation,best=baseRot;float bestScore=before.size.y;
        foreach(var q in new[]{baseRot*Quaternion.Euler(90f,0,0),baseRot*Quaternion.Euler(-90f,0,0),baseRot*Quaternion.Euler(0,0,90f),baseRot*Quaternion.Euler(0,0,-90f)})
        {visual.localRotation=q;Bounds b=BoundsOf(go);if(b.size.y>bestScore){bestScore=b.size.y;best=q;}}
        visual.localRotation=best;Ground(go,t);
    }
    static void OneCactus(GameObject go,int seed)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).OrderBy(r=>r.gameObject.name).ToArray(); if(rs.Length==0)return; int keep=Mathf.Abs(seed)%rs.Length;
        for(int i=0;i<rs.Length;i++){rs[i].gameObject.SetActive(i==keep); if(i==keep){int n=Mathf.Clamp((keep%9)+1,1,9);var m=AssetDatabase.LoadAssetAtPath<Material>($"Assets/Environment/DesertVegetation/Materials/Cactus_{n}.mat");if(m)rs[i].sharedMaterial=m;}}
    }
    static string ShrubStem(string n)
    {
        n=n.ToLowerInvariant(); string[] s={"agave","yacca_leaf_01","yacca_leaf_02","ocotillo_branch_01","ocotillo_branch_02","creosote_branch_02","cholla_01","cholla_02","bark_02","bark"};
        return s.FirstOrDefault(x=>n.Contains(x));
    }
    static void OneShrub(GameObject go,int seed)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>ShrubStem(r.gameObject.name)!=null||(r.sharedMaterial&&ShrubStem(r.sharedMaterial.name)!=null)).OrderBy(r=>r.gameObject.name).ToArray();
        if(rs.Length==0)return; int keep=Mathf.Abs(seed)%rs.Length;
        for(int i=0;i<rs.Length;i++){rs[i].enabled=i==keep;if(i==keep){string st=ShrubStem(rs[i].gameObject.name)??ShrubStem(rs[i].sharedMaterial.name);var m=AssetDatabase.LoadAssetAtPath<Material>($"Assets/Environment/DesertVegetation/Materials/Desert_{st}.mat");if(m)rs[i].sharedMaterial=m;}}
    }
    static void WinterMat(GameObject go,int id)
    {
        var m=AssetDatabase.LoadAssetAtPath<Material>($"Assets/Environment/WinterVegetation/Materials/WinterTree{id}.mat"); if(!m)return;
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)){var a=r.sharedMaterials;if(a.Length==0)a=new Material[1];for(int i=0;i<a.Length;i++)a[i]=m;r.sharedMaterials=a;}
    }
    static bool Road(byte[] tile,byte[] grp,byte[] sem,float x,float z)
    {
        byte f=F(tile,sem,x,z),g=G(tile,grp,x,z);return (f&8)!=0||(g>=8&&g<255);
    }
    static bool SafeVegetationPosition(Transform root,byte[] tile,byte[] grp,byte[] sem,ref float x,ref float z,float archMargin)
    {
        // Source X/Z is immutable. Never relocate a source anchor to make it fit.
        return (F(tile,sem,x,z)&1)==0 && !Road(tile,grp,sem,x,z) && !NearArchitecture(root,x,z,archMargin);
    }
    static GameObject PickTree(Z z,string name,byte group,int seed,GameObject[] normal,GameObject winter5,GameObject winter8,GameObject swampA,GameObject swampB,GameObject dead,GameObject cactus,GameObject shrub)
    {
        if(z.key=="Dragonsand"||z.key=="ParadiseValley") return ((seed&1)==0?dead:shrub) ?? dead ?? shrub;
        if(z.key=="SweetWater"||z.key=="HermitsIsle") return dead ?? shrub;
        if(z.key=="Kriegspire") return ((seed&3)==0?shrub:dead) ?? dead ?? shrub;
        if(z.key=="FrozenHighlands") return ((seed&1)==0?winter5:winter8) ?? winter5 ?? winter8;
        if(z.key=="Blackshire") return (seed%3==0?swampA:((seed&1)==0?dead:shrub)) ?? swampA ?? dead ?? shrub;
        if(name.StartsWith("snotre")||group==1) return ((seed&1)==0?winter5:winter8) ?? winter5 ?? winter8;
        if(name.StartsWith("swptree")||group==7||z.key=="MireOfTheDamned") return ((seed&1)==0?swampA:swampB) ?? swampA ?? swampB;
        if((group==3||group==6)&&(seed&3)==0&&dead) return dead;
        return normal[Mathf.Abs(seed)%normal.Length];
    }
    static int RebuildVegetation(Z z,Transform root,Terrain t,byte[] tile,byte[] grp,byte[] sem)
    {
        foreach(string n in new[]{"Vegetation - MM Anchors + Natural Groves","Biome Vegetation - Source Grid","Vegetation - Source Anchored Final"}){var o=root.Find(n);if(o)UnityEngine.Object.DestroyImmediate(o.gameObject);}
        var outRoot=new GameObject("Vegetation - Source Anchored Final"); outRoot.transform.SetParent(root,false);
        var normal=new[]{Load("Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_1k.fbx"),Load("Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_1k.fbx")}.Where(x=>x).ToArray();
        var w5=Load("Assets/Environment/WinterVegetation/WinterTree5/winter-tree5_LOW_RES.fbx"); var w8=Load("Assets/Environment/WinterVegetation/WinterTree8/winter-tree8_LOW_RES.fbx");
        var swampA=Load("Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_1k.fbx"); var swampB=Load("Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_1k.fbx");
        var dead=Load("Assets/Environment/PolyHaven/Models/dead_quiver_trunk/dead_quiver_trunk_1k.fbx"); var cactus=(GameObject)null; var shrub=Load("Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx");
        string path=$"Assets/World/{z.key}/Data/decorations.csv"; int made=0;
        foreach(var line in File.ReadAllLines(path).Skip(1))
        {
            var q=line.Split(','); if(q.Length<10)continue; string name=q[1].Trim().ToLowerInvariant(); if(!VegName(name))continue;
            if(!P(q[4],out float x)||!P(q[6],out float oz))continue; float zp=-oz;
            int sourceIndex=made; if(q.Length>0)int.TryParse(q[0],out sourceIndex); int seed=Hash(name,sourceIndex+1);
            if(!SafeVegetationPosition(root,tile,grp,sem,ref x,ref zp,1.8f))continue; byte g=G(tile,grp,x,zp); var prefab=PickTree(z,name,g,seed,normal,w5,w8,swampA,swampB,dead,cactus,shrub); if(!prefab)continue;
            float h=(prefab==cactus||prefab==shrub)?Mathf.Lerp(1.4f,3.5f,(Mathf.Abs(seed)%1000)/999f):Mathf.Lerp(4.8f,9.8f,(Mathf.Abs(seed)%1000)/999f);
            float naturalYaw=((seed&0x7fffffff)%10000)/10000f*360f; var go=Spawn(prefab,outRoot.transform,new Vector3(x,0,zp),naturalYaw,h,t); if(!go)continue;
            float width=.90f+((seed>>13)&1023)/1023f*.22f;go.transform.localScale=new Vector3(go.transform.localScale.x*width,go.transform.localScale.y,go.transform.localScale.z*(2f-width));
            go.name=$"SourceVeg_{sourceIndex:0000}_{name}"; if(prefab==cactus)OneCactus(go,seed);if(prefab==w5)WinterMat(go,5);if(prefab==w8)WinterMat(go,8);NormalizeVisibleUpright(go,t);Ground(go,t);made++;
        }
        int mod=z.key=="Dragonsand"?31:z.key=="MireOfTheDamned"?21:z.key=="FrozenHighlands"?43:0;
        int target=z.key=="Dragonsand"?2:z.key=="MireOfTheDamned"?7:1;
        if(mod>0) for(int sy=2;sy<N-2;sy++)for(int sx=2;sx<N-2;sx++)
        {
            byte raw=tile[sy*N+sx]; if(grp[raw]!=target)continue; int seed=(sx*73856093)^(sy*19349663)^z.key.GetHashCode(); if((seed&0x7fffffff)%mod!=0)continue;
            float x=(sx-64f)*4f+(((seed>>4)&255)/255f-.5f)*2.4f,zp=(64f-sy)*4f+(((seed>>12)&255)/255f-.5f)*2.4f; if(!SafeVegetationPosition(root,tile,grp,sem,ref x,ref zp,3f))continue;
            GameObject prefab=z.key=="Dragonsand"?(((seed&1)==0?dead:shrub)??dead??shrub):z.key=="MireOfTheDamned"?(((seed&1)==0?swampA:swampB)??swampA??swampB):(((seed&1)==0?w5:w8)??w5??w8); if(!prefab)continue;
            float hgt=z.key=="Dragonsand"?Mathf.Lerp(1.2f,3.2f,(Mathf.Abs(seed)%1000)/999f):Mathf.Lerp(4.2f,8.2f,(Mathf.Abs(seed)%1000)/999f);
            var go=Spawn(prefab,outRoot.transform,new Vector3(x,0,zp),(seed&1023)*.3519f,hgt,t);if(!go)continue;go.name=$"BiomeFill_{sx}_{sy}";if(prefab==cactus)OneCactus(go,seed);if(prefab==w5)WinterMat(go,5);if(prefab==w8)WinterMat(go,8);NormalizeVisibleUpright(go,t);Ground(go,t);made++;
        }
        return made;
    }

    static bool VisibleTexturesValid(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return false;
        foreach(var r in rs)
        {
            var mats=r.sharedMaterials;if(mats==null||mats.Length==0)return false;
            foreach(var m in mats)if(!m||!m.mainTexture)return false;
        }
        return true;
    }
    static int CullDirectUntextured(Transform root)
    {
        if(!root)return 0;int removed=0;
        foreach(Transform c in root.Cast<Transform>().ToList())if(c&&!VisibleTexturesValid(c.gameObject)){UnityEngine.Object.DestroyImmediate(c.gameObject);removed++;}
        return removed;
    }
    static int CullGroupedUntextured(Transform root)
    {
        if(!root)return 0;int removed=0;
        foreach(Transform g in root.Cast<Transform>().ToList())foreach(Transform c in g.Cast<Transform>().ToList())if(c&&!VisibleTexturesValid(c.gameObject)){UnityEngine.Object.DestroyImmediate(c.gameObject);removed++;}
        return removed;
    }
    static int CullUntexturedVegetation(Transform root)
    {
        int removed=0;removed+=CullDirectUntextured(root.Find("Vegetation - Source Anchored Final"));
        removed+=CullGroupedUntextured(root.Find("Vegetation - Preserved User Additions"));return removed;
    }

    static Material FormMaterial(bool snow)
    {
        string dir="Assets/Materials/Terrain";Directory.CreateDirectory(dir);string path=dir+(snow?"/FinalSnowMountain.mat":"/FinalDesertMountain.mat");
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);var sh=Shader.Find("Standard");if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        string tex=snow?"Assets/Environment/TerrainForms/SnowMountain/a bit Lighter.tif":"Assets/Environment/PolyHaven/Textures/coast_sand_02/coast_sand_02_diff_1k.jpg";m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(tex));
        if(snow){var n=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Environment/TerrainForms/SnowMountain/Normal Map.tif");if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}}else m.color=new Color(.86f,.75f,.53f);
        m.SetFloat("_Glossiness",.08f);EditorUtility.SetDirty(m);return m;
    }    static void AssignMaterial(GameObject go,Material m)
    {
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)){var a=r.sharedMaterials;if(a.Length==0)a=new Material[1];for(int i=0;i<a.Length;i++)a[i]=m;r.sharedMaterials=a;}
    }
    static void ScaleFootprint(GameObject go,float target)
    {
        Bounds b=BoundsOf(go);float w=Mathf.Max(b.size.x,b.size.z);if(w>.01f)go.transform.localScale*=target/w;
    }
    static int BuildTerrainForms(Z z,Transform root,Terrain t,byte[] h,byte[] tile,byte[] grp,byte[] sem)
    {
        var old=root.Find("Terrain Forms - Final");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject); return 0;
    }
    static Material MakeCC0PlantMaterial(string assetName,string suffix,string diff,string normal,string alpha=null)
    {
        string dir="Assets/Materials/Terrain";Directory.CreateDirectory(dir);string path=$"{dir}/CC0_{assetName}_{suffix}.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);var sh=Shader.Find("Standard");if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        var d=AssetDatabase.LoadAssetAtPath<Texture2D>(diff);var n=AssetDatabase.LoadAssetAtPath<Texture2D>(normal);if(d)m.SetTexture("_MainTex",d);if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
        if(!string.IsNullOrEmpty(alpha)){var a=AssetDatabase.LoadAssetAtPath<Texture2D>(alpha);m.SetFloat("_Mode",1f);m.SetFloat("_Cutoff",.32f);m.SetOverrideTag("RenderType","TransparentCutout");m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;if(m.HasProperty("_Cull"))m.SetInt("_Cull",0);}
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.06f);EditorUtility.SetDirty(m);return m;
    }
    static void ApplyCC0FirMaterials(GameObject go)
    {
        string b="Assets/Environment/PolyHaven/Models/fir_sapling/textures/";
        var branch=MakeCC0PlantMaterial("Fir","Branches",b+"fir_sapling_branches_diff_1k.png",b+"fir_sapling_branches_nor_gl_1k.png");
        var twig=MakeCC0PlantMaterial("Fir","Twigs",b+"fir_sapling_twigs_rgba_1k.png",b+"fir_sapling_twigs_nor_gl_1k.png",b+"fir_sapling_twigs_alpha_1k.png");
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)){var a=r.sharedMaterials;if(a.Length<2)Array.Resize(ref a,2);for(int i=0;i<a.Length;i++){string n=a[i]?a[i].name.ToLowerInvariant():"";a[i]=n.Contains("twig")?twig:branch;}r.sharedMaterials=a;}
    }
    static void ApplyCC0SearsiaMaterials(GameObject go)
    {
        string b="Assets/Environment/PolyHaven/Models/searsia_lucida/textures/";
        var wood=MakeCC0PlantMaterial("Searsia","Wood",b+"searsia_lucida_diff_1k.jpg",b+"searsia_lucida_nor_gl_1k.exr");
        var leaf=MakeCC0PlantMaterial("Searsia","Leaves",b+"searsia_lucida_rgba_1k.png",b+"searsia_lucida_nor_gl_1k.exr",b+"searsia_lucida_alpha_1k.png");
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)){var a=r.sharedMaterials;if(a.Length<3)Array.Resize(ref a,3);for(int i=0;i<a.Length;i++){string n=a[i]?a[i].name.ToLowerInvariant():"";a[i]=(n.Contains("leaf")||n.Contains("twig"))?leaf:wood;}r.sharedMaterials=a;}
    }
    static void ApplyCC0DeadTrunkMaterial(GameObject go)
    {
        string b="Assets/Environment/PolyHaven/Models/dead_quiver_trunk/textures/";
        var m=MakeCC0PlantMaterial("DeadQuiver","Wood",b+"dead_quiver_trunk_diff_1k.jpg",b+"dead_quiver_trunk_nor_gl_1k.exr");
        foreach(var r in go.GetComponentsInChildren<Renderer>(true)){var a=r.sharedMaterials;if(a.Length==0)a=new Material[1];for(int i=0;i<a.Length;i++)a[i]=m;r.sharedMaterials=a;}
    }
    static Material CC0PineMaterial(bool twig)
    {
        string dir="Assets/Materials/Terrain";Directory.CreateDirectory(dir);string path=dir+(twig?"/CC0_PineTwig.mat":"/CC0_PineBark.mat");
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);var sh=Shader.Find("Standard");if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        string b="Assets/Environment/PolyHaven/Models/pine_sapling_small/";
        var d=AssetDatabase.LoadAssetAtPath<Texture2D>(b+(twig?"pine_sapling_small_twig_diff_1k.png":"pine_sapling_small_bark_diff_1k.png"));
        var n=AssetDatabase.LoadAssetAtPath<Texture2D>(b+(twig?"pine_sapling_small_twig_nor_gl_1k.png":"pine_sapling_small_bark_nor_gl_1k.png"));
        if(d)m.SetTexture("_MainTex",d);if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
        if(twig){var a=AssetDatabase.LoadAssetAtPath<Texture2D>(b+"pine_sapling_small_twig_alpha_1k.png");if(a&&m.HasProperty("_MainTex")){}m.SetFloat("_Mode",1f);m.SetFloat("_Cutoff",.32f);m.SetOverrideTag("RenderType","TransparentCutout");m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;if(m.HasProperty("_Cull"))m.SetInt("_Cull",0);}
        if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",twig?.05f:.12f);EditorUtility.SetDirty(m);return m;
    }
    static Material CC0ShrubMaterial()
    {
        string path="Assets/Materials/Terrain/CC0_Shrub02.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var sh=Shader.Find("Standard");if(!m){m=new Material(sh);AssetDatabase.CreateAsset(m,path);}else m.shader=sh;
        string b="Assets/Environment/PolyHaven/Models/shrub_02/";var d=AssetDatabase.LoadAssetAtPath<Texture2D>(b+"shrub_02_rgba_1k.png");var n=AssetDatabase.LoadAssetAtPath<Texture2D>(b+"shrub_02_nor_gl_1k.exr");if(d)m.SetTexture("_MainTex",d);if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}m.SetFloat("_Mode",1f);m.SetFloat("_Cutoff",.30f);m.SetOverrideTag("RenderType","TransparentCutout");m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;if(m.HasProperty("_Cull"))m.SetInt("_Cull",0);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.05f);EditorUtility.SetDirty(m);return m;
    }
    static void ApplyCC0PineMaterials(GameObject go)
    {
        var bark=CC0PineMaterial(false);var twig=CC0PineMaterial(true);foreach(var r in go.GetComponentsInChildren<Renderer>(true)){var a=r.sharedMaterials;if(a.Length==0)a=new Material[1];for(int i=0;i<a.Length;i++){string n=a[i]?a[i].name.ToLowerInvariant():r.gameObject.name.ToLowerInvariant();a[i]=(n.Contains("twig")||n.Contains("leaf")||n.Contains("needle")||n.Contains("branch"))?twig:bark;}r.sharedMaterials=a;}
    }
    static void ApplyCC0ShrubMaterial(GameObject go){var m=CC0ShrubMaterial();foreach(var r in go.GetComponentsInChildren<Renderer>(true)){var a=r.sharedMaterials;if(a.Length==0)a=new Material[1];for(int i=0;i<a.Length;i++)a[i]=m;r.sharedMaterials=a;}}
    static GameObject ReplacePreserved(GameObject old,GameObject prefab,bool tree,Terrain t)
    {
        if(!prefab)return old;Transform parent=old.transform.parent;Vector3 p=old.transform.position;float yaw=old.transform.eulerAngles.y;string name=old.name;int seed=Hash(name,Mathf.RoundToInt(p.x*10f)^Mathf.RoundToInt(p.z*10f));string pp=AssetDatabase.GetAssetPath(prefab).ToLowerInvariant();float h=pp.Contains("dead_quiver_trunk")?Mathf.Lerp(2.2f,4.2f,(Mathf.Abs(seed)%1000)/999f):tree?Mathf.Lerp(5.0f,8.8f,(Mathf.Abs(seed)%1000)/999f):Mathf.Lerp(.7f,1.7f,(Mathf.Abs(seed)%1000)/999f);UnityEngine.Object.DestroyImmediate(old);var go=Spawn(prefab,parent,p,yaw,h,t);if(go)go.name=name;return go;
    }
    static void GroundPreserved(Z z,Transform root,Terrain t)
    {
        var p=root.Find("Vegetation - Preserved User Additions");if(!p)return;
        var fir=Load("Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_1k.fbx");
        var broad=Load("Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_1k.fbx");
        var shrub=Load("Assets/Environment/PolyHaven/Models/shrub_02/shrub_02_1k.fbx");
        var dead=Load("Assets/Environment/PolyHaven/Models/dead_quiver_trunk/dead_quiver_trunk_1k.fbx");
        foreach(Transform group in p.Cast<Transform>().ToList())
        {
            bool volcanic=group.name.ToLowerInvariant().Contains("volcanic");
            foreach(Transform c in group.Cast<Transform>().ToList())
            {
                Bounds b=BoundsOf(c.gameObject);string ap=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(c.gameObject).ToLowerInvariant();string n=c.name.ToLowerInvariant();
                bool tree=b.size.y>2.2f||ap.Contains("tree")||ap.Contains("pine")||n.Contains("tree")||n.Contains("pine");
                int hs=Hash(c.name,Mathf.RoundToInt(c.position.x*10f)^Mathf.RoundToInt(c.position.z*10f));
                bool dryOnly=z.key=="SweetWater"||z.key=="HermitsIsle"||z.key=="Kriegspire"||z.key=="ParadiseValley";
                GameObject repl=(volcanic||dryOnly)?(tree?dead:shrub):z.key=="Blackshire"?(tree?(((hs&3)==0)?broad:dead):shrub):tree?(((hs&1)==0)?fir:broad):shrub;
                ReplacePreserved(c.gameObject,repl,tree,t);
            }
        }
    }

}

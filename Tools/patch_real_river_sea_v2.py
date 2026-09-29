from pathlib import Path
import re

P=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=P.read_text(encoding='utf-8')

def sub(pattern,repl,label):
    global s
    ns,n=re.subn(pattern,repl,s,count=1,flags=re.S)
    if n!=1:
        raise SystemExit(f'{label}: expected 1 match, got {n}')
    s=ns
    print('PATCH',label)

river_core=r'''    static readonly float[] RiverSy={58f,62f,67f,73.2f,79f,84.5f,90f,95f,99f,100f,101f,102f,103f};
    static readonly float[] RiverX={89f,90f,92f,95.5f,100f,103.5f,104.1f,104.2f,104f,103.8f,103.5f,103.2f,103f};
    static readonly float[] RiverY={.14f,.18f,.24f,.34f,.44f,.58f,.82f,1.10f,3.0f,10f,27f,48f,66f};

    static int RiverSegment(float sy)
    {
        for(int i=0;i<RiverSy.Length-1;i++)
            if(sy>=RiverSy[i]&&sy<=RiverSy[i+1])return i;
        return -1;
    }

    static float RiverCenterX(float sy)
    {
        int i=RiverSegment(sy);if(i<0)return float.NaN;
        float t=Mathf.InverseLerp(RiverSy[i],RiverSy[i+1],sy);
        float u=t*t*(3f-2f*t);
        float x=Mathf.Lerp(RiverX[i],RiverX[i+1],u);
        return x+(Mathf.PerlinNoise(sy*.19f+7.2f,4.1f)-.5f)*.22f;
    }
'''
river_core+=r'''    static float RiverSurfaceY(float sy)
    {
        int i=RiverSegment(sy);if(i<0)return .12f;
        float t=Mathf.InverseLerp(RiverSy[i],RiverSy[i+1],sy);
        return Mathf.Lerp(RiverY[i],RiverY[i+1],t);
    }

    static float RiverDownstream01(float sy)
    { return Mathf.Clamp01((103f-sy)/45f); }

    static float RiverHalfWidthWorld(float sy)
    {
        if(RiverSegment(sy)<0)return 0f;
        float d=RiverDownstream01(sy);
        float w=Mathf.Lerp(1.05f,3.35f,d);
        return w*Mathf.Lerp(.90f,1.10f,Mathf.PerlinNoise(sy*.27f+11.3f,2.2f));
    }

    static float RiverDepth(float sy)
    { return Mathf.Lerp(.42f,1.55f,RiverDownstream01(sy)); }

    static float RiverDistanceWorld(float sx,float sy)
    {
        float cx=RiverCenterX(sy);if(float.IsNaN(cx))return 9999f;
        return Mathf.Abs(sx-cx)*Cell;
    }

    static float RiverReplacementRadiusWorld(float sy)
    {
        if(RiverSegment(sy)<0)return 0f;
        float mouth=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(58f,64f,sy));
        return Mathf.Lerp(8f,48f,mouth);
    }
'''
river_core+=r'''    static float SampleSeaWaterMask(byte[] tiles,float sx,float sy)
    {
        float original=SampleSmoothWaterMask(tiles,sx,sy);
        float d=RiverDistanceWorld(sx,sy);
        float suppress=RiverReplacementRadiusWorld(sy);
        if(suppress>0f&&d<suppress)original=0f;
        return original;
    }

    static float SampleNaturalWaterMask(byte[] tiles,float[] unused,float sx,float sy)
    { return Mathf.Max(SampleSeaWaterMask(tiles,sx,sy),RiverDistanceWorld(sx,sy)<=RiverHalfWidthWorld(sy)?1f:0f); }

    static float[] GetRiverCenterline(byte[] tiles)
    {
        var c=Enumerable.Repeat(float.NaN,N).ToArray();
        for(int y=58;y<=103;y++)c[y]=RiverCenterX(y);
        return c;
    }

    static bool TryRiverCenter(float[] c,float sy,out float cx)
    { cx=RiverCenterX(sy);return !float.IsNaN(cx); }

    static float RiverDistanceSource(float[] c,float sx,float sy)
    { return RiverDistanceWorld(sx,sy)/Cell; }

    static float RiverHalfWidth(float sy)
    { return RiverHalfWidthWorld(sy)/Cell; }
'''

sub(r'    static float\[\] riverCenterCache;.*?    static float SampleDistanceBilinear',river_core+'\n    static float SampleDistanceBilinear','river core')

terrain=r'''    static Terrain BuildTerrain(Transform parent, EnvAssets e)
    {
        byte[] heights=File.ReadAllBytes(HeightPath);
        byte[] tiles=File.ReadAllBytes(TilePath);
        int[,] waterDist=ComputeWaterDistance(tiles);
        string dataPath="Assets/World/NewSorpigal/Generated/NewSorpigalTerrain.asset";
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
        if(!td){td=new TerrainData();AssetDatabase.CreateAsset(td,dataPath);}
        td.heightmapResolution=TerrainResolution;
        td.size=new Vector3(TerrainSize,TerrainHeight,TerrainSize);
        td.terrainLayers=new[]{e.grassLayer,e.dirtLayer,e.roadLayer,e.rockLayer};
'''
terrain+=r'''        int hmMax=TerrainResolution-1;
        float[,] hm=new float[TerrainResolution,TerrainResolution];
        for(int y=0;y<TerrainResolution;y++)for(int x=0;x<TerrainResolution;x++)
        {
            float sx=(N-1)-x/(float)hmMax*(N-1),sy=y/(float)hmMax*(N-1);
            float rawY=SampleByteBilinear(heights,sx,sy)*.25f;
            float worldY=CreativeLandHeight(rawY,sx,sy);
            float seaMask=SampleSeaWaterMask(tiles,sx,sy);
            if(seaMask>=.5f)
            {
                float d=SampleDistanceBilinear(waterDist,sx,sy);
                worldY=-Mathf.Clamp(.75f+d*.82f,.75f,13.5f);
            }
            else
            {
                float protectedBlend=HasNearbyForbiddenTile(tiles,sx,sy,1,true)?.78f:0f;
                worldY=Mathf.Lerp(worldY,BlueprintBaseHeight(rawY),protectedBlend);
                worldY=Mathf.Max(worldY,.38f);
            }

            float riverD=RiverDistanceWorld(sx,sy);
            float halfW=RiverHalfWidthWorld(sy);
            if(halfW>0f)
            {
                float bankExtra=Mathf.Lerp(2.6f,6.8f,RiverDownstream01(sy));
                float outer=halfW+bankExtra;
                if(riverD<outer)
                {
                    float surface=RiverSurfaceY(sy);
                    if(riverD<=halfW)
                    {
                        float edge=Mathf.Clamp01(riverD/Mathf.Max(.01f,halfW));
                        float bed=surface-Mathf.Lerp(RiverDepth(sy),.16f,Mathf.Pow(edge,.80f));
                        worldY=Mathf.Min(worldY,bed);
                    }
                    else
                    {
                        float t=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(halfW,outer,riverD));
                        worldY=Mathf.Min(worldY,Mathf.Lerp(surface+.18f,worldY,t));
                    }
                }
            }
            hm[y,x]=Mathf.Clamp01((worldY-TerrainBaseY)/TerrainHeight);
        }
        td.SetHeights(0,0,hm);
'''
terrain+=r'''        td.alphamapResolution=512;
        float[,,] alpha=new float[512,512,4];
        for(int y=0;y<512;y++)for(int x=0;x<512;x++)
        {
            float sx=(N-1)-x/511f*(N-1),sy=y/511f*(N-1);
            byte tile=TileAtSource(tiles,sx,sy);
            float h=CreativeLandHeight(SampleByteBilinear(heights,sx,sy)*.25f,sx,sy);
            float slope=td.GetSteepness(x/511f,y/511f);
            float macro=Mathf.PerlinNoise(sx*.071f+2.8f,sy*.067f+11.4f);
            float seaMask=SampleSeaWaterMask(tiles,sx,sy);
            float riverD=RiverDistanceWorld(sx,sy),halfW=RiverHalfWidthWorld(sy);
            float outer=halfW>0f?halfW+Mathf.Lerp(2.6f,6.8f,RiverDownstream01(sy)):0f;
            if(halfW>0f&&riverD<outer)
            {
                float edge=Mathf.InverseLerp(0f,outer,riverD);
                alpha[y,x,1]=Mathf.Lerp(.56f,.30f,edge);
                alpha[y,x,3]=Mathf.Lerp(.36f,.12f,edge);
                alpha[y,x,0]=1f-alpha[y,x,1]-alpha[y,x,3];
            }
            else if(seaMask>=.5f){alpha[y,x,1]=.54f;alpha[y,x,3]=.46f;}
            else if(IsRoad(tile)){alpha[y,x,2]=.91f;alpha[y,x,1]=.09f;}
            else if(IsDirt(tile)){alpha[y,x,1]=.70f;alpha[y,x,0]=.22f;alpha[y,x,3]=.08f;}
            else
            {
                float rock=Mathf.Clamp01(Mathf.InverseLerp(19f,43f,slope)*.92f+Mathf.InverseLerp(66f,118f,h)*.30f);
                float dirt=(.045f+macro*.14f)*(1f-rock);
                float grass=Mathf.Max(0f,1f-rock-dirt);
                alpha[y,x,0]=grass;alpha[y,x,1]=dirt;alpha[y,x,3]=rock;
            }
        }
        td.SetAlphamaps(0,0,alpha);
'''
terrain+=r'''        var go=Terrain.CreateTerrainGameObject(td);
        go.name="Terrain_MM_Blueprint_Realistic_x3";go.transform.SetParent(parent);
        go.transform.position=new Vector3(-TerrainSize/2f,TerrainBaseY,-TerrainSize/2f);
        var terrain=go.GetComponent<Terrain>();terrain.drawInstanced=true;terrain.heightmapPixelError=1.5f;terrain.basemapDistance=1600f;
        terrain.detailObjectDistance=185f;terrain.detailObjectDensity=1f;terrain.treeDistance=1650f;terrain.treeBillboardDistance=260f;
        terrain.treeCrossFadeLength=22f;terrain.treeMaximumFullLODCount=180;
        BuildGrassDetails(terrain,tiles,e.grassPrefab);EditorUtility.SetDirty(td);return terrain;
    }
'''
sub(r'    static Terrain BuildTerrain\(Transform parent, EnvAssets e\).*?\n    static bool HasNearbyForbiddenTile',terrain+'\n    static bool HasNearbyForbiddenTile','terrain restore + one river carve')

water=r'''    static Material RealWaterMaterial(string name,bool river)
    {
        var mat=CreateSimpleMaterial(name,Shader.Find("MMUnity/RealCoastalWater"),Color.white);
        mat.SetColor("_ShallowColor",river?new Color(.07f,.38f,.37f,.78f):new Color(.045f,.28f,.36f,.72f));
        mat.SetColor("_DeepColor",river?new Color(.015f,.10f,.12f,.94f):new Color(.003f,.025f,.075f,.97f));
        mat.SetColor("_FoamColor",new Color(.88f,.96f,.94f,.96f));
        mat.SetColor("_ReflectionColor",new Color(.28f,.48f,.62f,1f));
        mat.SetFloat("_DepthMax",river?3.5f:16f);mat.SetFloat("_FoamDepth",river?.38f:1.15f);
        mat.SetFloat("_WaveAmp",river?.035f:.18f);mat.SetFloat("_WaveScale",river?.16f:.052f);mat.SetFloat("_WaveSpeed",river?.95f:.62f);
        mat.SetFloat("_WaveAmp2",river?.018f:.075f);mat.SetFloat("_WaveScale2",river?.29f:.115f);mat.SetFloat("_WaveSpeed2",river?1.45f:1.05f);
        mat.SetFloat("_Distortion",river?.006f:.012f);mat.SetFloat("_FresnelPower",river?3.2f:4f);mat.SetFloat("_ReflectionStrength",river?.26f:.44f);
        mat.SetFloat("_WorldSize",TerrainSize);mat.renderQueue=3000;EditorUtility.SetDirty(mat);return mat;
    }
'''
water+=r'''    static Mesh BuildOceanMesh()
    {
        const int grid=160;int row=grid+1;float size=TerrainSize*2.6f,origin=-size*.5f,step=size/grid;
        var verts=new List<Vector3>(row*row);var tris=new List<int>(grid*grid*6);
        for(int y=0;y<=grid;y++)for(int x=0;x<=grid;x++)verts.Add(new Vector3(origin+x*step,.12f,origin+y*step));
        for(int y=0;y<grid;y++)for(int x=0;x<grid;x++)
        { int a=y*row+x,b=a+1,d=(y+1)*row+x,c=d+1;tris.Add(a);tris.Add(c);tris.Add(b);tris.Add(a);tris.Add(d);tris.Add(c); }
        string path="Assets/World/NewSorpigal/Generated/RealOceanSurface.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetTriangles(tris,0);
        mesh.SetNormals(Enumerable.Repeat(Vector3.up,verts.Count).ToList());mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
    }

    static GameObject BuildRealRiver(Transform parent)
    {
        const int segs=360;var verts=new List<Vector3>((segs+1)*2);var uvs=new List<Vector2>((segs+1)*2);var tris=new List<int>(segs*6);
        for(int i=0;i<=segs;i++)
        {
            float sy=Mathf.Lerp(103f,58f,i/(float)segs);float cx=RiverCenterX(sy);
            float sy0=Mathf.Max(58f,sy-.06f),sy1=Mathf.Min(103f,sy+.06f);
            float cx0=RiverCenterX(sy0),cx1=RiverCenterX(sy1);
            Vector2 p0=new Vector2((64f-cx0)*Cell,(sy0-64f)*Cell),p1=new Vector2((64f-cx1)*Cell,(sy1-64f)*Cell);
            Vector2 tangent=(p0-p1).normalized,perp=new Vector2(-tangent.y,tangent.x);
            Vector2 c=new Vector2((64f-cx)*Cell,(sy-64f)*Cell);float hw=RiverHalfWidthWorld(sy),wy=RiverSurfaceY(sy)+.045f;
            verts.Add(new Vector3(c.x+perp.x*hw,wy,c.y+perp.y*hw));verts.Add(new Vector3(c.x-perp.x*hw,wy,c.y-perp.y*hw));
            uvs.Add(new Vector2(0,i*.065f));uvs.Add(new Vector2(1,i*.065f));
        }
'''
water+=r'''        for(int i=0;i<segs;i++)
        { int a=i*2,b=a+1,c=a+2,d=a+3;tris.Add(a);tris.Add(b);tris.Add(c);tris.Add(b);tris.Add(d);tris.Add(c); }
        string path="Assets/World/NewSorpigal/Generated/RealRiverSurface.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.SetUVs(0,uvs);
        mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var go=new GameObject("River - Mountain Spring To Sea");go.transform.SetParent(parent);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=RealWaterMaterial("River_Real",true);
        mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;return go;
    }

    static GameObject BuildWater(Transform parent,Terrain terrain,Material unused)
    {
        var root=new GameObject("Water - Real Sea + Mountain River");root.transform.SetParent(parent);
        var ocean=new GameObject("Sea - Real Coastal Water");ocean.transform.SetParent(root.transform);
        ocean.AddComponent<MeshFilter>().sharedMesh=BuildOceanMesh();
        var omr=ocean.AddComponent<MeshRenderer>();omr.sharedMaterial=RealWaterMaterial("Sea_RealCoastal",false);
        omr.shadowCastingMode=ShadowCastingMode.Off;omr.receiveShadows=false;
        var river=BuildRealRiver(root.transform);
        Debug.Log($"NS_REAL_WATER ocean=yes river={(river?"yes":"no")} source=(103,103) upperBridge=(103.5,84.5) lowerBridge=(95.5,73.2) mouth=(89,58)");
        return root;
    }
'''
sub(r'    static GameObject BuildRiverRibbon\(Transform parent,float\[\] center,Material material\).*?\n    static bool IsSpecialPlacement',water+'\n    static bool IsSpecialPlacement','real sea + real river')

old='''                float surface=WaterAffiliated(go.name)?0.12f:SampleTerrainY(terrain,b.center.x,b.center.z);'''
new='''                float surface;
                if(go.name.IndexOf("Bridge",StringComparison.OrdinalIgnoreCase)>=0)
                { Vector2 src=WorldToSource(b.center.x,b.center.z);surface=RiverSurfaceY(src.y); }
                else surface=WaterAffiliated(go.name)?.12f:SampleTerrainY(terrain,b.center.x,b.center.z);'''
if old not in s: raise SystemExit('bridge surface line not found')
s=s.replace(old,new,1);print('PATCH bridge elevation')

old='''        cam.farClipPlane = 2500f;'''
new='''        cam.farClipPlane = 2500f;
        cam.depthTextureMode |= DepthTextureMode.Depth;'''
if old not in s: raise SystemExit('camera depth line not found')
s=s.replace(old,new,1);print('PATCH camera depth')

P.write_text(s,encoding='utf-8')
print('PATCH_REAL_RIVER_SEA_V2_DONE',len(s.splitlines()))

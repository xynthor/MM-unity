from pathlib import Path
import re
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')

def sub(pattern,repl,label,flags=re.S):
    global s
    ns,n=re.subn(pattern,repl,s,count=1,flags=flags)
    if n!=1: raise SystemExit(f'{label}: {n} matches')
    s=ns
    print('PATCH',label)

river=r'''    static readonly float[] RiverSy={67f,70f,73.2f,80f,84.5f,89.5f,93.5f,97f,100f,101f,102f,102.8f};
    static readonly float[] RiverX={90f,92.8f,95.5f,100.8f,103.5f,104.2f,105f,105f,104f,103.7f,103.3f,103f};
    static readonly float[] RiverY={0.12f,0.13f,0.15f,0.18f,0.24f,1.0f,3.8f,0.60f,10.0f,25.0f,44.0f,60.0f};
    static float[] riverCenterCache;

    static int RiverSegment(float sy)
    {
        for(int i=0;i<RiverSy.Length-1;i++) if(sy>=RiverSy[i]&&sy<=RiverSy[i+1]) return i;
        return -1;
    }

    static float RiverCenterX(float sy)
    {
        int i=RiverSegment(sy); if(i<0)return float.NaN;
        float t=Mathf.InverseLerp(RiverSy[i],RiverSy[i+1],sy);
        float p0=RiverX[Mathf.Max(0,i-1)],p1=RiverX[i],p2=RiverX[i+1],p3=RiverX[Mathf.Min(RiverX.Length-1,i+2)];
        float t2=t*t,t3=t2*t;
        return .5f*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t2+(-p0+3*p1-3*p2+p3)*t3);
    }

    static float RiverSurfaceY(float sy)
    {
        int i=RiverSegment(sy); if(i<0)return .12f;
        float t=Mathf.InverseLerp(RiverSy[i],RiverSy[i+1],sy);
        return Mathf.Lerp(RiverY[i],RiverY[i+1],t);
    }

    static float RiverDownstream01(float sy)
    { return Mathf.Clamp01((102.8f-sy)/(102.8f-67f)); }

    static float RiverHalfWidth(float sy)
    {
        float d=RiverDownstream01(sy);
        float baseW=Mathf.Lerp(.075f,.32f,d);
        return baseW*Mathf.Lerp(.92f,1.08f,Mathf.PerlinNoise(sy*.31f+7.7f,3.1f));
    }

    static float RiverDepth(float sy)
    { return Mathf.Lerp(.38f,1.65f,RiverDownstream01(sy)); }

    static float[] GetRiverCenterline(byte[] tiles)
    {
        if(riverCenterCache!=null)return riverCenterCache;
        var c=Enumerable.Repeat(float.NaN,N).ToArray();
        for(int y=0;y<N;y++)if(y>=67&&y<=103)c[y]=RiverCenterX(y);
        riverCenterCache=c;return c;
    }

    static bool TryRiverCenter(float[] c,float sy,out float cx)
    {
        cx=RiverCenterX(sy);return !float.IsNaN(cx);
    }

    static float RiverDistanceSource(float[] center,float sx,float sy)
    { float cx=RiverCenterX(sy);return float.IsNaN(cx)?999f:Mathf.Abs(sx-cx); }

    static float SampleRiverMask(float sx,float sy)
    {
        float cx=RiverCenterX(sy);if(float.IsNaN(cx))return 0f;
        float d=Mathf.Abs(sx-cx),half=RiverHalfWidth(sy);
        return 1f-Mathf.SmoothStep(half,half+.075f,d);
    }

    static float SampleNaturalWaterMask(byte[] tiles,float[] center,float sx,float sy)
    {
        float original=SampleSmoothWaterMask(tiles,sx,sy);
        float cx=RiverCenterX(sy);
        if(!float.IsNaN(cx)&&Mathf.Abs(sx-cx)<3.35f) original=0f;
        return Mathf.Max(original,SampleRiverMask(sx,sy));
    }

    static float SampleNonRiverWaterMask(byte[] tiles,float[] center,float sx,float sy)
    {
        float original=SampleSmoothWaterMask(tiles,sx,sy);
        float cx=RiverCenterX(sy);
        if(!float.IsNaN(cx)&&Mathf.Abs(sx-cx)<3.35f) original=0f;
        return original;
    }
'''
sub(r'    static float\[\] riverCenterCache;.*?    static float SampleDistanceBilinear',river+'\n    static float SampleDistanceBilinear','real river core')
height_block=r'''            float rawY=SampleByteBilinear(heights,sx,sy)*0.25f;
            float worldY=CreativeLandHeight(rawY,sx,sy);
            float riverD=RiverDistanceSource(riverCenter,sx,sy);
            float riverHalf=RiverHalfWidth(sy);
            float riverSurface=RiverSurfaceY(sy);
            if(riverD<riverHalf+.62f)
            {
                if(riverD<=riverHalf)
                {
                    float edge=Mathf.Clamp01(riverD/Mathf.Max(.01f,riverHalf));
                    float bedDepth=Mathf.Lerp(RiverDepth(sy),.16f,Mathf.Pow(edge,.72f));
                    worldY=Mathf.Min(worldY,riverSurface-bedDepth);
                }
                else
                {
                    float t=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(riverHalf,riverHalf+.62f,riverD));
                    float bankTarget=Mathf.Lerp(riverSurface+.20f,worldY,t);
                    worldY=Mathf.Min(worldY,bankTarget);
                }
            }
            else
            {
                float waterMask=SampleNonRiverWaterMask(tiles,riverCenter,sx,sy);
                if(waterMask>=.5f)
                {
                    float d=SampleDistanceBilinear(waterDist,sx,sy);
                    worldY=-Mathf.Clamp(.8f+d*.90f,.8f,14.5f);
                }
                else
                {
                    float protectedBlend=HasNearbyForbiddenTile(tiles,sx,sy,1,true)?.78f:0f;
                    worldY=Mathf.Lerp(worldY,BlueprintBaseHeight(rawY),protectedBlend);
                    worldY=Mathf.Max(worldY,.38f);
                }
            }'''
sub(r'            float mask=SampleNaturalWaterMask\(tiles,riverCenter,sx,sy\);.*?                worldY=Mathf.Max\(worldY,0\.38f\); // blueprint roads/town/coast stay stable\n            \}',height_block,'terrain river carve')
alpha_old=r'''            float waterMask=SampleNaturalWaterMask(tiles,riverCenter,sx,sy);
            float riverD=RiverDistanceSource(riverCenter,sx,sy);
            if (waterMask>=0.5f) { alpha[y,x,1]=0.62f; alpha[y,x,3]=0.38f; }
            else if (riverD<1.20f) { alpha[y,x,1]=0.58f; alpha[y,x,0]=0.28f; alpha[y,x,3]=0.14f; }'''
alpha_new=r'''            float waterMask=SampleNonRiverWaterMask(tiles,riverCenter,sx,sy);
            float riverD=RiverDistanceSource(riverCenter,sx,sy);
            float riverHalf=RiverHalfWidth(sy);
            if (riverD<riverHalf+.62f) { alpha[y,x,1]=0.53f; alpha[y,x,3]=0.34f; alpha[y,x,0]=0.13f; }
            else if (waterMask>=0.5f) { alpha[y,x,1]=0.56f; alpha[y,x,3]=0.44f; }'''
if alpha_old not in s: raise SystemExit('alpha block not found')
s=s.replace(alpha_old,alpha_new,1);print('PATCH terrain water textures')

old='''            bool grass=water<0.10f&&!IsRoad(t)&&!IsDirt(t)&&riverD>0.72f&&slope<34f;'''
new='''            bool grass=water<0.10f&&!IsRoad(t)&&!IsDirt(t)&&riverD>RiverHalfWidth(sy)+.48f&&slope<34f;'''
if old not in s: raise SystemExit('grass exclusion not found')
s=s.replace(old,new,1);print('PATCH grass river exclusion')
water=r'''    static Texture2D BuildRealWaterMask(byte[] tiles,float[] center)
    {
        const int res=1024;
        string path="Assets/World/NewSorpigal/Generated/RealWaterMask.asset";
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(!tex||tex.width!=res||tex.height!=res)
        {
            if(tex)AssetDatabase.DeleteAsset(path);
            tex=new Texture2D(res,res,TextureFormat.RGBA32,false,true);
            AssetDatabase.CreateAsset(tex,path);
        }
        var px=new Color32[res*res];
        for(int y=0;y<res;y++)for(int x=0;x<res;x++)
        {
            float sx=(N-1)-x/(float)(res-1)*(N-1),sy=y/(float)(res-1)*(N-1);
            float v=SampleNonRiverWaterMask(tiles,center,sx,sy);
            v=Mathf.SmoothStep(.14f,.86f,v);
            byte b=(byte)Mathf.RoundToInt(v*255f);px[y*res+x]=new Color32(b,b,b,255);
        }
        tex.SetPixels32(px);tex.Apply(false,false);tex.filterMode=FilterMode.Bilinear;tex.wrapMode=TextureWrapMode.Clamp;
        EditorUtility.SetDirty(tex);return tex;
    }

    static Mesh BuildRealSeaMesh()
    {
        const int grid=160;int row=grid+1;float size=TerrainSize*2.35f,origin=-size*.5f,step=size/grid;
        var verts=new List<Vector3>(row*row);var tris=new List<int>(grid*grid*6);
        for(int y=0;y<=grid;y++)for(int x=0;x<=grid;x++)verts.Add(new Vector3(origin+x*step,.11f,origin+y*step));
'''
        for(int y=0;y<grid;y++)for(int x=0;x<grid;x++)
        {
            int a=y*row+x,b=a+1,d=(y+1)*row+x,c=d+1;
            tris.Add(a);tris.Add(c);tris.Add(b);tris.Add(a);tris.Add(d);tris.Add(c);
        }
        string path="Assets/World/NewSorpigal/Generated/RealSeaSurface.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetTriangles(tris,0);
        mesh.SetNormals(Enumerable.Repeat(Vector3.up,verts.Count).ToList());mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
    }

    static GameObject BuildRiverRibbon(Transform parent,float[] center,Material material)
    {
        const int segs=320;var verts=new List<Vector3>((segs+1)*2);var uvs=new List<Vector2>((segs+1)*2);var tris=new List<int>(segs*6);
        for(int i=0;i<=segs;i++)
        {
            float sy=Mathf.Lerp(102.75f,67.05f,i/(float)segs);float cx=RiverCenterX(sy);if(float.IsNaN(cx))continue;
            float syA=Mathf.Max(67f,sy-.08f),syB=Mathf.Min(102.8f,sy+.08f);
            float ax=RiverCenterX(syA),bx=RiverCenterX(syB);
            Vector2 pa=new Vector2((64f-ax)*Cell,(syA-64f)*Cell),pb=new Vector2((64f-bx)*Cell,(syB-64f)*Cell);
            Vector2 tangent=(pa-pb).normalized,perp=new Vector2(-tangent.y,tangent.x);
            Vector2 c=new Vector2((64f-cx)*Cell,(sy-64f)*Cell);float hw=RiverHalfWidth(sy)*Cell,yw=RiverSurfaceY(sy)+.055f;
            verts.Add(new Vector3(c.x+perp.x*hw,yw,c.y+perp.y*hw));verts.Add(new Vector3(c.x-perp.x*hw,yw,c.y-perp.y*hw));
            uvs.Add(new Vector2(0,i*.08f));uvs.Add(new Vector2(1,i*.08f));
        }
        for(int i=0;i<segs;i++)
        {
            int a=i*2,b=a+1,c=a+2,d=a+3;
            tris.Add(a);tris.Add(b);tris.Add(c);tris.Add(b);tris.Add(d);tris.Add(c);
        }
        string path="Assets/World/NewSorpigal/Generated/RealRiverSurface.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.SetUVs(0,uvs);
        mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var go=new GameObject("River - Mountain To Sea");go.transform.SetParent(parent);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=material;mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;return go;
    }

    static GameObject BuildWater(Transform parent,Terrain terrain,Material unused)
    {
        byte[] tiles=File.ReadAllBytes(TilePath);float[] center=GetRiverCenterline(tiles);var mask=BuildRealWaterMask(tiles,center);
        var seaMat=CreateSimpleMaterial("Sea_RealCoastal",Shader.Find("MMUnity/RealCoastalWater"),Color.white);
        seaMat.SetTexture("_WaterMask",mask);seaMat.SetColor("_ShallowColor",new Color(.055f,.34f,.40f,.68f));
        seaMat.SetColor("_DeepColor",new Color(.004f,.035f,.085f,.96f));seaMat.SetColor("_FoamColor",new Color(.88f,.96f,.94f,.95f));
        seaMat.SetColor("_ReflectionColor",new Color(.30f,.52f,.66f,1f));seaMat.SetFloat("_DepthMax",14f);seaMat.SetFloat("_FoamDepth",.9f);
        seaMat.SetFloat("_WaveAmp",.20f);seaMat.SetFloat("_WaveScale",.052f);seaMat.SetFloat("_WaveSpeed",.62f);
        seaMat.SetFloat("_WaveAmp2",.085f);seaMat.SetFloat("_WaveScale2",.115f);seaMat.SetFloat("_WaveSpeed2",1.05f);
        seaMat.SetFloat("_Distortion",.012f);seaMat.SetFloat("_FresnelPower",4f);seaMat.SetFloat("_ReflectionStrength",.44f);seaMat.SetFloat("_WorldSize",TerrainSize);
        EditorUtility.SetDirty(seaMat);

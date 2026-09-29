from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')
start=s.index('    // One continuous drainage:')
end=s.index('    static float SampleDistanceBilinear', start)
new=r'''    // Bridge-aligned world-space drainage. Anchors come from the actual rebuilt MM6 bridge transforms.
    // Terrain carve, water mesh, source rocks and riverside dressing share this exact route.
    static readonly Vector2[] RiverWorldXZ={
        new Vector2(445f,350f), new Vector2(430f,330f), new Vector2(405f,310f), new Vector2(380f,285f),
        new Vector2(350f,255f), new Vector2(325f,225f), new Vector2(300f,190f), new Vector2(278f,160f),
        new Vector2(260.59686f,135.40312f), // 021_M020_BridgeE_1
        new Vector2(250f,118f), new Vector2(238f,95f), new Vector2(228f,75f),
        new Vector2(216.10f,60.74f),        // 020_M019_BridgeE + 070_M069_BridgeT
        new Vector2(200f,35f), new Vector2(180f,10f), new Vector2(160f,-20f),
        new Vector2(140f,-55f), new Vector2(122f,-90f), new Vector2(105f,-125f)
    };
    static readonly float[] RiverWorldY={
        25f,23f,18f,12f,7f,4.5f,2.2f,1.0f,.42f,.38f,.34f,.30f,.25f,.22f,.20f,.18f,.16f,.14f,.12f
    };
    static float[] riverWorldCum; static float riverWorldTotal;
'''
new += r'''    static void EnsureRiverWorldLengths()
    {
        if(riverWorldCum!=null)return;
        riverWorldCum=new float[RiverWorldXZ.Length];riverWorldCum[0]=0f;
        for(int i=1;i<RiverWorldXZ.Length;i++)riverWorldCum[i]=riverWorldCum[i-1]+Vector2.Distance(RiverWorldXZ[i-1],RiverWorldXZ[i]);
        riverWorldTotal=riverWorldCum[riverWorldCum.Length-1];
    }
    static void RiverPointAtT(float t,out Vector2 p,out float y,out Vector2 tangent)
    {
        EnsureRiverWorldLengths();t=Mathf.Clamp01(t);float target=t*riverWorldTotal;int seg=RiverWorldXZ.Length-2;
        for(int i=0;i<RiverWorldXZ.Length-1;i++)if(target<=riverWorldCum[i+1]){seg=i;break;}
        float len=Mathf.Max(.001f,riverWorldCum[seg+1]-riverWorldCum[seg]);float u=Mathf.Clamp01((target-riverWorldCum[seg])/len);
        p=Vector2.Lerp(RiverWorldXZ[seg],RiverWorldXZ[seg+1],u);y=Mathf.Lerp(RiverWorldY[seg],RiverWorldY[seg+1],u);
        tangent=(RiverWorldXZ[seg+1]-RiverWorldXZ[seg]).normalized;
    }
    static void RiverNearestWorld(float wx,float wz,out float distance,out float t,out Vector2 center,out Vector2 tangent)
    {
        EnsureRiverWorldLengths();Vector2 p=new Vector2(wx,wz);distance=float.MaxValue;t=0f;center=RiverWorldXZ[0];tangent=(RiverWorldXZ[1]-RiverWorldXZ[0]).normalized;
        for(int i=0;i<RiverWorldXZ.Length-1;i++)
        {
            Vector2 a=RiverWorldXZ[i],b=RiverWorldXZ[i+1],ab=b-a;float len2=Mathf.Max(.0001f,ab.sqrMagnitude);
            float u=Mathf.Clamp01(Vector2.Dot(p-a,ab)/len2);Vector2 q=a+ab*u;float d=Vector2.Distance(p,q);
            if(d<distance){distance=d;center=q;tangent=ab.normalized;t=(riverWorldCum[i]+Mathf.Sqrt(len2)*u)/riverWorldTotal;}
        }
    }
'''
new += r'''    static float RiverHalfWidthT(float t)
    {
        float baseW=Mathf.Lerp(.35f,3.7f,Mathf.Pow(Mathf.Clamp01(t),.76f));
        float var=Mathf.Lerp(.92f,1.08f,Mathf.PerlinNoise(t*8.7f+2.1f,5.3f));
        return baseW*var;
    }
    static float RiverEffectiveHalfWidth(float t)
    { return RiverHalfWidthT(t)*Mathf.SmoothStep(.08f,1f,Mathf.InverseLerp(0f,.025f,t)); }
    static float RiverDepthT(float t){return Mathf.Lerp(.16f,1.20f,Mathf.Pow(Mathf.Clamp01(t),.78f));}
    static float RiverSurfaceT(float t){RiverPointAtT(t,out _,out float y,out _);return y;}
    static Vector2 LegacySourceToWorld(float sx,float sy){return new Vector2((64f-sx)*Cell,(sy-64f)*Cell);}

    // Retained wrappers for existing vegetation suitability code. They now reference the same world-space river.
    static float SampleSeaWaterMask(byte[] tiles,float sx,float sy){return SampleCoastalWaterMask(tiles,sx,sy);}
    static float SampleNaturalWaterMask(byte[] tiles,float[] unused,float sx,float sy)
    {
        Vector2 w=LegacySourceToWorld(sx,sy);RiverNearestWorld(w.x,w.y,out float d,out float t,out _,out _);
        return Mathf.Max(SampleSeaWaterMask(tiles,sx,sy),d<=RiverEffectiveHalfWidth(t)?1f:0f);
    }
    static float[] GetRiverCenterline(byte[] tiles){return Enumerable.Repeat(float.NaN,N).ToArray();}
    static bool TryRiverCenter(float[] c,float sy,out float cx){cx=float.NaN;return false;}
    static float RiverDistanceSource(float[] c,float sx,float sy){Vector2 w=LegacySourceToWorld(sx,sy);RiverNearestWorld(w.x,w.y,out float d,out _,out _,out _);return d/Cell;}
    static float RiverHalfWidth(float sy){return .35f;}
'''
s=s[:start]+new+s[end:]
old=r'''            float riverD=RiverDistanceWorld(sx,sy);
            float halfW=RiverHalfWidthWorld(sy);
            if(halfW>0f)
            {
                float wetClear=halfW+.25f;
                float down=RiverDownstream01(sy);
                float floodExtra=Mathf.Lerp(6.0f,12.0f,down);
                float blendExtra=Mathf.Lerp(24.0f,42.0f,down);
                float floodOuter=wetClear+floodExtra;
                float outer=floodOuter+blendExtra;
                if(riverD<outer)
                {
                    float surface=RiverSurfaceY(sy);
                    if(riverD<=wetClear)
                    {
                        float edge=Mathf.Clamp01(riverD/Mathf.Max(.01f,wetClear));
                        float bed=surface-Mathf.Lerp(RiverDepth(sy),.08f,Mathf.Pow(edge,.82f));
                        worldY=Mathf.Min(worldY,bed);
                    }
'''
new2=r'''            float wx=-TerrainSize*.5f+x/(float)hmMax*TerrainSize,wz=-TerrainSize*.5f+y/(float)hmMax*TerrainSize;
            RiverNearestWorld(wx,wz,out float riverD,out float riverT,out _,out _);
            float halfW=RiverEffectiveHalfWidth(riverT);
            float wetClear=halfW+.18f;
            float floodExtra=Mathf.Lerp(2.4f,9.0f,riverT),blendExtra=Mathf.Lerp(9f,26f,riverT);
            float floodOuter=wetClear+floodExtra,outer=floodOuter+blendExtra;
            if(riverD<outer)
            {
                float surface=RiverSurfaceT(riverT);
                if(riverD<=wetClear)
                {
                    float edge=Mathf.Clamp01(riverD/Mathf.Max(.01f,wetClear));
                    float bed=surface-Mathf.Lerp(RiverDepthT(riverT),.05f,Mathf.Pow(edge,.86f));
                    worldY=Mathf.Min(worldY,bed);
                }
'''
assert old in s,'height river block head not found'; s=s.replace(old,new2,1)
oldtail=r'''                    else if(riverD<=floodOuter)
                    {
                        float t=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(wetClear,floodOuter,riverD));
                        float floodY=surface+Mathf.Lerp(.16f,.72f,t);
                        worldY=Mathf.Min(worldY,floodY);
                    }
                    else
                    {
                        float t=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(floodOuter,outer,riverD));
                        worldY=Mathf.Min(worldY,Mathf.Lerp(surface+.72f,worldY,t));
                    }
                }
            }
'''
newtail=r'''                else if(riverD<=floodOuter)
                {
                    float t=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(wetClear,floodOuter,riverD));
                    float floodY=surface+Mathf.Lerp(.12f,.50f,t);
                    worldY=Mathf.Min(worldY,floodY);
                }
                else
                {
                    float t=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(floodOuter,outer,riverD));
                    worldY=Mathf.Min(worldY,Mathf.Lerp(surface+.50f,worldY,t));
                }
            }
'''
assert oldtail in s,'height river tail not found'; s=s.replace(oldtail,newtail,1)
olda=r'''            float riverD=RiverDistanceWorld(sx,sy),halfW=RiverHalfWidthWorld(sy);
            float outer=halfW>0f?halfW+.25f+Mathf.Lerp(6.0f,12.0f,RiverDownstream01(sy))+Mathf.Lerp(24.0f,42.0f,RiverDownstream01(sy)):0f;
            if(halfW>0f&&riverD<outer)
            {
                float wetOuter=halfW+Mathf.Lerp(1.25f,2.65f,RiverDownstream01(sy));
                float wet=riverD<=halfW?1f:(riverD<wetOuter?Mathf.Lerp(.84f,.03f,Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(halfW,wetOuter,riverD))):0f);
                alpha[y,x,4]=wet;
                alpha[y,x,1]=(1f-wet)*.14f;
                alpha[y,x,3]=(1f-wet)*.015f;
                alpha[y,x,0]=1f-alpha[y,x,4]-alpha[y,x,1]-alpha[y,x,3];
            }
'''
newa=r'''            float wx=-TerrainSize*.5f+x/511f*TerrainSize,wz=-TerrainSize*.5f+y/511f*TerrainSize;
            RiverNearestWorld(wx,wz,out float riverD,out float riverT,out _,out _);float halfW=RiverEffectiveHalfWidth(riverT);
            float outer=halfW+.18f+Mathf.Lerp(2.4f,9.0f,riverT)+Mathf.Lerp(9f,26f,riverT);
            if(riverD<outer)
            {
                float wetOuter=halfW+Mathf.Lerp(.8f,2.2f,riverT);
                float wet=riverD<=halfW?.42f:(riverD<wetOuter?Mathf.Lerp(.35f,0f,Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(halfW,wetOuter,riverD))):0f);
                float dirt=Mathf.Lerp(.22f,.10f,Mathf.Clamp01((riverD-halfW)/Mathf.Max(1f,outer-halfW)));
                float rock=Mathf.Clamp01(Mathf.InverseLerp(26f,48f,slope))*.18f;
                float grass=Mathf.Max(0f,1f-wet-dirt-rock);
                alpha[y,x,0]=grass;alpha[y,x,1]=dirt;alpha[y,x,3]=rock;alpha[y,x,4]=wet;
            }
'''
assert olda in s,'alpha river block not found'; s=s.replace(olda,newa,1)
m0=s.index('    static GameObject BuildRealRiver(Transform parent)')
m1=s.index('    static GameObject BuildRiverBanks',m0)
newm=r'''    static GameObject BuildRealRiver(Transform parent)
    {
        const int segs=520;var verts=new List<Vector3>((segs+1)*2);var uvs=new List<Vector2>((segs+1)*2);var tris=new List<int>(segs*6);
        for(int i=0;i<=segs;i++)
        {
            float t=i/(float)segs;RiverPointAtT(t,out Vector2 c,out float wy,out Vector2 tangent);Vector2 perp=new Vector2(-tangent.y,tangent.x);
            float hw=RiverEffectiveHalfWidth(t);float ripple=(Mathf.PerlinNoise(t*15.2f+4.3f,7.7f)-.5f)*.16f*hw;
            Vector2 l=c+perp*(hw+ripple),r=c-perp*(hw-ripple);
            verts.Add(new Vector3(l.x,wy+.055f,l.y));verts.Add(new Vector3(r.x,wy+.055f,r.y));
            uvs.Add(new Vector2(0,i*.055f));uvs.Add(new Vector2(1,i*.055f));
        }
        for(int i=0;i<segs;i++){int a=i*2,b=a+1,c=a+2,d=a+3;tris.Add(a);tris.Add(b);tris.Add(c);tris.Add(b);tris.Add(d);tris.Add(c);}
        string path="Assets/World/NewSorpigal/Generated/RealRiverSurface.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
        mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.SetUVs(0,uvs);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var go=new GameObject("River - Mountain Spring Through MM6 Bridges To Sea");go.transform.SetParent(parent);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=RealWaterMaterial("River_Real",true);mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;return go;
    }

'''
s=s[:m0]+newm+s[m1:]
b0=s.index('    static GameObject BuildRiverBanks')
b1=s.index('    static void BuildRiverSourceRocks',b0)
s=s[:b0]+'''    static GameObject BuildRiverBanks(Transform parent,Terrain terrain) { return null; }\n\n'''+s[b1:]
r0=s.index('    static void BuildRiverSourceRocks')
r1=s.index('    static GameObject BuildWater',r0)
newr=r'''    static void BuildRiverSourceRocks(Transform parent,Terrain terrain,EnvAssets e)
    {
        GameObject sourceRock=e.rockB?e.rockB:e.botdRockPrefab;if(!sourceRock)return;
        var root=new GameObject("Mountain Spring Source - Bridge Aligned");root.transform.SetParent(parent);
        RiverPointAtT(.004f,out Vector2 source,out _,out Vector2 tangent);Vector2 perp=new Vector2(-tangent.y,tangent.x);
        Vector2[] offsets={-tangent*1.5f+perp*1.15f,-tangent*1.5f-perp*1.15f,-tangent*.25f+perp*1.30f,-tangent*.25f-perp*1.30f,tangent*.95f+perp*1.45f,tangent*.95f-perp*1.45f};
        float[] heights={2.7f,2.4f,2.0f,2.2f,1.7f,1.8f};
        for(int i=0;i<offsets.Length;i++)
        {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(sourceRock);go.name="SpringBoulder_"+i;go.transform.SetParent(root.transform);
            Vector2 q=source+offsets[i];float gy=SampleTerrainY(terrain,q.x,q.y);go.transform.position=new Vector3(q.x,gy-.22f,q.y);
            go.transform.rotation=Quaternion.Euler(Deterministic01(18100+i*7)*10f-5f,Deterministic01(18200+i*11)*360f,Deterministic01(18300+i*13)*8f-4f);
            ScaleToHeight(go,heights[i]);foreach(var rr in go.GetComponentsInChildren<Renderer>(true))rr.sharedMaterial=e.rockMaterial;
        }
    }

'''
s=s[:r0]+newr+s[r1:]
rv0=s.index('    static int BuildRiverside(')
rv1=s.index('    static void NormalizeTreeUpright',rv0)
newrv=r'''    static int BuildRiverside(Terrain terrain,Transform architectureRoot,Transform rocks,Transform understory,EnvAssets e)
    {
        int added=0;var roots=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Ground/Roots/Roots_System_01_Prefab.prefab");
        var debris=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Ground/Sticks_debris/sticks_debris_00_prefab.prefab");
        for(int i=0;i<120;i++)
        {
            float t=Mathf.Lerp(.055f,.94f,i/119f);RiverPointAtT(t,out Vector2 c,out _,out Vector2 tangent);Vector2 perp=new Vector2(-tangent.y,tangent.x);
            foreach(int side in new[]{-1,1})
            {
                int seed=120000+i*7+(side>0?3:0);float bank=RiverEffectiveHalfWidth(t)+Mathf.Lerp(1.6f,4.8f,Deterministic01(seed*11));
                Vector2 q=c+perp*(side*bank)+tangent*((Deterministic01(seed*13)-.5f)*5.5f);float wx=q.x,wz=q.y;
                if(!IsSuitableNaturalSpot(terrain,architectureRoot,wx,wz,false,false))continue;
                int choice=Mathf.Abs(seed)%4;GameObject pf=null;Material mat=null;float ph=.6f;
                if(choice==0){pf=e.botdFernPrefab;mat=e.botdFernMaterial;ph=Mathf.Lerp(.45f,.95f,Deterministic01(seed*19));}
                else if(choice==1){pf=e.botdShrubPrefab;mat=e.botdShrubMaterial;ph=Mathf.Lerp(.85f,1.7f,Deterministic01(seed*19));}
                else if(choice==2){pf=e.botdCloverPrefab;mat=e.botdCloverMaterial;ph=Mathf.Lerp(.22f,.48f,Deterministic01(seed*19));}
                else {pf=e.botdGrassPrefab;mat=e.botdGrassMaterial;ph=Mathf.Lerp(.35f,.78f,Deterministic01(seed*19));}
                if(pf){SpawnPlant(pf,understory,mat,wx,wz,SampleTerrainY(terrain,wx,wz),seed,ph,"Riverbank");added++;}
'''
newrv += r'''                if(i%7==0 && e.botdRockPrefab)
                {
                    Vector2 rq=q+perp*(side*Mathf.Lerp(.8f,2.8f,Deterministic01(seed*23)));
                    if(IsSuitableNaturalSpot(terrain,architectureRoot,rq.x,rq.y,false,false)){SpawnPlant(e.botdRockPrefab,rocks,e.rockMaterial,rq.x,rq.y,SampleTerrainY(terrain,rq.x,rq.y),seed+1,Mathf.Lerp(.38f,1.35f,Deterministic01(seed*29)),"RiverRock");added++;}
                }
                if(i%13==3 && roots)
                {
                    Vector2 rq=q+tangent*Mathf.Lerp(-1.8f,1.8f,Deterministic01(seed*31));
                    if(IsSuitableNaturalSpot(terrain,architectureRoot,rq.x,rq.y,false,false)){SpawnPlant(roots,understory,e.treeBark,rq.x,rq.y,SampleTerrainY(terrain,rq.x,rq.y),seed+2,Mathf.Lerp(.45f,.85f,Deterministic01(seed*37)),"RiverRoots");added++;}
                }
                if(i%11==5 && debris)
                {
                    Vector2 dq=q+tangent*Mathf.Lerp(-1.5f,1.5f,Deterministic01(seed*41));
                    if(IsSuitableNaturalSpot(terrain,architectureRoot,dq.x,dq.y,false,false)){SpawnPlant(debris,understory,e.treeBark,dq.x,dq.y,SampleTerrainY(terrain,dq.x,dq.y),seed+3,Mathf.Lerp(.35f,.72f,Deterministic01(seed*43)),"RiverDebris");added++;}
                }
            }
        }
        return added;
    }

'''
s=s[:rv0]+newrv+s[rv1:]
s=s.replace('Debug.Log($"NS_REAL_WATER ocean=yes river={(river?"yes":"no")} spring=rocks source=(103,103) upperBridge=(103.5,84.5) lowerBridge=(95.5,73.2) mouth=(89,58)");','Debug.Log($"NS_REAL_WATER_BRIDGE_ALIGNED ocean=yes river={(river?"yes":"no")} sourceWorld=(445,350) upperBridgeWorld=(260.60,135.40) lowerBridgeWorld=(216.10,60.74) mouthWorld=(105,-125)");',1)
p.write_text(s,encoding='utf-8')
print('PATCH_BRIDGE_ALIGNED_RIVER_DONE',len(s.splitlines()))

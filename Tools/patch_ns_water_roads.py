from pathlib import Path
import re
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8-sig')

helpers=r'''
    static bool[,] correctedWaterCache;
    static bool[,] correctedRoadCache;
    static byte[] maskHeightCache;

    static float SourceSlope(byte[] heights,int x,int y)
    {
        int xl=Mathf.Max(0,x-1),xr=Mathf.Min(N-1,x+1),yd=Mathf.Max(0,y-1),yu=Mathf.Min(N-1,y+1);
        float dx=(heights[y*N+xr]-heights[y*N+xl])*.125f;
        float dy=(heights[yu*N+x]-heights[yd*N+x])*.125f;
        return Mathf.Sqrt(dx*dx+dy*dy);
    }

    static void MarkGridLine(bool[,] mask,Vector2Int a,Vector2Int b)
    {
        int steps=Mathf.Max(Mathf.Abs(b.x-a.x),Mathf.Abs(b.y-a.y));
        if(steps==0){mask[a.y,a.x]=true;return;}
        for(int i=0;i<=steps;i++)
        {
            int x=Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.x,b.x,i/(float)steps)),0,N-1);
            int y=Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(a.y,b.y,i/(float)steps)),0,N-1);
            if(!correctedWaterCache[y,x]) mask[y,x]=true;
        }
    }
'''
helpers+=r'''
    static void EnsureHydroRoadMasks(byte[] tiles)
    {
        if(correctedWaterCache!=null && correctedRoadCache!=null) return;
        correctedWaterCache=new bool[N,N];
        for(int y=0;y<N;y++)for(int x=0;x<N;x++) correctedWaterCache[y,x]=IsWater(tiles[y*N+x]);
        for(int pass=0;pass<2;pass++)
        {
            var next=(bool[,])correctedWaterCache.Clone();
            for(int y=1;y<N-1;y++)for(int x=1;x<N-1;x++) if(!correctedWaterCache[y,x])
            {
                int n=0;for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)if((ox!=0||oy!=0)&&correctedWaterCache[y+oy,x+ox])n++;
                bool bridge=(correctedWaterCache[y,x-1]&&correctedWaterCache[y,x+1])||(correctedWaterCache[y-1,x]&&correctedWaterCache[y+1,x]);
                if(bridge || n>=6) next[y,x]=true;
            }
            correctedWaterCache=next;
        }
        maskHeightCache=File.ReadAllBytes(HeightPath);
        var rawRoad=new bool[N,N];var mapped=new Vector2Int[N,N];correctedRoadCache=new bool[N,N];
        for(int y=0;y<N;y++)for(int x=0;x<N;x++) rawRoad[y,x]=IsRoad(tiles[y*N+x]);
        for(int y=0;y<N;y++)for(int x=0;x<N;x++) if(rawRoad[y,x])
        {
            Vector2Int best=new Vector2Int(x,y);float current=SourceSlope(maskHeightCache,x,y);
            if(current>.80f)
            {
                float bestScore=float.MaxValue;
                for(int oy=-3;oy<=3;oy++)for(int ox=-3;ox<=3;ox++)
                {
                    int xx=x+ox,yy=y+oy;if(xx<0||yy<0||xx>=N||yy>=N||correctedWaterCache[yy,xx])continue;
                    float dist=Mathf.Sqrt(ox*ox+oy*oy),sl=SourceSlope(maskHeightCache,xx,yy);
                    float dh=Mathf.Abs(maskHeightCache[yy*N+xx]-maskHeightCache[y*N+x])*.25f;
                    float score=sl*3.4f+dist*.16f+dh*.18f;
                    if(score<bestScore){bestScore=score;best=new Vector2Int(xx,yy);}
                }
            }
            mapped[y,x]=best;correctedRoadCache[best.y,best.x]=true;
        }
'''
helpers+=r'''
        for(int y=0;y<N;y++)for(int x=0;x<N;x++) if(rawRoad[y,x])
        {
            Vector2Int a=mapped[y,x];
            if(x+1<N&&rawRoad[y,x+1]) MarkGridLine(correctedRoadCache,a,mapped[y,x+1]);
            if(y+1<N&&rawRoad[y+1,x]) MarkGridLine(correctedRoadCache,a,mapped[y+1,x]);
        }
    }

    static float SampleBoolMask(bool[,] mask,float sx,float sy)
    {
        sx=Mathf.Clamp(sx,0f,N-1f);sy=Mathf.Clamp(sy,0f,N-1f);
        int x0=Mathf.FloorToInt(sx),y0=Mathf.FloorToInt(sy),x1=Mathf.Min(x0+1,N-1),y1=Mathf.Min(y0+1,N-1);
        float tx=sx-x0,ty=sy-y0;
        float a=Mathf.Lerp(mask[y0,x0]?1f:0f,mask[y0,x1]?1f:0f,tx);
        float b=Mathf.Lerp(mask[y1,x0]?1f:0f,mask[y1,x1]?1f:0f,tx);
        return Mathf.Lerp(a,b,ty);
    }

    static float SampleImprovedWaterMask(byte[] tiles,float sx,float sy)
    {
        EnsureHydroRoadMasks(tiles);float baseV=SampleBoolMask(correctedWaterCache,sx,sy),sum=0f,wSum=0f;
        for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)
        {
            float w=(ox==0&&oy==0)?2.4f:((ox==0||oy==0)?1f:.55f);
            sum+=SampleBoolMask(correctedWaterCache,sx+ox*.34f,sy+oy*.34f)*w;wSum+=w;
        }
        return Mathf.Clamp01(Mathf.Lerp(baseV,sum/wSum,.46f));
    }

    static float SampleCorrectedRoadMask(byte[] tiles,float sx,float sy)
    {
        EnsureHydroRoadMasks(tiles);float baseV=SampleBoolMask(correctedRoadCache,sx,sy),sum=0f,wSum=0f;
        for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)
        { float w=(ox==0&&oy==0)?2f:1f;sum+=SampleBoolMask(correctedRoadCache,sx+ox*.28f,sy+oy*.28f)*w;wSum+=w; }
        return Mathf.Clamp01(Mathf.Lerp(baseV,sum/wSum,.30f));
    }
'''
helpers+=r'''
    static float RoadDistanceSource(byte[] tiles,float sx,float sy)
    {
        EnsureHydroRoadMasks(tiles);int cx=Mathf.RoundToInt(sx),cy=Mathf.RoundToInt(sy);float best=99f;
        for(int oy=-4;oy<=4;oy++)for(int ox=-4;ox<=4;ox++)
        {
            int x=cx+ox,y=cy+oy;if(x<0||y<0||x>=N||y>=N||!correctedRoadCache[y,x])continue;
            float dx=sx-x,dy=sy-y;best=Mathf.Min(best,Mathf.Sqrt(dx*dx+dy*dy));
        }
        return best;
    }

    static Vector2 SourceToWorld(float sx,float sy)
    { return new Vector2((64f-sx)*Cell,(sy-64f)*Cell); }

    static int[,] ComputeWaterDistance(byte[] tiles)
    {
        EnsureHydroRoadMasks(tiles);var dist=new int[N,N];var q=new Queue<Vector2Int>();const int inf=9999;
        for(int y=0;y<N;y++)for(int x=0;x<N;x++)
        {
            if(!correctedWaterCache[y,x]){dist[y,x]=0;q.Enqueue(new Vector2Int(x,y));}else dist[y,x]=inf;
        }
        int[] dx={1,-1,0,0},dy={0,0,1,-1};
        while(q.Count>0)
        {
            var p=q.Dequeue();int nd=dist[p.y,p.x]+1;
            for(int k=0;k<4;k++){int nx=p.x+dx[k],ny=p.y+dy[k];if(nx<0||ny<0||nx>=N||ny>=N||nd>=dist[ny,nx])continue;dist[ny,nx]=nd;q.Enqueue(new Vector2Int(nx,ny));}
        }
        return dist;
    }
'''

helpers+=r'''
    static float SampleByteBilinear(byte[] data, float sx, float sy)
    {
        sx=Mathf.Clamp(sx,0f,N-1f);sy=Mathf.Clamp(sy,0f,N-1f);
        int x0=Mathf.FloorToInt(sx),y0=Mathf.FloorToInt(sy),x1=Mathf.Min(x0+1,N-1),y1=Mathf.Min(y0+1,N-1);
        float tx=sx-x0,ty=sy-y0;
        float a=Mathf.Lerp(data[y0*N+x0],data[y0*N+x1],tx);
        float b=Mathf.Lerp(data[y1*N+x0],data[y1*N+x1],tx);
        return Mathf.Lerp(a,b,ty);
    }
'''

# Replace old water-distance + fake-river helpers, preserving SampleDistanceBilinear onward.
s=re.sub(r'    static int\[,] ComputeWaterDistance\(byte\[] tiles\).*?    static float SampleDistanceBilinear\(int\[,] dist, float sx, float sy\)',helpers+'\n    static float SampleDistanceBilinear(int[,] dist, float sx, float sy)',s,flags=re.S)
terrain=r'''
    static Terrain BuildTerrain(Transform parent, EnvAssets e)
    {
        byte[] heights=File.ReadAllBytes(HeightPath),tiles=File.ReadAllBytes(TilePath);EnsureHydroRoadMasks(tiles);
        int[,] waterDist=ComputeWaterDistance(tiles);string dataPath="Assets/World/NewSorpigal/Generated/NewSorpigalTerrain.asset";
        var td=AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);if(!td){td=new TerrainData();AssetDatabase.CreateAsset(td,dataPath);}
        td.heightmapResolution=TerrainResolution;td.size=new Vector3(TerrainSize,TerrainHeight,TerrainSize);td.terrainLayers=new[]{e.grassLayer,e.dirtLayer,e.roadLayer,e.rockLayer};
        int hmMax=TerrainResolution-1;float[,] hm=new float[TerrainResolution,TerrainResolution];
        for(int y=0;y<TerrainResolution;y++)for(int x=0;x<TerrainResolution;x++)
        {
            float sx=(N-1)-x/(float)hmMax*(N-1),sy=y/(float)hmMax*(N-1);
            float rawY=SampleByteBilinear(heights,sx,sy)*.25f;
            float landY=CreativeLandHeight(rawY,sx,sy);
            float protectedBlend=HasNearbyForbiddenTile(tiles,sx,sy,1,true)?.78f:0f;
            landY=Mathf.Max(Mathf.Lerp(landY,BlueprintBaseHeight(rawY),protectedBlend),.38f);
            float mask=SampleImprovedWaterMask(tiles,sx,sy),d=SampleDistanceBilinear(waterDist,sx,sy);
            float bed=-Mathf.Clamp(.65f+d*.78f,.65f,11.5f);
            float waterBlend=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.30f,.70f,mask));
            float worldY=Mathf.Lerp(landY,bed,waterBlend);
            hm[y,x]=Mathf.Clamp01((worldY-TerrainBaseY)/TerrainHeight);
        }
        td.SetHeights(0,0,hm);td.alphamapResolution=512;float[,,] alpha=new float[512,512,4];
        for(int y=0;y<512;y++)for(int x=0;x<512;x++)
        {
            float sx=(N-1)-x/511f*(N-1),sy=y/511f*(N-1);byte tile=TileAtSource(tiles,sx,sy);
            float h=CreativeLandHeight(SampleByteBilinear(heights,sx,sy)*.25f,sx,sy),slope=td.GetSteepness(x/511f,y/511f);
            float macro=Mathf.PerlinNoise(sx*.071f+2.8f,sy*.067f+11.4f),water=SampleImprovedWaterMask(tiles,sx,sy),road=SampleCorrectedRoadMask(tiles,sx,sy);
            if(water>.48f){alpha[y,x,1]=.58f;alpha[y,x,3]=.42f;}
'''
terrain+=r'''
            else if(road>.20f)
            {
                float rw=Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.18f,.58f,road));
                alpha[y,x,2]=.94f*rw;alpha[y,x,1]=.06f*rw;alpha[y,x,0]=1f-rw;
            }
            else if(IsDirt(tile)){alpha[y,x,1]=.70f;alpha[y,x,0]=.22f;alpha[y,x,3]=.08f;}
            else
            {
                float rock=Mathf.Clamp01(Mathf.InverseLerp(19f,43f,slope)*.92f+Mathf.InverseLerp(66f,118f,h)*.30f);
                float dirt=(.045f+macro*.14f)*(1f-rock),grass=Mathf.Max(0f,1f-rock-dirt);
                alpha[y,x,0]=grass;alpha[y,x,1]=dirt;alpha[y,x,3]=rock;
            }
        }
        td.SetAlphamaps(0,0,alpha);
        var go=Terrain.CreateTerrainGameObject(td);go.name="Terrain_MM_Blueprint_Realistic_x3";go.transform.SetParent(parent);
        go.transform.position=new Vector3(-TerrainSize/2f,TerrainBaseY,-TerrainSize/2f);
        var terrain=go.GetComponent<Terrain>();terrain.drawInstanced=true;terrain.heightmapPixelError=1.5f;terrain.basemapDistance=1600f;
        terrain.detailObjectDistance=185f;terrain.detailObjectDensity=1f;terrain.treeDistance=1650f;terrain.treeBillboardDistance=260f;
        terrain.treeCrossFadeLength=22f;terrain.treeMaximumFullLODCount=180;
        BuildGrassDetails(terrain,tiles,e.grassPrefab);EditorUtility.SetDirty(td);return terrain;
    }
'''
s=re.sub(r'    static Terrain BuildTerrain\(Transform parent, EnvAssets e\).*?\n    static bool HasNearbyForbiddenTile',terrain+'\n    static bool HasNearbyForbiddenTile',s,flags=re.S)

grass=r'''
    static void BuildGrassDetails(Terrain terrain, byte[] tiles, GameObject grassPrefab)
    {
        if(!grassPrefab)return;EnsureHydroRoadMasks(tiles);var td=terrain.terrainData;td.SetDetailResolution(512,16);
        var lush=new DetailPrototype{prototype=grassPrefab,usePrototypeMesh=true,renderMode=DetailRenderMode.VertexLit,minWidth=.34f,maxWidth=.72f,minHeight=.38f,maxHeight=.92f,noiseSpread=.31f,healthyColor=new Color(.39f,.58f,.24f),dryColor=new Color(.48f,.43f,.21f)};
        var meadow=new DetailPrototype{prototype=grassPrefab,usePrototypeMesh=true,renderMode=DetailRenderMode.VertexLit,minWidth=.24f,maxWidth=.52f,minHeight=.58f,maxHeight=1.18f,noiseSpread=.47f,healthyColor=new Color(.31f,.50f,.20f),dryColor=new Color(.57f,.50f,.27f)};
        td.detailPrototypes=new[]{lush,meadow};int[,] dense=new int[512,512],tall=new int[512,512];
        for(int y=0;y<512;y++)for(int x=0;x<512;x++)
        {
            float sx=(N-1)-x/511f*(N-1),sy=y/511f*(N-1);byte t=TileAtSource(tiles,sx,sy);float slope=td.GetSteepness(x/511f,y/511f);
            float water=SampleImprovedWaterMask(tiles,sx,sy),roadDist=RoadDistanceSource(tiles,sx,sy);
            bool ok=water<.12f&&roadDist>.72f&&!IsDirt(t)&&slope<34f;if(!ok)continue;
            float n=Mathf.PerlinNoise(sx*.093f+5.7f,sy*.087f+1.9f),patch=Mathf.PerlinNoise(sx*.037f+18.4f,sy*.041f+3.2f);
            dense[y,x]=Mathf.Clamp(Mathf.RoundToInt(6f+n*5.5f+patch*2f),5,13);
            if(n>.30f&&slope<24f)tall[y,x]=n>.72f?5:(n>.50f?3:2);
        }
        td.SetDetailLayer(0,0,0,dense);td.SetDetailLayer(0,0,1,tall);
    }
'''
s=re.sub(r'    static void BuildGrassDetails\(Terrain terrain, byte\[] tiles, GameObject grassPrefab\).*?\n    static bool\[,] BuildArchitectureProtectionMask',grass+'\n    static bool[,] BuildArchitectureProtectionMask',s,flags=re.S)

water=r'''
    static GameObject BuildWater(Transform parent,Terrain terrain,Material material)
    {
        byte[] tiles=File.ReadAllBytes(TilePath);EnsureHydroRoadMasks(tiles);int grid=WaterGrid;float step=TerrainSize/grid,origin=-TerrainSize*.5f;
        var verts=new List<Vector3>(900000);var uvs=new List<Vector2>(900000);var tris=new List<int>(1200000);const float th=.48f;
        Vector2 Edge(Vector2 a,Vector2 b,float va,float vb){float u=Mathf.Abs(vb-va)<.0001f?.5f:Mathf.Clamp01((th-va)/(vb-va));return Vector2.Lerp(a,b,u);}
        void Emit(List<Vector2> poly)
        {
            if(poly.Count<3)return;int start=verts.Count;
            foreach(var q in poly){verts.Add(new Vector3(q.x,.12f,q.y));uvs.Add(new Vector2((q.x-origin)*.018f,(q.y-origin)*.018f));}
            for(int k=1;k<poly.Count-1;k++){tris.Add(start);tris.Add(start+k+1);tris.Add(start+k);}
        }
        for(int y=0;y<grid;y++)for(int x=0;x<grid;x++)
        {
            float x0=origin+x*step,x1=x0+step,z0=origin+y*step,z1=z0+step;
            Vector2[] p4={new Vector2(x0,z0),new Vector2(x1,z0),new Vector2(x1,z1),new Vector2(x0,z1)};float[] v=new float[4];int bits=0;
            for(int i=0;i<4;i++){Vector2 src=WorldToSource(p4[i].x,p4[i].y);v[i]=SampleImprovedWaterMask(tiles,src.x,src.y);if(v[i]>=th)bits|=1<<i;}
            if(bits==0)continue;if(bits==15){Emit(new List<Vector2>(p4));continue;}
            if((bits==5||bits==10)&&((v[0]+v[1]+v[2]+v[3])*.25f)<th)
            {
                for(int i=0;i<4;i++)if((bits&(1<<i))!=0){int prev=(i+3)%4,next=(i+1)%4;Emit(new List<Vector2>{p4[i],Edge(p4[i],p4[next],v[i],v[next]),Edge(p4[prev],p4[i],v[prev],v[i])});}
                continue;
            }
            var poly=new List<Vector2>(8);
            for(int i=0;i<4;i++){int j=(i+1)%4;bool a=v[i]>=th,b=v[j]>=th;if(a)poly.Add(p4[i]);if(a!=b)poly.Add(Edge(p4[i],p4[j],v[i],v[j]));}
            Emit(poly);
        }
'''
water+=r'''
        string meshPath="Assets/World/NewSorpigal/Generated/WaterSurface.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,meshPath);}mesh.Clear();mesh.indexFormat=IndexFormat.UInt32;
        mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.SetUVs(0,uvs);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        if(material)
        {
            material.renderQueue=3000;if(material.HasProperty("_ShallowColor"))material.SetColor("_ShallowColor",new Color(.08f,.58f,.67f,.50f));
            if(material.HasProperty("_DeepColor"))material.SetColor("_DeepColor",new Color(.02f,.16f,.30f,.84f));
            if(material.HasProperty("_DepthRange"))material.SetFloat("_DepthRange",11f);if(material.HasProperty("_FoamDepth"))material.SetFloat("_FoamDepth",.55f);
        }
        var go=new GameObject("Water - Smoothed Original River Pond Shore");go.transform.SetParent(parent);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=material;mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;
        Debug.Log($"NS_WATER_SMOOTH verts={verts.Count} tris={tris.Count/3}");return go;
    }
'''
s=re.sub(r'    static GameObject BuildWater\(Transform parent, Terrain terrain, Material material\).*?\n    static bool IsSpecialPlacement',water+'\n    static bool IsSpecialPlacement',s,flags=re.S)

spot=r'''
    static bool IsSuitableNaturalSpot(Terrain terrain,Transform architectureRoot,float x,float z,bool tree,bool requireGrass)
    {
        if(tileCache==null)tileCache=File.ReadAllBytes(TilePath);EnsureHydroRoadMasks(tileCache);
        Vector3 tp=terrain.transform.position;float nx=(x-tp.x)/terrain.terrainData.size.x,nz=(z-tp.z)/terrain.terrainData.size.z;
        if(nx<.01f||nz<.01f||nx>.99f||nz>.99f)return false;
        Vector2 src=WorldToSource(x,z);byte t=TileAtSource(tileCache,src.x,src.y);float water=SampleImprovedWaterMask(tileCache,src.x,src.y);
        float roadClear=RoadDistanceSource(tileCache,src.x,src.y);if(water>.16f||roadClear<(tree?1.18f:.68f))return false;
        if(requireGrass&&IsDirt(t))return false;
        if(tree)
        {
            float edge=Mathf.Max(Mathf.Max(SampleImprovedWaterMask(tileCache,src.x+.45f,src.y),SampleImprovedWaterMask(tileCache,src.x-.45f,src.y)),Mathf.Max(SampleImprovedWaterMask(tileCache,src.x,src.y+.45f),SampleImprovedWaterMask(tileCache,src.x,src.y-.45f)));
            if(edge>.28f)return false;
        }
        float steep=terrain.terrainData.GetSteepness(nx,nz);if(steep>(tree?31f:43f))return false;
        if(NearArchitecture(architectureRoot,x,z,tree?3.5f:1.4f))return false;return true;
    }
'''
s=re.sub(r'    static bool IsSuitableNaturalSpot\(Terrain terrain, Transform architectureRoot, float x, float z, bool tree, bool requireGrass\).*?\n    static GameObject PickTreePrefab',spot+'\n    static GameObject PickTreePrefab',s,flags=re.S)

riverside=r'''
    static int BuildRiverside(Terrain terrain,Transform architectureRoot,Transform rocks,Transform understory,EnvAssets e)
    {
        if(tileCache==null)tileCache=File.ReadAllBytes(TilePath);EnsureHydroRoadMasks(tileCache);int added=0;
        var roots=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Ground/Roots/Roots_System_01_Prefab.prefab");
        var debris=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Environment/Ground/Sticks_debris/sticks_debris_00_prefab.prefab");
        int[] dx={1,-1,0,0},dy={0,0,1,-1};
        for(int y=1;y<N-1&&added<260;y++)for(int x=1;x<N-1&&added<260;x++) if(correctedWaterCache[y,x])
        {
            for(int k=0;k<4&&added<260;k++)
            {
                int lx=x+dx[k],ly=y+dy[k];if(correctedWaterCache[ly,lx])continue;
                int seed=150000+y*521+x*31+k*7;if(Mathf.Abs(seed)%6!=0)continue;
                float sx=x+dx[k]*.82f+(Deterministic01(seed*13)-.5f)*.28f,sy=y+dy[k]*.82f+(Deterministic01(seed*17)-.5f)*.28f;
                Vector2 w=SourceToWorld(sx,sy);if(!IsSuitableNaturalSpot(terrain,architectureRoot,w.x,w.y,false,false))continue;
                int choice=Mathf.Abs(seed)%4;GameObject pf=null;Material mat=null;float ph=.6f;
                if(choice==0){pf=e.botdFernPrefab;mat=e.botdFernMaterial;ph=Mathf.Lerp(.45f,.95f,Deterministic01(seed*19));}
                else if(choice==1){pf=e.botdShrubPrefab;mat=e.botdShrubMaterial;ph=Mathf.Lerp(.85f,1.7f,Deterministic01(seed*19));}
                else if(choice==2){pf=e.botdCloverPrefab;mat=e.botdCloverMaterial;ph=Mathf.Lerp(.22f,.48f,Deterministic01(seed*19));}
                else {pf=e.botdGrassPrefab;mat=e.botdGrassMaterial;ph=Mathf.Lerp(.35f,.78f,Deterministic01(seed*19));}
                if(pf){SpawnPlant(pf,understory,mat,w.x,w.y,SampleTerrainY(terrain,w.x,w.y),seed,ph,"WaterEdge");added++;}
                if(seed%29==0&&e.botdRockPrefab){Vector2 rw=SourceToWorld(sx+dx[k]*.35f,sy+dy[k]*.35f);if(IsSuitableNaturalSpot(terrain,architectureRoot,rw.x,rw.y,false,false)){SpawnPlant(e.botdRockPrefab,rocks,e.rockMaterial,rw.x,rw.y,SampleTerrainY(terrain,rw.x,rw.y),seed+1,Mathf.Lerp(.4f,1.25f,Deterministic01(seed*23)),"WaterRock");added++;}}
            }
        }
        return added;
    }
'''
s=re.sub(r'    static int BuildRiverside\(Terrain terrain,Transform architectureRoot,Transform rocks,Transform understory,EnvAssets e\).*?\n    static void NormalizeTreeUpright',riverside+'\n    static void NormalizeTreeUpright',s,flags=re.S)

# Remove unused fake-river water material helpers if they survived and are no longer referenced.
s=s.replace('        RenderNewSorpigalRiverCheck.RunNow();\n','')
p.write_text(s,encoding='utf-8')
print('PATCH_NS_WATER_ROADS_DONE')

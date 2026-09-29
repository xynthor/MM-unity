from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\MMSourceGridExactPostPass.cs')
s=p.read_text(encoding='utf-8')
start=s.index('    static float ShoreExtensionY')
end=s.index('    static int CountSourceModels', start)
block=r'''    static bool EdgeLand(byte[] tilemap,byte[] groups,byte[] sem,bool north,float along)
    {
        float x=north?along:255.5f;
        float z=north?-255.5f:along;
        Vector2 src=WorldToSource(x,z);
        int sx=Mathf.Clamp(Mathf.RoundToInt(src.x),0,N-1);
        int sy=Mathf.Clamp(Mathf.RoundToInt(src.y),0,N-1);
        byte raw=tilemap[sy*N+sx],f=sem[raw];
        return (f&1)==0;
    }

    static float ShoreExtensionY(float inner,float d,bool land)
    {
        if(!land) return Mathf.Lerp(Mathf.Min(inner,-.65f),-4.5f,Mathf.SmoothStep(0f,1f,d/64f));
        if(d<=18f) return Mathf.Lerp(inner,.20f,Mathf.SmoothStep(0f,1f,d/18f));
        return Mathf.Lerp(.20f,-4.5f,Mathf.SmoothStep(0f,1f,(d-18f)/46f));
    }
''block+=r'''
    static GameObject MakeEdgeBed(Zone z,Transform parent,Terrain terrain,byte[] tilemap,byte[] groups,byte[] sem,bool north,Material sand)
    {
        const int alongN=128,across=16;const float cell=4f;
        var v=new List<Vector3>((alongN+1)*(across+1));var uv=new List<Vector2>(v.Capacity);var tr=new List<int>(alongN*across*6);
        for(int j=0;j<=across;j++) for(int i=0;i<=alongN;i++)
        {
            float a=-256f+i*cell,d=j*cell;
            float x=north?a:256f+d,zp=north?-256f-d:a;
            float sampleX=north?Mathf.Clamp(a,-255.5f,255.5f):255.5f;
            float sampleZ=north?-255.5f:Mathf.Clamp(a,-255.5f,255.5f);
            float inner=terrain.SampleHeight(new Vector3(sampleX,0f,sampleZ))+terrain.transform.position.y;
            bool land=EdgeLand(tilemap,groups,sem,north,a);
            v.Add(new Vector3(x,ShoreExtensionY(inner,d,land),zp));uv.Add(new Vector2(i/16f,j/4f));
        }
        int row=alongN+1;for(int j=0;j<across;j++)for(int i=0;i<alongN;i++){int a=j*row+i,b=a+1,d=(j+1)*row+i,c=d+1;tr.Add(a);tr.Add(c);tr.Add(b);tr.Add(a);tr.Add(d);tr.Add(c);}
        string edge=north?"North":"West";string dir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(dir);
        var mesh=SaveMeshAsset($"{dir}/OuterShore_{edge}.asset",v,tr,uv);var go=new GameObject("Outer Shore + Seabed - "+edge);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=sand;return go;
    }
'''
block+=r'''
    static GameObject MakeEdgeWater(Zone z,Transform parent,byte[] tilemap,byte[] groups,byte[] sem,bool north,Material water)
    {
        const int alongN=128,across=16;const float cell=4f;
        var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
        for(int j=0;j<across;j++)for(int i=0;i<alongN;i++)
        {
            float a0=-256f+i*cell,a1=a0+cell,d0=j*cell,d1=d0+cell,am=(a0+a1)*.5f;
            bool land=EdgeLand(tilemap,groups,sem,north,am);if(land&&d1<=20f)continue;
            float x00=north?a0:256f+d0,z00=north?-256f-d0:a0;
            float x10=north?a1:256f+d0,z10=north?-256f-d0:a1;
            float x11=north?a1:256f+d1,z11=north?-256f-d1:a1;
            float x01=north?a0:256f+d1,z01=north?-256f-d1:a0;
            int b=v.Count;v.Add(new Vector3(x00,.12f,z00));v.Add(new Vector3(x10,.12f,z10));v.Add(new Vector3(x11,.12f,z11));v.Add(new Vector3(x01,.12f,z01));
            uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.one);uv.Add(Vector2.up);tr.Add(b);tr.Add(b+2);tr.Add(b+1);tr.Add(b);tr.Add(b+3);tr.Add(b+2);
        }
        string edge=north?"North":"West";string dir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(dir);
        var mesh=SaveMeshAsset($"{dir}/OuterWater_{edge}.asset",v,tr,uv);var go=new GameObject("Outer Ocean - "+edge);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=water;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return go;
    }
'''
block+=r'''
    static GameObject MakeOceanQuad(Zone z,Transform parent,string name,float x0,float x1,float z0,float z1,Material water)
    {
        var v=new List<Vector3>{new Vector3(x0,.12f,z0),new Vector3(x1,.12f,z0),new Vector3(x1,.12f,z1),new Vector3(x0,.12f,z1)};
        var uv=new List<Vector2>{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};var tr=new List<int>{0,2,1,0,3,2};string dir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(dir);
        var mesh=SaveMeshAsset($"{dir}/{name.Replace(" ","")}.asset",v,tr,uv);var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=water;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return go;
    }

    static void BuildOuterShoreExtensions(Zone z,Transform root,Terrain terrain,byte[] tilemap,byte[] groups,byte[] sem)
    {
        // In the linked world reference, geographic north is the row containing Sweet Water/Kriegspire/etc.
        // Geographic west is the column containing Sweet Water/Paradise Valley/Hermit's Isle.
        // Extend outward from those source edges: north = -Z, west = +X.
        bool north=z.key=="EelInfestedWaters"||z.key=="SilverCove"||z.key=="FrozenHighlands"||z.key=="Kriegspire"||z.key=="SweetWater";
        bool west=z.key=="HermitsIsle"||z.key=="ParadiseValley"||z.key=="SweetWater";
        var old=root.Find("Outer Ocean + Shore Extension");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);if(!north&&!west)return;
        var g=new GameObject("Outer Ocean + Shore Extension");g.transform.SetParent(root,false);
        var sand=SurfaceMaterial("OuterShoreSand","Assets/EnvironmentAssets/Biomes/sand.png");var water=SurfaceMaterial("OuterOcean","",true);
        if(north){MakeEdgeBed(z,g.transform,terrain,tilemap,groups,sem,true,sand);MakeEdgeWater(z,g.transform,tilemap,groups,sem,true,water);}
        if(west){MakeEdgeBed(z,g.transform,terrain,tilemap,groups,sem,false,sand);MakeEdgeWater(z,g.transform,tilemap,groups,sem,false,water);}
        if(north&&west)MakeOceanQuad(z,g.transform,"Outer Ocean - NW Corner",256f,320f,-320f,-256f,water);
        Debug.Log($"OUTER_SHORE_EXTENSION {z.display} north={north} west={west} coastAware=true width=64m");
    }

'''
s=s[:start]+block+s[end:]
s=s.replace('        BuildOuterShoreExtensions(z,root.transform,terrain);','        BuildOuterShoreExtensions(z,root.transform,terrain,tilemap,groups,sem);')
p.write_text(s,encoding='utf-8')

q=Path(r'C:\MMUnityPort\Assets\Editor\BuildEnrothLinkedOpenWorld.cs')
t=q.read_text(encoding='utf-8')
t=t.replace('new Layout{id="Dragon Isle",scene="DragonIsle_Reference",col=3,row=3},','new Layout{id="Dragon Isle",scene="DragonIsle_Reference",col=5,row=-1},')
t=t.replace('Dragon Isle is a separate 512m reference tile north-west of Sweet Water.','Dragon Isle is a separate 512m reference tile visually north-west of Sweet Water in the linked editor layout.')
t=t.replace('DragonIsle=NW_OF_SWEETWATER','DragonIsle=VISUAL_NW_OF_SWEETWATER')
q.write_text(t,encoding='utf-8')
print('patched coast and dragon')

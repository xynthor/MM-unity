from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\MMSourceGridExactPostPass.cs')
s=p.read_text(encoding='utf-8')
start=s.index('    static GameObject MakeEdgeShore(')
end=s.index('    static GameObject MakeOceanQuad(',start)
new=r'''    static bool EdgeLand(Zone z,bool north,int i)
    {
        string data=$"Assets/World/{z.key}/Data";
        byte[] tile=File.ReadAllBytes(data+"/tilemap_u8.bin");
        byte[] sem=File.ReadAllBytes(data+"/tile_semantics_u8.bin");
        int sx=north?Mathf.Clamp(127-i,0,127):127;
        int sy=north?127:Mathf.Clamp(i,0,127);
        byte f=sem[tile[sy*N+sx]];
        return (f&1)==0;
    }

    static GameObject MakeEdgeShore(Zone z,Transform parent,Terrain terrain,bool north,Material sand)
    {
        const int along=128,across=12;const float cell=4f;
        var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
        for(int i=0;i<along;i++)
        {
            bool land=EdgeLand(z,north,i)||EdgeLand(z,north,Mathf.Min(127,i+1));
            if(!land)continue;
            float a0=-256f+i*cell,a1=a0+cell;
            for(int j=0;j<across;j++)
            {
                float d0=j*cell,d1=(j+1)*cell;
                int b=v.Count;
                float sx0=north?Mathf.Clamp(a0,-255.5f,255.5f):-255.5f;
                float sz0=north?255.5f:Mathf.Clamp(a0,-255.5f,255.5f);
                float sx1=north?Mathf.Clamp(a1,-255.5f,255.5f):-255.5f;
                float sz1=north?255.5f:Mathf.Clamp(a1,-255.5f,255.5f);
                float h0=terrain.SampleHeight(new Vector3(sx0,0f,sz0))+terrain.transform.position.y;
                float h1=terrain.SampleHeight(new Vector3(sx1,0f,sz1))+terrain.transform.position.y;
                v.Add(new Vector3(north?a0:-256f-d0,ShoreExtensionY(h0,d0),north?256f+d0:a0));
                v.Add(new Vector3(north?a1:-256f-d0,ShoreExtensionY(h1,d0),north?256f+d0:a1));
                v.Add(new Vector3(north?a1:-256f-d1,ShoreExtensionY(h1,d1),north?256f+d1:a1));
                v.Add(new Vector3(north?a0:-256f-d1,ShoreExtensionY(h0,d1),north?256f+d1:a0));
                uv.Add(new Vector2(0,j));uv.Add(new Vector2(1,j));uv.Add(new Vector2(1,j+1));uv.Add(new Vector2(0,j+1));
                tr.Add(b);tr.Add(b+2);tr.Add(b+1);tr.Add(b);tr.Add(b+3);tr.Add(b+2);
            }
        }
        if(v.Count==0)return null;
        string edge=north?"North":"West";string dir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(dir);
        var mesh=SaveMeshAsset($"{dir}/OuterShore_{edge}.asset",v,tr,uv);var go=new GameObject("Outer Shore - "+edge);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=sand;return go;
    }

'''
s=s[:start]+new+s[end:]
p.write_text(s,encoding='utf-8')
print('shore mask patched')

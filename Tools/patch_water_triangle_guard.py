from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMInternalCoastPass.cs")
s=p.read_text(encoding="utf-8-sig")
needle='''        string dir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(dir);string path=$"{dir}/InternalWaterSmooth.asset";
        var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!m){m=new Mesh();AssetDatabase.CreateAsset(m,path);}m.Clear();m.indexFormat=IndexFormat.UInt32;
        m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);'''
repl='''        // Final triangle-level guard: reject any triangle whose centroid rises above land
        // or falls outside source water/shore. This eliminates edge-fan spill onto terrain.
        var kept=new List<int>(tr.Count);
        for(int i=0;i<tr.Count;i+=3)
        {
            Vector3 ca=v[tr[i]],cb=v[tr[i+1]],cc=v[tr[i+2]];
            Vector3 cp=(ca+cb+cc)/3f;
            bool source=SourceWaterOrShore(tile,sem,cp.x,cp.z);
            float gy=terrain.SampleHeight(new Vector3(cp.x,0f,cp.z))+terrain.transform.position.y;
            if(!source||gy>WaterY+.08f)continue;
            kept.Add(tr[i]);kept.Add(tr[i+1]);kept.Add(tr[i+2]);
        }
        tr=kept;
        string dir=$"Assets/World/{z.key}/Generated";Directory.CreateDirectory(dir);string path=$"{dir}/InternalWaterSmooth.asset";
        var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!m){m=new Mesh();AssetDatabase.CreateAsset(m,path);}m.Clear();m.indexFormat=IndexFormat.UInt32;
        m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tr,0);m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);'''
if needle not in s: raise SystemExit("mesh save needle missing")
p.write_text(s.replace(needle,repl,1),encoding="utf-8")
print("WATER_TRIANGLE_GUARD_PATCHED")

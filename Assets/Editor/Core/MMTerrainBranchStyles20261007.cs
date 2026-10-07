using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

// Branch-local, one-tile-at-a-time terrain editing. Never changes water assets or splats.
public static class MMTerrainBranchStyles20261007
{
    public static Terrain[] Tiles()=>Terrain.activeTerrains.OrderBy(t=>t.transform.position.z).ThenBy(t=>t.transform.position.x).ToArray();
    static float S(float a,float b,float x)=>MMNorthAuthoredTools20261007.S(a,b,x);
    public static string Key(Terrain t)=>((int)t.transform.position.x)+"_"+((int)t.transform.position.z);
    static bool Dressing(Transform t)=>t.name.StartsWith("Rock_")||t.name.StartsWith("Bush_")||t.name.Contains("AuthoredPine")||t.name.StartsWith("Pine_")||t.name.StartsWith("YoungPine_")||t.name.StartsWith("SourceTree_")||t.name.StartsWith("Eco_")||t.name.Contains("TracePine")||t.name.Contains("TransitionPine")||t.name.Contains("Conifer_");
    static float[,] Blur(float[,] h,int radius)
    {
        int n=h.GetLength(0);var a=new float[n,n];var b=new float[n,n];
        for(int z=0;z<n;z++)for(int x=0;x<n;x++){float sum=0,w=0;for(int k=-radius;k<=radius;k++){float q=radius+1-Mathf.Abs(k);sum+=h[z,Mathf.Clamp(x+k,0,n-1)]*q;w+=q;}a[z,x]=sum/w;}
        for(int z=0;z<n;z++)for(int x=0;x<n;x++){float sum=0,w=0;for(int k=-radius;k<=radius;k++){float q=radius+1-Mathf.Abs(k);sum+=a[Mathf.Clamp(z+k,0,n-1),x]*q;w+=q;}b[z,x]=sum/w;}return b;
    }
    public static float[,] Roads(TerrainData d)
    {
        int n=d.alphamapResolution;var a=d.GetAlphamaps(0,0,n,n);var mask=new float[513,513];var layers=d.terrainLayers;
        var slots=Enumerable.Range(0,layers.Length).Where(i=>layers[i]&&(layers[i].name.ToLower().Contains("road")||(layers[i].diffuseTexture&&layers[i].diffuseTexture.name.ToLower().Contains("road")))).ToArray();
        for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){int az=Mathf.Min(n-1,z*n/512),ax=Mathf.Min(n-1,x*n/512);foreach(int k in slots)mask[z,x]=Mathf.Max(mask[z,x],a[az,ax,k]);}
        // Preserve an eight-metre shoulder around painted roads.
        var wide=(float[,])mask.Clone();for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)if(mask[z,x]>.08f)for(int dz=-8;dz<=8;dz+=2)for(int dx=-8;dx<=8;dx+=2)if(z+dz>=0&&z+dz<=512&&x+dx>=0&&x+dx<=512)wide[z+dz,x+dx]=Mathf.Max(wide[z+dz,x+dx],mask[z,x]*(1-S(3,10,Mathf.Sqrt(dx*dx+dz*dz))));return wide;
    }
    static List<Bounds> Protected(Terrain t)
    {
        var list=new List<Bounds>();var area=new Bounds(t.transform.position+new Vector3(256,160,256),new Vector3(512,400,512));
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>())
        {
            if(!r.enabled||!r.gameObject.activeInHierarchy||!r.bounds.Intersects(area))continue;
            string n=r.name.ToLower();bool water=r.sharedMaterial&&r.sharedMaterial.shader.name.Contains("Water");
            // Inland elevated meshes are protected including banks; sea/low lakes are protected by elevation.
            if(water)continue;
            if(n.Contains("bridge")||n.Contains("castle")||n.Contains("building")||n.Contains("house")||n.Contains("tower")||n.Contains("temple")||n.Contains("gate")||n.Contains("road")){var b=r.bounds;if(b.size.x<150&&b.size.z<150){b.Expand(new Vector3(16,100,16));list.Add(b);}}
        }
        return list;
    }
    static bool InXZ(Bounds b,float x,float z)=>x>=b.min.x&&x<=b.max.x&&z>=b.min.z&&z<=b.max.z;
    static float[,] WaterDistance(Terrain t)
    {
        var d=new float[513,513];for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)d[z,x]=1000;
        Vector3 origin=t.transform.position;
        foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>())
        {
            if(!r.enabled||!r.gameObject.activeInHierarchy||!r.sharedMaterial||!r.sharedMaterial.shader.name.Contains("Water")||r.bounds.max.y<1)continue;
            var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;var v=f.sharedMesh.vertices;var tri=f.sharedMesh.triangles;
            for(int k=0;k<tri.Length;k+=3){var a=f.transform.TransformPoint(v[tri[k]])-origin;var b=f.transform.TransformPoint(v[tri[k+1]])-origin;var c=f.transform.TransformPoint(v[tri[k+2]])-origin;
                int x0=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x)))),x1=Mathf.Min(512,Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x)))),z0=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.z,Mathf.Min(b.z,c.z)))),z1=Mathf.Min(512,Mathf.CeilToInt(Mathf.Max(a.z,Mathf.Max(b.z,c.z))));
                float den=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(den)<.00001f)continue;
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/den,w=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/den;if(u>=-.01f&&w>=-.01f&&u+w<=1.01f)d[z,x]=0;}
            }
        }
        for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){if(z>0)d[z,x]=Mathf.Min(d[z,x],d[z-1,x]+1);if(x>0)d[z,x]=Mathf.Min(d[z,x],d[z,x-1]+1);if(z>0&&x>0)d[z,x]=Mathf.Min(d[z,x],d[z-1,x-1]+1.4142f);}
        for(int z=512;z>=0;z--)for(int x=512;x>=0;x--){if(z<512)d[z,x]=Mathf.Min(d[z,x],d[z+1,x]+1);if(x<512)d[z,x]=Mathf.Min(d[z,x],d[z,x+1]+1);if(z<512&&x<512)d[z,x]=Mathf.Min(d[z,x],d[z+1,x+1]+1.4142f);}return d;
    }
    public static void Tile(int index,bool northern,bool keep=false,bool revisit=false)
    {
        var scene=SceneManager.GetActiveScene();if(Application.isPlaying||scene.isDirty||scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Expected clean Enroth in Edit Mode");
        var t=Tiles()[index];string key=Key(t),mode=northern?"Northern":"Canonical",stamp="Validation/TerrainBranches20261007/"+mode+"/"+key+".txt";
        if(File.Exists(stamp)&&!revisit)throw new Exception("Tile already kept; no cumulative application");
        var original=t.terrainData;var h=original.GetHeights(0,0,513,513);var current=(float[,])h.Clone();if(revisit){var loaded=UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget("Backups/TerrainBranches20261007/"+mode+"/"+key+"/Terrain.asset");var baseData=loaded.OfType<TerrainData>().First();h=baseData.GetHeights(0,0,513,513);foreach(var o in loaded)if(o&&!EditorUtility.IsPersistent(o))UnityEngine.Object.DestroyImmediate(o);}var smooth=Blur(h,northern?3:12);var candidate=(float[,])h.Clone();var roads=Roads(original);var zones=Protected(t);var waterDistance=WaterDistance(t);var origin=t.transform.position;
        float maximum=0;int changed=0;var properties=new List<Tuple<Transform,Vector3>>();var meshCopies=new List<Tuple<MeshFilter,UnityEngine.Mesh,UnityEngine.Mesh>>();var clone=UnityEngine.Object.Instantiate(original);clone.hideFlags=HideFlags.HideAndDontSave;bool saved=false;
        try
        {
            for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)
            {
                float y=h[z,x]*320-24,wx=origin.x+x,wz=origin.z+z;
                if(!northern&&origin.x==-1792&&(origin.z==256||origin.z==768)&&!(wz>=500&&wz<=1260&&(wz>=900||wx< -1435)))continue;
                float edge=S(10,northern?44:72,Mathf.Min(Mathf.Min(x,512-x),Mathf.Min(z,512-z)));
                float w=edge*S(northern?12:24,northern?32:52,y)*(1-S(.03f,.12f,roads[z,x]))*S(9,35,waterDistance[z,x]);
                foreach(var b in zones){float dx=Mathf.Max(b.min.x-wx,Mathf.Max(0,wx-b.max.x)),dz=Mathf.Max(b.min.z-wz,Mathf.Max(0,wz-b.max.z));w*=S(0,28,Mathf.Sqrt(dx*dx+dz*dz));}if(w==0)continue;
                float target;
                if(!northern){float broad=smooth[z,x]*320-24;target=24+(Mathf.Max(24,broad)-24)*.58f;}
                else
                {
                    // Derive crests and gullies from the source geography instead of inventing macro hills.
                    float low=smooth[z,x]*320-24,relief=y-low;
                    float ribs=Mathf.Sin(wx*.095f+wz*.047f+2*Mathf.Sin(wz*.017f));
                    float massif=S(18,60,y);float crest=1-Mathf.Abs(Mathf.Sin(wx*.018f+wz*.009f+1.3f*Mathf.Sin(wz*.012f)));
                    target=y+Mathf.Clamp(relief*1.55f,-5,9)+massif*(Mathf.Max(0,y-25)*.55f+crest*11+ribs*2.3f);
                }
                candidate[z,x]=Mathf.Clamp01((Mathf.Lerp(y,target,w)+24)/320);float delta=Mathf.Abs(candidate[z,x]-h[z,x])*320;maximum=Mathf.Max(maximum,delta);if(delta>.001f)changed++;
            }
            clone.SetHeights(0,0,candidate);t.terrainData=clone;t.GetComponent<TerrainCollider>().terrainData=clone;t.Flush();
            foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>())
            {
                var p=tr.position;if(!Dressing(tr)||p.x<origin.x||p.x>=origin.x+512||p.z<origin.z||p.z>=origin.z+512)continue;
                if(tr.GetComponentsInParent<Transform>().Skip(1).Any(Dressing))continue;
                float u=(p.x-origin.x)/512,v=(p.z-origin.z)/512;float delta=clone.GetInterpolatedHeight(u,v)-original.GetInterpolatedHeight(u,v);if(Mathf.Abs(delta)<.001f)continue;
                properties.Add(Tuple.Create(tr,p));tr.position+=Vector3.up*delta;
            }
            // Refit the accepted rock surfaces to the edited collider, preserving their triangulation.
            foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>())
            {
                if(!(f.name.StartsWith("North Alpine Rock Surface")||f.name.StartsWith("DragonRockSurface"))||Vector3.Distance(f.transform.position,origin)>.1f)continue;
                var old=f.sharedMesh;var m=UnityEngine.Object.Instantiate(old);m.name=old.name.Replace("(Clone)","");var vertices=m.vertices;
                for(int k=0;k<vertices.Length;k++){int x=Mathf.Clamp(Mathf.RoundToInt(vertices[k].x),0,512),z=Mathf.Clamp(Mathf.RoundToInt(vertices[k].z),0,512);vertices[k].y+=(candidate[z,x]-current[z,x])*320;}
                m.vertices=vertices;m.RecalculateNormals();m.RecalculateBounds();f.sharedMesh=m;meshCopies.Add(Tuple.Create(f,old,m));
            }
            string dir="Preview/TerrainBranches20261007/"+mode+"/"+key+"/";Directory.CreateDirectory(dir);Capture(t,dir);
            if(keep)
            {
                string backup="Backups/TerrainBranches20261007/"+mode+"/"+key;Directory.CreateDirectory(backup);string path=AssetDatabase.GetAssetPath(original);if(!File.Exists(backup+"/Terrain.asset"))File.Copy(path,backup+"/Terrain.asset");
                original.SetHeights(0,0,candidate);EditorUtility.SetDirty(original);AssetDatabase.SaveAssetIfDirty(original);t.terrainData=original;t.GetComponent<TerrainCollider>().terrainData=original;
                foreach(var item in meshCopies){EditorUtility.CopySerialized(item.Item3,item.Item2);item.Item1.sharedMesh=item.Item2;EditorUtility.SetDirty(item.Item2);AssetDatabase.SaveAssetIfDirty(item.Item2);}
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");saved=true;
                Directory.CreateDirectory(Path.GetDirectoryName(stamp));File.WriteAllText(stamp,"tile="+index+" key="+key+" mode="+mode+" changed="+changed+" maxDelta="+maximum+" movedDressing="+properties.Count+" roadAndBanksProtected=True alphaChanged=False");
            }
            Debug.Log("STYLE_TILE "+index+" "+mode+" changed="+changed+" delta="+maximum+" saved="+saved);
        }
        finally
        {
            t.terrainData=original;t.GetComponent<TerrainCollider>().terrainData=original;t.Flush();UnityEngine.Object.DestroyImmediate(clone);
            foreach(var item in meshCopies){item.Item1.sharedMesh=item.Item2;UnityEngine.Object.DestroyImmediate(item.Item3);}if(!saved)foreach(var p in properties)if(p.Item1)p.Item1.position=p.Item2;
        }
    }
    public static void Capture(Terrain t,string dir)
    {
        var p=t.transform.position;float x=p.x+256,z=p.z+256;
        MMNorthAuthoredTools20261007.Capture(dir,"top",new Vector3(x,900,z),new Vector3(x,0,z),true);
        MMNorthAuthoredTools20261007.Capture(dir,"oblique",new Vector3(x-300,250,z-430),new Vector3(x,35,z+40),false);
        MMNorthAuthoredTools20261007.Capture(dir,"transition",new Vector3(x+320,100,p.z-110),new Vector3(x,25,p.z+110),false);
    }
}

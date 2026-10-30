using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Smooth in world space, across tile boundaries, with common protection masks.
public static class MMTerrainSeamBlend20261007
{
    static float S(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
    static float Sample(float[,] h,float x,float z){x=Mathf.Clamp(x,0,h.GetLength(1)-1);z=Mathf.Clamp(z,0,h.GetLength(0)-1);int ix=Mathf.Min(h.GetLength(1)-2,(int)x),iz=Mathf.Min(h.GetLength(0)-2,(int)z);return Mathf.Lerp(Mathf.Lerp(h[iz,ix],h[iz,ix+1],x-ix),Mathf.Lerp(h[iz+1,ix],h[iz+1,ix+1],x-ix),z-iz);}
    public static void Run(bool save=false)
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(Application.isPlaying||scene.isDirty)throw new Exception("Expected clean Edit Mode");
        if(File.Exists("Validation/TerrainBranches20261007/SeamBlend/result.txt"))throw new Exception("Seam pass already saved; do not apply cumulatively");
        var ts=MMTerrainBranchContrast20261007.Tiles();
        int ox=(int)ts.Min(t=>t.transform.position.x),oz=(int)ts.Min(t=>t.transform.position.z);
        int nx=(int)ts.Max(t=>t.transform.position.x)-ox+513,nz=(int)ts.Max(t=>t.transform.position.z)-oz+513;
        var grid=new float[nz,nx];var weight=new float[nz,nx];
        for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)weight[z,x]=1;
        var old=new Dictionary<Terrain,float[,]>();var candidate=new Dictionary<Terrain,float[,]>();
        foreach(var t in ts)
        {
            var d=t.terrainData;var h=d.GetHeights(0,0,513,513);old[t]=h;
            var roads=MMTerrainBranchContrast20261007.Roads(d);var water=MMTerrainBranchContrast20261007.WaterDistance(t);var zones=MMTerrainBranchContrast20261007.Protected(t);
            int tx=(int)t.transform.position.x-ox,tz=(int)t.transform.position.z-oz;
            for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)
            {
                float y=h[z,x]*320-24,wx=ox+tx+x,wz=oz+tz+z;
                grid[tz+z,tx+x]=h[z,x];
                float w=S(1,12,y)*(1-S(.01f,.05f,roads[z,x]))*S(9,55,water[z,x]);
                foreach(var b in zones){float dx=Mathf.Max(b.min.x-wx,Mathf.Max(0,wx-b.max.x)),dz=Mathf.Max(b.min.z-wz,Mathf.Max(0,wz-b.max.z));w*=S(0,28,Mathf.Sqrt(dx*dx+dz*dz));}
                weight[tz+z,tx+x]=Mathf.Min(weight[tz+z,tx+x],w);
            }
        }
        var temp=new float[nz,nx];var blur=new float[nz,nx];
        for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){float v=0;for(int k=-3;k<=3;k++)v+=grid[z,Mathf.Clamp(x+k*8,0,nx-1)]*(4-Mathf.Abs(k));temp[z,x]=v/16;}
        for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){float v=0;for(int k=-3;k<=3;k++)v+=temp[Mathf.Clamp(z+k*8,0,nz-1),x]*(4-Mathf.Abs(k));blur[z,x]=v/16;}
        float max=0;int count=0;
        foreach(var t in ts)
        {
            int tx=(int)t.transform.position.x-ox,tz=(int)t.transform.position.z-oz;var h=old[t];var next=(float[,])h.Clone();
            for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)
            {
                int gx=tx+x,gz=tz+z;float seam=1000;
                for(int b=512;b<nx-1;b+=512)seam=Mathf.Min(seam,Mathf.Abs(gx-b));
                for(int b=512;b<nz-1;b+=512)seam=Mathf.Min(seam,Mathf.Abs(gz-b));
                // Broad foothill blend eliminates tile-shaped walls; northern micro-spikes are softened too.
                float north=S(700,820,gz+oz),wx=gx+ox,wz=gz+oz;
                float w=Mathf.Max(1-S(28,88,seam),.30f+.36f*north);
                w*=weight[gz,gx]*S(0,32,Mathf.Min(Mathf.Min(gx,nx-1-gx),Mathf.Min(gz,nz-1-gz)));
                float dx=north*18*(Mathf.PerlinNoise(wx*.018f+19,wz*.018f+71)-.5f),dz=north*18*(Mathf.PerlinNoise(wx*.019f+95,wz*.019f+39)-.5f);
                float detail=north*S(20,70,h[z,x]*320-24)*(5*(Mathf.PerlinNoise(wx*.067f+31,wz*.067f+17)-.5f)+2*(Mathf.PerlinNoise(wx*.143f+61,wz*.143f+57)-.5f));
                next[z,x]=Mathf.Lerp(h[z,x],Sample(blur,gx+dx,gz+dz)+detail/320,w);max=Mathf.Max(max,Mathf.Abs(next[z,x]-h[z,x])*320);
            }
            candidate[t]=next;
        }
        double beforeJump=0,afterJump=0;int joins=0,samples=0;float seamGap=0;
        foreach(var a in ts)foreach(var b in ts){var p=b.transform.position-a.transform.position;bool east=p==new Vector3(512,0,0),north=p==new Vector3(0,0,512);if(!east&&!north)continue;joins++;for(int k=1;k<512;k++){var ah=old[a];var bh=old[b];var an=candidate[a];var bn=candidate[b];beforeJump+=Mathf.Abs(east?(ah[k,512]-ah[k,504])-(bh[k,8]-bh[k,0]):(ah[512,k]-ah[504,k])-(bh[8,k]-bh[0,k]))*40;afterJump+=Mathf.Abs(east?(an[k,512]-an[k,504])-(bn[k,8]-bn[k,0]):(an[512,k]-an[504,k])-(bn[8,k]-bn[0,k]))*40;seamGap=Mathf.Max(seamGap,Mathf.Abs(east?an[k,512]-bn[k,0]:an[512,k]-bn[0,k])*320);samples++;}}
        Directory.CreateDirectory("Validation/TerrainBranches20261007/SeamBlend");File.WriteAllText("Validation/TerrainBranches20261007/SeamBlend/slope_qa.txt","joins="+joins+" gap="+seamGap+" meanSlopeJumpBefore="+beforeJump/samples+" meanSlopeJumpAfter="+afterJump/samples);
        var moved=new Dictionary<Transform,Vector3>();var meshes=new Dictionary<Mesh,Vector3[]>();
        try
        {
            foreach(var t in ts){if(save){string backupDir="Backups/TerrainBranches20261007/SeamBlend/"+MMTerrainBranchContrast20261007.Key(t);Directory.CreateDirectory(backupDir);string path=AssetDatabase.GetAssetPath(t.terrainData);if(!File.Exists(backupDir+"/Terrain.asset"))File.Copy(path,backupDir+"/Terrain.asset");}t.terrainData.SetHeights(0,0,candidate[t]);t.Flush();}
            foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>())
            {
                Func<Transform,bool> dressing=a=>a.name.StartsWith("Rock_")||a.name.StartsWith("AuthoredBoulder_")||a.name.StartsWith("Bush_")||a.name.Contains("Pine")||a.name.StartsWith("SourceTree_")||a.name.StartsWith("Eco_")||a.name.Contains("Conifer_");
                if(!dressing(tr)||tr.GetComponentsInParent<Transform>().Skip(1).Any(dressing))continue;
                var p=tr.position;var t=ts.FirstOrDefault(a=>p.x>=a.transform.position.x&&p.x<a.transform.position.x+512&&p.z>=a.transform.position.z&&p.z<a.transform.position.z+512);if(!t)continue;
                float delta=Delta(t,p,old[t]);if(Mathf.Abs(delta)<.001f)continue;moved[tr]=p;tr.position+=Vector3.up*delta;
            }
            foreach(var f in UnityEngine.Object.FindObjectsByType<MeshFilter>())
            {
                if(!(f.name.StartsWith("North Alpine Rock Surface")||f.name.StartsWith("DragonRockSurface")||f.name.StartsWith("Northern World Rock "))||!f.sharedMesh)continue;
                var m=f.sharedMesh;if(meshes.ContainsKey(m))continue;var v=m.vertices;meshes[m]=(Vector3[])v.Clone();
                for(int i=0;i<v.Length;i++){var p=f.transform.TransformPoint(v[i]);var t=ts.FirstOrDefault(a=>p.x>=a.transform.position.x-.01f&&p.x<=a.transform.position.x+512.01f&&p.z>=a.transform.position.z-.01f&&p.z<=a.transform.position.z+512.01f);if(t)v[i].y+=Delta(t,p,old[t]);}
                m.vertices=v;m.RecalculateNormals();m.RecalculateBounds();
            }
            string dir="Preview/TerrainBranches20261007/SeamBlend/";Directory.CreateDirectory(dir);
            MMNorthAuthoredTools20261007.Capture(dir,"north",new Vector3(-480,230,600),new Vector3(-250,70,1030),false);
            MMNorthAuthoredTools20261007.Capture(dir,"north_join",new Vector3(-810,190,510),new Vector3(-700,65,900),false);
            MMNorthAuthoredTools20261007.Capture(dir,"dragon",new Vector3(-2030,150,650),new Vector3(-1525,40,1050),false);
            if(save){foreach(var t in ts){EditorUtility.SetDirty(t.terrainData);AssetDatabase.SaveAssetIfDirty(t.terrainData);count++;}foreach(var m in meshes.Keys){EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);}EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Directory.CreateDirectory("Validation/TerrainBranches20261007/SeamBlend");File.WriteAllText("Validation/TerrainBranches20261007/SeamBlend/result.txt","tiles="+count+" maximumHeightChange="+max+" movedDressing="+moved.Count+" rockMeshes="+meshes.Count+" biomesUnchanged=True");}
            Debug.Log("SEAM_BLEND saved="+save+" maximumDelta="+max+" terrainCount="+ts.Length);
        }
        finally
        {
            if(!save){foreach(var t in ts){t.terrainData.SetHeights(0,0,old[t]);t.Flush();}foreach(var p in moved)p.Key.position=p.Value;foreach(var p in meshes){p.Key.vertices=p.Value;p.Key.RecalculateNormals();p.Key.RecalculateBounds();}}
        }
    }
    static float Delta(Terrain t,Vector3 p,float[,] h)
    {
        float x=Mathf.Clamp(p.x-t.transform.position.x,0,512),z=Mathf.Clamp(p.z-t.transform.position.z,0,512);int ix=Mathf.Min(511,(int)x),iz=Mathf.Min(511,(int)z);
        float before=Mathf.Lerp(Mathf.Lerp(h[iz,ix],h[iz,ix+1],x-ix),Mathf.Lerp(h[iz+1,ix],h[iz+1,ix+1],x-ix),z-iz)*320;
        return t.terrainData.GetInterpolatedHeight(x/512,z/512)-before;
    }
    public static void PruneCliffTrees()
    {
        if(Application.isPlaying)throw new Exception("Expected Edit Mode");
        var ts=Terrain.activeTerrains;var removed=new List<string>();
        foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>())
        {
            if(!tr.name.StartsWith("AuthoredPine_")&&!tr.name.StartsWith("DragonAuthoredPine_")&&!tr.name.StartsWith("TransitionPine_"))continue;
            if(!tr.gameObject.activeInHierarchy)continue;var p=tr.position;
            var t=ts.FirstOrDefault(a=>p.x>=a.transform.position.x&&p.x<a.transform.position.x+512&&p.z>=a.transform.position.z&&p.z<a.transform.position.z+512);if(!t)continue;
            if(t.terrainData.GetSteepness((p.x-t.transform.position.x)/512,(p.z-t.transform.position.z)/512)<=38)continue;
            Undo.RecordObject(tr.gameObject,"Disable conifer on cliff face");removed.Add(tr.name+" "+p);tr.gameObject.SetActive(false);
        }
        Directory.CreateDirectory("Validation/TerrainBranches20261007/SeamBlend");File.WriteAllLines("Validation/TerrainBranches20261007/SeamBlend/cliff_trees.txt",removed);
        if(removed.Count>0){var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        Debug.Log("CLIFF_CONIFERS_DISABLED="+removed.Count);
    }
}

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using static MMNorthAuthoredTools20261007;

public static class MMNorthLakeJunction20261007
{
    const string WaterPath="Assets/World/WorldExtensions/Generated/NorthReference20261001/NorthInlandIcyWater20261006.asset";
    static float Cross(Vector3 a,Vector3 b,Vector3 p){return (b.x-a.x)*(p.z-a.z)-(b.z-a.z)*(p.x-a.x);}
    static List<Vector3> Clip(List<Vector3> p,Vector3 a,Vector3 b,float sign,bool inside)
    {
        var output=new List<Vector3>();if(p.Count==0)return output;
        for(int i=0;i<p.Count;i++)
        {
            var u=p[i];var v=p[(i+1)%p.Count];float du=Cross(a,b,u)*sign,dv=Cross(a,b,v)*sign;
            bool iu=inside?du>=0:du<=0,iv=inside?dv>=0:dv<=0;
            if(iu)output.Add(u);if(iu!=iv)output.Add(Vector3.Lerp(u,v,du/(du-dv)));
        }
        return output;
    }
    static List<List<Vector3>> Subtract(List<Vector3> polygon,Vector3 a,Vector3 b,Vector3 c)
    {
        float sign=Mathf.Sign(Cross(a,b,c));var pieces=new List<List<Vector3>>();var inside=polygon;
        foreach(var edge in new[]{new[]{a,b},new[]{b,c},new[]{c,a}})
        {
            var outside=Clip(inside,edge[0],edge[1],sign,false);if(outside.Count>=3)pieces.Add(outside);
            inside=Clip(inside,edge[0],edge[1],sign,true);if(inside.Count<3)break;
        }
        return pieces;
    }
    static bool InsideLake(Vector3 p,Vector3[] v)
    {
        bool inside=false;for(int i=1,j=72;i<=72;j=i++){var a=v[j];var b=v[i];if((a.z>p.z)!=(b.z>p.z)&&p.x<(b.x-a.x)*(p.z-a.z)/(b.z-a.z)+a.x)inside=!inside;}return inside;
    }
    static Vector3 At(Vector3[] v,int start,int count,float q)
    {
        float f=Mathf.Clamp01(q)*(count-1);int i=Mathf.Min((int)f,count-2);return Vector3.Lerp((v[start+2*i]+v[start+2*i+1])*.5f,(v[start+2*i+2]+v[start+2*i+3])*.5f,f-i);
    }
    static float LakeEdgeDistance(float x,float z,Vector3[] vertices)
    {
        float distance=float.MaxValue;var p=new Vector2(x,z);
        for(int j=1,previous=72;j<=72;previous=j++)
        {var a=new Vector2(vertices[previous].x,vertices[previous].z);var b=new Vector2(vertices[j].x,vertices[j].z);var ab=b-a;float q=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);distance=Mathf.Min(distance,Vector2.Distance(p,a+q*ab));}
        return distance;
    }
    static void Channel(float x,float z,Vector3[] old,Vector3[] updated,out float dist,out float delta,out float half,out float surface)
    {
        dist=999;delta=0;half=0;surface=0;
        foreach(var range in new[]{new Vector2Int(73,29),new Vector2Int(131,25),new Vector2Int(181,23)})for(int i=0;i<range.y-1;i++)
        {
            int k=range.x+2*i;var a=(old[k]+old[k+1])*.5f;var b=(old[k+2]+old[k+3])*.5f;var ab=new Vector2(b.x-a.x,b.z-a.z);
            float q=Mathf.Clamp01(Vector2.Dot(new Vector2(x-a.x,z-a.z),ab)/ab.sqrMagnitude);
            float d=Vector2.Distance(new Vector2(x,z),new Vector2(a.x+q*ab.x,a.z+q*ab.y));
            if(d<dist){dist=d;surface=Mathf.Lerp(updated[k].y,updated[k+2].y,q);delta=surface-Mathf.Lerp(a.y,b.y,q);half=Mathf.Lerp(Vector3.Distance(old[k],old[k+1]),Vector3.Distance(old[k+2],old[k+3]),q)*.5f;}
        }
    }
    public static void Run(bool keep=false)
    {
        var scene=SceneManager.GetActiveScene();if(scene.isDirty||scene.path!="Assets/Scenes/World/Enroth.unity")throw new Exception("Need clean Enroth");
        if(File.Exists("Validation/SequentialRepair/lake_junction_kept.txt"))throw new Exception("Already saved");
        var water=AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(WaterPath);var originalVertices=water.vertices;if(originalVertices.Length!=227)throw new Exception("Unexpected inland topology");
        var vertices=(Vector3[])originalVertices.Clone();
        foreach(var range in new[]{new Vector2Int(73,29),new Vector2Int(131,25),new Vector2Int(181,23)})
        {
            float mouth=0;for(int i=0;i<=2000;i++){float q=i/2000f;if(!InsideLake(At(vertices,range.x,range.y,q),vertices)){mouth=q;break;}}
            float source=range.x==73?.12f:range.x==131?25.05f:24.55f;
            for(int i=0;i<range.y;i++){int k=range.x+2*i;float q=i/(range.y-1f);float y=Mathf.Lerp(21.05f,source,Mathf.InverseLerp(mouth,1,q));vertices[k].y=y;vertices[k+1].y=y;}
        }
        var newVertices=new List<Vector3>(vertices);var triangles=new List<int>();var originalTriangles=water.triangles;
        // Keep the lake fan, subtract its footprint from each tributary/outlet triangle.
        triangles.AddRange(originalTriangles.Take(216));
        for(int i=216;i<originalTriangles.Length;i+=3)
        {
            var pieces=new List<List<Vector3>>{new List<Vector3>{vertices[originalTriangles[i]],vertices[originalTriangles[i+1]],vertices[originalTriangles[i+2]]}};
            for(int j=0;j<216&&pieces.Count>0;j+=3)
            {
                var next=new List<List<Vector3>>();foreach(var piece in pieces)next.AddRange(Subtract(piece,vertices[originalTriangles[j]],vertices[originalTriangles[j+1]],vertices[originalTriangles[j+2]]));pieces=next;
            }
            foreach(var piece in pieces)for(int j=1;j<piece.Count-1;j++)
            {
                if(Mathf.Abs(Cross(piece[0],piece[j],piece[j+1]))<.0001f)continue;
                int k=newVertices.Count;newVertices.Add(piece[0]);newVertices.Add(piece[j]);newVertices.Add(piece[j+1]);triangles.Add(k);triangles.Add(k+1);triangles.Add(k+2);
            }
        }
        // Clipping interpolates heights across a sloping river triangle. Snap
        // the resulting lake boundary to the lake's exact level.
        for(int k=227;k<newVertices.Count;k++)
        {
            var p=newVertices[k];float distance=float.MaxValue;
            for(int j=1,previous=72;j<=72;previous=j++)
            {
                var a=new Vector2(vertices[previous].x,vertices[previous].z);var b=new Vector2(vertices[j].x,vertices[j].z);var ab=b-a;
                float q=Mathf.Clamp01(Vector2.Dot(new Vector2(p.x,p.z)-a,ab)/ab.sqrMagnitude);
                distance=Mathf.Min(distance,Vector2.Distance(new Vector2(p.x,p.z),a+q*ab));
            }
            if(distance<.05f){p.y=21.05f;newVertices[k]=p;}
        }
        var terrains=Terrain.activeTerrains.Where(t=>t.transform.position.z==768&&t.transform.position.x>=-768&&t.transform.position.x<=256).OrderBy(t=>t.transform.position.x).ToArray();
        var data=terrains.Select(t=>t.terrainData).ToArray();var clones=data.Select(d=>UnityEngine.Object.Instantiate(d)).ToArray();var heights=data.Select(d=>d.GetHeights(0,0,513,513)).ToArray();
        for(int i=0;i<terrains.Length;i++)for(int z=0;z<=512;z++)for(int x=0;x<=512;x++)
        {
            float wx=terrains[i].transform.position.x+x,wz=768+z;
            Channel(wx,wz,originalVertices,vertices,out float d,out float delta,out float half,out float surface);
            if(d>half+35)continue;
            float y=heights[i][z,x]*320-24;y+=delta*(1-S(half+2,half+35,d));
            float mouthDistance=LakeEdgeDistance(wx,wz,vertices);
            float depth=.8f+4.2f*(1-S(0,45,mouthDistance))*(1-S(half*.2f,half,d));
            if(d<half+1)y=Mathf.Min(y,surface-depth);
            heights[i][z,x]=(y+24)/320;
        }
        var filter=UnityEngine.Object.FindObjectsByType<MeshFilter>().First(f=>f.sharedMesh==water);var preview=UnityEngine.Object.Instantiate(water);preview.hideFlags=HideFlags.HideAndDontSave;preview.SetVertices(newVertices);preview.SetTriangles(triangles,0);preview.RecalculateNormals();preview.RecalculateBounds();
        var surfaceMeshes=new List<System.Tuple<UnityEngine.Mesh,UnityEngine.Mesh,MeshFilter>>();var moved=new List<System.Tuple<Transform,Vector3>>();bool saved=false;
        try
        {
            filter.sharedMesh=preview;
            for(int i=0;i<terrains.Length;i++)
            {
                clones[i].hideFlags=HideFlags.HideAndDontSave;clones[i].SetHeights(0,0,heights[i]);terrains[i].terrainData=clones[i];var collider=terrains[i].GetComponent<TerrainCollider>();if(collider)collider.terrainData=clones[i];terrains[i].Flush();
                int square=(int)((terrains[i].transform.position.x+1280)/512);var root=GameObject.Find("North Alpine Rock Surface "+square+" 20261007");var mf=root.GetComponent<MeshFilter>();var mesh=mf.sharedMesh;var duplicate=UnityEngine.Object.Instantiate(mesh);var mv=duplicate.vertices;var normals=duplicate.normals;
                for(int z=0;z<=512;z++)for(int x=0;x<=512;x++){int k=z*513+x;float height=clones[i].GetHeight(x,z);mv[k].y=height+Mathf.Lerp(.04f,.65f,S(3,30,height-24));normals[k]=clones[i].GetInterpolatedNormal(x/512f,z/512f);}
                duplicate.vertices=mv;duplicate.normals=normals;duplicate.RecalculateBounds();mf.sharedMesh=duplicate;surfaceMeshes.Add(System.Tuple.Create(mesh,duplicate,mf));
                foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>().Where(tr=>tr.name.StartsWith("AuthoredPine_")&&tr.position.x>=terrains[i].transform.position.x&&tr.position.x<terrains[i].transform.position.x+512&&tr.position.z>=768&&tr.position.z<=1280))
                {float u=(tr.position.x-terrains[i].transform.position.x)/512,v=(tr.position.z-768)/512;float delta=clones[i].GetInterpolatedHeight(u,v)-data[i].GetInterpolatedHeight(u,v);if(Mathf.Abs(delta)>.001f){moved.Add(System.Tuple.Create(tr,tr.position));tr.position+=Vector3.up*delta;}}
            }
            string dir="Preview/NorthAuthored20261007/LakeJunction/";Directory.CreateDirectory(dir);
            Capture(dir,"top",new Vector3(0,850,1024),new Vector3(0,0,1024),true);
            Capture(dir,"lake",new Vector3(-35,53,920),new Vector3(15,24,1070),false);
            Capture(dir,"outlet",new Vector3(72,42,1120),new Vector3(15,25,1040),false);
            if(keep)
            {
                string backup="Backups/NorthAuthored20261007/LakeJunctionBefore";Directory.CreateDirectory(backup);
                foreach(var path in data.Select(AssetDatabase.GetAssetPath).Concat(surfaceMeshes.Select(m=>AssetDatabase.GetAssetPath(m.Item1))).Concat(new[]{WaterPath,scene.path})){string dst=Path.Combine(backup,Path.GetFileName(path));if(!File.Exists(dst))File.Copy(path,dst);}
                water.SetVertices(newVertices);water.SetTriangles(triangles,0);water.RecalculateNormals();water.RecalculateBounds();EditorUtility.SetDirty(water);AssetDatabase.SaveAssetIfDirty(water);filter.sharedMesh=water;
                for(int i=0;i<terrains.Length;i++){data[i].SetHeights(0,0,heights[i]);EditorUtility.SetDirty(data[i]);AssetDatabase.SaveAssetIfDirty(data[i]);terrains[i].terrainData=data[i];var c=terrains[i].GetComponent<TerrainCollider>();if(c)c.terrainData=data[i];}
                foreach(var item in surfaceMeshes){item.Item1.vertices=item.Item2.vertices;item.Item1.normals=item.Item2.normals;item.Item1.RecalculateBounds();EditorUtility.SetDirty(item.Item1);AssetDatabase.SaveAssetIfDirty(item.Item1);item.Item3.sharedMesh=item.Item1;}
                EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Save failed");saved=true;
                File.WriteAllText("Validation/SequentialRepair/lake_junction_kept.txt","Lake/river levels meet at 21.05; descending outlet begins at lake edge; lake/river triangle overlap removed; channel beds and rock overlays updated. Water vertices="+newVertices.Count);
            }
        }
        finally
        {
            filter.sharedMesh=water;UnityEngine.Object.DestroyImmediate(preview);
            foreach(var item in surfaceMeshes){item.Item3.sharedMesh=item.Item1;UnityEngine.Object.DestroyImmediate(item.Item2);}
            if(!saved)foreach(var item in moved)if(item.Item1)item.Item1.position=item.Item2;
            for(int i=0;i<terrains.Length;i++){terrains[i].terrainData=data[i];var c=terrains[i].GetComponent<TerrainCollider>();if(c)c.terrainData=data[i];terrains[i].Flush();UnityEngine.Object.DestroyImmediate(clones[i]);}
        }
    }
}

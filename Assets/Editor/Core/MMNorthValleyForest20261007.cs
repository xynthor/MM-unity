using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class MMNorthValleyForest20261007
{
    public static List<KeyValuePair<Transform,Vector3>> GroundDressing(Terrain terrain)
    {
        var moved=new List<KeyValuePair<Transform,Vector3>>();
        foreach(var tr in UnityEngine.Object.FindObjectsByType<Transform>())
        {
            var p=tr.position;
            if(p.x<terrain.transform.position.x||p.x>=terrain.transform.position.x+512||p.z<768||p.z>1280)continue;
            if(!(tr.name.StartsWith("Rock_")||tr.name.StartsWith("Bush_")))continue;
            var renderers=tr.GetComponentsInChildren<Renderer>();if(renderers.Length==0)continue;
            Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            float ground=MMNorthAuthoredTools20261007.Ground(terrain,p.x,p.z);
            moved.Add(new KeyValuePair<Transform,Vector3>(tr,p));
            tr.position+=Vector3.up*(ground-bounds.min.y-.08f);
        }
        return moved;
    }
    public static GameObject Preview(Terrain terrain, List<GameObject> hidden, Func<float,float,float,bool> allowed, int seed)
    {
        var origin = terrain.transform.position;
        foreach (var tr in UnityEngine.Object.FindObjectsByType<Transform>())
        {
            var p = tr.position;
            if (p.x < origin.x || p.x >= origin.x + 512 || p.z < 768 || p.z > 1280 || !tr.gameObject.activeInHierarchy) continue;
            var n = tr.name;
            if (!(n.StartsWith("SourceTree_") || n.StartsWith("Eco_") || n.StartsWith("NorthRefForestCluster_") || n.StartsWith("NorthCoastalForest_") || n.StartsWith("Pine_") || n.StartsWith("YoungPine_") || n.StartsWith("TracePine") || n.StartsWith("SQ1TracePine_") || n.StartsWith("SQ2TracePine_") || n.StartsWith("SQ3TracePine_"))) continue;
            hidden.Add(tr.gameObject);
            tr.gameObject.SetActive(false);
        }
        var root = new GameObject("__AuthoredValleyForestPreview");
        root.hideFlags = HideFlags.HideAndDontSave;
        var mature = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TempDragonTemplate/TracePine.prefab");
        var young = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TempDragonTemplate/TraceYoungPine.prefab");
        if (!mature || !young) throw new Exception("Production conifer templates missing");
        var random = new System.Random(seed);
        int count = 0;
        for (float z=777; z<1230; z+=7.4f)
        for (float x=origin.x+8; x<origin.x+505; x+=7.4f)
        {
            float px=x+(float)random.NextDouble()*6-3, pz=z+(float)random.NextDouble()*6-3;
            float y=MMNorthAuthoredTools20261007.Ground(terrain,px,pz);
            float slope=terrain.terrainData.GetSteepness((px-origin.x)/512,(pz-768)/512);
            if (y<1 || y>78 || slope>32 || !allowed(px,pz,y)) continue;
            float altitude=1-MMNorthAuthoredTools20261007.S(43,78,y);
            float patch=.25f+.8f*Mathf.PerlinNoise(px*.019f+8,pz*.019f);
            if (random.NextDouble()>altitude*patch*.96f) continue;
            var source=count%5==0 ? young : mature;
            var go=UnityEngine.Object.Instantiate(source,root.transform);
            go.hideFlags=HideFlags.DontSave;
            go.name="AuthoredPine_"+count++;
            go.transform.position=new Vector3(px,y,pz);
            go.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0)*source.transform.rotation;
            go.transform.localScale=source.transform.localScale*(1.05f+(float)random.NextDouble()*.55f);
            var renderers=go.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0) continue;
            Bounds bounds=renderers[0].bounds;
            foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            go.transform.position+=Vector3.up*(y-bounds.min.y-.12f);
            foreach(var group in go.GetComponentsInChildren<LODGroup>()){var lods=group.GetLODs();if(lods.Length>0){lods[lods.Length-1].screenRelativeTransitionHeight=.0015f;group.SetLODs(lods);}}
        }
        return root;
    }
}



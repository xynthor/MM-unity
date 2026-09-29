using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public static class MMDragonIsleVegetationRepair
{
    const string Scene="Assets/Scenes/DragonIsle_Reference.unity";
    const string Report="Validation/EnvironmentRealism/dragon_isle_vegetation_repair.csv";
    static string F(float v)=>v.ToString("0.######",CultureInfo.InvariantCulture);
    static Bounds B(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }

    [MenuItem("MMUnity/Environment/Repair Dragon Isle Vegetation")]
    public static void Run()
    {
        Directory.CreateDirectory("Validation/EnvironmentRealism");
        var rows=new List<string>{"name,kind,gap_before,gap_after,ratio_before,ratio_after,y_delta,status"};
        var sc=EditorSceneManager.OpenScene(Scene,OpenSceneMode.Single);
        var root=sc.GetRootGameObjects().FirstOrDefault(g=>g.GetComponentInChildren<Terrain>(true));
        var terrain=root?root.GetComponentInChildren<Terrain>(true):null;
        var forest=root?root.transform.Find("Forest - Natural Sparse"):null;
        if(!terrain||!forest)throw new Exception("Dragon terrain/forest missing");
        int moved=0,rotated=0;
        foreach(Transform tr in forest.Cast<Transform>().ToList())
        {
            Vector3 p0=tr.position,s0=tr.localScale;Quaternion r0=tr.rotation;
            Bounds b0=B(tr.gameObject);float ratio0=b0.size.y/Mathf.Max(.05f,Mathf.Max(b0.size.x,b0.size.z));
            bool shrub=tr.name.StartsWith("Natural_shrub_",StringComparison.OrdinalIgnoreCase);
            if((!shrub&&ratio0<.55f)||(shrub&&ratio0<.18f))
            {
                Quaternion best=tr.rotation;float bestRatio=ratio0;
                foreach(var q in new[]{r0*Quaternion.Euler(90,0,0),r0*Quaternion.Euler(-90,0,0),r0*Quaternion.Euler(0,0,90),r0*Quaternion.Euler(0,0,-90)})
                {
                    tr.rotation=q;Bounds bt=B(tr.gameObject);
                    float rr=bt.size.y/Mathf.Max(.05f,Mathf.Max(bt.size.x,bt.size.z));
                    if(rr>bestRatio){bestRatio=rr;best=q;}
                }
                tr.rotation=best;if(Quaternion.Angle(best,r0)>.01f)rotated++;
            }
            Bounds b=B(tr.gameObject);
            float gy=terrain.SampleHeight(new Vector3(b.center.x,0,b.center.z))+terrain.transform.position.y;
            float gap0=b.min.y-gy;
            tr.position+=Vector3.up*(-gap0);
            Bounds b1=B(tr.gameObject);
            float gy1=terrain.SampleHeight(new Vector3(b1.center.x,0,b1.center.z))+terrain.transform.position.y;
            float gap1=b1.min.y-gy1;
            float ratio1=b1.size.y/Mathf.Max(.05f,Mathf.Max(b1.size.x,b1.size.z));
            bool xz=Mathf.Abs(tr.position.x-p0.x)<.0001f&&Mathf.Abs(tr.position.z-p0.z)<.0001f;
            bool scale=(tr.localScale-s0).sqrMagnitude<1e-10f;
            bool ok=xz&&scale&&Mathf.Abs(gap1)<=.06f&&((shrub&&ratio1>=.18f)||(!shrub&&ratio1>=.55f));
            if(Mathf.Abs(tr.position.y-p0.y)>.0001f)moved++;
            if(!ok){tr.position=p0;tr.rotation=r0;tr.localScale=s0;}
            else EditorUtility.SetDirty(tr);
            rows.Add(string.Join(",",new[]{tr.name,shrub?"shrub":"tree",F(gap0),F(gap1),F(ratio0),F(ratio1),F(tr.position.y-p0.y),ok?"PASS":"FAILED"}));
        }
        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc);
        File.WriteAllLines(Report,rows);AssetDatabase.SaveAssets();
        Debug.Log($"MM_DRAGON_VEG_REPAIR moved={moved} rotated={rotated}");
    }
}
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class MMTreeOrientationVisualAudit
{
    sealed class S{public string id,p;public S(string i,string x){id=i;p=x;}}
    static readonly S[] A={
        new S("searsia","Assets/Environment/PolyHaven/Models/searsia_lucida/searsia_lucida_1k.fbx"),
        new S("fir","Assets/Environment/PolyHaven/Models/fir_sapling/fir_sapling_1k.fbx"),
        new S("island1","Assets/Environment/PolyHaven/Downloaded/island_tree_01/island_tree_01_1k.fbx"),
        new S("island2","Assets/Environment/PolyHaven/Downloaded/island_tree_02/island_tree_02_1k.fbx"),
        new S("island3","Assets/Environment/PolyHaven/Downloaded/island_tree_03/island_tree_03_1k.fbx"),
        new S("jacaranda","Assets/Environment/PolyHaven/Downloaded/jacaranda_tree/jacaranda_tree_1k.fbx")
    };
    static Bounds B(GameObject go){
        var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();
        if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);
        var b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;
    }
    static GameObject L(string p){var g=AssetDatabase.LoadAssetAtPath<GameObject>(p);return g?g:AssetDatabase.LoadAllAssetsAtPath(p).OfType<GameObject>().FirstOrDefault();}
    [MenuItem("MMUnity/Debug/Tree Orientation Audit _F2")]
    public static void Run(){
        var sc=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var sh=Shader.Find("Standard");var mat=new Material(sh);mat.color=new Color(.22f,.55f,.18f);
        Quaternion[] qs={Quaternion.identity,Quaternion.Euler(90,0,0),Quaternion.Euler(-90,0,0),Quaternion.Euler(0,0,90),Quaternion.Euler(0,0,-90)};
        string[] qn={"ID","X90","Xm90","Z90","Zm90"};
        for(int r=0;r<A.Length;r++){
            var src=L(A[r].p);if(!src)continue;
            for(int c=0;c<qs.Length;c++){
                var wrap=new GameObject(A[r].id+"_"+qn[c]);wrap.transform.position=new Vector3((c-2)*12,0,r*14);
                var go=(GameObject)PrefabUtility.InstantiatePrefab(src);go.transform.SetParent(wrap.transform,false);go.transform.localRotation=qs[c];
                foreach(var rr in go.GetComponentsInChildren<Renderer>(true)){var aa=rr.sharedMaterials;if(aa==null||aa.Length==0)aa=new Material[1];for(int k=0;k<aa.Length;k++)aa[k]=mat;rr.sharedMaterials=aa;}
                var b=B(wrap);float max=Mathf.Max(.01f,Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z)));wrap.transform.localScale*=9f/max;
                b=B(wrap);wrap.transform.position+=Vector3.up*(-b.min.y);
                var label=new GameObject("LABEL");label.transform.SetParent(wrap.transform,false);label.transform.localPosition=new Vector3(0,1.1f,0);
                var tm=label.AddComponent<TextMesh>();tm.text=A[r].id+" "+qn[c];tm.characterSize=.35f;tm.fontSize=48;tm.anchor=TextAnchor.LowerCenter;tm.alignment=TextAlignment.Center;tm.color=Color.white;
                label.transform.rotation=Quaternion.Euler(90,0,0);
            }
        }
        var light=new GameObject("Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(45,-35,0);
        var camGo=new GameObject("Camera");var cam=camGo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=47;camGo.transform.position=new Vector3(0,70,35);camGo.transform.rotation=Quaternion.Euler(55,180,0);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.16f,.20f);
        Directory.CreateDirectory("C:/MMUnityPort/Validation");
        var rt=new RenderTexture(1600,1200,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1600,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1200),0,0);tex.Apply();
        File.WriteAllBytes("C:/MMUnityPort/Validation/TreeOrientationAudit.png",tex.EncodeToPNG());
        RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(mat);
        Debug.Log("TREE_ORIENTATION_AUDIT_DONE");
    }
}
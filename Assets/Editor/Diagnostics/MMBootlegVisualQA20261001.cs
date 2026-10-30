using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
public static class MMBootlegVisualQA20261001
{
 public static void Run(){
  var s=EditorSceneManager.OpenScene("Assets/Scenes/World/Enroth.unity",OpenSceneMode.Single);
  var r=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Bootleg Bay - LINKED REFERENCE");
  var t=r.GetComponentInChildren<Terrain>(true);
  Vector3 At(float x,float z)=>t.transform.position+new Vector3(x,t.terrainData.GetInterpolatedHeight(x/512f,z/512f),z);
  var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
  var lg=new GameObject("TEMP Bootleg QA Sun");SceneManager.MoveGameObjectToScene(lg,scratch);lg.transform.rotation=Quaternion.Euler(46,320,0);var l=lg.AddComponent<Light>();l.type=LightType.Directional;l.intensity=.75f;
  var cg=new GameObject("TEMP Bootleg QA Camera");SceneManager.MoveGameObjectToScene(cg,scratch);var c=cg.AddComponent<Camera>();c.fieldOfView=58;c.nearClipPlane=.2f;c.farClipPlane=900;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.60f,.72f,.80f);c.allowHDR=false;c.depthTextureMode=DepthTextureMode.Depth;
  var rt=new RenderTexture(1280,800,24);rt.antiAliasing=2;var old=RenderTexture.active;c.targetTexture=rt;
  var lights=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Where(x=>x.type==LightType.Directional).ToArray();var on=lights.Select(x=>x.enabled).ToArray();var am=RenderSettings.ambientMode;var al=RenderSettings.ambientLight;
  try{
   foreach(var x in lights)x.enabled=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.46f,.46f,.46f);
   var q=At(300,390);c.transform.position=q+new Vector3(-190,135,-185);c.transform.LookAt(q+new Vector3(0,3,0));
   c.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,800),0,0);tex.Apply();File.WriteAllBytes("Preview/Reference_BootlegBay_Islands_Oblique.png",tex.EncodeToPNG());Object.DestroyImmediate(tex);
  }finally{c.targetTexture=null;RenderTexture.active=old;RenderSettings.ambientMode=am;RenderSettings.ambientLight=al;for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=on[i];Object.DestroyImmediate(rt);EditorSceneManager.CloseScene(scratch,true);}
 }
}
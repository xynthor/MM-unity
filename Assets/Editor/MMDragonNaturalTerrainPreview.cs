using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class MMDragonNaturalTerrainPreview {
const string SC="Assets/Scenes/__PREVIEW_DragonIsle_Geology.unity";
const string LOG="Validation/CactusDragonAudit/dragon_natural_preview.txt";
static float Step(float a,float b,float x){float u=Mathf.Clamp01((x-a)/(b-a));return u*u*(3-2*u);}
static float Gauss(float x,float z,float cx,float cz,float rx,float rz,float angle=0f){
 float c=Mathf.Cos(angle),s=Mathf.Sin(angle),u=((x-cx)*c+(z-cz)*s)/rx,v=(-(x-cx)*s+(z-cz)*c)/rz;
 return Mathf.Exp(-2.0f*(u*u+v*v));
}
static float Model(float x,float z){
 float backbone=8.2f*Gauss(x,z,-28,-48,25,140,-.26f);
 float secondary=5.5f*Gauss(x,z,16,72,26,72,.16f);
 float shoulder=4.4f*Gauss(x,z,-85,-113,26,64,-.38f);
 float headlands=4.2f*Gauss(x,z,-115,-173,19,25)+5.0f*Gauss(x,z,96,194,23,27)+4.2f*Gauss(x,z,-20,93,20,27);
 float coves=-3.4f*Gauss(x,z,-75,-147,19,36)+-3.0f*Gauss(x,z,47,137,24,33);
 float drainage=-1.7f*Gauss(x,z,-50,-46,5,90,-.27f)-1.2f*Gauss(x,z,7,-140,6,50,.43f);
 float broad=Mathf.PerlinNoise((x+455)*.011f,(z+692)*.011f)-.5f;
 float local=Mathf.PerlinNoise((x+113)*.037f,(z-119)*.037f)-.5f;
 return backbone+secondary+shoulder+headlands+coves+drainage+1.8f*broad+.46f*local;
}
static bool Protected(Bounds[] bs,float x,float z){
 foreach(var b in bs)if(x>=b.min.x-9&&x<=b.max.x+9&&z>=b.min.z-9&&z<=b.max.z+9)return true;
 return false;
}
static Material Foliage(string name,string diffuse,string normal,bool cutout){
 string path="Assets/TempDragonRework/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
 if(m)return m;var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(diffuse);
 if(!tex)throw new Exception("Missing foliage diffuse "+diffuse);
 m=new Material(Shader.Find("Standard")){name=name};m.SetTexture("_MainTex",tex);
 var n=AssetDatabase.LoadAssetAtPath<Texture2D>(normal);if(n){m.SetTexture("_BumpMap",n);m.EnableKeyword("_NORMALMAP");}
 m.color=Color.white;m.SetFloat("_Glossiness",.07f);m.SetFloat("_Metallic",0f);
 if(cutout){m.SetFloat("_Mode",1);m.SetOverrideTag("RenderType","TransparentCutout");
 m.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);
 m.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.Zero);m.SetInt("_ZWrite",1);
 m.EnableKeyword("_ALPHATEST_ON");m.DisableKeyword("_ALPHABLEND_ON");m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
 m.SetFloat("_Cutoff",.38f);m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.AlphaTest;m.SetInt("_Cull",0);}
 AssetDatabase.CreateAsset(m,path);return m;
}
static int FixForest(Transform root){
 var forest=root.Find("Forest - Natural Sparse");if(!forest)return 0;
 var leaf=Foliage("Dragon_Leaves_Cutout","Assets/Environment/PolyHaven/Models/searsia_lucida/textures/searsia_lucida_rgba_1k.png","Assets/Environment/PolyHaven/Models/searsia_lucida/textures/searsia_lucida_nor_gl_1k.exr",true);
 var twig=Foliage("Dragon_Fir_Twigs_Cutout","Assets/Environment/PolyHaven/Models/fir_sapling/textures/fir_sapling_twigs_rgba_1k.png","Assets/Environment/PolyHaven/Models/fir_sapling/textures/fir_sapling_twigs_nor_gl_1k.png",true);
 int repaired=0;
 foreach(var r in forest.GetComponentsInChildren<Renderer>(true)){
 var a=r.sharedMaterials;bool changed=false;
 for(int i=0;i<a.Length;i++){var m=a[i];if(!m)continue;
 if(m.name=="searsia_lucida_leaves"){a[i]=leaf;changed=true;repaired++;}
 else if(m.name=="fir_sapling_twigs"){a[i]=twig;changed=true;repaired++;}
 }
 if(changed)r.sharedMaterials=a;
 }
 return repaired;
}
static void Reground(Transform group,Terrain t){
 if(!group)return;
 var objects=group.name=="Dragon Isle - Geology Preview"
   ?group.GetComponentsInChildren<Transform>(true).Where(o=>o.name.StartsWith("Cliff_Segment_")||o.name.StartsWith("Talus_")||o.name.StartsWith("Understory_"))
   :group.Cast<Transform>();
 foreach(Transform obj in objects){
 var rs=obj.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
 if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
 var pos=obj.position;float y=t.SampleHeight(new Vector3(pos.x,0,pos.z))+t.transform.position.y;
 obj.position+=Vector3.up*(y-b.min.y);
 }
}
[MenuItem("MMUnity/Dragon Isle/Natural terrain and forest PREVIEW")]
public static void Run(){
 Directory.CreateDirectory("Validation/CactusDragonAudit");
 var sc=EditorSceneManager.OpenScene(SC,OpenSceneMode.Single);
 var root=sc.GetRootGameObjects().First(g=>g.GetComponentInChildren<Terrain>(true));
 var t=root.GetComponentInChildren<Terrain>(true);var td=t.terrainData;
 string asset=AssetDatabase.GetAssetPath(td);
 if(asset!="Assets/TempDragonRework/BASE_DragonIsleTerrain.asset")throw new Exception("REFUSE: preview not on isolated TerrainData: "+asset);
 var lb=root.transform.Find("Landmarks - Reference");
 var bounds=lb?lb.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).Select(r=>r.bounds).ToArray():new Bounds[0];
 int n=td.heightmapResolution;var hm=td.GetHeights(0,0,n,n);
 float water=.12f;float deltaMax=0;int edited=0;
 for(int zi=0;zi<n;zi++)for(int xi=0;xi<n;xi++){
 float wx=t.transform.position.x+td.size.x*xi/(n-1f),wz=t.transform.position.z+td.size.z*zi/(n-1f);
 float world=t.transform.position.y+hm[zi,xi]*td.size.y;
 float mask=Step(water+.35f,water+5.4f,world);
 float rim=Mathf.Min(Mathf.Min(xi,zi),Mathf.Min(n-1-xi,n-1-zi))/(float)(n-1);
 mask*=Step(.012f,.065f,rim);
 if(Protected(bounds,wx,wz))mask=0;
 float delta=mask*Model(wx,wz);hm[zi,xi]=Mathf.Clamp01(hm[zi,xi]+delta/td.size.y);
 if(Mathf.Abs(delta)>.05f){edited++;deltaMax=Mathf.Max(deltaMax,Mathf.Abs(delta));}
 }
 td.SetHeights(0,0,hm);t.Flush();
 var lands=td.terrainLayers;int W=td.alphamapWidth,H=td.alphamapHeight;
 int grass=Array.FindIndex(lands,l=>l&&l.name=="Grass"),sand=Array.FindIndex(lands,l=>l&&l.name=="ShoreSand"),
 dirt=Array.FindIndex(lands,l=>l&&l.name=="Dirt"),rock=Array.FindIndex(lands,l=>l&&l.name=="Rock");
 if(grass<0||sand<0||dirt<0||rock<0||lands.Length!=4)throw new Exception("UNEXPECTED TERRAIN LAYERS");
 var alpha=new float[H,W,lands.Length];var counts=new double[lands.Length];int land=0;
 for(int zi=0;zi<H;zi++)for(int xi=0;xi<W;xi++){
 float wx=t.transform.position.x+td.size.x*xi/(W-1f),wz=t.transform.position.z+td.size.z*zi/(H-1f);
 float world=t.transform.position.y+hm[Mathf.RoundToInt(zi*(n-1f)/(H-1f)),Mathf.RoundToInt(xi*(n-1f)/(W-1f))]*td.size.y;
 float dx=(hm[Mathf.RoundToInt(zi*(n-1f)/(H-1f)),Mathf.Min(n-1,xi+1)]-hm[Mathf.RoundToInt(zi*(n-1f)/(H-1f)),Mathf.Max(0,xi-1)])*td.size.y;
 float dz=(hm[Mathf.Min(n-1,zi+1),Mathf.RoundToInt(xi*(n-1f)/(W-1f))]-hm[Mathf.Max(0,zi-1),Mathf.RoundToInt(xi*(n-1f)/(W-1f))])*td.size.y;
 float slope=Mathf.Atan(Mathf.Sqrt(dx*dx+dz*dz)*.5f)*Mathf.Rad2Deg;
 float shore=1f-Step(water+.8f,water+4.4f,world);
 float cliff=Step(15f,35f,slope),hills=Step(15f,52f,world);
 float soft=(Mathf.PerlinNoise((wx+200)*.017f,(wz+418)*.017f)-.5f)*.14f;
 float sheltered=Mathf.Max(Gauss(wx,wz,-60,-94,53,38),Mathf.Max(Gauss(wx,wz,-14,-163,45,27),Gauss(wx,wz,20,-50,46,40)));
 float sw=(1-Step(6f,19f,slope))*shore*1.15f;
 float gw=(1-cliff)*(1-shore)*(.13f+.7f*sheltered)*(1f-.65f*hills)+soft;
 float dw=(1-cliff)*(.23f+.19f*(1-sheltered))*(1f-.35f*shore);
 float rw=.25f+.9f*cliff+.38f*hills+.12f*(1-sheltered);
 if(world<water+.3f){sw=1f;gw=.005f;dw=.008f;rw=.06f;}
 float sum=Mathf.Max(.001f,Mathf.Max(0,sw)+Mathf.Max(0,gw)+Mathf.Max(0,dw)+Mathf.Max(0,rw));
 alpha[zi,xi,sand]=Mathf.Max(0,sw)/sum;alpha[zi,xi,grass]=Mathf.Max(0,gw)/sum;
 alpha[zi,xi,dirt]=Mathf.Max(0,dw)/sum;alpha[zi,xi,rock]=Mathf.Max(0,rw)/sum;
 if(world>water+.55f){land++;for(int k=0;k<lands.Length;k++)counts[k]+=alpha[zi,xi,k];}
 }
 td.SetAlphamaps(0,0,alpha);EditorUtility.SetDirty(td);
 int foliage=FixForest(root.transform);
 Reground(root.transform.Find("Forest - Natural Sparse"),t);
 Reground(root.transform.Find("Dragon Isle - Geology Preview"),t);
 EditorSceneManager.MarkSceneDirty(sc);
 if(!EditorSceneManager.SaveScene(sc,SC))throw new IOException("Preview save failed");
 AssetDatabase.SaveAssets();
 var report=new List<string>{$"terrain={asset}",$"landform_samples={edited}",$"maximum_change_m={deltaMax:F2}",$"foliage_material_slots_repaired={foliage}"};
 for(int i=0;i<lands.Length;i++)report.Add($"land_{lands[i].name}={counts[i]/Mathf.Max(land,1)*100:F1}%");
 File.WriteAllLines(LOG,report);Debug.Log("DRAGON_NATURAL_PREVIEW_DONE "+string.Join(";",report));
}
}
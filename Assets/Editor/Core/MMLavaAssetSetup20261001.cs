using System;
using System.IO;
using UnityEngine;
using UnityEditor;

public static class MMLavaAssetSetup20261001
{
 const string Root="Assets/World/WorldExtensions/Lava/Lava001/";
 const string ColorPath=Root+"Lava001_1K-JPG_Color.jpg";
 const string EmissionPath=Root+"Lava001_1K-JPG_Emission.jpg";
 const string NormalPath=Root+"Lava001_1K-JPG_NormalDX.jpg";
 const string RoughPath=Root+"Lava001_1K-JPG_Roughness.jpg";
 const string DispPath=Root+"Lava001_1K-JPG_Displacement.jpg";
 const string MatPath=Root+"Lava001_Emissive.mat";
 const string LayerPath=Root+"Lava001_Terrain.terrainlayer";

 static void Configure(string path,bool srgb,TextureImporterType type=TextureImporterType.Default){
  var ti=AssetImporter.GetAtPath(path) as TextureImporter;
  if(!ti)throw new Exception("Texture importer missing "+path);
  ti.textureType=type;ti.sRGBTexture=srgb;ti.wrapMode=TextureWrapMode.Repeat;
  ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=8;ti.mipmapEnabled=true;
  ti.maxTextureSize=1024;ti.textureCompression=TextureImporterCompression.CompressedHQ;
  ti.SaveAndReimport();
 }

 [MenuItem("MMUnity/Assets/Setup Lava001 PBR")]
 public static void Run(){
  foreach(var p in new[]{ColorPath,EmissionPath,NormalPath,RoughPath,DispPath})if(!File.Exists(p))throw new FileNotFoundException("Lava map missing",p);
  Configure(ColorPath,true);Configure(EmissionPath,true);Configure(NormalPath,false,TextureImporterType.NormalMap);Configure(RoughPath,false);Configure(DispPath,false);
  var color=AssetDatabase.LoadAssetAtPath<Texture2D>(ColorPath);
  var emission=AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionPath);
  var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
  if(!color||!emission||!normal)throw new Exception("Lava textures failed to import");

  var mat=AssetDatabase.LoadAssetAtPath<Material>(MatPath);
  if(!mat){mat=new Material(Shader.Find("Standard")){name="Lava001_Emissive"};AssetDatabase.CreateAsset(mat,MatPath);}
  mat.shader=Shader.Find("Standard");mat.SetTexture("_MainTex",color);mat.SetTexture("_BumpMap",normal);mat.SetFloat("_BumpScale",.85f);
  mat.SetTexture("_EmissionMap",emission);mat.SetColor("_EmissionColor",new Color(1.7f,.72f,.22f,1f));
  mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_EMISSION");mat.SetFloat("_Metallic",.02f);mat.SetFloat("_Glossiness",.34f);
  mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;mat.enableInstancing=true;EditorUtility.SetDirty(mat);

  var layer=AssetDatabase.LoadAssetAtPath<TerrainLayer>(LayerPath);
  if(!layer){layer=new TerrainLayer{name="Lava001_Terrain"};AssetDatabase.CreateAsset(layer,LayerPath);}
  layer.diffuseTexture=color;layer.normalMapTexture=normal;layer.tileSize=new Vector2(9f,9f);layer.normalScale=.85f;layer.metallic=.02f;layer.smoothness=.30f;
  EditorUtility.SetDirty(layer);
  AssetDatabase.SaveAssets();AssetDatabase.Refresh();
  Directory.CreateDirectory("Validation/EdgeGrid20260923/Lava20261001");
  File.WriteAllText("Validation/EdgeGrid20260923/Lava20261001/setup.txt",
    "PASS source=ambientCG_Lava001 license=CC0 maps=Color,Emission,NormalDX,Roughness,Displacement material="+MatPath+" terrainLayer="+LayerPath+" terrainWrites=0\n");
  Debug.Log("LAVA001_READY "+MatPath+" + "+LayerPath);
 }
}

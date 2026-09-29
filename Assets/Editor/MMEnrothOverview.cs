using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;

public static class MMEnrothOverview {
 const string ScenePath="Assets/Scenes/Enroth_Linked_OpenWorld.unity";
 const string PreviewPath="Assets/Previews/Enroth_Reference_20260923.png";

 [MenuItem("MMUnity/World/Show Enroth Overview",priority=12)]
 public static void Show(){
  var sc=SceneManager.GetActiveScene();
  if(sc.path!=ScenePath){
   if(sc.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  }
  var sv=SceneView.lastActiveSceneView;
  if(!sv)sv=EditorWindow.GetWindow<SceneView>();
  sv.Show();
  sv.in2DMode=false;
  sv.orthographic=true;
  sv.LookAtDirect(new Vector3(-256f,0f,0f),Quaternion.Euler(90f,0f,0f),2250f);
  sv.Focus();
  sv.Repaint();
  SceneView.RepaintAll();
  Debug.Log("ENROTH_OVERVIEW_SHOWING Scene="+sc.path+" preview="+PreviewPath);
 }

 [MenuItem("MMUnity/World/Show NEW Sweet Water and Paradise Western Coast",priority=11)]
 public static void ShowNewWestCoast(){
  var sc=SceneManager.GetActiveScene();
  if(sc.path!=ScenePath){
   if(sc.isDirty&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   sc=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
  }
  var sv=SceneView.lastActiveSceneView;
  if(!sv)sv=EditorWindow.GetWindow<SceneView>();
  sv.Show();sv.in2DMode=false;sv.orthographic=true;
  sv.LookAtDirect(new Vector3(-1280f,0f,256f),Quaternion.Euler(90f,0f,0f),1150f);
  sv.Focus();sv.Repaint();SceneView.RepaintAll();
  Debug.Log("NEW_WESTERN_COAST_FOCUSED SweetWaterWest + ParadiseValleyWest");
 }

 [MenuItem("MMUnity/World/Select NEW Western Coast Preview",priority=12)]
 public static void SelectNewWestPreview(){
  const string path="Assets/Previews/Enroth_NewWesternCoast_20260923.png";
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  if(!tex){AssetDatabase.Refresh();tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
  if(!tex){Debug.LogError("New coast preview not imported: "+path);return;}
  Selection.activeObject=tex;EditorGUIUtility.PingObject(tex);
  Debug.Log("NEW_WEST_COAST_PREVIEW_SELECTED "+path);
 }

 [MenuItem("MMUnity/World/Select Seven Reference Comparison",priority=14)]
 public static void SelectSevenReferenceComparison(){
  const string path="Assets/Previews/Enroth_Seven_Reference_Comparison_20260924.png";
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  if(!tex){AssetDatabase.Refresh();tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);}
  if(!tex){Debug.LogError("Seven-crop reference comparison missing: "+path);return;}
  Selection.activeObject=tex;EditorGUIUtility.PingObject(tex);
  Debug.Log("SEVEN_REFERENCE_COMPARISON_OPEN "+path);
 }

 [MenuItem("MMUnity/World/Select Enroth Reference Preview",priority=13)]
 public static void SelectPreview(){
  var image=AssetDatabase.LoadAssetAtPath<Texture2D>(PreviewPath);
  if(!image){Debug.LogError("Reference preview missing: "+PreviewPath);return;}
  Selection.activeObject=image;
  EditorGUIUtility.PingObject(image);
  Debug.Log("ENROTH_PREVIEW_SELECTED "+PreviewPath);
 }
}

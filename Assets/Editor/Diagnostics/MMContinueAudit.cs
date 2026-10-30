using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class MMContinueAudit
{
    [MenuItem("MMUnity/Continue Full Audit Fix Screens")]
    public static void Run()
    {
        AssetDatabase.Refresh();
        MMSourceGridExactPostPass.ApplyAll();
        AuditOneToOneWorld.Run();
        AuditWorldSourceExact.Run();
        CaptureAuditScreens.Run();
        AssetDatabase.SaveAssets();
        var p="Assets/Scenes/World/Enroth.unity";
        if(File.Exists(Path.GetFullPath(p))) EditorSceneManager.OpenScene(p,OpenSceneMode.Single);
        Debug.Log("MM_CONTINUE_AUDIT_FIX_SCREENS_DONE");
    }
}
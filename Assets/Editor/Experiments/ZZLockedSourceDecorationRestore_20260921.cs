using UnityEditor;
using UnityEngine;
using System;
using System.IO;

[InitializeOnLoad]
public static class ZZLockedSourceDecorationRestore_20260921
{
    const string Done="C:/MMUnityPort/Validation/LOCKED_SOURCE_DECORATION_RESTORE_20260921.done";
    const string Run="C:/MMUnityPort/Validation/LOCKED_SOURCE_DECORATION_RESTORE_20260921.running";
    static ZZLockedSourceDecorationRestore_20260921()
    {
        if(File.Exists(Done)||File.Exists(Run))return;
        File.WriteAllText(Run,DateTime.Now.ToString("O"));
        try{
            MMWorldPositionFreezeAudit.Snapshot();
            MMSourceDecorationRestorePass.ApplyAll();
            MMSourceDecorationRestorePass.AuditAll();
            MMWorldPositionFreezeAudit.Verify();
            File.WriteAllText(Done,DateTime.Now.ToString("O"));
            File.Delete(Run);
            Debug.Log("LOCKED_SOURCE_DECORATION_RESTORE_DONE");
        }catch(Exception e){
            File.WriteAllText("C:/MMUnityPort/Validation/LOCKED_SOURCE_DECORATION_RESTORE_20260921.failed",e.ToString());
            File.Delete(Run);
            Debug.LogException(e);
        }
    }
}
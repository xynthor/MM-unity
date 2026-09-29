from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRestoreOldGoodSorpigalVegetation.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('''    public static void Apply()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);''','''    public static void Apply()
    {
        MMWorldPositionFreezeAudit.Snapshot();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);''',1)
s=s.replace('''        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,ScenePath);AssetDatabase.SaveAssets();
        Debug.Log($"OLD_GOOD_SORPIGAL_VEGETATION_DONE source={source} grove={grove} shrubs={shrubs} ferns={ferns} rejected={rejected} variants={pool.Length}");''','''        EditorSceneManager.MarkSceneDirty(sc);EditorSceneManager.SaveScene(sc,ScenePath);AssetDatabase.SaveAssets();
        MMWorldPositionFreezeAudit.Verify();
        CaptureAuditScreens.Run();
        Debug.Log($"OLD_GOOD_SORPIGAL_VEGETATION_DONE source={source} grove={grove} shrubs={shrubs} ferns={ferns} rejected={rejected} variants={pool.Length}");''',1)
p.write_text(s,encoding="utf-8")
print("LOCKED_SORPIGAL_VEG_WRAPPED")
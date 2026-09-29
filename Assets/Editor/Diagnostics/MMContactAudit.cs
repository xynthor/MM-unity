using UnityEditor;

public static class MMContactAudit
{
    [MenuItem("MMUnity/Audit Building Ground And Water Contact")]
    public static void Run()
    {
        MMWorldComprehensiveAudit.Run();
    }
}

using UnityEditor;

public static class AuditOneToOneWorld
{
    [MenuItem("MMUnity/Audit Full One To One World")]
    public static void Run()
    {
        MMWorldComprehensiveAudit.Run();
    }
}

using UnityEditor;
internal class CommandScript:IRunCommand{public void Execute(ExecutionResult result){AssetDatabase.Refresh();result.Log("Player builder refresh requested");}}

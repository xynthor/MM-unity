using UnityEditor;
internal class CommandScript:IRunCommand{public void Execute(ExecutionResult result){AssetDatabase.Refresh();result.Log("Hero QA compile requested");}}

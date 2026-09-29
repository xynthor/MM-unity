using UnityEditor;
internal class CommandScript:IRunCommand{public void Execute(ExecutionResult result){UnityEditor.LogEntries.Clear();int errors=0,warnings=0,logs=0;UnityEditor.LogEntries.GetCountsByType(ref errors,ref warnings,ref logs);result.Log("Console errors="+errors+" warnings="+warnings);}}

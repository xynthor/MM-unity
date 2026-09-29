using UnityEngine;using UnityEditor;using System.IO;
internal class CommandScript:IRunCommand{public void Execute(ExecutionResult result){Debug.ClearDeveloperConsole();File.WriteAllText("Validation/StructuralQA/console_clear_attempt.txt","Debug.ClearDeveloperConsole executed");AssetDatabase.Refresh();result.Log("Editor refresh requested");}}

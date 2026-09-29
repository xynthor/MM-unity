using UnityEngine;using System.IO;using UnityEngine.SceneManagement;
internal class CommandScript:IRunCommand{public void Execute(ExecutionResult result){Debug.ClearDeveloperConsole();File.WriteAllText("Validation/StructuralQA/console_clear_attempt.txt","Executed Debug.ClearDeveloperConsole; active_scene="+SceneManager.GetActiveScene().path);result.Log("Console clear executed");}}

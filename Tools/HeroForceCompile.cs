using UnityEditor;using UnityEditor.Compilation;
internal class CommandScript:IRunCommand{public void Execute(ExecutionResult result){SessionState.SetBool("HeroMcpQA",false);AssetDatabase.ImportAsset("Assets/Editor/MMHeroMcpPlayQA.cs",ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();EditorApplication.QueuePlayerLoopUpdate();result.Log("Explicit player QA recompile requested");}}


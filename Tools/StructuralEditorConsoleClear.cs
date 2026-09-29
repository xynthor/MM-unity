using Unity.AI.MCP.Editor.Tools;using Unity.AI.MCP.Editor.Tools.Parameters;
internal class CommandScript:IRunCommand{public void Execute(ExecutionResult result){var response=ReadConsole.HandleCommand(new ReadConsoleParams{Action=ConsoleAction.Clear});result.Log(response.ToString());}}

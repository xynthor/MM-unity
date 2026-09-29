using UnityEditor;using UnityEngine;
internal class CommandScript:IRunCommand{public void Execute(ExecutionResult result){EditorGUIUtility.QueueGameViewInputEvent(new Event{type=EventType.KeyDown,keyCode=KeyCode.W});result.Log("Queued gameplay W event");}}

from pathlib import Path
p=Path('Assets/Editor/MMHeroMcpPlayQA.cs');s=p.read_text().replace('"Validation/Hero/','"C:/MMUnityPort/Validation/Hero/')
s=s.replace('EditorApplication.isPlaying=true;}','EditorApplication.delayCall+=()=>EditorApplication.isPlaying=true;}')
s=s.replace('static void Mode(PlayModeStateChange state){','static void Mode(PlayModeStateChange state){File.AppendAllText("C:/MMUnityPort/Validation/Hero/playmode_trace.txt",DateTime.UtcNow+" "+state+" armed="+SessionState.GetBool("HeroMcpQA",false)+"\\n");')
s=s.replace('previous=player.transform.position;rows.Add','previous=player.transform.position;File.AppendAllText("C:/MMUnityPort/Validation/Hero/playmode_trace.txt","Player assigned "+player.name+"\\n");rows.Add')
p.write_text(s)

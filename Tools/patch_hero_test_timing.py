from pathlib import Path
p=Path('Assets/Editor/MMHeroMcpPlayQA.cs');s=p.read_text().replace('EditorApplication.delayCall+=()=>EditorApplication.isPlaying=true;', 'EditorApplication.isPlaying=true;EditorApplication.QueuePlayerLoopUpdate();')
s=s.replace('state==PlayModeStateChange.EnteredPlayMode){','state==PlayModeStateChange.EnteredPlayMode){Application.runInBackground=true;')
s=s.replace('EditorApplication.timeSinceStartup','Time.time')
s=s.replace('if(game)game.SendEvent(', 'if(game){game.Focus();game.SendEvent(').replace('keyCode=key});}', 'keyCode=key});}}').replace('mousePosition=new Vector2(400,300)});}', 'mousePosition=new Vector2(400,300)});}}')
p.write_text(s)

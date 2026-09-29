from pathlib import Path
p=Path('Assets/Editor/MMHeroMcpPlayQA.cs');s=p.read_text()
a=s.index('    static void Key(');b=s.index('    static void Capture(',a)
s=s[:a]+'''    static void Key(KeyCode key,bool down){EditorGUIUtility.QueueGameViewInputEvent(new Event{type=down?EventType.KeyDown:EventType.KeyUp,keyCode=key});}
    static void Mouse(int button,bool down){EditorGUIUtility.QueueGameViewInputEvent(new Event{type=down?EventType.MouseDown:EventType.MouseUp,button=button,mousePosition=new Vector2(400,300)});}
'''+s[b:]
s=s.replace('Record("idle");Key(KeyCode.W,true);','Record("idle");Key(KeyCode.LeftControl,true);Key(KeyCode.W,true);')
s=s.replace('Capture("Moving");Key(KeyCode.LeftShift,true);','Capture("Moving");Key(KeyCode.LeftControl,false);Key(KeyCode.LeftShift,true);')
a=s.index('        case 20:');b=s.index('\n        }',a)
s=s[:a]+'''        case 20:Record("cast");Key(KeyCode.S,true);Key(KeyCode.Q,true);next=Time.time+.15;break;
        case 21:Key(KeyCode.Q,false);Key(KeyCode.S,false);break;
        case 22:Record("dodge_backward");Key(KeyCode.A,true);Key(KeyCode.Q,true);next=Time.time+.15;break;
        case 23:Key(KeyCode.Q,false);Key(KeyCode.A,false);break;
        case 24:Record("dodge_left");Key(KeyCode.D,true);Key(KeyCode.Q,true);next=Time.time+.15;break;
        case 25:Key(KeyCode.Q,false);Key(KeyCode.D,false);break;
        case 26:Record("dodge_right");player.GetComponent<MMHeroCombatController>().PlayHit();break;
        case 27:Record("hit");player.GetComponent<MMHeroCombatController>().PlayDeath();break;
        case 28:Record("death");rows.Add("native_events_observed W="+sawW+" Shift="+sawShift+" Jump="+sawJump);File.WriteAllLines("C:/MMUnityPort/Validation/Hero/mcp_input_test.txt",rows);SessionState.SetBool("HeroMcpQA",false);EditorApplication.isPlaying=false;break;'''+s[b:]
p.write_text(s)

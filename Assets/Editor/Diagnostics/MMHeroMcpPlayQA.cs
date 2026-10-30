using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class MMHeroMcpPlayQA
{
    static MMThirdPersonController player;
    static EditorWindow game;
    static double next;
    static int step;
    static readonly List<string> rows = new List<string>();
    static Vector3 previous;
    static bool sawW, sawShift, sawJump;
    static readonly HashSet<string> states = new HashSet<string>();
    static readonly string[] stateNames={"Locomotion","Jump","Attack 1","Attack 2","Attack 3","Heavy Attack","Kick","Dodge Forward","Dodge Backward","Dodge Left","Dodge Right","Hit","Block","Death","Cast"};
    static MMHeroMcpPlayQA(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Mode;}
    public static void Start(){if(EditorApplication.isPlaying)throw new Exception("Already playing");SessionState.SetBool("HeroMcpQA",true);EditorApplication.isPlaying=true;EditorApplication.QueuePlayerLoopUpdate();}
    static void Mode(PlayModeStateChange state){File.AppendAllText("C:/MMUnityPort/Validation/Hero/playmode_trace.txt",DateTime.UtcNow+" "+state+" armed="+SessionState.GetBool("HeroMcpQA",false)+"\n");if(!SessionState.GetBool("HeroMcpQA",false))return;if(state==PlayModeStateChange.EnteredPlayMode){Application.runInBackground=true;player=UnityEngine.Object.FindObjectsByType<MMThirdPersonController>(FindObjectsSortMode.None).Single(p=>p.gameObject.activeInHierarchy);EditorApplication.ExecuteMenuItem("Window/General/Game");game=EditorWindow.focusedWindow;rows.Clear();states.Clear();step=0;sawW=sawShift=sawJump=false;next=Time.time+3;previous=player.transform.position;File.AppendAllText("C:/MMUnityPort/Validation/Hero/playmode_trace.txt","Player assigned "+player.name+"\n");rows.Add("game_window="+(game?game.GetType().Name:"null"));}if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool("HeroMcpQA",false);}
    static void Key(KeyCode key,bool down){EditorGUIUtility.QueueGameViewInputEvent(new Event{type=down?EventType.KeyDown:EventType.KeyUp,keyCode=key});}
    static void Mouse(int button,bool down){EditorGUIUtility.QueueGameViewInputEvent(new Event{type=down?EventType.MouseDown:EventType.MouseUp,button=button,mousePosition=new Vector2(400,300)});}
    static void Capture(string label){MMSequentialEvidence.Render(player.playerCamera,"C:/MMUnityPort/Validation/Hero/MCP_Game_"+label+".png",1600,900);}
    static void Record(string label){rows.Add(label+"|position="+player.transform.position.ToString("R")+"|delta="+Vector3.Distance(previous,player.transform.position)+"|speed="+player.animator.GetFloat("Speed")+"|grounded="+player.GetComponent<CharacterController>().isGrounded+"|states="+string.Join(";",states));previous=player.transform.position;states.Clear();File.WriteAllLines("C:/MMUnityPort/Validation/Hero/mcp_input_test.txt",rows);}
    static void Tick(){if(!EditorApplication.isPlaying||!player||!SessionState.GetBool("HeroMcpQA",false))return;try{foreach(var n in stateNames)if(player.animator.GetCurrentAnimatorStateInfo(0).IsName(n))states.Add(n);sawW|=Input.GetKey(KeyCode.W);sawShift|=Input.GetKey(KeyCode.LeftShift);sawJump|=Input.GetButtonDown("Jump");if(Time.time<next)return;next=Time.time+1;
        switch(step++){
        case 0:Capture("Idle");Record("idle");Key(KeyCode.LeftControl,true);Key(KeyCode.W,true);break;
        case 1:Record("walk_forward");Capture("Moving");Key(KeyCode.LeftControl,false);Key(KeyCode.LeftShift,true);break;
        case 2:Record("sprint");Key(KeyCode.W,false);Key(KeyCode.LeftShift,false);Key(KeyCode.D,true);break;
        case 3:Record("turn_right");Key(KeyCode.D,false);Key(KeyCode.Space,true);next=Time.time+.15;break;
        case 4:Key(KeyCode.Space,false);Record("jump_start");break;
        case 5:Record("jump_end");Mouse(0,true);next=Time.time+.15;break;
        case 6:Mouse(0,false);next=Time.time+.3;break;
        case 7:Capture("Attack");Record("attack1");Mouse(0,true);next=Time.time+.15;break;
        case 8:Mouse(0,false);next=Time.time+.3;break;
        case 9:Record("attack2");Mouse(0,true);next=Time.time+.15;break;
        case 10:Mouse(0,false);break;
        case 11:Record("attack3");Mouse(1,true);break;
        case 12:Record("block");Mouse(1,false);Key(KeyCode.Q,true);next=Time.time+.15;break;
        case 13:Key(KeyCode.Q,false);break;
        case 14:Record("dodge");Key(KeyCode.F,true);next=Time.time+.15;break;
        case 15:Key(KeyCode.F,false);break;
        case 16:Record("heavy");Key(KeyCode.E,true);next=Time.time+.15;break;
        case 17:Key(KeyCode.E,false);break;
        case 18:Record("kick");Key(KeyCode.R,true);next=Time.time+.15;break;
        case 19:Key(KeyCode.R,false);break;
        case 20:Record("cast");Key(KeyCode.S,true);Key(KeyCode.Q,true);next=Time.time+.15;break;
        case 21:Key(KeyCode.Q,false);Key(KeyCode.S,false);break;
        case 22:Record("dodge_backward");Key(KeyCode.A,true);Key(KeyCode.Q,true);next=Time.time+.15;break;
        case 23:Key(KeyCode.Q,false);Key(KeyCode.A,false);break;
        case 24:Record("dodge_left");Key(KeyCode.D,true);Key(KeyCode.Q,true);next=Time.time+.15;break;
        case 25:Key(KeyCode.Q,false);Key(KeyCode.D,false);break;
        case 26:Record("dodge_right");player.GetComponent<MMHeroCombatController>().PlayHit();break;
        case 27:Record("hit");player.GetComponent<MMHeroCombatController>().PlayDeath();break;
        case 28:Record("death");rows.Add("native_events_observed W="+sawW+" Shift="+sawShift+" Jump="+sawJump);File.WriteAllLines("C:/MMUnityPort/Validation/Hero/mcp_input_test.txt",rows);SessionState.SetBool("HeroMcpQA",false);EditorApplication.isPlaying=false;break;
        }
    }catch(Exception e){File.WriteAllText("C:/MMUnityPort/Validation/Hero/mcp_test_error.txt",e.ToString());SessionState.SetBool("HeroMcpQA",false);EditorApplication.isPlaying=false;}}
}

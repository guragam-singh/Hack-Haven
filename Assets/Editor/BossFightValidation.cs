using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reverbat.Boss;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class BossFightValidation
{
    private const string Pending="Reverbat.BossQA.Pending";
    private static readonly List<string> checks=new List<string>();
    private static readonly HashSet<string> captures=new HashSet<string>(), coverage=new HashSet<string>();
    private static BossFightController f;
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static InputSettings settings;
    private static InputSettings.BackgroundBehavior background;
    private static InputSettings.EditorInputBehaviorInPlayMode focus;
    private static bool running,passed;
    private static int fights;
    private enum Try { Good, Wrong, Timeout, UnrelatedKey }
    static BossFightValidation()
    {
        EditorApplication.playModeStateChanged+=s=>{
            if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))EditorApplication.delayCall+=StartPending;
            if(s==PlayModeStateChange.ExitingPlayMode)Restore();
        };
    }
    [MenuItem("REVERBAT/Validate The Other Half in Play Mode")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)return;
        EditorSceneManager.OpenScene(BossFightSceneBuilder.Path);SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    private static void StartPending()
    {
        if(running || !EditorApplication.isPlaying)return;
        f=UnityEngine.Object.FindAnyObjectByType<BossFightController>();
        if(!f)return;
        running=passed=true;fights=0;checks.Clear();captures.Clear();coverage.Clear();
        // The validation host is separate from the turn controller, whose replay cancels its animations.
        var host=new GameObject("Editor QA driver").AddComponent<BossValidationHost>();
        host.StartCoroutine(Drive(Verify()));
    }
    private static IEnumerator Drive(IEnumerator root)
    {
        // Flatten nested enumerators so a failed deadline always produces a report and releases input.
        var stack=new Stack<IEnumerator>();stack.Push(root);
        while(stack.Count>0)
        {
            bool more=false;object next=null;
            try {var current=stack.Peek();more=current.MoveNext();if(more)next=current.Current;}
            catch(Exception ex){Check(false,ex.Message);break;}
            if(!more){stack.Pop();continue;}
            if(next is IEnumerator nested)stack.Push(nested);else yield return next;
        }
        string summary=(passed?"ALL PASSED":"FAILED")+" | "+checks.Count+" checks | "+fights+" fights | "+Screen.width+"x"+Screen.height;
        Directory.CreateDirectory("Temp/Reverbat");
        File.WriteAllLines("Temp/Reverbat/Boss_"+Screen.width+"x"+Screen.height+".txt",new[]{summary}.Concat(checks));
        Debug.Log("[REVERBAT boss QA] "+summary);SessionState.SetBool(Pending,false);Restore();
    }
    private static void Restore()
    {
        if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
        if(mouse!=null && mouse.added)InputSystem.RemoveDevice(mouse);
        if(settings!=null){settings.backgroundBehavior=background;settings.editorInputBehaviorInPlayMode=focus;}
        keyboard=null;mouse=null;settings=null;running=false;
    }
    private static void Check(bool ok,string label)
    {
        checks.Add((ok?"PASS ":"FAIL ")+label);
        if(!ok){passed=false;Debug.LogError("[REVERBAT boss QA] "+label);}
    }
    private static void Shot(string label)
    {
        if(!captures.Add(label))return;
        Directory.CreateDirectory("Captures");
        ScreenCapture.CaptureScreenshot("Captures/Boss_"+Screen.width+"x"+Screen.height+"_"+label+".png");
    }
    private static IEnumerator Until(Func<bool> condition,string label,float seconds=7)
    {
        float limit=Time.realtimeSinceStartup+seconds;
        while(!condition() && Time.realtimeSinceStartup<limit)yield return null;
        if(!condition())throw new InvalidOperationException("Timeout "+label+"; state="+f.State+"; prompt="+f.Prompt);
    }
    private static IEnumerator Frames(int n){while(n-->0)yield return null;}
    private static IEnumerator Click(Rect rect)
    {
        mouse.MakeCurrent();Vector2 point=f.hud.ToScreen(rect);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});
        yield return new WaitForSecondsRealtime(.06f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});
        yield return new WaitForSecondsRealtime(.055f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});
        yield return Frames(2);
    }
    private static void Keys(params Key[] keys)
    {
        keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    }
    private static IEnumerator Qte(Try how)
    {
        var kind=f.Prompt;
        if(how==Try.Timeout){yield return Until(()=>f.Prompt==Challenge.None,"QTE expiry",5);yield break;}
        if(kind==Challenge.Strike || kind==Challenge.HoldRelease)
        {
            if(kind==Challenge.HoldRelease)
            {
                yield return Frames(2);Keys(Key.Space);
                yield return Until(()=>f.Charging,"brace starts charging");
            }
            if(how==Try.Good)
                yield return Until(()=>f.Prompt!=kind || Mathf.Abs(f.Indicator-f.zoneCenter)<.052f,"blue zone",3);
            if(kind==Challenge.Strike){Keys(Key.Space);yield return Frames(2);Keys();}
            else Keys();
            yield return Until(()=>f.Prompt==Challenge.None,"bar resolves",3);
            yield return Frames(2);yield break;
        }
        int count=f.KeyCount;
        Check(count==(kind==Challenge.Barrage?4:2),"Correct prompt count for "+kind);
        for(int i=0;i<count && f.Prompt==kind;i++)
        {
            yield return Frames(1);
            Key expected=f.RequiredKey;
            Keys(how==Try.Good?expected:how==Try.UnrelatedKey?Key.F:(expected==Key.A?Key.S:Key.A));
            yield return Until(()=>f.KeyIndex!=i || f.Prompt!=kind,"key registered",2);
            if(i==0 && how==Try.Good && f.Prompt==kind){yield return Frames(2);Check(f.KeyIndex==1,"Holding one key advances "+kind+" once");}
            Keys();yield return Frames(2);
            if(how!=Try.Good)break;
        }
    }
    private static IEnumerator Verify()
    {
        settings=InputSystem.settings;background=settings.backgroundBehavior;focus=settings.editorInputBehaviorInPlayMode;
        settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();Keys();
        yield return Frames(2);
        var stage=f.stage;
        Check(stage.cameraRig.lens.GetUniversalAdditionalCameraData().scriptableRenderer is UniversalRenderer,"Boss camera uses forward renderer");
        Check(stage.bat.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>r.enabled) && stage.dictator.GetComponentsInChildren<Renderer>().Any(r=>r.enabled),"Both supplied character prefabs render");
        Check(stage.sounds.Length==16 && stage.sounds.All(c=>c!=null),"All 16 replaceable sound clips assigned");
        Check(!f.ChooseAttack(Attack.EchoMirror),"Intro rejects combat input");
        yield return new WaitForSecondsRealtime(.90f);Shot("Intro");
        yield return Until(()=>f.State==BattleState.PlayerChooseAttack,"intro to choice",5);
        Check(f.BatHP==100 && f.DictatorHP==100 && f.Turn==1,"Fresh encounter starts with both at 100 HP");
        Shot("Combat");
        Vector3 batHome=stage.bat.transform.position,bossHome=stage.dictator.transform.position;
        for(int run=0;run<3;run++)
        {
            var occurrences=new Dictionary<Counter,int>{{Counter.BatSwarm,0},{Counter.BloodSiphon,0}};
            Counter last=Counter.BatSwarm;int streak=0;
            for(int turn=1;turn<=15 && !f.Finished;turn++)
            {
                yield return Until(()=>f.State==BattleState.PlayerChooseAttack,"player choice");
                bool heavy=run==2 || (run==0?turn==2 || turn==4 || turn>=6:turn%2==0);
                Try attempt=run==2?Try.Good:run==1?(turn==1?Try.Good:turn==2?Try.UnrelatedKey:Try.Wrong):
                    turn<=2?Try.Wrong:turn<=4?Try.Timeout:Try.Good;
                int oldBoss=f.DictatorHP,oldBat=f.BatHP,hitCount=f.PlayerHits;
                var bounds=heavy?f.hud.RightChoice:f.hud.LeftChoice;
                yield return Click(bounds);
                Check(f.State==BattleState.PlayerAttackQTE && f.Prompt==(heavy?Challenge.Barrage:Challenge.Strike),"Mouse opens the selected attack QTE");
                Check(!f.ChooseAttack(Attack.EchoMirror) && !f.ChooseDefense(Defense.Brace),"QTE rejects second action");
                Shot(heavy?"BarrageKeys":"MirrorTiming");
                yield return Qte(attempt);
                if(attempt==Try.Good)
                {
                    yield return Until(()=>heavy?stage.fx.Crates.Any(c=>c.gameObject.activeSelf):stage.fx.Mirror.gameObject.activeSelf,"successful animation starts");
                    if(heavy){yield return new WaitForSecondsRealtime(.84f);Shot("Boxes");}
                    else {yield return Until(()=>stage.fx.BeamVisible,"echo beam");yield return new WaitForSecondsRealtime(.36f);Shot("Mirror");}
                }
                else Check(!stage.fx.BeamVisible && !stage.fx.Mirror.gameObject.activeSelf && stage.fx.Crates.All(c=>!c.gameObject.activeSelf),"Failed attack has no successful VFX");
                yield return Until(()=>f.State==BattleState.EnemyTelegraph || f.State==BattleState.PlayerChooseDefense || f.Finished,"player attack resolved");
                int damage=attempt==Try.Good?(heavy?38:22):0;
                Check(f.DictatorHP==Mathf.Max(0,oldBoss-damage) && f.PlayerHits==hitCount+(damage>0?1:0),"Fixed player damage exactly once: "+(heavy?"barrage":"mirror")+" "+attempt);
                Check(!stage.fx.Mirror.gameObject.activeSelf && stage.fx.Crates.All(c=>!c.gameObject.activeSelf) || f.Finished,"Player props clear before enemy turn");
                if(f.Finished)break;
                yield return Until(()=>f.State==BattleState.PlayerChooseDefense,"defense choice");
                Counter enemy=f.Incoming;int seen=++occurrences[enemy];
                streak=enemy==last?streak+1:1;last=enemy;
                Check(streak<=2,"Enemy attack streak at most two");
                Shot(enemy==Counter.BatSwarm?"SwarmTelegraph":"SiphonTelegraph");
                int before=f.DictatorHP,enemyHits=f.EnemyHits;
                Defense correct=enemy==Counter.BatSwarm?Defense.Evade:Defense.Brace;
                bool none=run==1 && turn%3==0;
                bool wrongChoice=(run==0 && seen==3 && oldBat>40) || (run==1 && turn%3==1);
                bool failKeys=(run==0 && seen==2 && oldBat>40) || (run==1 && !wrongChoice && !none);
                bool defended=!none && !wrongChoice && !failKeys;
                if(none)
                {
                    float t=Time.time;
                    yield return Until(()=>f.State==BattleState.EnemyAttackResolution,"no response",5);
                    Check(Time.time-t>=2.7f && f.ChosenDefense==Defense.None,"Three-second choice window expires undefended");
                }
                else
                {
                    Defense choice=wrongChoice?(correct==Defense.Brace?Defense.Evade:Defense.Brace):correct;
                    yield return Click(choice==Defense.Brace?f.hud.LeftChoice:f.hud.RightChoice);
                    Check(f.Prompt==(choice==Defense.Brace?Challenge.HoldRelease:Challenge.Dodge),"Correct defense prompt opens");
                    Shot(choice==Defense.Brace?"BraceTiming":"EvadeKeys");
                    yield return Qte(failKeys?(run==1 && turn==2?Try.Timeout:Try.Wrong):Try.Good);
                }
                if(enemy==Counter.BatSwarm){yield return Until(()=>stage.fx.VisibleSwarm>0,"swarm launched");yield return new WaitForSecondsRealtime(.43f);Shot("Swarm");}
                else {yield return Until(()=>stage.fx.BeamVisible,"siphon visible");yield return new WaitForSecondsRealtime(.27f);Shot(defended?"Barrier":"Siphon");}
                yield return Until(()=>f.Finished || f.State==BattleState.PlayerChooseAttack,"enemy resolved");
                int taken=defended?0:enemy==Counter.BatSwarm?20:18;
                int heal=!defended && enemy==Counter.BloodSiphon?10:0;
                Check(f.BatHP==Mathf.Max(0,oldBat-taken) && f.DictatorHP==Mathf.Min(100,before+heal) && f.EnemyHits==enemyHits+(defended?0:1),
                    enemy+" "+(defended?"blocked":"hit")+" applies one damage/healing event");
                coverage.Add(enemy.ToString()+(defended?" blocked":" hit"));
                if(heal>0 && before<100 && f.DictatorHP>before) coverage.Add("siphon restored missing HP");
                if(none)coverage.Add("no-choice");
                if(wrongChoice)coverage.Add("wrong-defense");
                if(!f.Finished)
                {
                    Check(!stage.fx.BeamVisible && stage.fx.VisibleSwarm==0 && stage.bat.transform.position==batHome && stage.dictator.transform.position==bossHome,"FX clear and actor anchors restored between turns");
                }
            }
            yield return Until(()=>f.ResultReady,"terminal tableau",36);
            bool win=run!=1;
            Check(f.State==(win?BattleState.Victory:BattleState.Defeat),"Encounter reaches "+(win?"victory":"defeat"));
            Check((win?f.DictatorHP:f.BatHP)==0 && f.Prompt==Challenge.None && !f.ChooseAttack(Attack.EchoMirror) && !f.ChooseDefense(Defense.Brace),"Zero HP clamps, QTE closes and terminal state rejects combat input");
            fights++;Shot(win?"Victory":"Defeat");
            if(run<2)
            {
                yield return Click(f.hud.ReplayButton);
                Check(f.State==BattleState.BattleIntro && f.Turn==1 && f.BatHP==100 && f.DictatorHP==100 && f.PlayerHits==0 && f.EnemyHits==0,
                    "Result button resets HP, turn and combat history");
                yield return Click(f.hud.SkipButton);
                yield return Until(()=>f.State==BattleState.PlayerChooseAttack,"skip intro");
                Check(UnityEngine.Object.FindObjectsByType<BossFightController>(FindObjectsInactive.Exclude).Length==1 && !stage.fx.BeamVisible && stage.fx.VisibleSwarm==0,"Replay keeps one controller and clean VFX");
            }
        }
        foreach(string required in new[]{"BatSwarm blocked","BatSwarm hit","BloodSiphon blocked","BloodSiphon hit","no-choice","wrong-defense","siphon restored missing HP"})
            Check(coverage.Contains(required),"Covered "+required);
    }
}
internal sealed class BossValidationHost : MonoBehaviour { }

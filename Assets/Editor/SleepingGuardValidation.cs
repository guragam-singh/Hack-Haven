using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Reverbat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Uses the same entry points, running scene, keyboard and mouse as a normal playthrough.
[InitializeOnLoad]
public static class SleepingGuardValidation
{
    private const string Pending = "Reverbat.GuardValidation.Pending";
    private const string WithVoice = "Reverbat.GuardValidation.Voice";
    private static readonly List<string> checks = new List<string>();
    private static bool running;
    private static InputSettings input;
    private static InputSettings.BackgroundBehavior oldBackground;
    private static InputSettings.EditorInputBehaviorInPlayMode oldFocus;
    private static Keyboard keyboard;
    private static Mouse mouse;

    static SleepingGuardValidation()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))
                EditorApplication.delayCall += StartInPlayMode;
            if (state == PlayModeStateChange.ExitingPlayMode) Restore();
        };
    }

    [MenuItem("REVERBAT/Validate Sleeping Guard in Play Mode")]
    public static void Run() { Run(false); }
    public static void Run(bool withTestVoice)
    {
        if (EditorApplication.isPlaying || running) return;
        EditorSceneManager.OpenScene(SleepingGuardSceneBuilder.ScenePath);
        SessionState.SetBool(Pending,true);
        SessionState.SetBool(WithVoice,withTestVoice);
        EditorApplication.isPlaying = true;
    }

    private static void StartInPlayMode()
    {
        var level = UnityEngine.Object.FindAnyObjectByType<SleepingGuardLevel>();
        if (level != null && !running) level.StartCoroutine(Verify(level));
    }

    private static void Check(bool ok, string label)
    {
        checks.Add((ok ? "PASS " : "FAIL ")+label);
        if (!ok) Debug.LogError("[Sleeping Guard QA] "+label);
    }
    private static void Restore()
    {
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        if (input != null)
        {
            input.backgroundBehavior = oldBackground;
            input.editorInputBehaviorInPlayMode = oldFocus;
        }
        keyboard = null; mouse = null; input = null;
        running = false;
    }
    private static void Keys(params Key[] keys)
    {
        keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    }
    private static IEnumerator Click(BatController2D bat, Vector3 position)
    {
        Vector2 screen = bat.roomCamera.WorldToScreenPoint(position);
        yield return ClickScreen(screen);
    }
    private static IEnumerator ClickScreen(Vector2 screen)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse,new MouseState { position=screen });
        yield return new WaitForSecondsRealtime(.07f);
        InputSystem.QueueStateEvent(mouse,new MouseState { position=screen, buttons=1 });
        yield return new WaitForSecondsRealtime(.065f);
        InputSystem.QueueStateEvent(mouse,new MouseState { position=screen });
        yield return new WaitForSecondsRealtime(.04f);
    }
    private static IEnumerator Until(Func<bool> condition,float timeout=5)
    {
        float end = Time.realtimeSinceStartup+timeout;
        while (!condition() && Time.realtimeSinceStartup < end) yield return null;
    }
    private static string Size => Screen.width+"x"+Screen.height;
    private static void Capture(string stage)
    {
        Directory.CreateDirectory("Captures");
        ScreenCapture.CaptureScreenshot("Captures/Guard_"+Size+"_"+stage+".png");
    }

    private static IEnumerator Verify(SleepingGuardLevel l)
    {
        running = true;
        checks.Clear();
        input = InputSystem.settings;
        oldBackground = input.backgroundBehavior;
        oldFocus = input.editorInputBehaviorInPlayMode;
        input.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        input.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();
        mouse=InputSystem.AddDevice<Mouse>();
        bool testVoice = SessionState.GetBool(WithVoice,false);
        if (testVoice) l.level2AfterFirstPulseDialogue = l.whistle; // Temporary Play Mode fixture.
        int voices = 0;
        l.voice.VoiceStarted += entry => voices++;
        var bat=l.bat;
        var echo=bat.echo;
        Vector3 start=bat.transform.position;
        yield return null;
        Check(l.State==GuardPuzzleState.AwaitingEchoPulse && bat.SelectedAbility==-1,"Starts awaiting an actual pulse, no selected ability");
        Check(bat.IsAbilityUnlocked(2) && !bat.IsAbilityUnlocked(3),"Resonance available and Mimicry locked");
        Check(!l.shieldTarget.enabled && l.shield.color.a==0 && !l.exitTarget.enabled && !l.exitArrow.enabled,"Hidden shield and exit have disabled hitboxes");
        Check(l.snoring.loop && l.snoring.isPlaying && Mathf.Approximately(l.snoring.volume,.64f) && l.snoring.volume==l.snoreVolume && l.snoring!=l.effects && l.snoring!=l.voice.VoiceSource,"Snoring loops on an independent source at baseline");
        Check(l.openWindow.enabled && l.rays.enabled && !l.closedWindow.enabled && l.female.enabled,"Window open, rays and female bat visible");
        Check(l.revealedEnvironment[0].enabled==false && l.snoreLetters.enabled,"Room dark with snore artwork");
        Capture("Start");
        Keys(Key.W,Key.A); yield return new WaitForSeconds(.15f);
        Keys(Key.S,Key.D); yield return new WaitForSeconds(.15f); Keys(); yield return null;
        Check(bat.transform.position==start,"WASD does not move Level 2 player");
        Check(!l.TryExit(l.exitArrow.transform.position),"Exit rejected before progression");
        Vector2 slot = AbilityToolbar.RectFor(1).center;
        yield return ClickScreen(new Vector2(slot.x*Screen.width/1280f,(1-slot.y/720f)*Screen.height));
        Check(bat.PulseSelected && l.State==GuardPuzzleState.AwaitingEchoPulse && l.pulse.ActivePulseCount==0,
            "Mouse toolbar selects the actual Pulse button without emitting into the room");
        slot = AbilityToolbar.RectFor(3).center;
        yield return ClickScreen(new Vector2(slot.x*Screen.width/1280f,(1-slot.y/720f)*Screen.height));
        Check(bat.PulseSelected,"Clicking a locked toolbar slot preserves selection");
        Keys(Key.Digit3); yield return null; Keys(); yield return null;
        yield return Click(bat,l.guard.transform.position);
        Check(bat.SelectedAbility==2 && l.State==GuardPuzzleState.AwaitingEchoPulse,"Resonance before first pulse cannot wake guard");
        Keys(Key.Digit1); yield return null; Keys(); yield return null;
        yield return Click(bat,l.shield.transform.position);
        Check(bat.SelectedAbility==0 && echo.LastHit==l.guardTarget.name && !echo.LastScriptedRoute && l.State==GuardPuzzleState.AwaitingEchoPulse,"Echo on sleeping guard / invisible shield fails (hit "+echo.LastHit+", ability "+bat.SelectedAbility+")");
        yield return Until(()=>!echo.IsEmitting);
        Keys(Key.Digit2); yield return null; Keys(); yield return null;
        Check(bat.SelectedAbility==1 && l.State==GuardPuzzleState.AwaitingEchoPulse,"Selecting Pulse alone does not advance");
        yield return Click(bat,l.guard.transform.position);
        Check(l.pulse.ActivePulseCount==1 && l.pulse.CooldownRemaining>0,"Click emits shared expanding pulse and starts cooldown (count "+l.pulse.ActivePulseCount+", remaining "+l.pulse.CooldownRemaining+")");
        Check(!l.pulse.Emit(),"Repeated emission rejected during cooldown");
        Check(l.pulse.RevealAmountAt(start)>l.pulse.RevealAmountAt(l.female.transform.position),"Reveal begins at player before reaching far side");
        if (l.level2AfterFirstPulseDialogue) Check(l.State==GuardPuzzleState.BatDialogue && voices==1 && l.voice.VoiceSource.isPlaying,"Assigned test clip plays on voice queue");
        else Check(voices==0 && l.State==GuardPuzzleState.AwaitingGuardResonance,"Empty voice hook advances immediately");
        yield return Until(()=>l.State==GuardPuzzleState.AwaitingGuardResonance);
        yield return new WaitForSeconds(1.4f);
        Check(l.pulse.RevealAmountAt(l.female.transform.position)>.8f,"Room revealed at far window");
        Capture("Reveal");
        yield return Until(()=>l.pulse.ActivePulseCount==0,15);
        Check(l.pulse.ActivePulseCount==0 && l.pulse.RevealAmountAt(l.female.transform.position)==1 && l.revealedEnvironment[0].enabled && Shader.GetGlobalFloat("_ReverbatMaskEnabled")==0,"Hallway stays fully bright after the first pulse expires");
        l.pulse.Emit();
        Check(l.State==GuardPuzzleState.AwaitingGuardResonance && voices==(l.level2AfterFirstPulseDialogue?1:0),"Additional pulses do not replay dialogue or reset progression");
        Keys(Key.Digit3); yield return null; Keys(); yield return null;
        yield return Click(bat,l.openWindow.transform.position);
        Check(l.State==GuardPuzzleState.AwaitingGuardResonance && l.resonanceWaves.enabled,"Misaimed Resonance shows directional waves and fails");
        yield return new WaitForSeconds(.5f);
        yield return Click(bat,l.guard.transform.position+Vector3.left*.45f);
        Check(l.State==GuardPuzzleState.GuardWaking && Mathf.Approximately(l.snoring.volume,Mathf.Min(1,2*l.snoreVolume)) && l.snoring.isPlaying,"Forgiving guard hit starts wake and raises the snore to its peak");
        yield return Until(()=>l.alert.enabled);
        Check(l.wakeThud!=null && l.wakeThud.name=="Guard_Wake_Thud" && l.effects.isPlaying,"Wake-up thud plays on the startled frame");
        Check(l.guard.sprite==l.startledGuard && !l.snoring.isPlaying && l.alert.enabled && !l.shieldTarget.enabled,"Startled swap and alert follow snoring stop");
        yield return Until(()=>l.shield.color.a>.05f);
        Check(l.shield.color.a<1 && !l.shieldTarget.enabled,"Shield collider stays off during fade");
        yield return Until(()=>l.State==GuardPuzzleState.ShieldReady);
        Check(l.shield.color.a==1 && l.shieldTarget.enabled && !l.guardTarget.enabled,"Shield fully opaque and hittable without guard interference");
        Capture("Shield");
        yield return Click(bat,l.shield.transform.position);
        Check(l.State==GuardPuzzleState.ShieldReady,"Resonance on shield cannot trigger female bat");
        Keys(Key.Digit1); yield return null; Keys(); yield return null;
        foreach (Transform target in new[]{l.female.transform,l.openWindow.transform,l.partition.transform})
        {
            Vector3 aim = target==l.partition.transform ? l.partition.bounds.center : target.position;
            yield return Click(bat,aim);
            Check(echo.LastHit==l.partition.name && !echo.LastScriptedRoute && l.State==GuardPuzzleState.ShieldReady,"Direct Echo blocked by partition: "+target.name);
            yield return Until(()=>!echo.IsEmitting);
            yield return new WaitForSeconds(echo.cooldown);
        }
        var path = new List<Vector3>{start};
        bool routed = l.TraceEcho(start,(l.shield.transform.position-start).normalized,path,out string hit);
        bool clear=routed && hit==l.shield.name && path.Count==5;
        for(int i=1;i<path.Count;i++)
            clear &= Physics2D.Linecast(path[i-1],path[i],1<<l.partition.gameObject.layer).collider==null;
        Check(clear,"Shield route visits both corner anchors and clears solid partition on every segment");
        l.pulse.Emit();
        yield return Click(bat,l.shield.transform.position);
        Check(echo.IsEmitting && echo.LastScriptedRoute && l.State==GuardPuzzleState.ShieldReady,"Shield hit starts outgoing beam before reaction");
        yield return Until(()=>echo.outgoing.positionCount>=4);
        Capture("Beam");
        yield return Until(()=>l.State==GuardPuzzleState.FemaleBatReaction);
        Check(l.female.sprite==l.heartEyes && l.hearts.enabled && l.effects.isPlaying && !l.shieldTarget.enabled,"Signal arrival triggers heart eyes, hearts and whistle once");
        Check(l.openWindow.enabled && !l.exitArrow.enabled,"Curtains remain open during reaction");
        Capture("Reaction");
        yield return Until(()=>l.State==GuardPuzzleState.CurtainsClosing);
        Check(Vector3.Distance(l.female.transform.position,l.curtainLatch.position)<.01f,"Female bat reaches latch before curtain closure");
        yield return Until(()=>l.State==GuardPuzzleState.ExitAvailable);
        Check(!l.openWindow.enabled && !l.rays.enabled && l.closedWindow.enabled && l.closedWindow.color.a==1,"Closed curtains replace open assembly and light rays");
        Check(!l.female.enabled && l.female.color.a==0 && l.exitTarget.enabled && l.exitArrow.enabled,"Exit unlocks after female disappears");
        Check(l.guard.sharedMaterial!=l.shield.sharedMaterial && l.guard.sharedMaterial.mainTexture==l.guard.sprite.texture &&
            l.female.sharedMaterial.mainTexture==l.heartEyes.texture && l.closedWindow.sharedMaterial.mainTexture==l.closedWindow.sprite.texture,"Sprite swaps and curtain states retain isolated texture bindings");
        foreach (var r in new[]{l.guard,l.shield,l.openWindow,l.female,l.exitArrow})
        {
            var bottom=bat.roomCamera.WorldToViewportPoint(r.bounds.min);
            var top=bat.roomCamera.WorldToViewportPoint(r.bounds.max);
            Check(bottom.x>=0 && bottom.y>100f/720f && top.x<=1 && top.y<=1,"Artwork fits above toolbar: "+r.name);
        }
        yield return Until(()=>!echo.IsEmitting);
        yield return Click(bat,l.shield.transform.position);
        Check(!echo.LastScriptedRoute && !l.effects.isPlaying && l.State==GuardPuzzleState.ExitAvailable,
            "Further shield echoes cannot replay the reaction or whistle");
        l.pulse.Emit();
        yield return new WaitForSeconds(1.5f);
        Capture("Closed");
        Check(!l.TryExit(l.curtainLatch.position),"Click away from exit cannot move player");
        yield return Click(bat,l.exitArrow.transform.position);
        Check(l.Exiting && !l.exitArrow.enabled && !l.TryExit(l.exitDestination.position),"Mouse click starts one exit tween and consumes arrow");
        yield return Until(()=>l.State==GuardPuzzleState.Completed);
        Check(Vector3.Distance(bat.transform.position,l.exitDestination.position)<.01f && l.State==GuardPuzzleState.Completed,"Player reaches exit and completes");
        Check(!l.snoring.isPlaying && !l.effects.isPlaying && !l.voice.VoiceSource.isPlaying && !l.pulse.Emit(),"Scene audio stopped and puzzle input disabled");
        string result=checks.Exists(c=>c.StartsWith("FAIL")) ? "FAILED" : "ALL PASSED";
        Directory.CreateDirectory("Temp/Reverbat");
        File.WriteAllLines("Temp/Reverbat/Guard_"+Size+".txt",new[]{result}.Concat(checks));
        Debug.Log("[Sleeping Guard QA] "+result+" ("+checks.Count+" checks, "+Size+")");
        SessionState.SetBool(Pending,false);
        Restore();
    }

}
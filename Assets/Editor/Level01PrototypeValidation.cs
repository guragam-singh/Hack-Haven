using System.Collections;
using System.Collections.Generic;
using System.IO;
using Reverbat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class Level01PrototypeValidation
{
    private static readonly List<string> results = new List<string>();
    private static bool passed, running;
    private static Keyboard keyboard;
    static Level01PrototypeValidation()
    {
        EditorApplication.playModeStateChanged += state => { if(state == PlayModeStateChange.EnteredPlayMode) StartPending(); };
        EditorApplication.delayCall += StartPending;
    }
    [MenuItem("REVERBAT/Validate Level 01 in Play Mode")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) return;
        EditorSceneManager.OpenScene(Level01PrototypeBuilder.ScenePath);
        SessionState.SetBool("ReverbatValidate",true);
        EditorApplication.isPlaying=true;
    }
    private static void StartPending()
    {
        if(!EditorApplication.isPlaying || running || !SessionState.GetBool("ReverbatValidate",false)) return;
        var l=Object.FindFirstObjectByType<Level01Manager>();
        if(l!=null) l.StartCoroutine(Verify(l));
    }
    private static void Check(bool ok,string message)
    {
        results.Add((ok?"PASS: ":"FAIL: ")+message);
        if(!ok){passed=false;Debug.LogError("[REVERBAT] "+message);}
    }
    private static void Keys(params Key[] keys) { keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys)); }
    private static IEnumerator Verify(Level01Manager l)
    {
        passed=running=true; results.Clear();
        var bat=l.bat;
        yield return null;
        var art=l.GetComponent<Level01ArtPresentation>();
        var stand=l.transform.Find("Reflector Stand Artwork");
        Quaternion standRotation=stand==null?Quaternion.identity:stand.rotation;
        Vector3 standPosition=stand==null?Vector3.zero:stand.position;
        if(art!=null)
        {
            Check(bat.GetComponent<SpriteRenderer>().sprite.name=="Bat_Dracula" && l.coffin.sprite.name=="Coffin_Closed","Supplied bat and closed-coffin sprites are installed");
            int visible=0;
            foreach(var sr in l.GetComponentsInChildren<SpriteRenderer>()) if(sr.enabled && sr.sprite!=null) visible++;
            Check(visible==1 && l.coffin.enabled,"Art scene initially shows only the coffin");
            Check(stand!=null && stand.parent==l.transform,"Reflector stand is stationary outside the rotating plank hierarchy");
        }
        var dialogue=l.dialogue;
        var shown=new List<string>();
        if(dialogue.ActiveId!=null) shown.Add(dialogue.ActiveId);
        System.Action<DialogueEntry> observe=e=>shown.Add(e.triggerId);
        dialogue.DialogueShown+=observe;
        keyboard=InputSystem.AddDevice<Keyboard>(); Keys(); yield return null;
        Check(dialogue.HasTriggered(DialogueIds.LevelBegin),"Opening dialogue still triggers");
        Check(bat.SelectedAbility==0,"Default selected ability matches first toolbar slot");
        Vector2 start=bat.transform.position;
        Keys(Key.D); yield return new WaitForSeconds(.25f); Keys(); yield return null;
        Check(bat.transform.position.x>start.x+.7f,"D moves right at existing speed during dialogue");
        Vector2 before=bat.transform.position;
        Keys(Key.A); yield return new WaitForSeconds(.25f); Keys(); yield return null;
        Check(bat.transform.position.x<before.x-.7f,"A moves left");
        before=bat.transform.position;
        Keys(Key.W); yield return new WaitForSeconds(.25f); Keys(); yield return null;
        Check(bat.transform.position.y>before.y+.7f,"W moves up");
        before=bat.transform.position;
        Keys(Key.S); yield return new WaitForSeconds(.25f); Keys(); yield return null;
        Check(bat.transform.position.y<before.y-.7f,"S moves down");
        bat.transform.position=new Vector3(-8.3f,0);
        bat.ApplyMovement(Vector2.left,1);
        Check(bat.transform.position.x>=bat.roomMin.x,"WASD movement clamps to room bounds");
        bat.transform.position=new Vector3(-2,0);
        bat.ApplyMovement(Vector2.right,1);
        Check(bat.transform.position.x<-1.54f,"Existing circle collision prevents passing through plank");
        bat.transform.position=new Vector3(-6,0);
        Keys(Key.Digit2); yield return new WaitForSeconds(.12f); Keys(); yield return new WaitForSeconds(.12f);
        Check(bat.PulseSelected && !l.darkness.IsEmitting,"2 selects second ability without activating it");
        Keys(Key.Digit3); yield return new WaitForSeconds(.12f); Keys(); yield return new WaitForSeconds(.12f);
        Check(bat.PulseSelected && !bat.IsAbilityUnlocked(2),"3 cannot select locked third ability");
        Check(bat.SelectAbility(0) && bat.EchoSelected,"Mouse toolbar selection uses same shared selected ability state");
        Keys(Key.Digit2); yield return new WaitForSeconds(.12f); Keys(); yield return new WaitForSeconds(.12f);
        Check(bat.PulseSelected,"Keyboard selection remains synchronized after mouse selection");
        Vector2 position=bat.transform.position;
        bat.ClickWorld(new Vector2(-4,1));
        Check(l.darkness.IsEmitting && !bat.IsMoving && Vector2.Distance(bat.transform.position,position)<.01f,"World click emits selected pulse without click-to-move");
        Check(dialogue.HasTriggered(DialogueIds.FirstPulse),"First pulse dialogue remains intact");
        yield return new WaitForSeconds(1);
        Check(l.darkness.Visible(l.reflector.GetComponent<SpriteRenderer>()),"Echo Pulse still reveals environment");
        if(art!=null)
        {
            Check(l.transform.Find("Room Artwork").GetComponent<SpriteRenderer>().enabled && stand.GetComponent<SpriteRenderer>().enabled,"Echo Pulse reveals supplied background and reflector stand");
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory("Captures");
            ScreenCapture.CaptureScreenshot("Captures/Level01_Art_Revealed.png");
        }
        while(dialogue.IsShowing) dialogue.Dismiss();
        Keys(Key.Digit1); yield return new WaitForSeconds(.12f); Keys(); yield return new WaitForSeconds(.12f);
        Check(bat.EchoSelected,"1 selects first ability");
        bat.ClickWorld(l.reflector.TransformPoint(new Vector3(.35f,0,0)));
        yield return new WaitUntil(()=>!bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        Check(!l.CoffinOpen && !bat.echo.LastScriptedRoute,"Flat plank still cannot unlock coffin");
        // WASD route above the flat plank, then descend onto its right end.
        yield return DriveTo(bat,new Vector2(-3,1.2f));
        yield return DriveTo(bat,new Vector2(2.5f,1.2f));
        yield return DriveTo(bat,l.tiltable.EndPosition(1),.18f);
        yield return new WaitForSeconds(1.5f);
        Check(l.tiltable.CorrectAngle && l.tiltable.HasBeenTiltedToCorrectAngle,"WASD boarding right end tilts plank to -45 degrees");
        if(art!=null) Check(Quaternion.Angle(stand.rotation,standRotation)<.01f && Vector3.Distance(stand.position,standPosition)<.001f,"Stand does not rotate or move with the tilted plate");
        Check(dialogue.HasTriggered(DialogueIds.Reflector) && dialogue.HasTriggered(DialogueIds.Tilt),"Discovery and tilt dialogue still trigger");
        before=bat.transform.position;
        Keys(Key.D); yield return new WaitForSeconds(.4f); Keys(); yield return null;
        Check(bat.transform.position.x>before.x+.8f,"WASD can leave the plank without being trapped");
        yield return DriveTo(bat,new Vector2(4,1));
        bat.SelectAbility(0);
        bat.ClickWorld(l.reflector.TransformPoint(new Vector3(-.35f,0,0)));
        yield return new WaitUntil(()=>!bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        Check(!l.CoffinOpen,"Left half remains invalid at correct angle");
        bat.ClickWorld(l.reflector.TransformPoint(new Vector3(.35f,0,0)));
        yield return new WaitUntil(()=>!bat.echo.IsEmitting);
        Check(l.CoffinOpen && l.artifact.activeSelf && dialogue.HasTriggered(DialogueIds.Coffin),"Right-half echo still opens coffin with dialogue");
        if(art!=null)
        {
            Check(l.coffin.sprite==art.openCoffin && l.coffin.color==Color.white,"Valid unlock swaps to supplied open coffin with untinted artwork");
            Check(l.artifact.GetComponent<SpriteRenderer>().sprite.name=="Collectible_BloodDrop_Red","Exposed artifact uses supplied red blood-drop art");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Captures/Level01_Art_CoffinOpen.png");
        }
        position=bat.transform.position;
        bat.ClickWorld(l.artifact.transform.position);
        yield return new WaitForSeconds(.2f);
        Check(!l.Complete && Vector2.Distance(bat.transform.position,position)<.01f,"Distant artifact click does not auto-walk or collect");
        yield return DriveTo(bat,l.artifact.transform.position);
        bat.ClickWorld(l.artifact.transform.position);
        Check(l.Complete && !l.artifact.activeSelf,"WASD approach then mouse interaction collects artifact and completes level");
        while(dialogue.IsShowing){dialogue.Dismiss();yield return null;}
        Check(shown.IndexOf(DialogueIds.Artifact)>=0 && shown.IndexOf(DialogueIds.Announcement)==shown.IndexOf(DialogueIds.Artifact)+1,"Artifact and dictator dialogue preserve sequential order");
        InputSystem.RemoveDevice(keyboard);
        dialogue.DialogueShown-=observe;
        Directory.CreateDirectory("Temp/Reverbat");
        File.WriteAllText("Temp/Reverbat/validation.txt",(passed?"ALL PASSED\n":"FAILED\n")+string.Join("\n",results));
        Debug.Log("[REVERBAT CONTROLS] "+(passed?"ALL PASSED":"FAILED"));
        SessionState.SetBool("ReverbatValidate",false);running=false;
    }
    private static IEnumerator DriveTo(BatController2D bat,Vector2 target,float tolerance=.1f)
    {
        Keys(); yield return null;
        float deadline=Time.time+6;
        while(Vector2.Distance(bat.transform.position,target)>tolerance && Time.time<deadline)
        {
            Vector2 delta=target-(Vector2)bat.transform.position;
            bat.ApplyMovement(delta.normalized,Mathf.Min(.04f,delta.magnitude/bat.speed));
            yield return null;
        }
        yield return null;
    }
}


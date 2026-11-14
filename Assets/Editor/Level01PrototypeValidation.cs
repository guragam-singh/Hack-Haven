using System.Collections;
using System.Collections.Generic;
using System.IO;
using Reverbat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class Level01PrototypeValidation
{
    private static readonly List<string> results = new List<string>();
    private static bool passed, running;
    static Level01PrototypeValidation()
    {
        EditorApplication.playModeStateChanged += state => { if (state == PlayModeStateChange.EnteredPlayMode) StartPending(); };
        EditorApplication.delayCall += StartPending;
    }
    [MenuItem("REVERBAT/Validate Level 01 in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) return;
        EditorSceneManager.OpenScene(Level01PrototypeBuilder.ScenePath);
        SessionState.SetBool("ReverbatValidate", true);
        EditorApplication.isPlaying = true;
    }
    private static void StartPending()
    {
        if (!EditorApplication.isPlaying || running || !SessionState.GetBool("ReverbatValidate", false)) return;
        var level = Object.FindFirstObjectByType<Level01Manager>();
        if (level != null) level.StartCoroutine(Verify(level));
    }
    private static void Check(bool value, string message)
    {
        results.Add((value ? "PASS: " : "FAIL: ") + message);
        if (!value) { passed = false; Debug.LogError("[REVERBAT] " + message); }
    }
    private static IEnumerator Verify(Level01Manager level)
    {
        passed = running = true;
        results.Clear();
        var bat = level.bat;
        yield return null;
        var dialogue = level.dialogue;
        var shown = new List<string>();
        if (dialogue.ActiveId != null) shown.Add(dialogue.ActiveId);
        System.Action<DialogueEntry> observe = entry => shown.Add(entry.triggerId);
        dialogue.DialogueShown += observe;
        Check(dialogue.ActiveId == DialogueIds.LevelBegin, "Opening dialogue automatically appears at level start");
        Check(dialogue.Content != null && dialogue.Content.entries.Length == 7, "Seven editable voice-ready dialogue entries loaded");
        yield return new WaitForEndOfFrame();
        Directory.CreateDirectory("Captures");
        ScreenCapture.CaptureScreenshot("Captures/Level01_Dialogue_Opening.png");
        float coffinLeft = bat.roomCamera.WorldToViewportPoint(level.coffin.bounds.min).x * 1280;
        Check(dialogue.PanelRect.yMax < 620 && dialogue.PanelRect.xMax < coffinLeft, "Dialogue panel leaves toolbar, coffin and artifact clear");
        Check(!dialogue.TryDismissAtScreenPoint(new Vector2(Screen.width - 1, Screen.height - 1)), "Clicks outside dialogue do not dismiss it");
        bat.SelectEcho(false);
        bat.ClickWorld(new Vector2(-5, 0));
        yield return new WaitForSeconds(.35f);
        Check(dialogue.IsShowing && Vector2.Distance(bat.transform.position, new Vector2(-5, 0)) < .1f && Time.timeScale == 1, "Bat moves while dialogue remains visible without pausing gameplay");
        bat.ClickWorld(new Vector2(-6, 0));
        yield return new WaitForSeconds(.35f);
        int visible = 0;
        foreach (var sr in level.GetComponentsInChildren<SpriteRenderer>()) if (sr.enabled) visible++;
        Check(visible == 1 && level.coffin.enabled && !level.artifact.activeSelf, "Only coffin sprite visible initially");
        Check(bat.roomCamera.backgroundColor == Color.black, "Room starts completely black");
        bat.SelectPulse();
        Check(dialogue.HasTriggered(DialogueIds.FirstPulse) && dialogue.ActiveId == DialogueIds.LevelBegin && dialogue.PendingCount == 1, "First successful circular pulse queues its line without interrupting opening");
        int queued = dialogue.PendingCount;
        Check(!level.darkness.Emit() && dialogue.PendingCount == queued, "Rejected repeat pulse adds no extra dialogue");
        Vector2 panelPoint = new Vector2(dialogue.PanelRect.center.x * Screen.width / 1280, Screen.height - dialogue.PanelRect.center.y * Screen.height / 720);
        Vector2 oldDestination = bat.Destination;
        Check(dialogue.TryDismissAtScreenPoint(panelPoint) && dialogue.ActiveId == DialogueIds.FirstPulse, "Clicking panel dismisses opening and advances to queued pulse line");
        Check(!bat.HandleScreenClick(panelPoint) && bat.Destination == oldDestination, "Dialogue dismiss click cannot leak into gameplay on the same frame");
        Check(level.darkness.IsEmitting, "Echo Pulse creates an expanding ripple");
        yield return new WaitForSeconds(1);
        Check(level.darkness.Visible(level.reflector.GetComponent<SpriteRenderer>()) && level.darkness.Visible(bat.GetComponent<SpriteRenderer>()), "Ripple reveals bat and reflector on arrival");
        yield return new WaitForSeconds(7);
        Check(!dialogue.IsShowing, "Configured duration automatically dismisses short dialogue");
        Check(!level.darkness.Visible(level.reflector.GetComponent<SpriteRenderer>()), "Revealed environment fades back into darkness");
        Check(Mathf.Abs(level.tiltable.Angle) < .01f, "Plank starts at zero degrees");
        level.HitCoffinByReflection();
        Check(!level.CoffinOpen, "Direct unlock callback cannot bypass progression");
        bat.SelectEcho(false);
        bat.ClickWorld(new Vector2(4, 1));
        yield return WaitForMove(bat);
        bat.SelectEcho(true);
        bat.ClickWorld(level.reflector.TransformPoint(new Vector3(.35f, 0, 0)));
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        Check(!bat.echo.LastScriptedRoute && !level.CoffinOpen && bat.echo.outgoing.positionCount == 3, "Flat plank visibly deflects away without reaching bell or coffin");
        Check(dialogue.HasTriggered(DialogueIds.Reflector) && !dialogue.HasTriggered(DialogueIds.Tilt), "First plank interaction triggers discovery, but not premature tilt dialogue");
        level.reflector.rotation = Quaternion.Euler(0, 0, -45);
        bat.ClickWorld(level.reflector.TransformPoint(new Vector3(.35f, 0, 0)));
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        Check(!bat.echo.LastScriptedRoute && !level.CoffinOpen, "Setting angle alone cannot bypass bat-weight progression");
        level.reflector.rotation = Quaternion.identity;
        bat.SelectEcho(false);
        bat.ClickWorld(level.reflector.TransformPoint(new Vector3(.45f, 0, 0)));
        yield return new WaitForSeconds(4);
        Check(level.tiltable.CorrectAngle && level.tiltable.HasBeenTiltedToCorrectAngle, "Bat boards correct end and tilts to -45 degrees");
        Check(dialogue.HasTriggered(DialogueIds.Tilt), "First weight-driven correct tilt triggers sitting-on-things dialogue");
        bat.ClickWorld(new Vector2(4, 1));
        yield return WaitForMove(bat);
        bat.SelectEcho(true);
        foreach (float angle in new[] { 0f, 45f, -55.1f, -34.9f })
        {
            level.reflector.rotation = Quaternion.Euler(0, 0, angle);
            bat.ClickWorld(level.reflector.TransformPoint(new Vector3(.35f, 0, 0)));
            yield return new WaitUntil(() => !bat.echo.IsEmitting);
            yield return new WaitForSeconds(bat.echo.cooldown);
            Check(!bat.echo.LastScriptedRoute && !level.CoffinOpen && bat.echo.outgoing.positionCount == 3, "Wrong angle " + angle + " deflects harmlessly even after earlier tilt");
        }
        foreach (float angle in new[] { -55f, -35f, -45f })
        {
            level.reflector.rotation = Quaternion.Euler(0, 0, angle);
            Check(level.tiltable.CorrectAngle, "Tolerance accepts angle " + angle);
        }
        bat.ClickWorld(level.reflector.TransformPoint(new Vector3(-.35f, 0, 0)));
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        Check(!bat.echo.LastScriptedRoute && !level.CoffinOpen, "Correct angle but left-half hit cannot solve");
        bat.transform.position = new Vector3(-4, 1);
        bat.ClickWorld(level.reflector.TransformPoint(new Vector3(.35f, 0, 0)));
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        Check(!bat.echo.LastScriptedRoute && !level.CoffinOpen, "Correct angle but left-side bat cannot solve");
        bat.transform.position = new Vector3(4, 1);
        bat.ClickWorld(level.reflector.TransformPoint(new Vector3(.35f, 0, 0)));
        Check(bat.echo.LastScriptedRoute && !level.CoffinOpen, "Correct tilt and right-half hit schedule route without early unlock");
        level.reflector.rotation = Quaternion.identity;
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        Check(!level.CoffinOpen, "Changing tilt during flight cannot bypass arrival gate");
        level.reflector.rotation = Quaternion.Euler(0, 0, -45);
        bat.ClickWorld(level.reflector.TransformPoint(new Vector3(.35f, 0, 0)));
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        var line = bat.echo.outgoing;
        Check(line.positionCount == 4 && Vector3.Distance(line.GetPosition(2), level.bell.transform.position) < .01f && Vector3.Distance(line.GetPosition(3), level.coffin.transform.position) < .01f, "Valid beam visits plank then bell then coffin");
        Check(level.CoffinOpen && level.artifact.activeSelf, "Only valid progression opens coffin and exposes artifact");
        Check(dialogue.HasTriggered(DialogueIds.Coffin), "Successful coffin unlock triggers its dialogue");
        Check(!level.TryCollect(bat.transform.position), "Artifact still requires proximity");
        bat.ClickWorld(level.artifact.transform.position);
        yield return WaitForMove(bat);
        Check(level.Complete && !level.artifact.activeSelf, "Mouse-only click-to-collect completes puzzle");
        Check(dialogue.HasTriggered(DialogueIds.Artifact) && dialogue.HasTriggered(DialogueIds.Announcement), "Collection queues artifact dialogue followed by dictator announcement");
        while (dialogue.IsShowing) { dialogue.Dismiss(); yield return null; }
        int artifactIndex = shown.IndexOf(DialogueIds.Artifact);
        Check(artifactIndex >= 0 && shown.IndexOf(DialogueIds.Announcement) == artifactIndex + 1, "Villain announcement appears immediately after artifact dialogue");
        bool once = true;
        foreach (var entry in dialogue.Content.entries)
            once &= dialogue.HasTriggered(entry.triggerId) && shown.FindAll(id => id == entry.triggerId).Count == 1 && !dialogue.TryTrigger(entry.triggerId);
        Check(once && !dialogue.IsShowing && dialogue.PendingCount == 0, "All seven lines appear exactly once; repeats do not requeue");
        Check(dialogue.Content.Find(DialogueIds.Announcement).characterName == "DICTATOR DRACULA" && dialogue.GetComponent<AudioSource>() != null, "Distinct speaker and optional voice playback source are available");
        dialogue.DialogueShown -= observe;
        Directory.CreateDirectory("Temp/Reverbat");
        File.WriteAllText("Temp/Reverbat/validation.txt", (passed ? "ALL PASSED\n" : "FAILED\n") + string.Join("\n", results));
        Debug.Log("[REVERBAT VALIDATION] " + (passed ? "ALL PASSED" : "FAILED"));
        SessionState.SetBool("ReverbatValidate", false);
        running = false;
    }
    private static IEnumerator WaitForMove(BatController2D bat)
    {
        float deadline = Time.time + 7;
        while (bat.IsMoving && Time.time < deadline) yield return null;
        yield return null;
    }
}

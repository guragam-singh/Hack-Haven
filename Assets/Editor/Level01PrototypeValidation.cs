using System.Collections;
using System.Collections.Generic;
using System.IO;
using Reverbat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Runs against the real scene in Play Mode, using the same click/echo/movement APIs as input.
[InitializeOnLoad]
public static class Level01PrototypeValidation
{
    private static readonly List<string> results = new List<string>();
    private static bool passed;
    private static bool running;

    static Level01PrototypeValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayState;
        EditorApplication.delayCall += StartPending;
    }

    [MenuItem("REVERBAT/Validate Level 01 in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Stop play mode before validating."); return; }
        EditorSceneManager.OpenScene(Level01PrototypeBuilder.ScenePath);
        SessionState.SetBool("ReverbatValidate", true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("ReverbatValidate", false)) return;
        StartPending();
    }

    private static void StartPending()
    {
        if (!EditorApplication.isPlaying || running || !SessionState.GetBool("ReverbatValidate", false)) return;
        var level = Object.FindFirstObjectByType<Level01Manager>();
        if (level != null) level.StartCoroutine(Verify(level));
    }

    private static void Check(bool condition, string description)
    {
        results.Add((condition ? "PASS: " : "FAIL: ") + description);
        if (!condition) { passed = false; Debug.LogError("[REVERBAT VALIDATION] " + description); }
    }

    private static IEnumerator Verify(Level01Manager level)
    {
        passed = true;
        running = true;
        results.Clear();
        var bat = level.bat;
        Check(!level.CoffinOpen && !level.artifact.activeSelf && !level.MechanismVisible, "Initial coffin locked, artifact and mechanism hidden");
        bat.MoveTo(new Vector2(-100, 100), false);
        Check(bat.Destination == new Vector2(bat.roomMin.x, bat.roomMax.y), "Out-of-room movement targets are clamped");
        bat.MoveTo(bat.transform.position, false);
        Physics2D.SyncTransforms();
        bool generousAim = true;
        for (int i = -2; i <= 2; i++)
        {
            Vector2 direction = (new Vector2(0, i * .15f) - (Vector2)bat.transform.position).normalized;
            var first = Physics2D.Raycast(bat.transform.position, direction, 20);
            if (first.collider == null || first.collider.name != "Reflector") { generousAim = false; continue; }
            Vector2 reflected = Vector2.Reflect(direction, first.normal);
            var second = Physics2D.Raycast(first.point + reflected * .035f, reflected, 20);
            if (second.collider == null || second.collider.name != "Bell") generousAim = false;
        }
        Check(generousAim, "Five nearby aim points across a 0.6-unit band reflect into the bell");
        bat.SelectEcho(false);
        bat.ClickWorld(new Vector2(-5, 0));
        yield return new WaitForSeconds(.4f);
        Check(Vector2.Distance(bat.transform.position, new Vector2(-5, 0)) < .06f, "Mouse movement reaches a clear target smoothly");
        bat.ClickWorld(new Vector2(-6, 0));
        yield return new WaitForSeconds(.4f);
        bat.SelectEcho(true);
        bat.ClickWorld(level.bell.transform.position);
        Check(bat.echo.IsEmitting, "Selected Echo Sight emits toward clicked target");
        Check(!bat.echo.Emit(Vector2.up), "Active pulse/cooldown prevents spam");
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        Check(!level.CoffinOpen && bat.echo.LastHit == "Bell" && bat.echo.LastReflectionCount == 0, "Direct bell hit does not solve puzzle");
        bat.ClickWorld(level.coffin.transform.position);
        Check(!level.MechanismDiscovered, "Coffin clue remains hidden during outgoing wave");
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        Check(level.MechanismDiscovered && level.MechanismVisible, "Coffin return echo reveals bell-lock connection");
        yield return new WaitForSeconds(3.1f);
        Check(!level.MechanismVisible, "Revealed connection expires after about three seconds");
        bat.ClickWorld(level.reflector.position);
        Check(bat.echo.LastReflectionCount == 1 && bat.echo.LastHit == "Bell", "Real 45-degree collider redirects horizontal ray into bell");
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        Check(level.CoffinOpen && level.artifact.activeSelf, "Reflected bell hit opens coffin and exposes artifact");
        Check(!level.TryCollect(bat.transform.position), "Artifact cannot be collected remotely");
        bat.ClickWorld(level.artifact.transform.position);
        float deadline = Time.time + 5;
        while (!level.Complete && Time.time < deadline) yield return null;
        Check(level.Complete && !level.artifact.activeSelf, "Clicking artifact moves bat into range and completes level");
        Directory.CreateDirectory("Temp/Reverbat");
        File.WriteAllText("Temp/Reverbat/validation.txt", (passed ? "ALL PASSED\n" : "FAILED\n") + string.Join("\n", results));
        Debug.Log("[REVERBAT VALIDATION] " + (passed ? "ALL PASSED" : "FAILED") + "\n" + string.Join("\n", results));
        yield return new WaitForEndOfFrame();
        Directory.CreateDirectory("Captures");
        ScreenCapture.CaptureScreenshot("Captures/Level01_Complete.png");
        SessionState.SetBool("ReverbatValidate", false);
        running = false;
    }
}

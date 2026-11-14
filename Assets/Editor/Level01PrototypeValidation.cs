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
        int visible = 0;
        foreach (var sr in level.GetComponentsInChildren<SpriteRenderer>()) if (sr.enabled) visible++;
        Check(visible == 1 && level.coffin.enabled && !level.artifact.activeSelf, "Only coffin sprite visible initially");
        Check(bat.roomCamera.backgroundColor == Color.black, "Room starts completely black");
        bat.SelectPulse();
        Check(level.darkness.IsEmitting, "Echo Pulse creates an expanding ripple");
        yield return new WaitForSeconds(1);
        Check(level.darkness.Visible(level.reflector.GetComponent<SpriteRenderer>()) && level.darkness.Visible(bat.GetComponent<SpriteRenderer>()), "Ripple reveals bat and reflector on arrival");
        yield return new WaitForSeconds(7);
        Check(!level.darkness.Visible(level.reflector.GetComponent<SpriteRenderer>()), "Revealed environment fades back into darkness");
        bat.SelectEcho(true);
        bat.ClickWorld(level.reflector.TransformPoint(new Vector3(.35f, 0, 0)));
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        Check(!bat.echo.LastScriptedRoute && !level.CoffinOpen, "Bat on left cannot solve by hitting right half");
        bat.SelectEcho(false);
        bat.ClickWorld(new Vector2(4, 1));
        yield return WaitForMove(bat);
        Check(bat.transform.position.x > 0, "Mouse movement reaches the right side of screen");
        bat.SelectEcho(true);
        bat.ClickWorld(level.reflector.TransformPoint(new Vector3(-.35f, 0, 0)));
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        Check(!bat.echo.LastScriptedRoute && !level.CoffinOpen, "Bat on right hitting left half cannot solve");
        bat.ClickWorld(level.reflector.position);
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        Check(!bat.echo.LastScriptedRoute && !level.CoffinOpen, "Exact divider hit does not count as right half");
        bat.ClickWorld(level.bell.transform.position);
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        Check(!level.CoffinOpen, "Direct bell shot cannot bypass plank route");
        bat.ClickWorld(level.reflector.TransformPoint(new Vector3(.35f, 0, 0)));
        Check(bat.echo.LastScriptedRoute && !level.CoffinOpen, "Right side and right half schedule route without opening coffin early");
        yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
        var line = bat.echo.outgoing;
        Check(line.positionCount == 4 && Vector3.Distance(line.GetPosition(2), level.bell.transform.position) < .01f && Vector3.Distance(line.GetPosition(3), level.coffin.transform.position) < .01f, "Visible beam follows bat to plank to bell to coffin in order");
        Check(level.CoffinOpen && level.artifact.activeSelf, "Scripted route opens coffin at default tilt");
        bool forgiving = true;
        foreach (float angle in new[] { -45f, 0f, 45f })
        {
            level.reflector.rotation = Quaternion.Euler(0, 0, angle);
            foreach (Vector2 source in new[] { new Vector2(2, 3), new Vector2(7, -1), new Vector2(1, -2) })
            {
                bat.transform.position = source;
                bat.ClickWorld(level.reflector.TransformPoint(new Vector3(.35f, 0, 0)));
                forgiving &= bat.echo.LastScriptedRoute;
                yield return new WaitUntil(() => !bat.echo.IsEmitting);
        yield return new WaitForSeconds(bat.echo.cooldown);
            }
        }
        Check(forgiving, "Right-half shots work from varied right-side positions at -45, 0 and +45 degrees");
        bat.SelectEcho(false);
        bat.ClickWorld(level.reflector.TransformPoint(new Vector3(.45f, 0, 0)));
        yield return new WaitForSeconds(5);
        Check(Mathf.Abs(level.tiltable.Angle + 45) < 1, "Weight-driven right-end tilting is retained");
        bat.ClickWorld(new Vector2(4, 1));
        yield return WaitForMove(bat);
        Check(!level.TryCollect(bat.transform.position), "Artifact still requires proximity");
        bat.ClickWorld(level.artifact.transform.position);
        yield return WaitForMove(bat);
        Check(level.Complete && !level.artifact.activeSelf, "Mouse-only click-to-collect completes puzzle");
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



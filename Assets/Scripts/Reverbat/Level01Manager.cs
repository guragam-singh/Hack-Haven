using System.Collections;
using UnityEngine;

namespace Reverbat
{
    public sealed class Level01Manager : MonoBehaviour
    {
        public event System.Action<string> DialogueTriggered;
        public DialogueManager dialogue { get; private set; }
        public TutorialManager tutorial { get; private set; }
        public BatController2D bat;
        public SpriteRenderer coffin;
        public SpriteRenderer bell;
        public GameObject artifact;
        public LineRenderer mechanism;
        public Transform reflector;
        public EchoPulse2D darkness { get; private set; }
        public TiltableReflector2D tiltable { get; private set; }
        public bool CoffinOpen { get; private set; }
        public bool Complete { get; private set; }
        public bool MechanismDiscovered { get; private set; }
        public bool MechanismVisible => mechanism.enabled;
        private float revealUntil;
        private float hintUntil;
        private string hint = "";
        private GUIStyle text, title, counter, small, button, instruction;
        public static Rect EchoSightButtonRect => AbilityToolbar.RectFor(0);
        public static Rect EchoPulseButtonRect => AbilityToolbar.RectFor(1);
        private Color bellBaseColor;
        private bool levelBegun;

        private void Awake()
        {
            artifact.SetActive(false);
            bellBaseColor = bell.color;
            mechanism.enabled = false;
            hintUntil = float.PositiveInfinity;
            tiltable = reflector.gameObject.AddComponent<TiltableReflector2D>();
            tiltable.level = this;
            darkness = GetComponent<EchoPulse2D>();
            if (darkness == null) darkness = gameObject.AddComponent<EchoPulse2D>();
            darkness.Initialize(this);
            var voiceHost = new GameObject("Level dialogue");
            voiceHost.transform.SetParent(transform);
            dialogue = voiceHost.AddComponent<DialogueManager>();
            dialogue.Initialize(this);
            tutorial = GetComponent<TutorialManager>();
            if (tutorial == null) tutorial = gameObject.AddComponent<TutorialManager>();
            tutorial.Initialize(this);
            if (StartingCutscene.Requested) gameObject.AddComponent<StartingCutscene>();
        }

        public void BeginLevel()
        {
            if (levelBegun) return;
            levelBegun = true;
            NotifyDialogue(DialogueIds.LevelBegin);
        }
        public void NotifyDialogue(string triggerId) { DialogueTriggered?.Invoke(triggerId); }

        private void Update()
        {
            if (tutorial != null && tutorial.BlocksGameplayInput) return;
            if (mechanism.enabled && Time.time >= revealUntil) mechanism.enabled = false;
            if (Vector2.Distance(bat.transform.position, reflector.GetComponent<Collider2D>().ClosestPoint(bat.transform.position)) <= 1.5f)
                NotifyDialogue(DialogueIds.Reflector);
        }

        public void ShowHint(string message, float duration = 4)
        {
            if (Complete) return;
            hint = message;
            hintUntil = Time.time + duration;
        }

        public void RevealMechanism()
        {
            MechanismDiscovered = true;
            mechanism.enabled = true;
            revealUntil = Time.time + 3;
            if (!CoffinOpen) ShowHint("Sit on the mirror's right half to tilt it to -45°, then echo that half from the right: bell → coffin.", 5);
        }

        public void HitBell(bool throughReflector)
        {
            if (CoffinOpen) return;
            if (!throughReflector)
            {
                ShowHint("No useful response. The bell needs an echo redirected by the cyan reflector.");
                return;
            }
            StartCoroutine(FlashBell());
            ShowHint("Bell activated! The echo is travelling to the coffin.");
        }

        public void HitCoffinByReflection()
        {
            // Check again on arrival: a changed tilt or a direct callback must never bypass progression.
            if (CoffinOpen || !bat.echo.ValidPuzzleRoute) return;
            CoffinOpen = true;
            coffin.color = new Color(.55f, .22f, .25f);
            coffin.GetComponent<Collider2D>().enabled = false;
            artifact.SetActive(true);
            StartCoroutine(FlashBell());
            ShowHint("Coffin unlocked! Use WASD to approach the red artifact, then click it.", 8);
            NotifyDialogue(DialogueIds.Coffin);
        }

        private IEnumerator FlashBell()
        {
            bell.color = Color.green;
            yield return new WaitForSeconds(.8f);
            bell.color = bellBaseColor;
        }

        public bool TryCollect(Vector2 player)
        {
            if (!CoffinOpen || Complete || Vector2.Distance(player, artifact.transform.position) > .65f) return false;
            Complete = true;
            artifact.SetActive(false);
            hint = "Artifact Collected! Resonance Unlocked.";
            NotifyDialogue(DialogueIds.Artifact);
            NotifyDialogue(DialogueIds.Announcement);

            if (Application.CanStreamedLevelBeLoaded(SleepingGuardLevel.SceneName))
            {
                SleepingGuardLevel.EnterFromLevelOne(dialogue);
                UnityEngine.SceneManagement.SceneManager.LoadScene(SleepingGuardLevel.SceneName);
            }
            return true;
        }

        private void OnGUI()
        {
            if (StartingCutscene.IsActive || (tutorial != null && tutorial.AwaitingFirstClick)) return;
            if (text == null)
            {
                text = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                text.normal.textColor = Color.white;
                instruction = new GUIStyle(text) { fontSize = TutorialPresentation.MainInstructionFontSize, fontStyle = FontStyle.Bold };
                title = new GUIStyle(text) { fontSize = 19, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
                counter = new GUIStyle(text) { fontSize = 18, alignment = TextAnchor.MiddleRight };
                small = new GUIStyle(text) { fontSize = 15 };
                button = new GUIStyle(GUI.skin.button) { fontSize = 17 };
            }
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            GUI.Box(new Rect(0, 0, 1280, 100), GUIContent.none);
            GUI.Label(new Rect(24, 7, 800, 30), "REVERBAT / LEVEL 01", title);
            GUI.Label(new Rect(1000, 7, 256, 30), "Artifacts: " + (Complete ? "1/1" : "0/1"), counter);
            // Tutorial labels never consume input. Legacy action feedback cannot overwrite milestones.
            TutorialPresentation.DrawReadableText(new Rect(24, 41, 1232, 50), tutorial != null ? tutorial.CurrentMessage : Time.time < hintUntil ? hint : "", instruction);
            if (darkness.Visible(bat.GetComponent<SpriteRenderer>())) LabelWorld(bat.transform, "BAT", new Vector2(0, 28));
            if (darkness.Visible(reflector.GetComponent<SpriteRenderer>()))
            {
                LabelWorld(reflector, "REFLECTOR / " + tiltable.Angle.ToString("0") + "°", new Vector2(0, 68));
                LabelWorld(reflector, "ECHO: RIGHT HALF", new Vector2(0, 95));
            }
            if (darkness.Visible(bell)) LabelWorld(bell.transform, "BELL", new Vector2(0, -48));
            LabelWorld(coffin.transform, CoffinOpen ? "COFFIN / OPEN" : "COFFIN / LOCKED", new Vector2(0, -85));
            GUI.Box(new Rect(0, 620, 1280, 100), GUIContent.none);
            bool inputEnabled = tutorial == null || !tutorial.BlocksGameplayInput;
            AbilityToolbar.Draw(bat, button, inputEnabled);
            var art = GetComponent<Level01ArtPresentation>();
            if (art != null) art.DrawAbilityIcons();
            GUI.Label(new Rect(40, 681, 1200, 28), "WASD: fly  •  1–3 or toolbar: select ability  •  Left click: use ability  •  Approach artifact with WASD, then click to collect", small);
        }

        private void LabelWorld(Transform obj, string label, Vector2 offset)
        {
            Vector3 p = bat.roomCamera.WorldToViewportPoint(obj.position);
            GUI.Label(new Rect(p.x * 1280 - 120 + offset.x, (1 - p.y) * 720 + offset.y, 240, 25), label, small);
        }
    }
}

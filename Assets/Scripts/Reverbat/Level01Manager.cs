using System.Collections;
using UnityEngine;

namespace Reverbat
{
    public sealed class Level01Manager : MonoBehaviour
    {
        public BatController2D bat;
        public SpriteRenderer coffin;
        public SpriteRenderer bell;
        public GameObject artifact;
        public LineRenderer mechanism;
        public Transform reflector;
        public bool CoffinOpen { get; private set; }
        public bool Complete { get; private set; }
        public bool MechanismDiscovered { get; private set; }
        public bool MechanismVisible => mechanism.enabled;
        private float revealUntil;
        private float hintUntil;
        private string hint = "Select Echo Sight, then click the coffin to listen for its hidden mechanism.";
        private GUIStyle text, title, small, button;

        private void Awake()
        {
            artifact.SetActive(false);
            mechanism.enabled = false;
            hintUntil = float.PositiveInfinity;
        }

        private void Update()
        {
            if (mechanism.enabled && Time.time >= revealUntil) mechanism.enabled = false;
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
            ShowHint("The bell controls the coffin lock.", 3);
        }

        public void HitBell(bool throughReflector)
        {
            if (CoffinOpen) return;
            if (!throughReflector)
            {
                ShowHint("No useful response. The bell needs an echo redirected by the cyan reflector.");
                return;
            }
            CoffinOpen = true;
            coffin.color = new Color(.55f, .22f, .25f);
            coffin.GetComponent<Collider2D>().enabled = false;
            artifact.SetActive(true);
            StartCoroutine(FlashBell());
            ShowHint("Coffin unlocked! Click the red artifact to fly over and collect it.", 8);
        }

        private IEnumerator FlashBell()
        {
            bell.color = Color.green;
            yield return new WaitForSeconds(.8f);
            bell.color = new Color(1, .82f, .15f);
        }

        public bool TryCollect(Vector2 player)
        {
            if (!CoffinOpen || Complete || Vector2.Distance(player, artifact.transform.position) > .65f) return false;
            Complete = true;
            artifact.SetActive(false);
            hint = "Artifact Collected! Resonance Unlocked.";
            return true;
        }

        private void OnGUI()
        {
            if (text == null)
            {
                text = new GUIStyle(GUI.skin.label) { fontSize = 19, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                text.normal.textColor = Color.white;
                title = new GUIStyle(text) { fontSize = 23, fontStyle = FontStyle.Bold };
                small = new GUIStyle(text) { fontSize = 15 };
                button = new GUIStyle(GUI.skin.button) { fontSize = 17 };
            }
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            GUI.Box(new Rect(0, 0, 1280, 100), GUIContent.none);
            GUI.Label(new Rect(20, 7, 1240, 34), "REVERBAT  /  LEVEL 01 — THE CRYPT                         Artifacts: " + (Complete ? "1/1" : "0/1"), title);
            GUI.Label(new Rect(25, 42, 1230, 52), Complete || Time.time < hintUntil ? hint : "Bounce an echo off the cyan reflector into the bell.", text);
            LabelWorld(bat.transform, "BAT", new Vector2(0, 28));
            LabelWorld(reflector, "REFLECTOR / 45°", new Vector2(0, 68));
            LabelWorld(bell.transform, "BELL", new Vector2(0, -48));
            LabelWorld(coffin.transform, CoffinOpen ? "COFFIN / OPEN" : "COFFIN / LOCKED", new Vector2(0, -85));
            GUI.Box(new Rect(0, 620, 1280, 100), GUIContent.none);
            GUI.backgroundColor = !bat.EchoSelected ? new Color(.65f, .55f, 1) : Color.white;
            if (GUI.Button(new Rect(65, 635, 220, 43), "Move", button)) bat.SelectEcho(false);
            GUI.backgroundColor = bat.EchoSelected ? Color.cyan : Color.white;
            if (GUI.Button(new Rect(300, 635, 270, 43), "Echo Sight", button)) bat.SelectEcho(true);
            GUI.backgroundColor = Color.white;
            GUI.enabled = false;
            GUI.Button(new Rect(585, 635, 300, 43), Complete ? "Resonance / Unlocked" : "Resonance / Locked", button);
            GUI.Button(new Rect(900, 635, 260, 43), "Mimicry / Locked", button);
            GUI.enabled = true;
            GUI.Label(new Rect(40, 681, 1200, 28), "Mouse only  •  Move: click a clear location  •  Echo Sight: click to aim and emit  •  Artifact: click to collect", small);
            if (Complete)
            {
                GUI.Box(new Rect(400, 265, 480, 125), GUIContent.none);
                GUI.Label(new Rect(410, 280, 460, 40), "Level 1 Complete", title);
                GUI.Label(new Rect(410, 326, 460, 48), hint, text);
            }
        }

        private void LabelWorld(Transform obj, string label, Vector2 offset)
        {
            Vector3 p = bat.roomCamera.WorldToViewportPoint(obj.position);
            GUI.Label(new Rect(p.x * 1280 - 120 + offset.x, (1 - p.y) * 720 + offset.y, 240, 25), label, small);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reverbat
{
    // Presentation-only: no time-scale changes and no puzzle state is modified here.
    [DefaultExecutionOrder(-100)]
    public sealed class DialogueManager : MonoBehaviour
    {
        [SerializeField] private DialogueLibrary content;
        private readonly Queue<DialogueEntry> pending = new Queue<DialogueEntry>();
        private readonly HashSet<string> triggered = new HashSet<string>();
        private Level01Manager level;
        private DialogueEntry active;
        private AudioSource voice;
        private float expiresAt;
        private int consumedFrame = -1;
        private Rect consumedRect;
        private GUIStyle speakerStyle, textStyle, dismissStyle;
        public event Action<DialogueEntry> DialogueShown;
        public string ActiveId => active == null ? null : active.triggerId;
        public bool IsShowing => active != null;
        public int PendingCount => pending.Count;
        public DialogueLibrary Content => content;
        public Rect PanelRect { get; private set; } = new Rect(24, 500, 850, 112);

        public void Initialize(Level01Manager source)
        {
            level = source;
            if (content == null) content = Resources.Load<DialogueLibrary>("Level01Dialogue");
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0;
            if (FindFirstObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            level.DialogueTriggered += OnTrigger;
            if (content == null) Debug.LogError("[REVERBAT] Missing Resources/Level01Dialogue dialogue asset.");
        }

        private void OnDestroy()
        {
            if (level != null) level.DialogueTriggered -= OnTrigger;
        }

        private void OnTrigger(string id) { TryTrigger(id); }
        public bool HasTriggered(string id) => triggered.Contains(id);

        public bool TryTrigger(string id)
        {
            var entry = content == null ? null : content.Find(id);
            if (entry == null || !triggered.Add(id)) return false;
            pending.Enqueue(entry);
            if (active == null) ShowNext();
            return true;
        }

        private void ShowNext()
        {
            if (pending.Count == 0) { active = null; return; }
            active = pending.Dequeue();
            float duration = Mathf.Max(.1f, active.duration);
            if (active.voiceClip != null)
            {
                voice.clip = active.voiceClip;
                voice.Play();
                duration = Mathf.Max(duration, active.voiceClip.length);
            }
            expiresAt = Time.unscaledTime + duration;
            DialogueShown?.Invoke(active);
        }

        public void Dismiss()
        {
            if (active == null) return;
            voice.Stop();
            ShowNext();
        }

        private static Vector2 ReferencePoint(Vector2 screen)
        {
            return new Vector2(screen.x * 1280f / Screen.width, (Screen.height - screen.y) * 720f / Screen.height);
        }

        public bool BlocksGameplayPointer(Vector2 screen)
        {
            var point = ReferencePoint(screen);
            return (active != null && PanelRect.Contains(point)) || (consumedFrame == Time.frameCount && consumedRect.Contains(point));
        }

        public bool TryDismissAtScreenPoint(Vector2 screen)
        {
            if (active == null || !PanelRect.Contains(ReferencePoint(screen))) return false;
            consumedFrame = Time.frameCount;
            consumedRect = PanelRect;
            Dismiss();
            return true;
        }

        private void Update()
        {
            // Runs before the bat's Update so a dismissed final line cannot leak its click.
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && TryDismissAtScreenPoint(mouse.position.ReadValue())) return;
            if (active != null && Time.unscaledTime >= expiresAt) Dismiss();
        }

        private void OnGUI()
        {
            if (active == null) return;
            if (speakerStyle == null)
            {
                speakerStyle = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
                speakerStyle.normal.textColor = new Color(.81f, .65f, 1);
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true };
                textStyle.normal.textColor = Color.white;
                dismissStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleRight };
                dismissStyle.normal.textColor = new Color(.76f, .72f, .82f);
            }
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            int oldDepth = GUI.depth;
            GUI.depth = -100;
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            // Keep the fixed coffin/artifact on the right and the toolbar below y=620 clear.
            float coffinLeft = level.bat.roomCamera.WorldToViewportPoint(level.coffin.bounds.min).x * 1280;
            float width = Mathf.Clamp(coffinLeft - 48, 340, 960);
            float height = Mathf.Max(112, textStyle.CalcHeight(new GUIContent(active.text), width - 36) + 59);
            PanelRect = new Rect(24, 612 - height, width, height);
            Fill(PanelRect, new Color(.45f, .24f, .66f, .95f));
            Fill(new Rect(PanelRect.x + 2, PanelRect.y + 2, width - 4, height - 4), new Color(.035f, .025f, .06f, .9f));
            GUI.Label(new Rect(PanelRect.x + 18, PanelRect.y + 9, width - 36, 24), active.characterName.ToUpperInvariant(), speakerStyle);
            GUI.Label(new Rect(PanelRect.x + 18, PanelRect.y + 34, width - 36, height - 54), active.text, textStyle);
            GUI.Label(new Rect(PanelRect.x + 12, PanelRect.yMax - 19, width - 24, 16), "CLICK PANEL TO DISMISS", dismissStyle);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
            GUI.depth = oldDepth;
        }

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}

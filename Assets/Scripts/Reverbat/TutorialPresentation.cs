using UnityEngine;

namespace Reverbat
{
    // Overlay artwork and pointers only; never changes puzzle state or consumes gameplay clicks.
    [DefaultExecutionOrder(200)]
    public sealed class TutorialPresentation : MonoBehaviour
    {
        public Texture2D keyW, keyA, keyS, keyD, mouseIcon;
        private TutorialManager tutorial;
        private Level01Manager level;
        private GUIStyle heading, body, caption, hotbarInstruction;
        public const int MainInstructionFontSize = 24;
        public const float HotbarInstructionScale = 1.15f;
        public static Rect StartingGuideRect => new Rect(350, 240, 580, 100);
        public static Rect HighlightRect(int index) => index == 1 ? Level01Manager.EchoPulseButtonRect : Level01Manager.EchoSightButtonRect;
        public bool HasControlArtwork => keyW != null && keyA != null && keyS != null && keyD != null && mouseIcon != null;

        public void Initialize(TutorialManager source, Level01Manager owner) { tutorial = source; level = owner; }

        private void OnGUI()
        {
            if (tutorial == null || level == null) return;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            int previousDepth = GUI.depth;
            GUI.depth = -200;
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 29, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                heading.normal.textColor = new Color(.83f, .7f, 1);
                body = new GUIStyle(GUI.skin.label) { fontSize = 21, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                body.normal.textColor = Color.white;
                caption = new GUIStyle(body) { fontSize = 19 };
                hotbarInstruction = new GUIStyle(caption) { fontSize = Mathf.RoundToInt(19 * HotbarInstructionScale) };
            }
            if (tutorial.AwaitingFirstClick)
            {
                DrawControls();
                var e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0) { tutorial.DismissControls(); e.Use(); }
            }
            else
            {
                float pulse = .5f + .5f * Mathf.Sin(Time.unscaledTime * 4);
                for (int i = 0; i < 2; i++)
                {
                    if (!tutorial.HighlightsAbility(i)) continue;
                    var target = HighlightRect(i);
                    // Share the real button rectangles with the HUD so the glow cannot drift off target.
                    for (int n = 7; n >= 1; n--)
                        Border(new Rect(target.x - n, target.y - n, target.width + n * 2, target.height + n * 2),
                            new Color(0, 1, 1, (.08f + pulse * .16f) * (8 - n) / 7), 2);
                    Border(target, new Color(.15f, 1, 1, .75f + pulse * .25f), 3);
                }
                if (tutorial.ShowAbilityBarGuide)
                {
                    var guide = StartingGuideRect;
                    Fill(guide, new Color(.025f, .012f, .045f, .92f));
                    DrawReadableText(new Rect(guide.x + 12, guide.y + 8, guide.width - 24, guide.height - 16),
                        "Special abilities — use them to locate things through the room.", hotbarInstruction);
                    DrawArrow(new Vector2(640, 360), new Vector2(Level01Manager.EchoPulseButtonRect.center.x, 628));
                }
                if (tutorial.ShowArtifactGuide)
                {
                    Vector3 viewport = level.bat.roomCamera.WorldToViewportPoint(level.artifact.transform.position);
                    Vector2 target = new Vector2(viewport.x * 1280, (1 - viewport.y) * 720);
                    float labelX = Mathf.Clamp(target.x - 172, 24, 912);
                    float labelY = Mathf.Clamp(target.y - 145, 106, 410);
                    Fill(new Rect(labelX, labelY, 344, 65), new Color(.025f, .012f, .045f, .92f));
                    GUI.Label(new Rect(labelX + 8, labelY + 3, 328, 59), "Fly closer with WASD.\nClick the blood artifact to collect it!", caption);
                    DrawArrow(new Vector2(target.x, target.y - 12), 43);
                }
            }
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        private void DrawControls()
        {
            Fill(new Rect(0, 0, 1280, 720), new Color(.015f, .008f, .025f, 1));
            Border(new Rect(285, 122, 710, 466), new Color(.53f, .32f, .7f), 2);
            GUI.Label(new Rect(310, 148, 660, 53), "BEFORE YOU TAKE FLIGHT", heading);
            Icon(new Rect(443, 225, 76, 76), keyW, "W");
            Icon(new Rect(359, 309, 76, 76), keyA, "A");
            Icon(new Rect(443, 309, 76, 76), keyS, "S");
            Icon(new Rect(527, 309, 76, 76), keyD, "D");
            Icon(new Rect(763, 237, 95, 139), mouseIcon, "MOUSE");
            GUI.Label(new Rect(337, 407, 288, 40), "WASD to move", body);
            GUI.Label(new Rect(665, 407, 298, 40), "Mouse to aim / interact", body);
            GUI.Label(new Rect(310, 510, 660, 40), "Click anywhere to begin", body);
        }

        private void Icon(Rect rect, Texture2D texture, string fallback)
        {
            if (texture != null) GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
            else GUI.Label(rect, fallback, heading);
        }

        private static void DrawArrow(Vector2 tip, float length)
        {
            DrawArrow(tip - new Vector2(0, length), tip);
        }

        private static void DrawArrow(Vector2 from, Vector2 tip)
        {
            float wobble = Mathf.Sin(Time.unscaledTime * 4.5f);
            tip.y += wobble * 4;
            tip.x += Mathf.Sin(Time.unscaledTime * 3) * 3;
            Color color = new Color(.84f, .64f, 1, 1);
            Vector2 direction = (tip - from).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x);
            Line(from + side * wobble * 4, tip, color, 5);
            Line(tip - direction * 20 - side * 16, tip, color, 5);
            Line(tip - direction * 20 + side * 16, tip, color, 5);
        }

        public static void DrawReadableText(Rect rect, string message, GUIStyle style)
        {
            Color previous = GUI.contentColor;
            GUI.contentColor = new Color(0, 0, 0, .65f);
            GUI.Label(new Rect(rect.x + 2, rect.y + 3, rect.width, rect.height), message, style);
            GUI.contentColor = new Color(0, 0, 0, .8f);
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
                if (x != 0 || y != 0) GUI.Label(new Rect(rect.x + x, rect.y + y, rect.width, rect.height), message, style);
            GUI.contentColor = Color.white;
            GUI.Label(rect, message, style);
            GUI.contentColor = previous;
        }

        private static void Line(Vector2 from, Vector2 to, Color color, float width)
        {
            Matrix4x4 old = GUI.matrix;
            Vector2 delta = to - from;
            GUI.matrix = old * Matrix4x4.TRS(from, Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg), Vector3.one);
            Fill(new Rect(0, -width / 2, delta.magnitude, width), color);
            GUI.matrix = old;
        }

        private static void Border(Rect r, Color c, float width)
        {
            Fill(new Rect(r.x, r.y, r.width, width), c);
            Fill(new Rect(r.x, r.yMax - width, r.width, width), c);
            Fill(new Rect(r.x, r.y, width, r.height), c);
            Fill(new Rect(r.xMax - width, r.y, width, r.height), c);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reverbat
{
    public sealed class MainMenu : MonoBehaviour
    {
        public const string VolumeKey = "Reverbat.MasterVolume";
        public const float ArtWidth = 1672, ArtHeight = 941;
        public Texture2D background;
        public bool SettingsOpen { get; private set; }
        public float MasterVolume => AudioListener.volume;
        private GUIStyle button, label, heading;
        private int pressedButton = -1;
        private static readonly Rect backRect = new Rect(650, 744, 372, 76);
        private static readonly Rect volumeRect = new Rect(615, 632, 430, 48);
        private static readonly Vector2[][] outlines =
        {
            new[] {new Vector2(579,414),new Vector2(622,396),new Vector2(834,386),new Vector2(1074,411),new Vector2(1054,525),new Vector2(602,531)},
            new[] {new Vector2(591,579),new Vector2(633,567),new Vector2(844,561),new Vector2(1062,575),new Vector2(1044,683),new Vector2(612,691)},
            new[] {new Vector2(598,738),new Vector2(640,724),new Vector2(827,717),new Vector2(1055,736),new Vector2(1038,840),new Vector2(619,850)}
        };

        private void Awake() { AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1)); }
        private void OnApplicationFocus(bool focused) { if (!focused) pressedButton = -1; }
        public void Play() { StartingCutscene.Requested = true; SceneManager.LoadScene("Level01_Prototype", LoadSceneMode.Single); }
        public void OpenSettings() { pressedButton = -1; SettingsOpen = true; }
        public void Back() { SettingsOpen = false; }
        public void SetMasterVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
            PlayerPrefs.Save();
        }
        public void QuitGame() { Application.Quit(); }

        public static Vector2 ScreenToArtwork(Vector2 point, float width, float height)
        {
            float scale = Mathf.Min(width / ArtWidth, height / ArtHeight);
            return new Vector2((point.x - (width - ArtWidth * scale) / 2) / scale,
                (height - point.y - (height - ArtHeight * scale) / 2) / scale);
        }

        public static int HitTestArtwork(Vector2 point)
        {
            for (int k = 0; k < outlines.Length; k++)
            {
                bool inside = false;
                var polygon = outlines[k];
                for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
                {
                    var a = polygon[i]; var b = polygon[j];
                    if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                        inside = !inside;
                }
                if (inside) return k;
            }
            return -1;
        }

        // Uses the same polygon hit areas as the visible menu; also usable by automated input checks.
        public bool HandleScreenClick(Vector2 point)
        {
            Vector2 artworkPoint = ScreenToArtwork(point, Screen.width, Screen.height);
            int index = HitTestTarget(artworkPoint);
            if (index < 0) return false;
            ActivateTarget(index, artworkPoint);
            return true;
        }

        private int HitTestTarget(Vector2 point) => SettingsOpen ? (backRect.Contains(point) ? 3 : volumeRect.Contains(point) ? 4 : -1) : HitTestArtwork(point);

        private void ActivateTarget(int index, Vector2 point)
        {
            if (index == 3) Back();
            else if (index == 4) SetMasterVolume((point.x - volumeRect.x) / volumeRect.width);
            else Activate(index);
        }

        private void Activate(int index)
        {
            if (index == 0) Play();
            else if (index == 1) OpenSettings();
            else if (index == 2) QuitGame();
        }

        private void OnGUI()
        {
            // Capture pointer coordinates before applying the artwork's letterbox transform.
            Vector2 screenPointer = Event.current.mousePosition;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                label.normal.textColor = new Color(.96f, .88f, .72f);
                heading = new GUIStyle(label) { fontSize = 38, fontStyle = FontStyle.Bold };
                button = new GUIStyle(GUI.skin.button) { fontSize = 30 };
            }
            Matrix4x4 old = GUI.matrix;
            Color color = GUI.color;
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(.09f, .045f, .12f));
            float scale = Mathf.Min(Screen.width / ArtWidth, Screen.height / ArtHeight);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - ArtWidth * scale) / 2, (Screen.height - ArtHeight * scale) / 2), Quaternion.identity, Vector3.one * scale);
            GUI.color = Color.white;
            if (background != null) GUI.DrawTexture(new Rect(0, 0, ArtWidth, ArtHeight), background, ScaleMode.StretchToFill);
            Vector2 artworkPointer = ScreenToArtwork(new Vector2(screenPointer.x, Screen.height - screenPointer.y), Screen.width, Screen.height);
            if (!SettingsOpen)
            {
                int hover = HitTestArtwork(artworkPointer);
                if (hover >= 0) Outline(outlines[hover], new Color(1, .92f, .73f, .65f));
            }
            else
            {
                Fill(new Rect(515, 365, 650, 520), new Color(.46f, .27f, .51f));
                Fill(new Rect(518, 368, 644, 514), new Color(.075f, .035f, .105f));
                GUI.Label(new Rect(560, 410, 560, 70), "SETTINGS", heading);
                GUI.Label(new Rect(560, 520, 560, 60), "Master Volume  " + Mathf.RoundToInt(MasterVolume * 100) + "%", label);
                Fill(new Rect(volumeRect.x, 651, volumeRect.width, 6), new Color(.28f, .17f, .34f));
                Fill(new Rect(volumeRect.x, 651, volumeRect.width * MasterVolume, 6), new Color(.83f, .64f, .91f));
                Fill(new Rect(volumeRect.x + volumeRect.width * MasterVolume - 8, 642, 16, 24), new Color(.96f, .88f, .72f));
                GUI.Box(backRect, "Back", button);
            }
            HandleArtworkInput(Event.current, artworkPointer);
            GUI.matrix = old;
            GUI.color = color;
        }

        private void HandleArtworkInput(Event e, Vector2 point)
        {
            int target = HitTestTarget(point);
            if (e.type == EventType.MouseDown && e.button == 0 && target >= 0)
            {
                pressedButton = target;
                if (target == 4) SetMasterVolume((point.x - volumeRect.x) / volumeRect.width);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && pressedButton == 4)
            {
                SetMasterVolume((point.x - volumeRect.x) / volumeRect.width);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0 && pressedButton >= 0)
            {
                int pressed = pressedButton;
                pressedButton = -1;
                e.Use();
                if (pressed == 4) SetMasterVolume((point.x - volumeRect.x) / volumeRect.width);
                else if (pressed == target)
                {
                    ActivateTarget(pressed, point);
                }
            }
        }

        private static void Outline(Vector2[] points, Color color)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Matrix4x4 old = GUI.matrix;
                Vector2 start = points[i], delta = points[(i + 1) % points.Length] - start;
                GUI.matrix = old * Matrix4x4.TRS(start, Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg), Vector3.one);
                Fill(new Rect(0, -1, delta.magnitude, 2), color);
                GUI.matrix = old;
            }
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
    }
}

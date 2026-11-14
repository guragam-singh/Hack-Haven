using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reverbat
{
    public sealed class MainMenu : MonoBehaviour
    {
        public const string VolumeKey = "Reverbat.MasterVolume";
        public bool SettingsOpen { get; private set; }
        public float MasterVolume => AudioListener.volume;
        private GUIStyle title, button, label;

        private void Awake() { AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1)); }
        public void Play() { SceneManager.LoadScene("Level01_Prototype", LoadSceneMode.Single); }
        public void OpenSettings() { SettingsOpen = true; }
        public void Back() { SettingsOpen = false; }
        public void SetMasterVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, AudioListener.volume);
            PlayerPrefs.Save();
        }
        public void QuitGame() { Application.Quit(); }

        private void OnGUI()
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                title.normal.textColor = new Color(.8f, .64f, 1);
                label = new GUIStyle(GUI.skin.label) { fontSize = 23, alignment = TextAnchor.MiddleCenter };
                label.normal.textColor = Color.white;
                button = new GUIStyle(GUI.skin.button) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
                button.normal.background = button.hover.background = button.active.background = null;
                button.normal.textColor = button.hover.textColor = button.active.textColor = Color.white;
            }
            Matrix4x4 old = GUI.matrix;
            Color color = GUI.color;
            GUI.color = new Color(.035f, .025f, .055f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2), Quaternion.identity, Vector3.one * scale);
            GUI.Label(new Rect(240, 115, 800, 100), "REVERBAT", title);
            if (!SettingsOpen)
            {
                if (Button(new Rect(420, 290, 440, 60), "Play")) Play();
                if (Button(new Rect(420, 370, 440, 60), "Settings")) OpenSettings();
                if (Button(new Rect(420, 450, 440, 60), "Quit Game")) QuitGame();
            }
            else
            {
                GUI.Label(new Rect(420, 255, 440, 45), "SETTINGS", label);
                GUI.Label(new Rect(420, 320, 440, 40), "Master Volume  " + Mathf.RoundToInt(MasterVolume * 100) + "%", label);
                float volume = GUI.HorizontalSlider(new Rect(440, 385, 400, 30), MasterVolume, 0, 1);
                if (!Mathf.Approximately(volume, MasterVolume)) SetMasterVolume(volume);
                if (Button(new Rect(420, 450, 440, 60), "Back")) Back();
            }
            GUI.matrix = old;
            GUI.color = color;
        }

        private bool Button(Rect rect, string caption)
        {
            GUI.color = new Color(.42f, .22f, .61f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(.1f, .065f, .15f);
            GUI.DrawTexture(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), Texture2D.whiteTexture);
            GUI.color = Color.white;
            return GUI.Button(rect, caption, button);
        }
    }
}

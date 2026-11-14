using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace Reverbat
{
    // Plays over the loaded level, allowing its existing music source to continue uninterrupted.
    [DefaultExecutionOrder(500)]
    public sealed class StartingCutscene : MonoBehaviour
    {
        public static bool Requested;
        private static bool active;
        private static int releasedFrame = -1;
        public static bool IsActive => active || releasedFrame == Time.frameCount;
        [Min(.1f)] public float fadeDuration = .8f;
        private VideoPlayer player;
        private AudioSource audio;
        private RenderTexture picture;
        private bool finished, failed;
        private float fade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() { Requested = active = false; releasedFrame = -1; }

        private void Awake() { active = true; Requested = false; }

        private IEnumerator Start()
        {
            audio = gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0;
            picture = new RenderTexture(1280, 720, 0);
            picture.Create();
            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = false;
            player.waitForFirstFrame = true;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = picture;
            player.aspectRatio = VideoAspectRatio.FitInside;
            player.source = VideoSource.Url;
            player.url = Path.Combine(Application.streamingAssetsPath, "Cutscenes", "Starting cutscene.mp4");
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.controlledAudioTrackCount = 1;
            player.SetTargetAudioSource(0, audio);
            player.errorReceived += OnError;
            player.loopPointReached += OnFinished;
            player.Prepare();
            float timeout = Time.realtimeSinceStartup + 15;
            while (!player.isPrepared && !failed && Time.realtimeSinceStartup < timeout) yield return null;
            if (player.isPrepared && !failed)
            {
                player.Play();
                while (!finished && !failed) yield return null;
            }
            else if (!failed) Debug.LogWarning("[REVERBAT] Cutscene preparation timed out; continuing to controls.");
            float started = Time.unscaledTime;
            while (Time.unscaledTime - started < fadeDuration)
            {
                fade = Mathf.Clamp01((Time.unscaledTime - started) / fadeDuration);
                yield return null;
            }
            active = false;
            releasedFrame = Time.frameCount;
            Destroy(this);
        }

        private void OnFinished(VideoPlayer source) { finished = true; }
        private void OnError(VideoPlayer source, string message) { failed = true; Debug.LogWarning("[REVERBAT] Cutscene: " + message); }

        private void OnGUI()
        {
            var oldMatrix = GUI.matrix;
            var oldColor = GUI.color;
            GUI.depth = -1000;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (player != null && player.isPrepared && picture != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), picture, ScaleMode.ScaleToFit);
            GUI.color = new Color(0, 0, 0, fade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void OnDestroy()
        {
            active = false;
            if (player != null) { player.Stop(); Destroy(player); }
            if (audio != null) Destroy(audio);
            if (picture != null) { picture.Release(); Destroy(picture); }
        }
    }
}

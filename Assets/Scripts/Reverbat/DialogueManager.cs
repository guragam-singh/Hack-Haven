using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reverbat
{
    // Audio-only event queue. Tutorial text is rendered independently by the tutorial HUD.
    [DefaultExecutionOrder(-100)]
    public sealed class DialogueManager : MonoBehaviour
    {
        [SerializeField] private DialogueLibrary content;
        private readonly Queue<DialogueEntry> pending = new Queue<DialogueEntry>();
        private readonly HashSet<string> triggered = new HashSet<string>();
        private Level01Manager level;
        private DialogueEntry active;
        private AudioSource voice;
        private float finishesAt;
        public event Action<DialogueEntry> VoiceStarted;
        public event Action<DialogueEntry> VoiceFinished;
        public string ActiveId => active == null ? null : active.triggerId;
        public bool IsShowing => active != null;
        public int PendingCount => pending.Count;
        public DialogueLibrary Content => content;
        public AudioSource VoiceSource => voice;

        public void Initialize(Level01Manager source)
        {
            level = source;
            if (content == null) content = Resources.Load<DialogueLibrary>("Level01Dialogue");
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.loop = false;
            voice.spatialBlend = 0;
            voice.volume = 1;
            voice.ignoreListenerVolume = false;
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MainMenu.VolumeKey, AudioListener.volume));
            if (FindFirstObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            level.DialogueTriggered += OnTrigger;
            if (content == null) Debug.LogError("[REVERBAT] Missing Resources/Level01Dialogue audio library.");
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
            if (entry == null || entry.voiceClip == null || !triggered.Add(id)) return false;
            pending.Enqueue(entry);
            if (active == null) ShowNext();
            return true;
        }

        private void ShowNext()
        {
            if (pending.Count == 0) { active = null; return; }
            active = pending.Dequeue();
            voice.clip = active.voiceClip;
            voice.Play();
            finishesAt = Time.unscaledTime + active.voiceClip.length;
            VoiceStarted?.Invoke(active);
        }

        private void Update()
        {
            // One source plays full recordings. Mouse clicks never interrupt or skip speech.
            if (active == null || Time.unscaledTime < finishesAt || voice.isPlaying) return;
            VoiceFinished?.Invoke(active);
            active = null;
            ShowNext();
        }
    }
}

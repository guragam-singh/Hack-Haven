using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reverbat
{
    [Serializable]
    public sealed class TutorialEntry
    {
        public string triggerId;
        [TextArea(1, 3)] public string text;

        public TutorialEntry(string id, string message) { triggerId = id; text = message; }
    }

    // Presentation only. Entries are ordered milestones; late events cannot restore outdated steps.
    [DefaultExecutionOrder(-200)]
    public sealed class TutorialManager : MonoBehaviour
    {
        [SerializeField] private TutorialEntry[] entries =
        {
            new TutorialEntry(DialogueIds.LevelBegin, "Only the coffin is visible. Use Echolocation to reveal the room."),
            new TutorialEntry(DialogueIds.FirstPulse, "Find a way to open the coffin."),
            new TutorialEntry(DialogueIds.Tilt, "Maybe combine Echo Sight with reflection?"),
            new TutorialEntry(DialogueIds.Coffin, "Collect the blood artifact!"),
            new TutorialEntry(DialogueIds.Artifact, "Level 1 Complete!")
        };
        [Header("Optional hint after the first emitted pulse")]
        [Min(0)] public float hintDelay = 10;
        [Min(.1f)] public float hintDuration = 5;
        [SerializeField, TextArea(1, 3)] private string sittingHint = "Maybe try sitting down for a bit...";
        private readonly HashSet<string> triggered = new HashSet<string>();
        private Level01Manager level;
        private int currentIndex = -1;
        private float hintAt = float.PositiveInfinity;
        private float hintUntil;
        private bool hintShown, hintActive;
        private int dismissedFrame = -1;
        private bool beginPending;
        public bool AwaitingFirstClick { get; private set; } = true;
        public bool BlocksGameplayInput => StartingCutscene.IsActive || AwaitingFirstClick || dismissedFrame == Time.frameCount;
        public bool ShowAbilityBarGuide => !AwaitingFirstClick && CurrentTriggerId == DialogueIds.LevelBegin;
        public bool ShowArtifactGuide => !AwaitingFirstClick && level != null && level.CoffinOpen && !level.Complete && level.artifact.activeSelf;
        public string CurrentMessage => hintActive ? sittingHint : currentIndex < 0 ? string.Empty : entries[currentIndex].text;
        public string CurrentTriggerId => currentIndex < 0 ? null : entries[currentIndex].triggerId;
        public bool HintShown => hintShown;
        public bool HintActive => hintActive;

        public void Initialize(Level01Manager source)
        {
            if (level != null) level.DialogueTriggered -= OnProgress;
            level = source;
            level.DialogueTriggered += OnProgress;
            var presentation = GetComponent<TutorialPresentation>();
            if (presentation == null) presentation = gameObject.AddComponent<TutorialPresentation>();
            presentation.Initialize(this, source);
        }

        public bool DismissControls()
        {
            if (StartingCutscene.IsActive || !AwaitingFirstClick) return false;
            AwaitingFirstClick = false;
            dismissedFrame = Time.frameCount;
            beginPending = true;
            return true;
        }

        public bool HighlightsAbility(int index) => !AwaitingFirstClick &&
            ((CurrentTriggerId == DialogueIds.LevelBegin && index == 1) ||
             (CurrentTriggerId == DialogueIds.Tilt && index == 0));

        private void OnDestroy()
        {
            if (level != null) level.DialogueTriggered -= OnProgress;
        }

        public bool HasTriggered(string id) => triggered.Contains(id);

        private void OnProgress(string id)
        {
            int index = Array.FindIndex(entries, entry => entry.triggerId == id);
            if (index < 0 || !triggered.Add(id) || index <= currentIndex) return;
            currentIndex = index;
            hintActive = false;
            // Selecting an ability never emits this event. Start the timer when discovery is possible.
            if (id == DialogueIds.FirstPulse) hintAt = Time.time + hintDelay;
        }

        private void Update()
        {
            if (StartingCutscene.IsActive) return;
            if (level == null) return;
            if (AwaitingFirstClick)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) DismissControls();
                return;
            }
            // Start on the following frame so the opening click cannot dismiss dialogue underneath.
            if (beginPending && dismissedFrame != Time.frameCount)
            {
                beginPending = false;
                level.BeginLevel();
            }
            bool tilted = level.tiltable != null && (level.tiltable.HasBeenTiltedToCorrectAngle || level.tiltable.CorrectAngle);
            if (tilted || level.CoffinOpen || level.Complete)
            {
                hintActive = false;
                hintAt = float.PositiveInfinity;
                return;
            }
            if (hintActive && Time.time >= hintUntil) hintActive = false;
            if (!hintShown && CurrentTriggerId == DialogueIds.FirstPulse && Time.time >= hintAt)
            {
                hintShown = hintActive = true;
                hintUntil = Time.time + hintDuration;
            }
        }
    }
}

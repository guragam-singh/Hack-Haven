using UnityEngine;

namespace Reverbat
{
    // Visual state only. Collider geometry, movement and the puzzle remain owned by existing scripts.
    public sealed class Level01ArtPresentation : MonoBehaviour
    {
        public Sprite openCoffin;
        public Texture2D echoSightIcon;
        public Texture2D echoPulseIcon;
        public Texture2D lockedIcon;
        private Level01Manager level;

        private void OnEnable()
        {
            level = GetComponent<Level01Manager>();
            if (level != null) level.DialogueTriggered += OnLevelEvent;
        }

        private void OnDisable()
        {
            if (level != null) level.DialogueTriggered -= OnLevelEvent;
        }

        private void OnLevelEvent(string id)
        {
            if (id != DialogueIds.Coffin || openCoffin == null || !level.CoffinOpen) return;
            Vector3 originalScale = level.coffin.transform.localScale;
            level.coffin.sprite = openCoffin;
            level.coffin.transform.localScale = originalScale;
            level.coffin.color = Color.white;
            // Both state drawings retain their natural proportions at the same visible height.
            const float height = 1.8f;
            float width = height * openCoffin.rect.width / openCoffin.rect.height;
            level.coffin.size = new Vector2(width / Mathf.Abs(level.coffin.transform.lossyScale.x), height / Mathf.Abs(level.coffin.transform.lossyScale.y));
        }

        public void DrawAbilityIcons()
        {
            Color old = GUI.color;
            GUI.color = Color.white;
            AbilityToolbar.DrawIcons(echoSightIcon, echoPulseIcon, level.Complete ? null : lockedIcon, lockedIcon);
            GUI.color = old;
        }

    }
}

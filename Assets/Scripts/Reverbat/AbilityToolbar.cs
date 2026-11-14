using UnityEngine;

namespace Reverbat
{
    public static class AbilityToolbar
    {
        public static Rect RectFor(int index) => new Rect(index == 3 ? 960 : 20 + 310 * index, 635, index < 2 ? 290 : 300, 43);
        public static int HitTest(Vector2 point)
        {
            for (int index = 0; index < 4; index++)
                if (RectFor(index).Contains(point)) return index;
            return -1;
        }

        public static void Draw(BatController2D bat, GUIStyle button, bool inputEnabled)
        {
            Color previous = GUI.backgroundColor;
            bool enabled = GUI.enabled;
            for (int i = 0; i < 4; i++)
            {
                bool unlocked = bat.IsAbilityUnlocked(i);
                GUI.enabled = enabled && inputEnabled && unlocked;
                GUI.backgroundColor = bat.SelectedAbility == i ? Color.cyan : Color.white;
                string label = i == 0 ? "1  Echo Sight" : i == 1 ? "2  Echo Pulse" :
                    i == 2 ? (unlocked ? "3  Resonance / Unlocked" : "3  Resonance / Locked") : "Mimicry / Locked";
                if (GUI.Button(RectFor(i), label, button)) bat.SelectAbility(i);
            }
            GUI.backgroundColor = previous;
            GUI.enabled = enabled;
        }

        public static void DrawIcons(Texture2D sight, Texture2D pulse, Texture2D resonance, Texture2D locked)
        {
            Icon(0, sight);
            Icon(1, pulse);
            Icon(2, resonance);
            Icon(3, locked);
        }

        private static void Icon(int index, Texture2D icon)
        {
            if (icon != null) GUI.DrawTexture(new Rect(RectFor(index).x+10,638,35,35), icon, ScaleMode.ScaleToFit,true);
        }
    }
}

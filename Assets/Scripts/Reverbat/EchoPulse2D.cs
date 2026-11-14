using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Reverbat
{
    public sealed class EchoPulse2D : MonoBehaviour
    {
        private readonly Dictionary<SpriteRenderer, float> visibleUntil = new Dictionary<SpriteRenderer, float>();
        private Level01Manager level;
        private LineRenderer ripple;
        private bool emitting;
        public bool IsEmitting => emitting;

        public void Initialize(Level01Manager manager)
        {
            level = manager;
            level.bat.roomCamera.backgroundColor = Color.black;
            foreach (var sprite in level.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sprite == level.coffin || sprite.gameObject == level.artifact) continue;
                visibleUntil[sprite] = -100;
                sprite.enabled = false;
            }
            var obj = new GameObject("Echo Pulse Ripple");
            obj.transform.SetParent(transform);
            ripple = obj.AddComponent<LineRenderer>();
            ripple.sharedMaterial = level.bat.echo.outgoing.sharedMaterial;
            ripple.startColor = ripple.endColor = Color.cyan;
            ripple.startWidth = ripple.endWidth = .035f;
            ripple.positionCount = 65;
            ripple.sortingOrder = 15;
            ripple.useWorldSpace = true;
            ripple.enabled = false;
        }

        public bool Visible(SpriteRenderer sprite) => sprite != null && sprite.enabled;

        private void Update()
        {
            foreach (var item in visibleUntil)
            {
                float remaining = item.Value - Time.time;
                item.Key.enabled = remaining > 0;
                Color color = item.Key.color;
                color.a = Mathf.Clamp01(remaining / 2f);
                item.Key.color = color;
            }
        }

        public bool Emit()
        {
            if (emitting || level.Complete) return false;
            StartCoroutine(Pulse());
            return true;
        }

        private IEnumerator Pulse()
        {
            emitting = true;
            ripple.enabled = true;
            Vector2 origin = level.bat.transform.position;
            for (float radius = .01f; radius < 20; radius += Time.deltaTime * 10)
            {
                for (int i = 0; i < 65; i++)
                {
                    float angle = i * Mathf.PI * 2 / 64;
                    ripple.SetPosition(i, origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                }
                // Only stamp once per object, at the expanding wavefront, so fading is consistent.
                foreach (var item in visibleUntil)
                    if (!reached.Contains(item.Key) && Vector2.Distance(origin, item.Key.transform.position) <= radius)
                        reached.Add(item.Key);
                foreach (var sprite in reached)
                    if (!stamped.Contains(sprite)) { visibleUntil[sprite] = Time.time + 5; stamped.Add(sprite); }
                yield return null;
            }
            ripple.enabled = false;
            emitting = false;
            reached.Clear(); stamped.Clear();
        }
        private readonly HashSet<SpriteRenderer> reached = new HashSet<SpriteRenderer>();
        private readonly HashSet<SpriteRenderer> stamped = new HashSet<SpriteRenderer>();
    }
}

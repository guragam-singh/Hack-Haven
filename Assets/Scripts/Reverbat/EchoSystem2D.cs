using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Reverbat
{
    public sealed class EchoSystem2D : MonoBehaviour
    {
        public Level01Manager level;
        public LineRenderer outgoing;
        public LineRenderer returning;
        public float cooldown = .7f;
        public float pulseSpeed = 18;
        public int maxReflections = 3;
        public bool IsEmitting { get; private set; }
        public int LastReflectionCount { get; private set; }
        public string LastHit { get; private set; }
        private float readyAt;

        private struct Contact
        {
            public SoundInteractable surface;
            public bool reflected;
        }

        public bool Emit(Vector2 target)
        {
            if (IsEmitting || Time.time < readyAt || level.Complete) return false;
            Vector2 origin = transform.position;
            Vector2 direction = (target - origin).normalized;
            if (direction.sqrMagnitude < .01f) return false;
            readyAt = Time.time + cooldown;
            Physics2D.SyncTransforms();
            var points = new List<Vector3> { origin };
            var contacts = new List<Contact>();
            LastReflectionCount = 0;
            LastHit = "None";
            // Normal surfaces finish the outgoing trace; return travels back over its segments.
            for (int bounce = 0; bounce <= maxReflections; bounce++)
            {
                RaycastHit2D hit = Physics2D.Raycast(origin, direction, 30);
                if (hit.collider == null) { points.Add(origin + direction * 20); break; }
                points.Add(hit.point);
                var surface = hit.collider.GetComponent<SoundInteractable>();
                LastHit = hit.collider.name;
                contacts.Add(new Contact { surface = surface, reflected = LastReflectionCount > 0 });
                if (surface == null || surface.response != SoundResponse.Reflector || bounce == maxReflections) break;
                LastReflectionCount++;
                direction = Vector2.Reflect(direction, hit.normal).normalized;
                origin = hit.point + direction * .035f;
            }
            StartCoroutine(PlayPulse(points, contacts));
            return true;
        }

        private IEnumerator PlayPulse(List<Vector3> points, List<Contact> contacts)
        {
            IsEmitting = true;
            outgoing.enabled = true;
            returning.enabled = false;
            for (int i = 1; i < points.Count; i++)
            {
                yield return DrawSegment(outgoing, points, i);
                if (i - 1 < contacts.Count && contacts[i - 1].surface != null)
                    contacts[i - 1].surface.OnSoundHit(contacts[i - 1].reflected);
            }
            var reversed = new List<Vector3>(points);
            reversed.Reverse();
            // A scripted retrace makes the received echo unmistakable; forward hits are real raycasts.
            // Follow the bat's current position if another input moved it during the pulse.
            reversed[reversed.Count - 1] = transform.position;
            returning.enabled = true;
            for (int i = 1; i < reversed.Count; i++) yield return DrawSegment(returning, reversed, i);
            bool useful = false;
            foreach (var contact in contacts)
            {
                if (contact.surface == null) continue;
                contact.surface.OnEchoReturned();
                if (contact.surface.response == SoundResponse.HiddenMechanism || contact.surface.response == SoundResponse.Bell) useful = true;
            }
            if (!useful) level.ShowHint(LastReflectionCount > 0 ? "Sound redirects along the reflector's angle." : "No useful response");
            yield return new WaitForSeconds(.18f);
            outgoing.enabled = returning.enabled = false;
            IsEmitting = false;
        }

        private IEnumerator DrawSegment(LineRenderer line, List<Vector3> points, int end)
        {
            line.positionCount = end + 1;
            for (int i = 0; i < end; i++) line.SetPosition(i, points[i]);
            float distance = Vector3.Distance(points[end - 1], points[end]);
            float duration = Mathf.Max(.03f, distance / pulseSpeed);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                line.SetPosition(end, Vector3.Lerp(points[end - 1], points[end], elapsed / duration));
                yield return null;
            }
            line.SetPosition(end, points[end]);
        }
    }
}

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
        public bool IsEmitting { get; private set; }
        public int LastReflectionCount { get; private set; }
        public string LastHit { get; private set; }
        public bool LastScriptedRoute { get; private set; }
        public bool ValidPuzzleRoute => IsEmitting && LastScriptedRoute
            && level.tiltable.CorrectAngle && level.tiltable.HasBeenTiltedToCorrectAngle;
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
            LastScriptedRoute = false;
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, 30);
            var plankCollider = level.reflector.GetComponent<Collider2D>();
            Vector2 closest = plankCollider.ClosestPoint(target);
            bool aimedAtPlank = Vector2.Distance(closest, target) <= .25f;
            // Small click forgiveness around the thin plank. Deliberate left-half clicks stay left
            // even when a ray from the right would enter the plank through its right edge first.
            bool plankHit = aimedAtPlank || hit.collider == plankCollider;
            if (plankHit)
            {
                level.NotifyDialogue(DialogueIds.Reflector);
                Vector2 impact = aimedAtPlank ? closest : hit.point;
                points.Add(impact);
                contacts.Add(new Contact { surface = plankCollider.GetComponent<SoundInteractable>() });
                LastHit = plankCollider.name;
                bool batOnRight = level.bat.roomCamera.WorldToViewportPoint(origin).x > .5f;
                bool rightHalf = (aimedAtPlank ? target.x : impact.x) > level.reflector.position.x;
                LastReflectionCount = 1;
                if (batOnRight && rightHalf && level.tiltable.CorrectAngle && level.tiltable.HasBeenTiltedToCorrectAngle)
                {
                    // Scripted puzzle route, gated by the bat having tilted the plank correctly.
                    LastScriptedRoute = true;
                    LastReflectionCount = 1;
                    points.Add(level.bell.transform.position);
                    points.Add(level.coffin.transform.position);
                    contacts.Add(new Contact { surface = level.bell.GetComponent<SoundInteractable>(), reflected = true });
                    contacts.Add(new Contact { surface = level.coffin.GetComponent<SoundInteractable>(), reflected = true });
                    LastHit = level.coffin.name;
                }
                else
                {
                    // Deliberately harmless scripted deflection; no realistic reflection math.
                    points.Add((Vector3)impact + new Vector3(rightHalf ? 3 : -3, rightHalf ? 1.5f : -.8f, 0));
                    level.ShowHint("Sit on the right half to tilt to -45° first. Then, from the right, echo the plank's right half.");
                }
            }
            else if (hit.collider != null)
            {
                points.Add(hit.point);
                LastHit = hit.collider.name;
                contacts.Add(new Contact { surface = hit.collider.GetComponent<SoundInteractable>() });
            }
            else points.Add(origin + direction * 20);
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
            // Retrace the outgoing path to show the echo returning to its source.
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
            if (!useful && LastHit != "Reflector") level.ShowHint("No useful response");
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
                if (line == returning && end == points.Count - 1) points[end] = transform.position;
                line.SetPosition(end, Vector3.Lerp(points[end - 1], points[end], elapsed / duration));
                yield return null;
            }
            line.SetPosition(end, points[end]);
        }
    }
}

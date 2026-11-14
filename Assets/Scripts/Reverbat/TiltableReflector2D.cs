using UnityEngine;

namespace Reverbat
{
    // A constrained seesaw: the bat is the load and is carried by the rotating surface.
    // The settled tilt stays latched when the bat flies off to aim a sound beam.
    public sealed class TiltableReflector2D : MonoBehaviour
    {
        public Level01Manager level;
        public float Angle => Mathf.DeltaAngle(0, transform.eulerAngles.z);
        public bool CorrectAngle => Mathf.Abs(Mathf.DeltaAngle(Angle, -45)) <= 10.001f;
        public bool HasBeenTiltedToCorrectAngle { get; private set; }
        private int landingSide;
        private bool landed;
        public Vector2 LaunchPoint => new Vector2(-1.5f, 3.214286f);

        private void Awake() { transform.rotation = Quaternion.identity; }

        public Vector2 EndPosition(int side)
        {
            return (Vector2)transform.position + (Vector2)transform.right * (side * 1.12f)
                + (Vector2)transform.up * (level.bat.radius + .16f);
        }

        public bool TryBoard(Vector2 point)
        {
            Vector2 local = transform.InverseTransformPoint(point);
            if (Mathf.Abs(local.x) < .25f || Mathf.Abs(local.x) > .75f || Mathf.Abs(local.y) > 5) return false;
            level.NotifyDialogue(DialogueIds.Reflector);
            landingSide = local.x > 0 ? 1 : -1;
            landed = false;
            level.bat.MoveTo(EndPosition(landingSide), false);
            level.ShowHint("Fly onto the reflector end. Your weight tilts that side down.");
            return true;
        }

        public void Leave() { landingSide = 0; landed = false; }

        private void LateUpdate()
        {
            if (landingSide == 0) return;
            if (!landed && Vector2.Distance(level.bat.transform.position, EndPosition(landingSide)) > .12f) return;
            landed = true;
            level.bat.StopMoving();
            float target = landingSide > 0 ? -45 : 45;
            float angle = Mathf.MoveTowardsAngle(Angle, target, 90 * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, 0, angle);
            if (CorrectAngle)
            {
                if (!HasBeenTiltedToCorrectAngle) level.NotifyDialogue(DialogueIds.Tilt);
                HasBeenTiltedToCorrectAngle = true;
            }
            level.bat.transform.position = EndPosition(landingSide);
            Physics2D.SyncTransforms();
        }
    }
}

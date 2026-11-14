using UnityEngine;

namespace Reverbat
{
    // A forgiving scripted seesaw: contact on a half latches a target; the bat is never teleported.
    public sealed class TiltableReflector2D : MonoBehaviour
    {
        public Level01Manager level;
        [Min(.05f)] public float tiltSmoothTime=.18f;
        public float Angle => Mathf.DeltaAngle(0,transform.eulerAngles.z);
        public bool CorrectAngle => Mathf.Abs(Mathf.DeltaAngle(Angle,-45))<=10.001f;
        public bool HasBeenTiltedToCorrectAngle {get;private set;}
        public bool IsTurning => Mathf.Abs(Mathf.DeltaAngle(Angle,targetAngle))>.1f;
        private float targetAngle,angularVelocity;
        private bool rightHalfWeighted;
        private Collider2D surface;
        private void Awake(){transform.rotation=Quaternion.identity; surface=GetComponent<Collider2D>();}
        public Vector2 EndPosition(int side)
        {
            return (Vector2)transform.position+(Vector2)transform.right*(side*1.12f)+(Vector2)transform.up*(level.bat.radius+.16f);
        }
        public bool TryBoard(Vector2 point)
        {
            if(level==null || surface==null) return false;
            Vector2 bat=level.bat.transform.position;
            Vector2 delta=bat-(Vector2)transform.position;
            float height=Vector2.Dot(delta,transform.up);
            float span=Mathf.Abs(Vector2.Dot(delta,transform.right));
            // A generous area above either half replaces the tiny end-point target.
            if(Mathf.Abs(delta.x)<.08f || span>1.5f || height<-.05f || height>level.bat.radius+.5f || Vector2.Distance(bat,surface.ClosestPoint(bat))>level.bat.radius+.5f) return false;
            bool right=delta.x>0;
            if((point.x>transform.position.x)!=right) return false;
            float next=right?-45:45;
            if(IsTurning && Mathf.Abs(Mathf.DeltaAngle(targetAngle,next))>1) return false;
            targetAngle=next;
            rightHalfWeighted|=right;
            level.NotifyDialogue(DialogueIds.Reflector);
            return true;
        }
        // Kept for the movement controller; moving away does not cancel an already latched tilt.
        public void Leave() { }
        private void LateUpdate()
        {
            if(level==null) return;
            TryBoard(level.bat.transform.position);
            float angle=Mathf.SmoothDampAngle(Angle,targetAngle,ref angularVelocity,tiltSmoothTime,Mathf.Infinity,Time.deltaTime);
            if(Mathf.Abs(Mathf.DeltaAngle(angle,targetAngle))<.05f){angle=targetAngle;angularVelocity=0;}
            if(Mathf.Abs(Mathf.DeltaAngle(Angle,angle))>.0001f)
            {
                transform.rotation=Quaternion.Euler(0,0,angle);
                Physics2D.SyncTransforms();
            }
            if(rightHalfWeighted && CorrectAngle)
            {
                if(!HasBeenTiltedToCorrectAngle) level.NotifyDialogue(DialogueIds.Tilt);
                HasBeenTiltedToCorrectAngle=true;
            }
        }
    }
}

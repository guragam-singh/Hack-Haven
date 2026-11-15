using UnityEngine;

namespace Reverbat.Boss
{
    // Poses affect only the presentation pivot and the assigned joints, never the combat anchors.
    public sealed class BossDuelActor : MonoBehaviour
    {
        public bool isBat;
        public Transform modelPivot, leftJoint, rightJoint, cape;
        public Vector3 leftAxis=Vector3.forward, rightAxis=Vector3.forward;
        public float restArmAngle;
        private Quaternion leftBind,rightBind;
        private Vector3 capeScale;
        private Vector3 offset,euler;
        private float spread,rightLift,life=1;
        public Vector3 Center => modelPivot.position+Vector3.up*(isBat?0:1.75f);
        public Vector3 Hand => modelPivot.TransformPoint(new Vector3(-.83f,2.38f,.33f));
        public void Initialize()
        {
            leftBind=Quaternion.AngleAxis(-restArmAngle,leftAxis)*leftJoint.localRotation; rightBind=Quaternion.AngleAxis(restArmAngle,rightAxis)*rightJoint.localRotation;
            if(cape) capeScale=cape.localScale;
        }
        public void Pose(Vector3 translation,Vector3 rotation,float openness=0,float raisedArm=0,float idle=1)
        {
            offset=translation;euler=rotation;spread=openness;rightLift=raisedArm;life=idle;
        }
        public void Neutral() => Pose(Vector3.zero,Vector3.zero);
        private void LateUpdate()
        {
            float beat=Mathf.Sin(Time.time*(isBat?9.2f:2.3f));
            float hover=isBat?.10f*Mathf.Sin(Time.time*3.1f):.017f*beat;
            modelPivot.localPosition=offset+Vector3.up*hover*life;
            modelPivot.localRotation=Quaternion.Euler(euler+new Vector3(isBat?beat*1.8f:beat*.5f,0,isBat?Mathf.Sin(Time.time*2)*3:0)*life);
            float joint=restArmAngle+(isBat?beat*20:beat*2)*life+spread;
            leftJoint.localRotation=Quaternion.AngleAxis(joint,leftAxis)*leftBind;
            rightJoint.localRotation=Quaternion.AngleAxis(-joint-rightLift,rightAxis)*rightBind;
            if(cape) cape.localScale=Vector3.Scale(capeScale,new Vector3(1+.008f*spread+.018f*beat*life,1,1));
        }
    }
}

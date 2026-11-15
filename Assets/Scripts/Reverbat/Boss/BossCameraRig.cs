using UnityEngine;

namespace Reverbat.Boss
{
    public enum BattleShot { Intro, Combat, Bat, Mirror, Wide, Ruler, Impact, Evade, Victory, Defeat }
    public sealed class BossCameraRig : MonoBehaviour
    {
        public Camera lens;
        public BattleShot Shot { get; private set; }
        private Vector3 position;
        private float shake;
        public void Aim(BattleShot shot) { Shot=shot; }
        public void Impact(float strength) { shake=Mathf.Max(strength,shake); }
        public void ResetRig() { shake=0; Shot=BattleShot.Intro; SetFrame(true); }
        private void LateUpdate() { SetFrame(false); }
        private void SetFrame(bool snap)
        {
            Vector3 pos=new Vector3(.15f,3.45f,-8.3f),focus=new Vector3(0,1.92f,2.5f);
            float fov=39f;
            switch(Shot)
            {
                case BattleShot.Intro: pos=new Vector3(-1.6f,2.7f,-3.4f);focus=new Vector3(.95f,2.65f,3.6f);fov=34;break;
                case BattleShot.Bat: pos+=new Vector3(-.35f,-.16f,.38f);focus+=new Vector3(-.5f,.05f,-.8f);break;
                case BattleShot.Mirror: focus+=new Vector3(-.10f,.50f,0);fov=41;break;
                case BattleShot.Wide: pos+=new Vector3(.42f,.25f,-.65f);fov=42;break;
                case BattleShot.Ruler: pos+=new Vector3(.15f,-.1f,.24f);focus+=new Vector3(.30f,.2f,.5f);break;
                case BattleShot.Impact: focus+=new Vector3(.30f,.12f,.32f);fov=37;break;
                case BattleShot.Evade: pos+=new Vector3(-.45f,.15f,-.30f);focus.x-=.32f;fov=43;break;
                case BattleShot.Victory:pos=new Vector3(-.58f,2.8f,-7.7f);focus=new Vector3(-.28f,1.9f,1.7f);break;
                case BattleShot.Defeat:pos=new Vector3(-.10f,2.65f,-7.8f);focus=new Vector3(-.25f,1.45f,2);break;
            }
            float minimumAspect=1.64f;
            if(lens.aspect<minimumAspect) fov=2*Mathf.Atan(Mathf.Tan(fov*Mathf.Deg2Rad/2)*minimumAspect/lens.aspect)*Mathf.Rad2Deg;
            float k=snap?1:1-Mathf.Exp(-5.5f*Time.deltaTime);
            position=Vector3.Lerp(position,pos,k);
            Quaternion look=Quaternion.LookRotation(focus-pos);
            transform.rotation=Quaternion.Slerp(transform.rotation,look,k);
            shake=Mathf.MoveTowards(shake,0,Time.deltaTime*.72f);
            transform.position=position+transform.right*(Mathf.Sin(Time.time*84)*shake)+transform.up*(Mathf.Cos(Time.time*63)*shake*.55f);
            lens.fieldOfView=Mathf.Lerp(lens.fieldOfView,fov,k);
        }
    }
}

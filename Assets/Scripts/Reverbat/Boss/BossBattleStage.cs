using System;
using System.Collections;
using UnityEngine;

namespace Reverbat.Boss
{
    public enum BossCue { Select,OnTime,Miss,Echo,Mirror,Boom,Crate,Cloak,Swarm,Siphon,Brace,Evade,BatHit,BossHit,Victory,Defeat }

    public sealed class BossBattleStage : MonoBehaviour
    {
        public BossDuelActor bat, dictator;
        public BossCameraRig cameraRig;
        public BossBattleEffects fx;
        public Transform mirrorAnchor;
        public AudioSource audioPlayer;
        [Tooltip("Select, OnTime, Miss, Echo, Mirror, Boom, Crate, Cloak, Swarm, Siphon, Brace, Evade, BatHit, BossHit, Victory, Defeat")]
        public AudioClip[] sounds;
        public float Black {get; private set;}
        public float Flash {get; private set;}
        private bool initialized;
        private Vector3 Bat => bat.Center;
        private Vector3 Boss => dictator.Center;
        private Vector3 Direction => (Boss-Bat).normalized;
        private static readonly Vector3 Zero=Vector3.zero;
        private static Color Alpha(Color c,float a){c.a=Mathf.Clamp01(a);return c;}

        public void ResetStage()
        {
            if(!initialized){bat.Initialize();dictator.Initialize();fx.Initialize();initialized=true;}
            fx.Clear();audioPlayer.Stop();bat.Neutral();dictator.Neutral();cameraRig.ResetRig();
            Flash=0;Black=1;
        }
        public void Sound(BossCue cue,float volume=1)
        {
            int index=(int)cue;if(sounds!=null && index<sounds.Length && sounds[index])audioPlayer.PlayOneShot(sounds[index],volume);
        }
        private void Update(){Flash=Mathf.MoveTowards(Flash,0,Time.deltaTime*5.5f);}
        private static IEnumerator Beat(float duration,Action<float> draw)
        {
            for(float t=0;t<duration;t+=Time.deltaTime){draw(t/duration);yield return null;}draw(1);
        }
        private static float Ease(float t)=>t*t*(3-2*t);
        public IEnumerator Intro(Func<bool> skip)
        {
            cameraRig.Aim(BattleShot.Intro);
            bool flourished=false;
            for(float t=0;t<3.5f && !skip();t+=Time.deltaTime)
            {
                Black=1-Mathf.SmoothStep(0,1,t/.75f);
                float rise=Mathf.Sin(Mathf.PI*Mathf.Clamp01((t-.25f)/2.6f));
                dictator.Pose(Vector3.up*rise*.06f,new Vector3(-4*rise,0,-3*rise),-32*rise,-12*rise);
                if(!flourished && t>.55f){Sound(BossCue.Cloak);flourished=true;}
                if(t>1.55f)cameraRig.Aim(BattleShot.Combat);
                bat.Pose(Vector3.down*Mathf.Max(0,1-t/2.8f)*.6f,Zero);
                yield return null;
            }
            Black=0;Neutral();
        }
        private void Neutral(){bat.Neutral();dictator.Neutral();cameraRig.Aim(BattleShot.Combat);}
        private void Hit(Vector3 p,Color c,bool heavy,BossCue sound)
        {
            fx.Burst(p,c,heavy?30:16,heavy?4:2.6f);
            cameraRig.Impact(heavy?.18f:.105f);Flash=heavy?.87f:.55f;Sound(sound);
        }

        public IEnumerator PlayerAction(Attack attack,bool good,Action impact)
        {
            fx.Clear();
            if(!good)
            {
                fx.Burst(Bat,BossBattleEffects.Violet,7,1.2f);
                yield return Beat(.50f,t=>bat.Pose(Vector3.down*.14f*Mathf.Sin(t*Mathf.PI),new Vector3(0,0,15*Mathf.Sin(t*11)*(1-t))));
            }
            else if(attack==Attack.EchoMirror)yield return MirrorStrike(impact);
            else yield return Barrage(impact);
            fx.Clear(false);Neutral();
        }

        private IEnumerator MirrorStrike(Action impact)
        {
            cameraRig.Aim(BattleShot.Bat);
            Vector3 mirror=mirrorAnchor.position;
            var m=fx.Mirror;m.gameObject.SetActive(true);m.position=mirror;
            Sound(BossCue.Mirror,.7f);
            yield return Beat(.46f,t=>{
                m.localScale=Vector3.one*Ease(t);m.rotation=Quaternion.Euler(9,-14,Mathf.Lerp(-38,13,Ease(t)));
                bat.Pose(-Direction*.15f*t,new Vector3(-12*t,-16*t,-8*t),7*t);
                fx.Ring(0,mirror,Vector3.back,1.05f-.38f*t,Alpha(BossBattleEffects.Echo,1-t));
            });
            Sound(BossCue.Echo);cameraRig.Aim(BattleShot.Mirror);
            yield return Beat(.22f,t=>{bat.Pose(Direction*.32f*Mathf.Sin(t*Mathf.PI*.5f),new Vector3(8,-8,0),-13);fx.Beam(Bat,Vector3.Lerp(Bat,mirror,t),mirror,false,BossBattleEffects.Echo);});
            fx.Burst(mirror,BossBattleEffects.Echo);Sound(BossCue.Mirror);
            yield return Beat(.26f,t=>fx.Beam(Bat,mirror,Vector3.Lerp(mirror,Boss,t),true,BossBattleEffects.Echo));
            impact();Hit(Boss,BossBattleEffects.Echo,false,BossCue.BossHit);cameraRig.Aim(BattleShot.Impact);
            yield return Beat(.53f,t=>{
                float recoil=Mathf.Sin(Mathf.PI*t);
                dictator.Pose(Direction*.25f*recoil,new Vector3(-11*recoil,2,7*recoil),-11*recoil);
                bat.Pose(Direction*.32f*(1-t),Zero);
                fx.Beam(Bat,mirror,Boss,true,Alpha(BossBattleEffects.Echo,1-t),.08f*(1-t));
                m.localScale=Vector3.one*(1-Ease(t));
            });
        }

        private IEnumerator Barrage(Action impact)
        {
            cameraRig.Aim(BattleShot.Bat);Sound(BossCue.Brace,.65f);
            Vector3 dir=Direction;Vector3 origin=Bat;
            var starts=new Vector3[3];
            for(int i=0;i<3;i++)
            {
                starts[i]=origin+dir*(1.05f+.26f*i)+new Vector3((i-1)*.64f,.12f+(.28f*(i%2)),0);
                fx.Crates[i].position=starts[i];fx.Crates[i].gameObject.SetActive(true);
            }
            yield return Beat(.7f,t=>{
                bat.Pose(-dir*.56f*Ease(t),new Vector3(-16*t,0,-11*t),14*t);
                for(int i=0;i<3;i++){
                    fx.Crates[i].localScale=Vector3.one*Ease(Mathf.Clamp01(t*1.5f-i*.16f));
                    fx.Crates[i].rotation=Quaternion.Euler(6,20+i*28,11*Mathf.Sin(t*3+i));
                    fx.Ring(i,Bat+dir*(.1f+i*.11f),dir,.2f+(1-t)*(.43f+i*.27f),Alpha(BossBattleEffects.Echo,.75f));
                }
            });
            Sound(BossCue.Boom);cameraRig.Aim(BattleShot.Wide);cameraRig.Impact(.075f);
            int arrived=0;Vector3 target=Boss;
            yield return Beat(.95f,t=>{
                bat.Pose(dir*Mathf.Lerp(-.56f,.65f,Mathf.Min(1,t*5))*(1-t),new Vector3(12*(1-t),0,0),-18*(1-t));
                for(int i=0;i<3;i++)
                {
                    float u=Mathf.Clamp01(t*1.48f-i*.20f);
                    Transform crate=fx.Crates[i];
                    crate.position=Vector3.Lerp(starts[i],target,u*u)+Vector3.up*(.45f+.19f*i)*Mathf.Sin(Mathf.PI*u);
                    crate.rotation=Quaternion.Euler(u*(140+i*50),u*170+i*38,u*95);
                    fx.Ring(i,Vector3.Lerp(origin,target,u),dir,.35f+u*1.25f,Alpha(BossBattleEffects.Echo,1-u),.06f);
                    if(u>=1 && crate.gameObject.activeSelf){crate.gameObject.SetActive(false);arrived++;fx.Burst(target,BossBattleEffects.Amber,13);Sound(BossCue.Crate,.9f);cameraRig.Impact(.075f);
                        if(arrived==3){impact();Hit(target,BossBattleEffects.Amber,true,BossCue.BossHit);}
                    }
                }
                if(arrived>0)dictator.Pose(dir*.13f*arrived,new Vector3(-5*arrived,4*arrived,3*arrived),-5*arrived);
            });
            cameraRig.Aim(BattleShot.Impact);
            yield return Beat(.52f,t=>{float a=1-Ease(t);dictator.Pose(dir*.39f*a,new Vector3(-15*a,12*a,9*a),-15*a);});
        }

        public IEnumerator Telegraph(Counter attack)
        {
            fx.Clear();cameraRig.Aim(BattleShot.Ruler);
            bool swarm=attack==Counter.BatSwarm;Sound(swarm?BossCue.Cloak:BossCue.Siphon,.85f);
            yield return Beat(.68f,t=>{
                float u=Ease(t);dictator.Pose(-Direction*.18f*u,new Vector3(7*u,swarm?0:12*u,swarm?0:-4*u),swarm?-48*u:-8*u,swarm?0:-55*u);
                Vector3 focus=swarm?Boss:dictator.Hand;
                fx.Ring(0,focus,Vector3.back,(swarm?.70f:.30f)*u+.05f,swarm?BossBattleEffects.Violet:BossBattleEffects.Blood);
                if(!swarm) fx.Beam(focus,Vector3.Lerp(focus,Bat,.21f*u),Bat,false,BossBattleEffects.Blood,.045f);
            });
            fx.Burst(swarm?Boss:dictator.Hand,swarm?BossBattleEffects.Violet:BossBattleEffects.Blood,10,1);
        }

        public IEnumerator EnemyAction(Counter attack,Defense selected,bool blocked,Action impact)
        {
            fx.Clear(false);
            Vector3 home=bat.transform.position;
            if(attack==Counter.BatSwarm)
            {
                cameraRig.Aim(blocked?BattleShot.Evade:BattleShot.Wide);Sound(BossCue.Swarm);
                if(selected==Defense.Evade)Sound(BossCue.Evade,.8f);
                if(selected==Defense.Brace)Sound(BossCue.Brace,.5f);
                Vector3 spawn=Boss;
                yield return Beat(.94f,t=>{
                    float roll=Ease(Mathf.Clamp01((t-.16f)*1.7f));
                    float shift=selected==Defense.Evade?(blocked?1.42f:.34f):0;
                    bat.Pose(Vector3.left*roll*shift+Vector3.up*Mathf.Sin(roll*Mathf.PI)*shift*.25f,new Vector3(0,0,roll*(blocked?360:selected==Defense.Evade?73:0)));
                    if(selected==Defense.Evade)fx.Ring(3,home+Vector3.left*roll*shift*.5f,Vector3.right,.17f+roll*.27f,Alpha(BossBattleEffects.Echo,Mathf.Sin(Mathf.PI*roll)));
                    if(selected==Defense.Brace)fx.Ring(3,Bat,Direction,.48f*(1-t),Alpha(BossBattleEffects.Echo,1-t));
                    for(int i=0;i<fx.Swarm.Length;i++)
                    {
                        float u=Mathf.Clamp01(t*1.43f-i*.042f);
                        Vector3 aim=home+new Vector3(.30f*Mathf.Sin(i*2.8f),.25f*Mathf.Cos(i*2),0);
                        if(blocked)aim+= (home-spawn).normalized*1.2f;
                        var path=Vector3.Lerp(spawn,aim,Ease(u))+new Vector3(Mathf.Sin(i*2.2f+u*4)*.85f,.55f*Mathf.Cos(i+u*6),0)*Mathf.Sin(Mathf.PI*u);
                        fx.SwarmBat(i,path,t);
                    }
                    dictator.Pose(-Direction*.18f*(1-t),new Vector3(7*(1-t),0,0),-48*(1-t));
                });
            }
            else
            {
                cameraRig.Aim(BattleShot.Bat);Sound(BossCue.Siphon);
                if(selected==Defense.Brace)Sound(BossCue.Brace);
                if(selected==Defense.Evade)Sound(BossCue.Evade,.55f);
                yield return Beat(.70f,t=>{
                    float u=Ease(Mathf.Clamp01(t*1.7f));
                    Vector3 stop=blocked?Bat+Direction*.75f:Bat;
                    if(selected==Defense.Brace)for(int i=0;i<3;i++)fx.Ring(i,Bat+Direction*(.38f+.14f*i),Direction,(.42f+.14f*i)*u*(blocked?1:1-t),Alpha(BossBattleEffects.Echo,blocked?1:1-t),.06f);
                    if(selected==Defense.Evade)bat.Pose(Vector3.left*.25f*u,new Vector3(0,0,45*u));
                    fx.Beam(dictator.Hand,Vector3.Lerp(dictator.Hand,stop,u),stop,false,BossBattleEffects.Blood,.11f+.025f*Mathf.Sin(t*32));
                });
            }
            impact();
            if(blocked){fx.Burst(Bat+Direction*.6f,BossBattleEffects.Echo,21);cameraRig.Impact(.045f);}
            else Hit(Bat,BossBattleEffects.Blood,false,BossCue.BatHit);
            if(attack==Counter.BatSwarm)fx.Clear(false);
            yield return Beat(.48f,t=>{
                float a=1-Ease(t), recoil=Mathf.Sin(t*Mathf.PI);
                float shift=selected==Defense.Evade?(blocked?1.42f:.32f):0;
                bat.Pose(Vector3.left*shift*a-Direction*(blocked?0:.28f)*recoil,new Vector3(0,0,(selected==Defense.Evade && !blocked?55*a:0)+recoil*(blocked?0:18)));
                dictator.Pose(Zero,new Vector3(0,0,0),0,attack==Counter.BloodSiphon?-52*a:0);
                if(attack==Counter.BloodSiphon){
                    if(!blocked){fx.Ring(4,Vector3.Lerp(Bat,dictator.Hand,t),Direction,.19f+.08f*Mathf.Sin(t*9),BossBattleEffects.Blood);}
                    fx.Beam(dictator.Hand,blocked?Bat+Direction*.75f:Bat,Bat,false,Alpha(BossBattleEffects.Blood,a),.11f*a);
                }
            });
            fx.Clear();Neutral();
        }
        public IEnumerator Ending(bool victory)
        {
            fx.Clear();cameraRig.Aim(victory?BattleShot.Victory:BattleShot.Defeat);Sound(victory?BossCue.Victory:BossCue.Defeat);
            if(victory)fx.Burst(Bat,BossBattleEffects.Amber,40,3.5f);
            yield return Beat(1.25f,t=>{
                float u=Ease(t);
                if(victory){
                    dictator.Pose(new Vector3(.58f*u,-.12f*u,.2f*u),new Vector3(-9*u,6*u,-77*u),-22*u,0,1-u);
                    bat.Pose(Vector3.up*(.42f*u+.24f*Mathf.Sin(t*Mathf.PI)),new Vector3(-5,360*u,-7*u),8*u);
                    fx.Ring(0,Bat,Vector3.back,.2f+t*1.28f,Alpha(BossBattleEffects.Amber,1-t));
                }else{
                    bat.Pose(new Vector3(.12f*u,(.40f-bat.transform.position.y)*u,0),new Vector3(18*u,7*u,52*u),12*u,0,1-u);
                    dictator.Pose(Vector3.up*.04f*u,new Vector3(-6*u,-6*u,0),-33*u,-24*u);
                }
            });
            fx.Clear();
        }
    }
}

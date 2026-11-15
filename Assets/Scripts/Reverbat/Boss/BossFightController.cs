using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reverbat.Boss
{
    public enum BattleState { BattleIntro, PlayerChooseAttack, PlayerAttackQTE, PlayerAttackAnimation, EnemyTelegraph, PlayerChooseDefense, DefenseQTE, EnemyAttackResolution, NextTurn, Victory, Defeat }
    public enum Attack { EchoMirror, ResonanceBarrage }
    public enum Counter { BatSwarm, BloodSiphon }
    public enum Defense { None, Brace, Evade }
    public enum Challenge { None, Strike, Barrage, HoldRelease, Dodge }

    public sealed class BossFightController : MonoBehaviour
    {
        public BossBattleStage stage;
        public BossCombatUI hud;
        [Header("Health and fixed damage")]
        [Min(1)] public int batMaxHP=100, dictatorMaxHP=100;
        [Min(1)] public int quickDamage=22, heavyDamage=38, swarmDamage=20, siphonDamage=18;
        [Min(0)] public int siphonHeal=10;
        [Header("Timing challenges")]
        [Min(.1f)] public float strikeSeconds=2.5f, braceSeconds=2.5f, decisionSeconds=3f;
        [Range(.05f,.8f)] public float zoneWidth=.22f;
        [Range(.1f,.9f)] public float zoneCenter=.66f;
        [Min(.1f)] public float strikeSpeed=1.08f, braceSpeed=.90f;
        [Min(1)] public int barrageKeys=4, evadeKeys=2;
        [Min(.1f)] public float barrageSecondsPerKey=.95f, evadeSecondsPerKey=.80f;

        public BattleState State { get; private set; }
        public int BatHP { get; private set; }
        public int DictatorHP { get; private set; }
        public int Turn { get; private set; }
        public Attack ChosenAttack { get; private set; }
        public Counter Incoming { get; private set; }
        public Defense ChosenDefense { get; private set; }
        public Challenge Prompt { get; private set; }
        public int KeyIndex { get; private set; }
        public int KeyCount => sequence.Length;
        public Key RequiredKey => KeyIndex < sequence.Length ? sequence[KeyIndex] : Key.None;
        public float Indicator => Prompt==Challenge.HoldRelease && !Charging ? 0 :
            Mathf.PingPong((Time.time-(Prompt==Challenge.HoldRelease?chargeStart:started))*
                (Prompt==Challenge.HoldRelease?braceSpeed:strikeSpeed),1);
        public bool InZone => Mathf.Abs(Indicator-zoneCenter)<=zoneWidth*.5f;
        public bool Charging { get; private set; }
        public float Remaining => Mathf.Clamp01((deadline-Time.time)/Mathf.Max(.01f,window));
        public bool ResultReady { get; private set; }
        public string Feedback { get; private set; }
        public bool GoodFeedback { get; private set; }
        public bool Finished => State==BattleState.Victory || State==BattleState.Defeat;
        public int PlayerHits { get; private set; }
        public int EnemyHits { get; private set; }

        private static readonly Key[] keys={Key.W,Key.A,Key.S,Key.D};
        private Key[] sequence=System.Array.Empty<Key>();
        private float started,deadline,window,chargeStart,feedbackUntil;
        private int openedFrame,repeatCount;
        private bool skipIntro;
        private void Start()
        {
            AudioListener.volume=Mathf.Clamp01(PlayerPrefs.GetFloat(MainMenu.VolumeKey,1));
            BeginFight();
        }

        private void BeginFight()
        {
            StopAllCoroutines();
            stage.ResetStage();
            BatHP=batMaxHP; DictatorHP=dictatorMaxHP; Turn=1;
            PlayerHits=EnemyHits=repeatCount=0;
            ResultReady=skipIntro=Charging=GoodFeedback=false;
            started=deadline=window=chargeStart=feedbackUntil=0;
            Incoming=Counter.BatSwarm;
            ChosenAttack=Attack.EchoMirror;
            ChosenDefense=Defense.None;
            Prompt=Challenge.None;
            Feedback="";
            sequence=System.Array.Empty<Key>(); KeyIndex=0;
            hud.ResetBars();
            State=BattleState.BattleIntro;
            StartCoroutine(Intro());
        }

        private IEnumerator Intro()
        {
            yield return stage.Intro(()=>skipIntro);
            State=BattleState.PlayerChooseAttack;
            openedFrame=Time.frameCount;
        }

        public void SkipIntro() { if(State==BattleState.BattleIntro) skipIntro=true; }
        public void Replay() { if(Finished && ResultReady) BeginFight(); }

        private void Update()
        {
            if(!string.IsNullOrEmpty(Feedback) && Time.time>=feedbackUntil) Feedback="";
            var keyboard=Keyboard.current;
            if(State==BattleState.BattleIntro && keyboard!=null && keyboard.escapeKey.wasPressedThisFrame) SkipIntro();
            var mouse=Mouse.current;
            if(mouse!=null && mouse.leftButton.wasPressedThisFrame) hud.Click(mouse.position.ReadValue());
            if(Time.frameCount==openedFrame) return;
            if(State==BattleState.PlayerChooseDefense)
            {
                if(Time.time>=deadline) ResolveDefense(false);
                return;
            }
            if(State!=BattleState.PlayerAttackQTE && State!=BattleState.DefenseQTE) return;
            if(Time.time>=deadline) { FinishPrompt(false); return; }
            if(keyboard==null) return;
            if(Prompt==Challenge.Strike && keyboard.spaceKey.wasPressedThisFrame) FinishPrompt(InZone);
            else if(Prompt==Challenge.HoldRelease)
            {
                if(!Charging && keyboard.spaceKey.wasPressedThisFrame) { Charging=true; chargeStart=Time.time; }
                if(Charging && keyboard.spaceKey.wasReleasedThisFrame) FinishPrompt(InZone);
            }
            else if(Prompt==Challenge.Barrage || Prompt==Challenge.Dodge)
            {
                bool correctKeyPressed=false;
                foreach(var key in keyboard.allKeys)
                {
                    if(!key.wasPressedThisFrame) continue;
                    if(key.keyCode!=RequiredKey) { FinishPrompt(false); return; }
                    correctKeyPressed=true;
                }
                if(!correctKeyPressed) return;
                KeyIndex++;
                if(KeyIndex==sequence.Length) FinishPrompt(true);
                else { stage.Sound(BossCue.Select,.4f); SetWindow(window); }
            }
        }

        private void SetWindow(float seconds)
        {
            window=Mathf.Max(.05f,seconds); started=Time.time; deadline=started+window;
            openedFrame=Time.frameCount;
        }

        public bool ChooseAttack(Attack attack)
        {
            if(State!=BattleState.PlayerChooseAttack) return false;
            ChosenAttack=attack; Feedback="";
            State=BattleState.PlayerAttackQTE;
            BeginPrompt(attack==Attack.EchoMirror?Challenge.Strike:Challenge.Barrage);
            return true;
        }

        public bool ChooseDefense(Defense defense)
        {
            if(State!=BattleState.PlayerChooseDefense || defense==Defense.None || Time.time>=deadline) return false;
            ChosenDefense=defense; Feedback="";
            State=BattleState.DefenseQTE;
            BeginPrompt(defense==Defense.Brace?Challenge.HoldRelease:Challenge.Dodge);
            return true;
        }

        private void BeginPrompt(Challenge challenge)
        {
            Prompt=challenge; Charging=false; KeyIndex=0;
            stage.Sound(BossCue.Select,.75f);
            if(challenge==Challenge.Barrage || challenge==Challenge.Dodge)
            {
                sequence=new Key[Mathf.Clamp(challenge==Challenge.Barrage?barrageKeys:evadeKeys,1,16)];
                for(int i=0;i<sequence.Length;i++) sequence[i]=keys[Random.Range(0,keys.Length)];
                SetWindow(challenge==Challenge.Barrage?barrageSecondsPerKey:evadeSecondsPerKey);
            }
            else { sequence=System.Array.Empty<Key>(); SetWindow(challenge==Challenge.Strike?strikeSeconds:braceSeconds); }
        }

        private void FinishPrompt(bool success)
        {
            if(Prompt==Challenge.None) return;
            Prompt=Challenge.None; Charging=false;
            if(State==BattleState.DefenseQTE) { ResolveDefense(success); return; }
            State=BattleState.PlayerAttackAnimation;
            Message(success?"On cue":"Missed! Blame the acoustics.",success);
            StartCoroutine(PlayerAction(success));
        }

        private void Message(string text,bool good)
        {
            Feedback=text; GoodFeedback=good; feedbackUntil=Time.time+1.05f;
            stage.Sound(good?BossCue.OnTime:BossCue.Miss,.7f);
        }

        private IEnumerator PlayerAction(bool success)
        {
            yield return stage.PlayerAction(ChosenAttack,success,()=>{
                int amount=ChosenAttack==Attack.EchoMirror?quickDamage:heavyDamage;
                DictatorHP=Mathf.Max(0,DictatorHP-amount); PlayerHits++;
                if(DictatorHP==0) State=BattleState.Victory;
            });
            if(Finished) { yield return EndFight(); yield break; }
            Counter next=(Counter)Random.Range(0,2);
            if(repeatCount>=2 && next==Incoming) next=Incoming==Counter.BatSwarm?Counter.BloodSiphon:Counter.BatSwarm;
            repeatCount=next==Incoming?repeatCount+1:1; Incoming=next;
            ChosenDefense=Defense.None;
            State=BattleState.EnemyTelegraph;
            yield return stage.Telegraph(Incoming);
            State=BattleState.PlayerChooseDefense;
            SetWindow(decisionSeconds);
        }

        private void ResolveDefense(bool executed)
        {
            bool matched=Incoming==Counter.BatSwarm?ChosenDefense==Defense.Evade:ChosenDefense==Defense.Brace;
            bool blocked=matched && executed;
            Prompt=Challenge.None; Charging=false;
            State=BattleState.EnemyAttackResolution;
            Message(blocked?(ChosenDefense==Defense.Evade?"Evaded!":"Braced!") :
                ChosenDefense==Defense.None?"Undefended":executed?"Wrong defense":"Missed!",blocked);
            StartCoroutine(EnemyAction(blocked));
        }

        private IEnumerator EnemyAction(bool blocked)
        {
            yield return stage.EnemyAction(Incoming,ChosenDefense,blocked,()=>{
                if(blocked) return;
                BatHP=Mathf.Max(0,BatHP-(Incoming==Counter.BatSwarm?swarmDamage:siphonDamage));
                if(Incoming==Counter.BloodSiphon) DictatorHP=Mathf.Min(dictatorMaxHP,DictatorHP+siphonHeal);
                EnemyHits++;
                if(BatHP==0) State=BattleState.Defeat;
            });
            if(Finished) { yield return EndFight(); yield break; }
            State=BattleState.NextTurn;
            yield return null;
            Turn++; Feedback=""; State=BattleState.PlayerChooseAttack;
        }

        private IEnumerator EndFight()
        {
            Prompt=Challenge.None; Feedback="";
            yield return stage.Ending(State==BattleState.Victory);
            ResultReady=true;
        }
    }
}

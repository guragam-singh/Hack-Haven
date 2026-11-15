using UnityEngine;
using UnityEngine.InputSystem;

namespace Reverbat.Boss
{
    public sealed class BossCombatUI : MonoBehaviour
    {
        public BossFightController fight;
        private Font display,body;
        private GUIStyle label;
        private float batFill, bossFill;
        private static readonly Color Ink=new Color(.046f,.030f,.080f,.95f), Paper=new Color(1,.95f,.86f),
            Gold=new Color(.88f,.68f,.40f), Cyan=new Color(.32f,.88f,1), Rose=new Color(1,.36f,.53f), Lilac=new Color(.78f,.70f,.90f);
        private float Scale => Mathf.Min(Screen.width/1280f, Screen.height/720f);
        private float Width => Screen.width/Scale;
        private float Height => Screen.height/Scale;
        public Rect LeftChoice => new Rect(Width*.5f-303,Height-99,293,72);
        public Rect RightChoice => new Rect(Width*.5f+10,Height-99,293,72);
        public Rect ReplayButton => new Rect(Width*.5f-100,Height-110,200,48);
        public Rect SkipButton => new Rect(Width-145,Height-62,116,36);
        public Vector2 ToScreen(Rect r) => new Vector2(r.center.x*Scale,Screen.height-r.center.y*Scale);

        private void Awake()
        {
            display=Font.CreateDynamicFontFromOSFont(new[]{"Georgia","Times New Roman","Liberation Serif"},32);
            body=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial","Liberation Sans"},22);
        }
        public void ResetBars() { batFill=bossFill=1; }
        private void Update()
        {
            batFill=Mathf.MoveTowards(batFill,(float)fight.BatHP/fight.batMaxHP,Time.deltaTime*1.15f);
            bossFill=Mathf.MoveTowards(bossFill,(float)fight.DictatorHP/fight.dictatorMaxHP,Time.deltaTime*1.15f);
        }
        private Vector2 GuiPoint(Vector2 screen) => new Vector2(screen.x,Screen.height-screen.y)/Scale;
        public void Click(Vector2 screen)
        {
            Vector2 p=GuiPoint(screen);
            if(fight.State==BattleState.BattleIntro) { if(SkipButton.Contains(p)) fight.SkipIntro(); return; }
            if(fight.Finished) { if(ReplayButton.Contains(p)) fight.Replay(); return; }
            bool left=LeftChoice.Contains(p);
            if(!left && !RightChoice.Contains(p)) return;
            if(fight.State==BattleState.PlayerChooseAttack) fight.ChooseAttack(left?Attack.EchoMirror:Attack.ResonanceBarrage);
            else if(fight.State==BattleState.PlayerChooseDefense) fight.ChooseDefense(left?Defense.Brace:Defense.Evade);
        }

        private static void Fill(Rect r,Color c)
        {
            GUI.color=c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=Color.white;
        }
        private void Text(Rect r,string text,int size,Color color,TextAnchor align=TextAnchor.MiddleCenter,bool serif=false,bool bold=false)
        {
            if(label==null)label=new GUIStyle(GUI.skin.label);
            label.font=serif?display:body;label.fontSize=size;label.alignment=align;
            label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;label.wordWrap=false;
            label.padding=new RectOffset(0,0,0,0);
            label.normal.textColor=color;
            GUI.Label(r,text,label);
        }
        private void OnGUI()
        {
            if(Event.current.type!=EventType.Repaint || !fight || !fight.stage) return;
            var matrix=GUI.matrix;var color=GUI.color;
            GUI.matrix=Matrix4x4.Scale(new Vector3(Scale,Scale,1)); GUI.color=Color.white;
            float w=Width,h=Height;
            if(fight.State==BattleState.BattleIntro)
            {
                Fill(new Rect(0,0,w,44),Ink); Fill(new Rect(0,h-57,w,57),Ink);
                Text(new Rect(w*.5f-230,h-130,460,28),"R E V E R B A T",15,Gold);
                Text(new Rect(w*.5f-350,h-107,700,54),"The Other Half",40,Paper,TextAnchor.MiddleCenter,true);
                SmallButton(SkipButton,"Skip intro");
            }
            else
            {
                Health(new Rect(28,22,300,61),"Bat Dracula",fight.BatHP,fight.batMaxHP,batFill,Cyan,false);
                Health(new Rect(w-328,22,300,61),"Dictator Dracula",fight.DictatorHP,fight.dictatorMaxHP,bossFill,Rose,true);
                if(fight.Finished && fight.ResultReady)
                {
                    Rect end=new Rect(w*.5f-300,h-246,600,198);Fill(end,Ink);
                    Fill(new Rect(end.x,end.y,end.width,2),Gold);
                    Text(new Rect(end.x,end.y+12,600,35),fight.State==BattleState.Victory?"VICTORY":"DEFEAT",17,Gold);
                    Text(new Rect(end.x,end.y+43,600,57),fight.State==BattleState.Victory?"Dracula Defeated":"Try Again",39,Paper,TextAnchor.MiddleCenter,true);
                    SmallButton(ReplayButton,fight.State==BattleState.Victory?"Replay":"Retry");
                }
                else if(fight.State==BattleState.PlayerChooseAttack)
                {
                    Caption("Choose an attack");
                    Choice(LeftChoice,"QUICK ATTACK","Echo Mirror",Cyan,0);
                    Choice(RightChoice,"HEAVY ATTACK","Resonance Barrage",Gold,1);
                }
                else if(fight.State==BattleState.PlayerChooseDefense)
                {
                    Caption((fight.Incoming==Counter.BatSwarm?"Bat Swarm":"Blood Siphon")+" incoming");
                    Choice(LeftChoice,"BLOCK THE SIPHON","Brace",Cyan,2);
                    Choice(RightChoice,"DODGE THE SWARM","Evade",Gold,3);
                    Fill(new Rect(LeftChoice.x,h-20,606*fight.Remaining,3),Rose);
                }
                else if(fight.Prompt!=Challenge.None) Qte();
                else if(fight.State==BattleState.EnemyTelegraph)
                    Caption(fight.Incoming==Counter.BatSwarm?"Bat Swarm incoming":"Blood Siphon incoming");
                else if(!string.IsNullOrEmpty(fight.Feedback)) Caption(fight.Feedback,fight.GoodFeedback?Cyan:Rose);
            }
            if(fight.stage.Flash>0) Fill(new Rect(0,0,w,h),new Color(1,.85f,.73f,fight.stage.Flash*.24f));
            if(fight.stage.Black>0) Fill(new Rect(0,0,w,h),new Color(0,0,0,fight.stage.Black));
            GUI.matrix=matrix;GUI.color=color;
        }
        private void Health(Rect r,string name,int hp,int max,float fill,Color accent,bool right)
        {
            Fill(r,Ink); Fill(new Rect(right?r.xMax-3:r.x,r.y,3,r.height),accent);
            Text(new Rect(r.x+15,r.y+3,r.width-30,30),name,21,Paper,TextAnchor.MiddleLeft,true);
            Text(new Rect(r.x+15,r.y+5,r.width-30,26),hp+" / "+max,14,Lilac,TextAnchor.MiddleRight);
            Rect track=new Rect(r.x+15,r.y+39,r.width-30,8);
            Fill(track,new Color(.24f,.19f,.28f));Fill(new Rect(track.x,track.y,track.width*fill,track.height),accent);
            for(int i=1;i<4;i++) Fill(new Rect(track.x+track.width*i/4f,track.y,2,track.height),new Color(.13f,.09f,.17f,.65f));
        }
        private void Caption(string message,Color? tint=null)
        {
            var r=new Rect(Width*.5f-150,Height-140,300,29);
            Fill(r,new Color(.04f,.025f,.065f,.87f));
            Text(r,message,17,tint??Paper);
        }
        private bool Hover(Rect r) => Mouse.current!=null && r.Contains(GuiPoint(Mouse.current.position.ReadValue()));
        private void SmallButton(Rect r,string name)
        {
            Fill(r,Hover(r)?new Color(.28f,.16f,.31f):Ink);
            Fill(new Rect(r.x,r.yMax-2,r.width,2),Gold);Text(r,name,18,Paper);
        }
        private void Choice(Rect r,string type,string name,Color accent,int icon)
        {
            Fill(r,Hover(r)?new Color(.23f,.15f,.27f,.98f):Ink);
            Fill(new Rect(r.x,r.y,r.width,2),accent);
            Fill(new Rect(r.x+58,r.y+14,1,r.height-28),new Color(.47f,.37f,.50f,.7f));
            Icon(new Vector2(r.x+30,r.center.y),accent,icon);
            Text(new Rect(r.x+75,r.y+8,r.width-81,21),type,12,accent,TextAnchor.MiddleLeft,false,true);
            Text(new Rect(r.x+75,r.y+25,r.width-80,35),name,22,Paper,TextAnchor.MiddleLeft,true);
        }
        private static void Line(Vector2 a,Vector2 b,float thickness,Color color)
        {
            var previous=GUI.matrix; Vector2 v=b-a;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg,a);
            Fill(new Rect(a.x,a.y-thickness*.5f,v.magnitude,thickness),color);GUI.matrix=previous;
        }
        private static void Icon(Vector2 p,Color c,int n)
        {
            if(n==1 || n==3)
            {
                for(int i=0;i<3;i++) { float x=-12+i*8;Line(p+new Vector2(x,-9),p+new Vector2(x+6,0),2,c);Line(p+new Vector2(x+6,0),p+new Vector2(x,9),2,c); }
            }
            else
            {
                Vector2 top=p+Vector2.up*(n==0?13:9),bottom=p+Vector2.down*14;
                Vector2 left=p+new Vector2(-11,n==0?0:-5),right=p+new Vector2(11,n==0?0:-5);
                Line(top,left,2,c); Line(left,bottom,2,c); Line(bottom,right,2,c); Line(right,top,2,c);
                if(n==0)Line(p+new Vector2(-4,3),p+new Vector2(3,-5),1,c);
            }
        }
        private void Qte()
        {
            Rect r=new Rect(Width*.5f-230,Height-137,460,110);
            Fill(r,Ink);Fill(new Rect(r.x,r.y,r.width*fight.Remaining,3),Gold);
            bool sequence=fight.Prompt==Challenge.Barrage || fight.Prompt==Challenge.Dodge;
            if(sequence)
            {
                Text(new Rect(r.x+15,r.y+6,160,21),fight.Prompt==Challenge.Dodge?"EVADE":"RESONANCE BARRAGE",13,Lilac,TextAnchor.MiddleLeft,false,true);
                Text(new Rect(r.xMax-115,r.y+6,100,21),(fight.KeyIndex+1)+" / "+fight.KeyCount,14,Gold,TextAnchor.MiddleRight);
                Rect key=new Rect(r.center.x-26,r.y+34,52,54);Fill(key,new Color(.17f,.16f,.25f));Fill(new Rect(key.x,key.yMax-3,key.width,3),Cyan);
                Text(key,fight.RequiredKey.ToString(),32,Paper,TextAnchor.MiddleCenter,false,true);
            }
            else
            {
                string instruction=fight.Prompt==Challenge.Strike?"PRESS SPACE TO STRIKE":fight.Charging?"RELEASE SPACE IN THE BLUE ZONE":"HOLD SPACE TO CHARGE";
                Text(new Rect(r.x,r.y+9,r.width,27),instruction,16,Paper,TextAnchor.MiddleCenter,false,true);
                Rect track=new Rect(r.x+30,r.y+54,r.width-60,21);
                Fill(track,new Color(.20f,.18f,.28f));
                Fill(new Rect(track.x+track.width*(fight.zoneCenter-fight.zoneWidth/2),track.y,track.width*fight.zoneWidth,track.height),new Color(.12f,.59f,.94f));
                float x=track.x+track.width*fight.Indicator;
                Fill(new Rect(x-2,track.y-6,4,track.height+12),Paper);
            }
        }
    }
}

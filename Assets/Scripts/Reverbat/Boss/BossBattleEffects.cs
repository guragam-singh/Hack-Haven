using UnityEngine;
using UnityEngine.Rendering;

namespace Reverbat.Boss
{
    public sealed class BossBattleEffects : MonoBehaviour
    {
        public Material lightInk, mirrorGlass, brass, crateWood, iron, wing, eyes;
        public static readonly Color Echo=new Color(.22f,.95f,1), Blood=new Color(1,.07f,.23f),
            Amber=new Color(1,.70f,.25f), Violet=new Color(.67f,.30f,1);
        public Transform Mirror { get; private set; }
        public readonly Transform[] Crates=new Transform[3];
        public readonly Transform[] Swarm=new Transform[10];
        private readonly Transform[,] wings=new Transform[10,2];
        private readonly LineRenderer[] trails=new LineRenderer[10], rings=new LineRenderer[5];
        private LineRenderer beam,glow;
        private ParticleSystem sparks;
        private Mesh batWing;
        private readonly Vector3[] circle=new Vector3[65];

        public void Initialize()
        {
            Mirror=Node("Conjured mirror",transform);
            Part(Mirror,PrimitiveType.Cube,Vector3.zero,new Vector3(1.0f,1.32f,.09f),brass);
            Part(Mirror,PrimitiveType.Cube,new Vector3(0,0,-.064f),new Vector3(.88f,1.19f,.06f),mirrorGlass);
            for(int i=0;i<4;i++)
            {
                var c=Part(Mirror,PrimitiveType.Cube,new Vector3((i%2==0?-.5f:.5f),(i<2?-.66f:.66f),-.07f),new Vector3(.19f,.19f,.12f),brass);
                c.localRotation=Quaternion.Euler(0,0,45);
            }
            var glint=Part(Mirror,PrimitiveType.Cube,new Vector3(-.13f,.14f,-.11f),new Vector3(.045f,.62f,.025f),eyes);glint.localRotation=Quaternion.Euler(0,0,-32);
            for(int i=0;i<Crates.Length;i++)
            {
                Transform root=Crates[i]=Node("Resonance crate "+(i+1),transform);
                Part(root,PrimitiveType.Cube,Vector3.zero,Vector3.one*.64f,crateWood);
                for(int sign=-1;sign<=1;sign+=2)
                    Part(root,PrimitiveType.Cube,new Vector3(0,sign*.24f,0),new Vector3(.69f,.066f,.69f),iron);
                for(int face=-1;face<=1;face+=2)
                    Part(root,PrimitiveType.Cube,new Vector3(0,0,face*.335f),new Vector3(.078f,.78f,.035f),brass).localRotation=Quaternion.Euler(0,0,-42);
            }
            batWing=new Mesh{name="Swarm wing silhouette"};
            // Scalloped wing tip, elbow and trailing membrane; doubled triangles for two-sided flight.
            batWing.vertices=new[]{Vector3.zero,new Vector3(.31f,.18f,0),new Vector3(.7f,.22f,0),new Vector3(.54f,-.02f,0),
                new Vector3(.43f,.015f,0),new Vector3(.34f,-.13f,0),new Vector3(.23f,-.07f,0),new Vector3(.12f,-.17f,0)};
            batWing.triangles=new[]{0,1,2,0,2,3,0,3,4,0,4,5,0,5,6,0,6,7,2,1,0,3,2,0,4,3,0,5,4,0,6,5,0,7,6,0};
            batWing.RecalculateNormals();batWing.RecalculateBounds();
            for(int i=0;i<Swarm.Length;i++)
            {
                Transform root=Swarm[i]=Node("Swarm bat "+(i+1),transform);
                Part(root,PrimitiveType.Sphere,Vector3.zero,new Vector3(.22f,.32f,.17f),wing);
                for(int j=0;j<2;j++)
                {
                    float sign=j==0?-1:1;
                    var w=wings[i,j]=Node("Wing",root);
                    w.localScale=new Vector3(sign,1,1);
                    w.gameObject.AddComponent<MeshFilter>().sharedMesh=batWing;
                    w.gameObject.AddComponent<MeshRenderer>().sharedMaterial=wing;
                    var ear=Part(root,PrimitiveType.Cube,new Vector3(sign*.064f,.135f,0),new Vector3(.06f,.13f,.06f),wing);ear.localRotation=Quaternion.Euler(0,0,-sign*20);
                    Part(root,PrimitiveType.Sphere,new Vector3(sign*.047f,.058f,-.073f),Vector3.one*.046f,eyes);
                }
                root.localScale=Vector3.one*(.42f+.055f*(i%4));
                trails[i]=Stroke("Swarm trail");
            }
            beam=Stroke("Signal core");glow=Stroke("Signal glow");
            for(int i=0;i<rings.Length;i++)rings[i]=Stroke("Echo ring");
            sparks=Node("Impact sparks",transform).gameObject.AddComponent<ParticleSystem>();
            sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=sparks.main;main.playOnAwake=false;main.loop=false;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startLifetime=.4f;main.maxParticles=180;main.gravityModifier=.20f;
            var emission=sparks.emission;emission.enabled=false;
            var render=sparks.GetComponent<ParticleSystemRenderer>();render.sharedMaterial=lightInk;render.renderMode=ParticleSystemRenderMode.Stretch;render.lengthScale=1.35f;render.velocityScale=.065f;
            Clear();
        }
        private static Transform Node(string name,Transform parent)
        {
            var node=new GameObject(name).transform;node.SetParent(parent,false);return node;
        }
        private static Transform Part(Transform parent,PrimitiveType type,Vector3 pos,Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(type);var t=go.transform;
            t.SetParent(parent,false);t.localPosition=pos;t.localScale=size;
            var col=go.GetComponent<Collider>();if(col){col.enabled=false;Destroy(col);}
            go.GetComponent<Renderer>().sharedMaterial=mat;return t;
        }
        private LineRenderer Stroke(string name)
        {
            var l=Node(name,transform).gameObject.AddComponent<LineRenderer>();
            l.useWorldSpace=true;l.sharedMaterial=lightInk;l.numCapVertices=4;l.numCornerVertices=3;
            l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;l.enabled=false;return l;
        }
        private static void Line(LineRenderer line,Color color,float width)
        {
            line.enabled=color.a>.001f;line.startWidth=line.endWidth=width;line.startColor=line.endColor=color;
        }
        public void Beam(Vector3 a,Vector3 b,Vector3 c,bool reflected,Color col,float width=.075f)
        {
            Color core=Color.Lerp(col,Color.white,.45f);
            core.a=col.a;
            Signal(beam,a,b,c,reflected,core,width);
            col.a*=.29f;Signal(glow,a,b,c,reflected,col,width*3.4f);
        }
        private static void Signal(LineRenderer line,Vector3 a,Vector3 b,Vector3 c,bool reflected,Color color,float width)
        {
            Line(line,color,width);line.positionCount=reflected?3:2;line.SetPosition(0,a);line.SetPosition(1,b);if(reflected)line.SetPosition(2,c);
        }
        public void Ring(int slot,Vector3 position,Vector3 normal,float radius,Color c,float width=.038f)
        {
            Quaternion q=Quaternion.FromToRotation(Vector3.forward,normal.normalized);
            for(int i=0;i<circle.Length;i++)
            {
                float angle=i*(Mathf.PI*2/(circle.Length-1));
                circle[i]=position+q*new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);
            }
            var l=rings[slot];Line(l,c,width);l.positionCount=circle.Length;l.SetPositions(circle);
        }
        public void SwarmBat(int i,Vector3 position,float t)
        {
            var b=Swarm[i];b.gameObject.SetActive(true);Vector3 prev=b.position;b.position=position;
            b.rotation=Quaternion.Euler(5,0,18*Mathf.Sin(i+t*11));
            for(int j=0;j<2;j++)wings[i,j].localRotation=Quaternion.Euler(0,0,(j==0?-1:1)*30*Mathf.Sin(Time.time*27+i));
            if(t<.02f)return;
            Vector3 delta=position-prev;
            var c=Violet;c.a=.48f;
            Signal(trails[i],position,position-(delta.sqrMagnitude>.000001f?delta.normalized:Vector3.back)*.47f,position,false,c,.035f);
        }
        public void Burst(Vector3 point,Color color,int count=16,float power=2.6f)
        {
            sparks.Play();
            for(int i=0;i<count;i++)
            {
                var p=new ParticleSystem.EmitParams{position=point,velocity=Random.onUnitSphere*Random.Range(.45f,power),
                    startLifetime=Random.Range(.18f,.50f),startSize=Random.Range(.045f,.12f),startColor=color};
                sparks.Emit(p,1);
            }
        }
        public void Clear(bool clearParticles=true)
        {
            Mirror.gameObject.SetActive(false);
            foreach(var c in Crates)c.gameObject.SetActive(false);
            foreach(var b in Swarm)b.gameObject.SetActive(false);
            foreach(var line in trails)line.enabled=false;
            foreach(var line in rings)line.enabled=false;
            beam.enabled=glow.enabled=false;
            if(clearParticles)sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        public bool BeamVisible => beam.enabled;
        public int VisibleSwarm => System.Array.FindAll(Swarm,x=>x.gameObject.activeSelf).Length;
        private void OnDestroy(){if(batWing)Destroy(batWing);}
    }
}

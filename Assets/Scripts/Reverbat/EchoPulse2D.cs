using System.Collections.Generic;
using UnityEngine;

namespace Reverbat
{
    public sealed class EchoPulse2D : MonoBehaviour
    {
        [Header("Echo Pulse Timing")]
        [Min(0), Tooltip("Minimum seconds between accepted pulse activations, independent of how long the room remains lit.")]
        public float cooldown = 2.5f;
        [Min(0), InspectorName("Duration"), Tooltip("Seconds each reached part of the room stays lit before fading back to darkness.")]
        public float duration = 5f;
        [Header("Visual Smoothing")]
        [Min(.1f)] public float waveSpeed = 10f;
        [Min(.05f)] public float revealFeather = 1.4f;
        [Min(.01f)] public float fadeOutDuration = .8f;
        public Material revealMaterial;
        private const float MaximumRadius = 22;
        private const int Segments = 192;
        private const int MaximumWaves = 8;
        private readonly List<SpriteRenderer> worldSprites = new List<SpriteRenderer>();
        private readonly List<Material> spriteMaterials = new List<Material>();
        private readonly List<Pulse> pulses = new List<Pulse>();
        private readonly Vector4[] data = new Vector4[MaximumWaves];
        private readonly Vector4[] settings = new Vector4[MaximumWaves];
        private readonly Vector3[] circle = new Vector3[Segments + 1];
        private Level01Manager level;
        private float readyAt;
        private bool ownsMaterial;
        public float CooldownRemaining => Mathf.Max(0, readyAt - Time.time);
        public int ActivePulseCount => pulses.Count;
        public float CurrentRadius => pulses.Count == 0 ? 0 : Mathf.Min(MaximumRadius,(Time.time-pulses[pulses.Count-1].started)*pulses[pulses.Count-1].speed);
        public bool IsEmitting
        {
            get
            {
                foreach (var pulse in pulses) if ((Time.time-pulse.started)*pulse.speed < MaximumRadius) return true;
                return false;
            }
        }
        private sealed class Pulse
        {
            public Vector2 origin;
            public float started, speed, hold, fade, feather;
            public LineRenderer ring;
        }

        public void Initialize(Level01Manager manager)
        {
            level=manager;
            readyAt=0;
            level.bat.roomCamera.backgroundColor=Color.black;
            if(revealMaterial==null)
            {
                var shader=Shader.Find("Reverbat/Echo Reveal Sprite");
                if(shader==null){Debug.LogError("[REVERBAT] Missing echo reveal shader."); return;}
                revealMaterial=new Material(shader); ownsMaterial=true;
            }
            worldSprites.Clear();
            foreach(var sprite in level.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if(sprite==level.coffin || sprite.gameObject==level.artifact) continue;
                worldSprites.Add(sprite);
                // Give every sprite its own explicit texture binding. Sharing the custom reveal
                // material can leak another sprite's texture through Unity's sliced-sprite batches.
                if(sprite.sprite!=null)
                {
                    var template=sprite.sharedMaterial!=null && sprite.sharedMaterial.shader.name=="Reverbat/Bat Highlight" ? sprite.sharedMaterial : revealMaterial;
                    var material=new Material(template){name="Echo Reveal - "+sprite.name};
                    material.SetTexture("_MainTex",sprite.sprite.texture);
                    material.enableInstancing=false;
                    sprite.sharedMaterial=material;
                    spriteMaterials.Add(material);
                }
                Color color=sprite.color; color.a=1; sprite.color=color;
                sprite.enabled=false;
            }
            Shader.SetGlobalInt("_ReverbatPulseCount",0);
            Shader.SetGlobalFloat("_ReverbatMaskEnabled",1);
        }

        public bool Visible(SpriteRenderer sprite)
        {
            if(sprite==null || !sprite.enabled || !sprite.gameObject.activeInHierarchy) return false;
            if(!worldSprites.Contains(sprite)) return true;
            return RevealAmountAt(sprite.bounds.center)>.025f;
        }

        public float RevealAmountAt(Vector2 point)
        {
            float value=0;
            foreach(var pulse in pulses)
            {
                float age=Time.time-pulse.started;
                float distance=Vector2.Distance(point,pulse.origin);
                float radius=Mathf.Min(MaximumRadius,age*pulse.speed);
                float front=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-pulse.feather*.5f,pulse.feather*.5f,radius-distance));
                float localAge=age-distance/pulse.speed;
                float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(pulse.hold,pulse.hold+pulse.fade,localAge));
                value=Mathf.Max(value,front*fade);
            }
            return value;
        }

        public bool Emit()
        {
            if(level==null || level.Complete || revealMaterial==null) return false;
            if(Time.time<readyAt)
            {
                level.ShowHint("Echo Pulse recharging ("+CooldownRemaining.ToString("0.0")+"s).");
                return false;
            }
            readyAt=Time.time+Mathf.Max(0,cooldown);
            if(pulses.Count==MaximumWaves){Destroy(pulses[0].ring.gameObject); pulses.RemoveAt(0);}
            var obj=new GameObject("Echo Pulse Ripple"); obj.transform.SetParent(transform,false);
            var ring=obj.AddComponent<LineRenderer>();
            ring.sharedMaterial=level.bat.echo.outgoing.sharedMaterial;
            ring.startColor=ring.endColor=Color.cyan;
            ring.startWidth=ring.endWidth=.035f;
            ring.positionCount=Segments+1;
            ring.sortingOrder=15;
            ring.useWorldSpace=true;
            pulses.Add(new Pulse {origin=level.bat.transform.position,started=Time.time,speed=Mathf.Max(.1f,waveSpeed),hold=Mathf.Max(0,duration),fade=Mathf.Max(.01f,fadeOutDuration),feather=Mathf.Max(.05f,revealFeather),ring=ring});
            foreach(var sprite in worldSprites) sprite.enabled=true;
            Publish();
            level.NotifyDialogue(DialogueIds.FirstPulse);
            return true;
        }

        private void Update()
        {
            for(int i=pulses.Count-1;i>=0;i--)
            {
                var pulse=pulses[i]; float age=Time.time-pulse.started;
                if(age>MaximumRadius/pulse.speed+pulse.hold+pulse.fade+pulse.feather/pulse.speed)
                {Destroy(pulse.ring.gameObject);pulses.RemoveAt(i);continue;}
                float radius=age*pulse.speed;
                pulse.ring.enabled=radius<MaximumRadius;
                if(pulse.ring.enabled)
                {
                    for(int n=0;n<=Segments;n++)
                    {
                        float angle=n*Mathf.PI*2/Segments;
                        circle[n]=pulse.origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                    }
                    pulse.ring.SetPositions(circle);
                    var color=Color.cyan; color.a=Mathf.Clamp01((MaximumRadius-radius)/1.5f);
                    pulse.ring.startColor=pulse.ring.endColor=color;
                }
            }
            Publish();
            if(pulses.Count==0) foreach(var sprite in worldSprites) sprite.enabled=false;
        }
        private void Publish()
        {
            for(int i=0;i<pulses.Count;i++)
            {
                var p=pulses[i]; data[i]=new Vector4(p.origin.x,p.origin.y,Time.time-p.started,p.speed);
                settings[i]=new Vector4(p.hold,p.fade,p.feather,MaximumRadius);
            }
            Shader.SetGlobalVectorArray("_ReverbatPulseData",data);
            Shader.SetGlobalVectorArray("_ReverbatPulseSettings",settings);
            Shader.SetGlobalInt("_ReverbatPulseCount",pulses.Count);
        }
        private void OnDisable()
        {
            Shader.SetGlobalInt("_ReverbatPulseCount",0);
            Shader.SetGlobalFloat("_ReverbatMaskEnabled",0);
        }
        private void OnDestroy()
        {
            foreach(var pulse in pulses) if(pulse.ring!=null) Destroy(pulse.ring.gameObject);
            foreach(var material in spriteMaterials) if(material!=null) Destroy(material);
            if(ownsMaterial && revealMaterial!=null) Destroy(revealMaterial);
        }
    }
}

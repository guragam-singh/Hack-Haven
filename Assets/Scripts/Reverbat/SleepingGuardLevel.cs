using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Reverbat
{
    public enum GuardPuzzleState
    {
        AwaitingEchoPulse, BatDialogue, AwaitingGuardResonance, GuardWaking, ShieldReady,
        FemaleBatReaction, CurtainsClosing, ExitAvailable, Completed
    }

    public sealed class SleepingGuardLevel : MonoBehaviour
    {
        public const string SceneName = "Level02_SleepingGuard";
        private static bool earnedResonance;
        private static DialogueManager incomingVoice;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetProgress() { earnedResonance = false; incomingVoice = null; }
        public static void EnterFromLevelOne(DialogueManager queue)
        {
            earnedResonance = true;
            queue.transform.SetParent(null);
            DontDestroyOnLoad(queue.gameObject);
            incomingVoice = queue;
        }
        public bool ResonanceAvailable => earnedResonance || Application.isEditor;
        public GuardPuzzleState State { get; private set; } = GuardPuzzleState.AwaitingEchoPulse;
        public bool Exiting { get; private set; }

        [Header("Shared player, reveal and voice")]
        public BatController2D bat;
        public EchoPulse2D pulse;
        public SpriteRenderer[] revealedEnvironment;
        public DialogueManager voice;
        public AudioClip level2AfterFirstPulseDialogue;
        [Header("Guard and reflection")]
        public SpriteRenderer guard, shield, snoreLetters, alert, impact, resonanceWaves;
        public Sprite startledGuard;
        public Collider2D guardTarget, shieldTarget, partition;
        public Transform cornerApproach, cornerDeparture;
        [Header("Window and exit")]
        public SpriteRenderer female, hearts, openWindow, closedWindow, rays, exitArrow;
        public Sprite heartEyes;
        public Transform femaleStart, curtainLatch, exitDestination;
        public Collider2D exitTarget;
        [Header("Audio")]
        public AudioSource snoring, effects;
        public AudioClip whistle;
        [Range(0,.5f)] public float snoreVolume = .32f;
        [Header("Sequence durations (seconds)")]
        [Min(.01f)] public float loudSnore = .6f, startledHold = .4f, shieldFade = .5f,
            heartHold = .7f, flight = 1f, curtainClose = .6f, femaleFade = .5f, exitTravel = .8f;
        [Header("Toolbar artwork")]
        public Texture2D sightIcon, pulseIcon, resonanceIcon, lockedIcon;

        private Vector3 arrowHome, snoreHome;
        private Material flightMaterial;
        private float nextResonance, rayOpacity;
        private Coroutine impactAnimation, waveAnimation;
        private GUIStyle instruction, button;
        private readonly List<Material> ownedMaterials = new List<Material>();

        private void Awake()
        {
            // Keep state swaps and alpha animation on independent sprite texture bindings.
            foreach (var actor in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (System.Array.IndexOf(revealedEnvironment, actor) >= 0) continue;
                if (actor.gameObject == bat.gameObject) flightMaterial = actor.sharedMaterial;
                bool alwaysVisible = actor == guard || actor == female;
                var template = alwaysVisible || actor == openWindow ? actor.sharedMaterial : pulse.revealMaterial;
                var material = new Material(template);
                material.enableInstancing = false;
                material.mainTexture = actor.sprite.texture;
                actor.sharedMaterial = material;
                ownedMaterials.Add(material);
            }
            pulse.Initialize(bat, revealedEnvironment);
            pulse.Emitted += OnPulse;
            if (incomingVoice != null)
            {
                voice = incomingVoice;
                voice.transform.SetParent(transform);
                incomingVoice = null;
            }
            voice.InitializeVoice();
            snoring.volume = snoreVolume;
            snoring.loop = true;
            snoring.Play();
            arrowHome = exitArrow.transform.position;
            snoreHome = snoreLetters.transform.position;
            rayOpacity = rays.color.a;
            SetAlpha(shield, 0);
            shieldTarget.enabled = false;
            closedWindow.enabled = exitArrow.enabled = exitTarget.enabled = false;
            alert.enabled = hearts.enabled = impact.enabled = resonanceWaves.enabled = false;
            FitCamera();
        }

        private void OnDestroy()
        {
            if (pulse != null) pulse.Emitted -= OnPulse;
            foreach (var material in ownedMaterials) Destroy(material);
        }

        private void FitCamera()
        {
            var cam = bat.roomCamera;
            cam.orthographicSize = Mathf.Max(4.705f, 8.36f / cam.aspect);
        }

        private void Update()
        {
            FitCamera();
            float bob = Mathf.Sin(Time.time * 3.5f);
            if (State < GuardPuzzleState.FemaleBatReaction)
                female.transform.position = femaleStart.position + Vector3.up * (.065f * bob);
            if (snoreLetters.enabled)
                snoreLetters.transform.position = snoreHome + Vector3.up * (.06f * bob);
            if (exitArrow.enabled)
                exitArrow.transform.position = arrowHome + Vector3.up * (.07f * bob);
        }

        private void OnPulse()
        {
            if (State != GuardPuzzleState.AwaitingEchoPulse) return;
            State = GuardPuzzleState.BatDialogue;
            if (voice.Enqueue(new DialogueEntry { triggerId = "level02.first-pulse", voiceClip = level2AfterFirstPulseDialogue }))
                StartCoroutine(WaitForVoice());
            else State = GuardPuzzleState.AwaitingGuardResonance;
        }

        private IEnumerator WaitForVoice()
        {
            yield return new WaitUntil(() => !voice.IsShowing);
            if (State == GuardPuzzleState.BatDialogue) State = GuardPuzzleState.AwaitingGuardResonance;
        }

        public void EmitResonance(Vector2 target)
        {
            if (Exiting || !ResonanceAvailable || Time.time < nextResonance) return;
            Vector2 origin = bat.transform.position;
            Vector2 direction = target - origin;
            if (direction.sqrMagnitude < .01f) return;
            nextResonance = Time.time + .45f;
            Vector2 end = origin + Vector2.ClampMagnitude(direction, 12);
            var wallHit = Physics2D.Linecast(origin, end, 1 << partition.gameObject.layer);
            if (wallHit.collider != null) end = wallHit.point;
            if (waveAnimation != null) StopCoroutine(waveAnimation);
            waveAnimation = StartCoroutine(ShowWave(origin, end));
            if (State != GuardPuzzleState.AwaitingGuardResonance || !guardTarget.OverlapPoint(target)) return;
            State = GuardPuzzleState.GuardWaking;
            StartCoroutine(Wake());
        }

        private IEnumerator ShowWave(Vector2 origin, Vector2 end)
        {
            resonanceWaves.enabled = true;
            Vector2 dir = end - origin;
            resonanceWaves.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            yield return Animate(.35f, t => {
                resonanceWaves.transform.position = Vector2.Lerp(origin, end, t);
                SetAlpha(resonanceWaves, Mathf.Sin(Mathf.PI * t));
            });
            resonanceWaves.enabled = false;
            waveAnimation = null;
        }

        private IEnumerator Wake()
        {
            snoring.volume = snoreVolume * 2;
            yield return new WaitForSeconds(loudSnore);
            snoring.Stop();
            snoreLetters.enabled = false;
            Swap(guard, startledGuard);
            alert.enabled = true;
            yield return new WaitForSeconds(startledHold);
            alert.enabled = false;
            yield return Animate(shieldFade, t => SetAlpha(shield, t));
            guardTarget.enabled = false;
            shieldTarget.enabled = true;
            State = GuardPuzzleState.ShieldReady;
        }

        // The first hit decides the outcome. The player's collider is excluded by layer.
        public bool TraceEcho(Vector2 origin, Vector2 direction, List<Vector3> points, out string hitName)
        {
            int mask = (1 << shieldTarget.gameObject.layer) | (1 << partition.gameObject.layer) | (1 << guardTarget.gameObject.layer);
            var hit = Physics2D.Raycast(origin, direction, 24, mask);
            hitName = hit.collider != null ? hit.collider.name : "None";
            points.Add(hit.collider != null ? (Vector3)hit.point : (Vector3)(origin + direction * 20));
            if (State != GuardPuzzleState.ShieldReady || hit.collider != shieldTarget) return false;
            // The generous pick collider catches edge clicks. Land the visible signal on the metal face.
            Vector3 impactPoint = shield.bounds.center;
            Vector2 last = impactPoint;
            Vector3[] route = { cornerApproach.position, cornerDeparture.position, female.transform.position };
            foreach (Vector3 next in route)
            {
                if (Physics2D.Linecast(last, next, 1 << partition.gameObject.layer).collider != null) return false;
                last = next;
            }
            points[points.Count-1] = impactPoint;
            points.AddRange(route);
            return true;
        }

        internal void EchoReached(int index, int last, Vector3 position)
        {
            if (State != GuardPuzzleState.ShieldReady || !bat.echo.IsEmitting || !bat.echo.LastScriptedRoute) return;
            if (index == 1 || index == last)
            {
                if (impactAnimation != null) StopCoroutine(impactAnimation);
                impactAnimation = StartCoroutine(FlashImpact(position));
            }
            if (index != last) return;
            State = GuardPuzzleState.FemaleBatReaction;
            shieldTarget.enabled = false;
            StartCoroutine(CloseWindow());
        }

        private IEnumerator FlashImpact(Vector3 position)
        {
            impact.transform.position = position;
            impact.enabled = true;
            yield return Animate(.22f, t => SetAlpha(impact, 1-t));
            impact.enabled = false;
            impactAnimation = null;
        }

        private IEnumerator CloseWindow()
        {
            Swap(female, heartEyes);
            effects.PlayOneShot(whistle);
            Vector3 start = female.transform.position;
            Vector3 heartsStart = start + new Vector3(-.28f,.53f);
            hearts.enabled = true;
            yield return Animate(heartHold, t => {
                hearts.transform.position = heartsStart + Vector3.up * (.32f*t);
                SetAlpha(hearts, 1-t);
            });
            hearts.enabled = false;
            yield return Animate(flight, t => female.transform.position =
                Vector3.Lerp(start, curtainLatch.position, t) + Vector3.up * (.14f * Mathf.Sin(Mathf.PI * t)));
            State = GuardPuzzleState.CurtainsClosing;
            SetAlpha(closedWindow,0);
            closedWindow.enabled = true;
            yield return Animate(curtainClose, t => {
                SetAlpha(closedWindow,t);
                SetAlpha(openWindow,1-t);
                SetAlpha(rays,rayOpacity*(1-t));
            });
            openWindow.enabled = rays.enabled = false;
            yield return Animate(femaleFade, t => SetAlpha(female,1-t));
            female.enabled = false;
            exitArrow.enabled = exitTarget.enabled = true;
            State = GuardPuzzleState.ExitAvailable;
        }

        public bool TryExit(Vector2 point)
        {
            if (State != GuardPuzzleState.ExitAvailable || Exiting || !exitTarget.OverlapPoint(point)) return false;
            Exiting = true;
            exitArrow.enabled = exitTarget.enabled = false;
            StartCoroutine(Leave());
            return true;
        }

        private IEnumerator Leave()
        {
            var start = bat.transform.position;
            var playerArt = bat.GetComponent<SpriteRenderer>();
            playerArt.sharedMaterial = flightMaterial;
            var cam = bat.roomCamera;
            Vector3 end = exitDestination.position;
            float rightEdge = cam.ViewportToWorldPoint(new Vector3(1, .5f, -cam.transform.position.z)).x;
            end.x = Mathf.Max(end.x, rightEdge + playerArt.bounds.extents.x + .1f);
            exitDestination.position = end;
            // Arc over the guard and clear the top of the partition before leaving the right edge.
            float clearance = Mathf.Max(guard.bounds.max.y, partition.bounds.max.y) + playerArt.bounds.extents.y + .18f;
            float rise = Mathf.Max(0, clearance - Mathf.Min(start.y, end.y));
            yield return Animate(exitTravel, t => bat.transform.position =
                Vector3.Lerp(start,end,t) + Vector3.up * (rise * Mathf.Sin(Mathf.PI*t)));
            bat.GetComponent<SpriteRenderer>().enabled = false;
            snoring.Stop();
            effects.Stop();
            voice.VoiceSource.Stop();
            State = GuardPuzzleState.Completed;
        }

        private static void SetAlpha(SpriteRenderer renderer, float value)
        {
            var color = renderer.color;
            color.a = value;
            renderer.color = color;
        }

        private static void Swap(SpriteRenderer renderer, Sprite next)
        {
            // Match the character's drawn height; separate PNGs have different source dimensions.
            float ratio = renderer.sprite.bounds.size.y / next.bounds.size.y;
            renderer.transform.localScale *= ratio;
            renderer.sprite = next;
            renderer.sharedMaterial.mainTexture = next.texture;
        }

        private static IEnumerator Animate(float seconds, System.Action<float> draw)
        {
            for (float elapsed = 0; elapsed < seconds; elapsed += Time.deltaTime)
            {
                draw(Mathf.SmoothStep(0,1,elapsed/seconds));
                yield return null;
            }
            draw(1);
        }

        private void OnGUI()
        {
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width/1280f, Screen.height/720f, 1));
            if (instruction == null)
            {
                instruction = new GUIStyle(GUI.skin.label) { fontSize = TutorialPresentation.MainInstructionFontSize,
                    fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                button = new GUIStyle(GUI.skin.button) { fontSize = 17 };
            }
            int highlight = State == GuardPuzzleState.AwaitingEchoPulse ? 1 : State == GuardPuzzleState.AwaitingGuardResonance ? 2 : -1;
            string message = highlight == 1 ? "Use Echo Pulse to reveal the room." :
                highlight == 2 ? "That guard is snoring rather loudly. Try Resonance!" :
                State == GuardPuzzleState.Completed ? "Level 2 Complete!" : "";
            if (message.Length > 0)
                TutorialPresentation.DrawReadableText(new Rect(24, 41, 1232, 50),message,instruction);
            GUI.Box(new Rect(0,620,1280,100),GUIContent.none);
            AbilityToolbar.Draw(bat, button, !Exiting);
            AbilityToolbar.DrawIcons(sightIcon,pulseIcon,ResonanceAvailable ? resonanceIcon : lockedIcon,lockedIcon);
            if (highlight >= 0) TutorialPresentation.DrawAbilityGlow(AbilityToolbar.RectFor(highlight));
            GUI.matrix = oldMatrix;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace Reverbat
{
    public sealed class BatController2D : MonoBehaviour
    {
        public Camera roomCamera;
        public EchoSystem2D echo;
        public Level01Manager level;
        public float speed = 4.5f;
        public float radius = .25f;
        public Vector2 roomMin = new Vector2(-8.45f, -3.55f);
        public Vector2 roomMax = new Vector2(8.45f, 3.65f);
        public int SelectedAbility { get; private set; }
        public bool EchoSelected => SelectedAbility == 0;
        public bool PulseSelected => SelectedAbility == 1;
        public bool IsMoving { get; private set; }
        private readonly System.Collections.Generic.HashSet<KeyCode> guiHeld = new System.Collections.Generic.HashSet<KeyCode>();

        private void OnApplicationFocus(bool focused) { if (!focused) guiHeld.Clear(); }

        private void OnGUI()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown && e.type != EventType.KeyUp) return;
            if (e.type == EventType.KeyUp) { guiHeld.Remove(e.keyCode); return; }
            // IMGUI receives editor/keyboard-layout events that can be missed by frame-edge polling.
            if (e.keyCode == KeyCode.Alpha1 || e.keyCode == KeyCode.Keypad1 || e.character == '1') SelectAbility(0);
            else if (e.keyCode == KeyCode.Alpha2 || e.keyCode == KeyCode.Keypad2 || e.character == '2') SelectAbility(1);
            else if (e.keyCode == KeyCode.Alpha3 || e.keyCode == KeyCode.Keypad3 || e.character == '3') SelectAbility(2);
            else if (e.keyCode == KeyCode.W || e.keyCode == KeyCode.A || e.keyCode == KeyCode.S || e.keyCode == KeyCode.D) guiHeld.Add(e.keyCode);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            Vector2 movement = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) SelectAbility(0);
                if (keyboard.digit2Key.wasPressedThisFrame) SelectAbility(1);
                if (keyboard.digit3Key.wasPressedThisFrame) SelectAbility(2);
                movement = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            }
            if (guiHeld.Contains(KeyCode.D)) movement.x = 1;
            if (guiHeld.Contains(KeyCode.A)) movement.x = -1;
            if (guiHeld.Contains(KeyCode.W)) movement.y = 1;
            if (guiHeld.Contains(KeyCode.S)) movement.y = -1;
            ApplyMovement(movement, Time.deltaTime);
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !level.Complete)
                HandleScreenClick(mouse.position.ReadValue());
        }

        public void ApplyMovement(Vector2 input, float deltaTime)
        {
            IsMoving = false;
            if (level.Complete || input.sqrMagnitude < .001f) return;
            level.tiltable.Leave();
            Vector2 delta = Vector2.ClampMagnitude(input, 1) * speed * deltaTime;
            float step = delta.magnitude;
            var hit = Physics2D.CircleCast(transform.position, radius, delta.normalized, step + .025f);
            if (hit.collider != null) step = Mathf.Max(0, hit.distance - .025f);
            Vector2 position = (Vector2)transform.position + delta.normalized * step;
            position.x = Mathf.Clamp(position.x, roomMin.x, roomMax.x);
            position.y = Mathf.Clamp(position.y, roomMin.y, roomMax.y);
            IsMoving = Vector2.Distance(position, transform.position) > .0001f;
            transform.position = position;
        }

        public bool HandleScreenClick(Vector2 screen)
        {
            if (level.dialogue != null && level.dialogue.BlocksGameplayPointer(screen)) return false;
            float hudY = (Screen.height - screen.y) * 720f / Screen.height;
            if (hudY <= 100 || hudY >= 620 || level.Complete) return false;
            ClickWorld(roomCamera.ScreenToWorldPoint(screen));
            return true;
        }

        public bool IsAbilityUnlocked(int index) => index == 0 || index == 1 || (index == 2 && level.Complete);
        public bool SelectAbility(int index)
        {
            if (!IsAbilityUnlocked(index)) return false;
            SelectedAbility = index;
            level.ShowHint(index == 0 ? "Echo Sight: aim and click. WASD moves the bat." : "Echo Pulse selected: click to reveal. WASD moves the bat.");
            return true;
        }
        public void SelectEcho(bool selected) { SelectAbility(selected ? 0 : 1); }
        public void SelectPulse() { SelectAbility(1); }
        public void StopMoving() { IsMoving = false; }

        public void ClickWorld(Vector2 point)
        {
            if (level.Complete) return;
            if (level.CoffinOpen && Vector2.Distance(point, level.artifact.transform.position) < .7f)
            {
                if (!level.TryCollect(transform.position)) level.ShowHint("Use WASD to get closer, then click the artifact.");
                return;
            }
            if (PulseSelected) level.darkness.Emit();
            else if (EchoSelected) echo.Emit(point);
        }
    }
}

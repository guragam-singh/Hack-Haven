using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
        public bool EchoSelected { get; private set; }
        public bool PulseSelected { get; private set; }
        public bool IsMoving { get; private set; }
        public Vector2 Destination { get; private set; }
        private bool collectOnArrival;
        private readonly Queue<Vector2> waypoints = new Queue<Vector2>();

        private void Update()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !level.Complete)
            {
                HandleScreenClick(mouse.position.ReadValue());
            }
            // Stay at the source until the return arrives; move clicks queue their destination.
            if (!IsMoving || echo.IsEmitting) return;
            Vector2 next = waypoints.Count > 0 ? waypoints.Peek() : Destination;
            Vector2 delta = next - (Vector2)transform.position;
            float step = Mathf.Min(speed * Time.deltaTime, delta.magnitude);
            if (step > 0)
            {
                RaycastHit2D hit = Physics2D.CircleCast(transform.position, radius, delta.normalized, step + .025f);
                if (hit.collider != null)
                {
                    step = Mathf.Max(0, hit.distance - .025f);
                    IsMoving = false;
                    level.ShowHint("Path blocked. Click around the solid shapes.");
                }
                transform.position += (Vector3)(delta.normalized * step);
            }
            if (collectOnArrival && level.TryCollect(transform.position))
            {
                collectOnArrival = false;
                IsMoving = false;
            }
            if (delta.magnitude < .04f)
            {
                if (waypoints.Count > 0) waypoints.Dequeue();
                else IsMoving = false;
            }
        }

        public bool HandleScreenClick(Vector2 screen)
        {
            if (level.dialogue != null && level.dialogue.BlocksGameplayPointer(screen)) return false;
            // Existing gameplay/toolbar hit regions stay unchanged.
            float hudY = (Screen.height - screen.y) * 720f / Screen.height;
            if (hudY <= 100 || hudY >= 620 || level.Complete) return false;
            ClickWorld(roomCamera.ScreenToWorldPoint(screen));
            return true;
        }

        public void SelectEcho(bool selected)
        {
            PulseSelected = false;
            EchoSelected = selected;
            level.ShowHint(selected ? "Echo Sight: click a target or location to emit sound." : "Move: click a clear location. Click the red artifact to collect it.");
        }

        public void SelectPulse()
        {
            PulseSelected = true;
            EchoSelected = false;
            level.darkness.Emit();
            level.ShowHint("Echo Pulse reveals the room. Move onto the plank's right end to tilt it to -45° first.", 6);
        }

        public void StopMoving() { IsMoving = false; waypoints.Clear(); }

        public void ClickWorld(Vector2 point)
        {
            if (level.Complete) return;
            level.tiltable.Leave();
            // Artifact clicks always mean walk over and collect, regardless of selected mode.
            if (level.CoffinOpen && !level.Complete && Vector2.Distance(point, level.artifact.transform.position) < .7f)
            {
                MoveTo(level.artifact.transform.position, true);
                return;
            }
            if (PulseSelected) { level.darkness.Emit(); return; }
            if (EchoSelected)
            {
                IsMoving = false;
                echo.Emit(point);
            }
            else if (!level.tiltable.TryBoard(point)) MoveTo(point, false);
        }

        public void MoveTo(Vector2 target, bool collect)
        {
            Destination = new Vector2(Mathf.Clamp(target.x, roomMin.x, roomMax.x), Mathf.Clamp(target.y, roomMin.y, roomMax.y));
            collectOnArrival = collect;
            PlanPath(Destination);
            IsMoving = true;
        }

        private bool ClearPath(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            return !Physics2D.CircleCast(from, radius + .03f, delta.normalized, delta.magnitude).collider;
        }

        // A tiny visibility graph around collider bounds is enough for a single greybox room.
        // This keeps click-to-collect usable without a navigation package or flight physics.
        private void PlanPath(Vector2 target)
        {
            waypoints.Clear();
            Physics2D.SyncTransforms();
            Vector2 start = transform.position;
            if (ClearPath(start, target)) return;
            var nodes = new List<Vector2> { start, target };
            foreach (var obstacle in FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
            {
                if (!obstacle.enabled || obstacle.isTrigger) continue;
                Bounds bounds = obstacle.bounds;
                bounds.Expand((radius + .09f) * 2);
                foreach (var corner in new[] {
                    new Vector2(bounds.min.x, bounds.min.y), new Vector2(bounds.min.x, bounds.max.y),
                    new Vector2(bounds.max.x, bounds.min.y), new Vector2(bounds.max.x, bounds.max.y) })
                    if (corner.x >= roomMin.x && corner.x <= roomMax.x && corner.y >= roomMin.y && corner.y <= roomMax.y)
                        nodes.Add(corner);
            }
            var cost = new float[nodes.Count];
            var previous = new int[nodes.Count];
            var visited = new bool[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) { cost[i] = float.PositiveInfinity; previous[i] = -1; }
            cost[0] = 0;
            for (int iteration = 0; iteration < nodes.Count; iteration++)
            {
                int nearest = -1;
                for (int i = 0; i < nodes.Count; i++)
                    if (!visited[i] && (nearest < 0 || cost[i] < cost[nearest])) nearest = i;
                if (nearest < 0 || float.IsPositiveInfinity(cost[nearest])) break;
                if (nearest == 1) break;
                visited[nearest] = true;
                for (int i = 1; i < nodes.Count; i++)
                {
                    if (visited[i] || !ClearPath(nodes[nearest], nodes[i])) continue;
                    float candidate = cost[nearest] + Vector2.Distance(nodes[nearest], nodes[i]);
                    if (candidate < cost[i]) { cost[i] = candidate; previous[i] = nearest; }
                }
            }
            if (previous[1] < 0) return; // Motion will stop safely at the blocking surface.
            var path = new Stack<Vector2>();
            for (int node = 1; node > 0; node = previous[node]) path.Push(nodes[node]);
            foreach (Vector2 point in path) waypoints.Enqueue(point);
        }
    }
}

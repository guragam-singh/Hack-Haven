# REVERBAT Level 01 — forgiving echo puzzle

Open Assets/Scenes/Level01_Prototype.unity and press Play. The existing scene is unchanged.

Mouse-only solution:
1. Use Echo Pulse to reveal the dark room. Objects fade again; pulse whenever needed.
2. Select Move and fly anywhere in the right-hand half of the screen.
3. Select Echo Sight and click the right half of the plank, measured to the right of its centre. A small margin around the thin surface makes aiming forgiving.
4. The cyan beam follows a scripted bat -> plank -> bell -> coffin path. The bell reacts as it is reached; the coffin opens only when the beam arrives there. Yellow return sound retraces the path.
5. Click the red artifact to approach and collect it automatically.

Left-half shots do not solve the puzzle, even if a ray from the right would enter the plank through its right edge first. A bat on the left cannot activate the route. Clicking the divider does not count as the right half. Direct bell/coffin hits do not unlock the coffin.

Tilt is retained: in Move mode, click either end of the plank. The bat's weight tips that end down and the actual collider rotates. No particular tilt angle or precise launch position is needed for the scripted puzzle.

Darkness, Echo Pulse fading, movement, cooldown, mouse-only controls, artifact collection and ability unlocks remain in place. Sound still uses a raycast to find ordinary geometry; this puzzle does not calculate reflection angles. The thin plank has a 0.25-unit targeting margin. No scene rebuild, new level, art, physics packages or unrelated settings changes.

Gameplay scripts: Assets/Scripts/Reverbat. Test harness: Assets/Editor/Level01PrototypeValidation.cs. Run REVERBAT > Validate Level 01 in Play Mode while stopped. Report: Temp/Reverbat/validation.txt.

The initial prototype commit remains 869804a (initial prototype), pushed to origin/main. Later gameplay changes remain uncommitted.

Verified: all 17 scripted-route Play Mode checks passed, including rejection of left-half/left-side shots, the exact four-point beam path, nine right-side-position/tilt combinations, retained tilting, darkness and artifact completion. Tests drive gameplay input methods. No runtime errors were reported.


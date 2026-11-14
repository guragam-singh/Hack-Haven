# Current controls — WASD update

- WASD flies at the existing 4.5 units/second, with normalized diagonal input, existing circle collision checks and room clamps. There is no Move button or click-to-move.
- Toolbar order: 1 Echo Sight, 2 Echo Pulse, 3 Resonance, then Mimicry. Mouse and keys share one selected-ability state and highlight. Locked abilities reject selection. Resonance remains the existing post-completion placeholder.
- Select an ability, then aim and left-click to activate it. Merely selecting Echo Pulse does not fire it.
- Fly onto the plank's right end with WASD and release the keys to weight/tilt it. WASD flies off again. The -45 degree / right-half / right-side puzzle gates are unchanged.
- Approach the exposed artifact with WASD, then click it nearby to collect. A distant click never moves the bat.
- Dialogue dismissal still blocks clicks underneath. Dialogue content, menu logic, level layout and progression are unchanged.

The earlier implementation notes below describe previous control mappings where applicable.
# REVERBAT Level 01 — forgiving echo puzzle

Open Assets/Scenes/Level01_Prototype.unity and press Play. The existing scene is unchanged.

Mouse-only solution:
1. Use Echo Pulse to reveal the dark room. Objects fade again; pulse whenever needed.
2. The plank starts flat (0 degrees). Select Move and board its right end. The bat must physically tilt it to -45 degrees (plus or minus 10). Then fly into the right-hand half of the screen.
3. Select Echo Sight and click the right half of the plank, measured to the right of its centre. A small margin around the thin surface makes aiming forgiving.
4. The cyan beam follows a scripted bat -> plank -> bell -> coffin path. The bell reacts as it is reached; the coffin opens only when the beam arrives there. Yellow return sound retraces the path.
5. Click the red artifact to approach and collect it automatically.

Left-half shots do not solve the puzzle, even if a ray from the right would enter the plank through its right edge first. A bat on the left cannot activate the route. Clicking the divider does not count as the right half. Direct bell/coffin hits do not unlock the coffin.

Tilt is retained: in Move mode, click either end of the plank. The bat's weight tips that end down and the actual collider rotates. The -45-degree tilt is required; no precise launch position is needed. Incorrect angles show a harmless bounce. A second gate checks the angle and valid route again at coffin arrival.

Darkness, Echo Pulse fading, movement, cooldown, mouse-only controls, artifact collection and ability unlocks remain in place. Sound still uses a raycast to find ordinary geometry; this puzzle does not calculate reflection angles. The thin plank has a 0.25-unit targeting margin. No scene rebuild, new level, art, physics packages or unrelated settings changes.

Gameplay scripts: Assets/Scripts/Reverbat. Test harness: Assets/Editor/Level01PrototypeValidation.cs. Run REVERBAT > Validate Level 01 in Play Mode while stopped. Report: Temp/Reverbat/validation.txt.

Current checkpoint: 0c9171c (V0.1) was committed and pushed to origin/main before this progression fix. The progression fix remains uncommitted.


Progression verification: all 25 Play Mode checks passed, including flat start, bat-weight prerequisite, harmless wrong-angle bounces, tolerance limits, wrong-half rejection, arrival-time angle check, and complete artifact collection.


## Level 1 dialogue

Dialogue content is editable in Assets/Resources/Level01Dialogue.asset. Each of the seven entries has a unique triggerId, characterName, text, optional voiceClip (all empty), and duration (default 5 seconds). Select that asset in Unity's Project window to change lines, durations or assign future AudioClips. No scene setup is required.

DialogueManager handles a once-per-level queue, unscaled-time expiry, click-to-dismiss, same-frame gameplay click blocking, UI drawing, and 2D voice playback. Assigned clips play when their entry becomes active; the display lasts at least the configured duration or the clip length, whichever is longer. Dismissal stops the clip. No audio was created or downloaded.

Level01Manager publishes presentation-only trigger events. Hooks cover level start, successful radial pulse emission, reflector approach or interaction, first correct weight-driven tilt, valid coffin unlock, and artifact collection. Artifact and dictator entries are enqueued consecutively. Gameplay does not pause. The dark panel has a thin purple border and stays above the existing toolbar and left of the coffin/artifact.

Verified: all 42 combined dialogue/puzzle Play Mode checks passed. Includes single-fire triggers, queue order, auto-expiry, continued movement, UI click shielding, and full puzzle completion. Opening panel rendered and visually inspected. Voice clips remain unassigned; actual voiced playback was not tested.

## Main menu

Assets/Scenes/MainMenu.unity is the first build scene. Play loads Level01_Prototype. Settings has a master-volume slider and Back; volume is saved with PlayerPrefs under Reverbat.MasterVolume and applied through AudioListener.volume. Quit Game calls Application.Quit (effective in player builds). The menu uses a centred reference layout scaled to fit the screen, with no changes to Level 1 logic or dialogue.

Verified in Unity Play Mode: Settings/Back, master-volume application and persistence, first build-scene order, and Play loading Level 1 with its dialogue, flat plank and locked coffin. Menu screenshot visually inspected. Standalone build execution and quitting a standalone player were not tested.


Controls verification: all 25 Play Mode checks passed. W/A/S/D and 1/2/3 were exercised using queued Input System keyboard events; precise puzzle navigation used the shared movement handler. Mouse-selection callbacks, world activation, collision/bounds, WASD boarding/leaving, coffin opening, nearby artifact interaction and dialogue order passed. No menu or dialogue content changes were made.


Art update: selected Guragam PNGs are integrated into the existing scene. See ART_INTEGRATION.md for the source mapping and sprite-region details. All 32 final art/gameplay Play Mode checks passed; all original collision geometry is preserved. Unused assets and baked-interactable reference sheets were not imported.


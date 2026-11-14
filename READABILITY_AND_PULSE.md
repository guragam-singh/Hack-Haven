# Bat readability, mirror contact and smooth Echo Pulse

The existing Level01_Prototype scene now includes a saved EchoPulse2D component on the Level01_Prototype root GameObject. Select that object in the Hierarchy to edit:

- **Cooldown** (default 2.5 seconds): minimum time between successful Echo Pulse activations. Independent of ripple travel and lit duration.
- **Duration** (default 5 seconds): how long each reached part of the room remains lit before fading.
- **Wave Speed**, **Reveal Feather**, **Fade Out Duration**: secondary visual smoothing controls.

Settings are editable before entering Play Mode. Settings changed during Play Mode follow Unity's normal temporary-play-edit behaviour. Each emitted pulse snapshots its timing values; changes apply to subsequent pulses. A short cooldown can overlap pulses while their soft reveals continue.

The reveal is a shared world-space shader mask. Darkness clears smoothly inside the circle as it expands from the emission position, instead of toggling an entire background sprite at its centre. The cyan ring has 192 segments and uses elapsed time rather than accumulated radius steps. Each revealed region holds for Duration, then fades back to black. Coffin/artifact visibility, dialogue, toolbar and the separate reflected-beam mechanic retain their existing rules. The bat is still revealed by Echo Pulse; the stroke follows that visibility.

Bat visible width increased from 0.9 to 1.12 world units (about 24%). A light-purple silhouette stroke is rendered by its material shader; no source artwork was edited, and movement/collision size did not change. BatHighlight.mat controls stroke colour and source-pixel width. Its stroke remains visible in the Scene editor outside Play Mode.

Mirror control is a scripted, forgiving two-half contact check. Position the bat over the right half and the target latches to -45 degrees, with smooth damped rotation. It no longer requires an exact end-point or repositions/drags the bat on every frame. Moving away does not cancel the tilt. The left half retains the opposite tilt; the stationary stand does not rotate. The existing valid-angle, right-half echo and coffin progression checks remain in place.

Changed code: EchoPulse2D, TiltableReflector2D, Level01Manager setup/hints and EchoSystem2D hint. New shaders are under Assets/Shaders/Reverbat; materials under Assets/Art/Guragam/Level01/Materials. REVERBAT > Apply Bat Highlight and Smooth Pulse reapplies the visual setup without resetting existing cooldown/duration values.

Validation: REVERBAT > Validate Level 01 in Play Mode. Report is Temp/Reverbat/validation.txt. Visual capture of the expanding soft mask is Captures/EchoPulse_SoftFront.png.

Final verification: all 43 Play Mode checks passed after the final hint/editor-visibility changes. Shader compilation and the runtime console report zero errors. The soft expanding mask was visually inspected in EchoPulse_SoftFront.png. Tests cover Inspector settings persistence, cooldown rejection/expiry/overlap, duration fading, right-half contact without bat teleportation, controls, and complete puzzle/dialogue progression.


Sprite binding fix: every reveal-affected SpriteRenderer now owns a runtime material instance with its source texture explicitly assigned. Sprite batching is disabled for these two custom shaders so Unity cannot reuse the bell texture on the sliced mirror (or vice versa). Material instances are cleaned up on scene unload. This preserves the original Bell_Hanging and Reflector_Plate_Rotating sprites throughout pulses, fades and tilting.


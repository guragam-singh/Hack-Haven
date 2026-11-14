# REVERBAT Level 01

Open `Assets/Scenes/Level01_Prototype.unity` and press Unity Play. No package or input setup is needed beyond the project's existing Input System and URP.

## Mouse controls and solution

- **Move**: click a destination; the bat flies smoothly, routing around solid shapes.
- **Echo Sight**: click a location/object to emit a pulse. Cyan travels out; yellow retraces the collision path. Pulses have a cooldown and cannot overlap.
- From the starting position, select Echo Sight and click the coffin. When the echo returns, the bell-to-coffin connection appears for three seconds.
- Still at the starting position, click the centre of the cyan 45-degree reflector. The horizontal ray reflects upward into the bell. Clicking the bell directly does not open the coffin.
- Click the red artifact after the coffin opens. The bat flies into range and collects it automatically, even with Echo Sight selected.
- Resonance unlocks on completion; its gameplay and Mimicry are deliberately outside Level 1's scope. No keyboard input is used.

## Code and scene

- `Assets/Scripts/Reverbat/BatController2D.cs`: mouse input, movement, bounds, small visibility graph around solid objects, automatic artifact approach.
- `Assets/Scripts/Reverbat/EchoSystem2D.cs`: actual Collider2D raycasts, up to three reflector bounces using Vector2.Reflect, travelling LineRenderers, scripted retrace and arrival callbacks.
- `Assets/Scripts/Reverbat/SoundInteractable.cs`: reusable wall/reflector/bell/hidden-mechanism responses.
- `Assets/Scripts/Reverbat/Level01Manager.cs`: puzzle state, temporary clue, toolbar and text feedback.
- `Assets/Editor/Level01PrototypeBuilder.cs`: creates the separate scene, two plain placeholder sprites, and one unlit material. Existing unsaved scenes are backed up before initial generation. All shapes and references are serialized in the scene for easy art replacement.
- `Assets/Editor/Level01PrototypeValidation.cs`: real Play Mode regression checks; run **REVERBAT > Validate Level 01 in Play Mode** with play stopped. Report: `Temp/Reverbat/validation.txt`.

The prototype scene is first in Build Settings; the existing SampleScene remains listed. The scene uses its own fixed orthographic camera and unlit shapes. No animations, audio, lights, particles, art assets, menus, extra levels, or external downloads were added.

Return sound retraces the calculated outgoing segments rather than simulating acoustics. The bottom toolbar is a compact immediate-mode GUI for this prototype. The reflector is fixed; movement can change the correct aiming direction. The easiest solution uses the initial bat position.

Validation result: all 14 checks passed in Unity Play Mode, including five nearby valid aiming points, bounds, cooldown, timed reveal, negative direct-bell test, reflected unlock, and click-to-collect completion. The checks drive the same gameplay methods as mouse input; the rendered completion screen was visually inspected. Unity reported zero runtime console errors. Latest report: Temp/Reverbat/validation.txt.


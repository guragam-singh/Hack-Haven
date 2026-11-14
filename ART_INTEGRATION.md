# Level 1 art integration — Guragam asset pack

Applied to the existing Assets/Scenes/Level01_Prototype.unity. The scene was not rebuilt.

Imported 12 original PNGs under Assets/Art/Guragam/Level01:
- Bat_Dracula, Bell_Hanging, Coffin_Closed, Coffin_Open_Interior.
- Reflector_Plate_Rotating and Reflector_Stand_Static.
- Collectible_BloodDrop_Red.
- BG_Room_Clean_GameplayObjectsRemoved and FG_Cracked_Stone_Floor_Edge.
- Echo Sight, Echo Pulse and Locked HUD icons.

All imported PNGs retain their exact original bytes. Other artwork stays in the supplied ZIP and is not imported into the game. The baked-interactable background, baked-label HUD and reflection/barrier reference sheet are not used. No new mechanics, animations, particles or sound were added.

The background uses a Unity Sprite region (0, 211, 1672, 730), excluding the lower coffin-removal remnant and the bottom black band. The floor uses its top 30-pixel strip (0, 169, 1401, 30), sized to the original floor thickness so it does not interfere with the toolbar. Source images are not edited. The region assets live in SpriteRegions.

The existing SpriteRenderers retain their GameObject transforms and all original colliders. Renderer sizes provide visible art dimensions separately from collision dimensions. The stand and room artwork are non-colliding children of the level root. The stand is not parented to the tilting plate.

Level01ArtPresentation swaps the open-coffin drawing when the existing valid-unlock event fires and draws the three supplied ability icons alongside the existing live labels. The level's existing darkness component automatically includes the new room and stand sprites, revealing/fading them through Echo Pulse. Coffin remains the only initially visible world object. Text, counters, dialogue and ability buttons remain functional Unity UI.

Unity can rescale a Transform when switching SpriteRenderer from Simple to Sliced. The installer explicitly restores each original scale to protect physics geometry. Geometry was checked against a pre-art snapshot of all seven original BoxCollider2D components.

To reapply visual assignments: open the existing Level01_Prototype scene outside Play Mode, then REVERBAT > Apply Guragam Level 01 Art. This is an editor art setup command, not a level rebuild. It is safe to repeat for this art configuration.

Play Mode validation: REVERBAT > Validate Level 01 in Play Mode. Report: Temp/Reverbat/validation.txt. Captures: Captures/Level01_Art_Revealed.png and Level01_Art_CoffinOpen.png. These generated captures and Temp files are not source assets.

Final verification: all 32 Play Mode checks passed after the floor/HUD cleanup; zero runtime errors. All seven original collider snapshots match. The closed/open states, red drop, fixed stand, darkness reveal, keyboard controls, puzzle completion and dialogue ordering were checked. The revealed-room and open-coffin captures were visually inspected.


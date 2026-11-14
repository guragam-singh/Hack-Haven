# REVERBAT Level 1 — individually named kid-drawn PNG assets

**Each PNG is a single visually identified object or UI/FX element.** All except the backgrounds have alpha transparency.

## Main Unity mapping

| Existing GameObject | Replace its visible sprite with | Notes |
|---|---|---|
| Bat | `01_Characters/Bat_Dracula.png` | Keep existing player movement and collider |
| Coffin | `02_Interactables/Coffin_Closed.png` | Use `Coffin_Open_Interior.png` for opened state if appropriate |
| Bell | `02_Interactables/Bell_Hanging.png` | Keep existing sound interaction |
| Reflector rotating part | `02_Interactables/Reflector_Plate_Rotating.png` | Align sprite pivot with rotating object |
| Reflector stand | `02_Interactables/Reflector_Stand_Static.png` | Keep base stationary |

**Background:** Use `00_Background/BG_Room_Clean_GameplayObjectsRemoved.png` as an optional starter, inspect first for patch artifacts. `BG_Room_WithBakedBellReflectorCoffin_REFERENCE_ONLY.png` contains duplicate interactables and must NOT be used behind the live interactables unless edited. The original art is 1672×941, not necessarily the Unity gameplay aspect ratio; position/scale to the active camera.

**UI:** Files marked `BakedText` or `WithBakedIcons` contain hardcoded labels/icons and are references; use Unity text/counters if values change.

**FX:** The sound reflection sheet includes a sample barrier shape; the file marked `REFERENCE` is NOT a standalone clean wave. Use Unity's existing wave logic and these visuals as appropriate.

**Darkness:** Do not bake darkness into the artwork. Arhaan's Unity logic controls reveal and visibility.

**No animations or facial-expression sequences** are included. All art deliberately has wobbly, imperfect childlike outlines and flat fills.

## Folder counts
- `00_Background/`: 2 PNG files
- `01_Characters/`: 1 PNG files
- `02_Interactables/`: 5 PNG files
- `03_Background_Decor/`: 21 PNG files
- `04_Foreground/`: 5 PNG files
- `05_FX/`: 8 PNG files
- `06_UI/`: 14 PNG files
- `07_Extras/`: 13 PNG files

## Asset names

### 00_Background
- `BG_Room_Clean_GameplayObjectsRemoved.png` — Room background with baked bell, reflector and coffin digitally painted out
- `BG_Room_WithBakedBellReflectorCoffin_REFERENCE_ONLY.png` — Original kid-drawn room with bell, reflector and coffin baked into wall

### 01_Characters
- `Bat_Dracula.png` — Single bat sprite, wings extended

### 02_Interactables
- `Bell_Hanging.png` — Yellow hanging bell
- `Coffin_Closed.png` — Coffin closed, red with yellow cross
- `Coffin_Open_Interior.png` — Coffin interior/open view; separate state
- `Reflector_Plate_Rotating.png` — Blue horizontal reflective plate
- `Reflector_Stand_Static.png` — Gray reflector pivot/base

### 03_Background_Decor
- `Banner_Bat_Red.png` — Tattered red bat-emblem banner
- `Books_Stack.png` — Stack of three books
- `Candle_Stand_Single.png` — One tall floor candle stand
- `Chandelier_Candles.png` — Ceiling chandelier with five candles
- `Cobweb_Corner.png` — Gray cobweb hanging from corner
- `Curtains_Red_Pair.png` — Two red curtain panels tied back
- `Gothic_Window_Moon.png` — Pointed Gothic window with moon
- `Metal_Chain_Arc.png` — Curved hanging metal chain
- `Portrait_Castle_Framed.png` — Framed painting of castle and moon
- `Potion_Green_Flask.png` — Pointed flask with green potion
- `Potion_Purple_Bottle.png` — Round flask with purple potion
- `Red_Rug.png` — Red rug with gold border and fringes
- `Skull.png` — Cartoon ivory skull
- `Stone_Column_Broken.png` — Broken masonry column and loose stones
- `Stone_Column_Full.png` — Full gray stone pillar
- `Wall_Shelf_Books_Potion.png` — Wood shelf with books and bottle
- `Wall_Stone_Ledge.png` — Plain low stone shelf/ledge
- `Wall_Torch_Flame.png` — Flaming wall-mounted torch
- `Wooden_Barrel.png` — Barrel with gray metal hoops
- `Wooden_Crate.png` — Square wooden storage crate
- `Wooden_Stool.png` — Round three-legged wooden stool

### 04_Foreground
- `FG_Cracked_Stone_Floor_Edge.png` — Wide rocky platform/floor lip
- `FG_Pale_Mist.png` — Pale purple mist cloud
- `FG_Rubble_Rock_Pile.png` — Pile of gray boulders
- `FG_Stone_Balustrade.png` — Short stone balcony balustrade
- `FG_Thorn_Bush.png` — Spiky dark thorn bush

### 05_FX
- `FX_Coffin_Reveal_Glow.png` — Coffin silhouette with cyan reveal glow
- `FX_Cyan_Sparkles.png` — Three uneven cyan star sparkles
- `FX_Darkness_Sweep.png` — Soft grayish-purple shadow smear
- `FX_Dust_Puff.png` — Pale gray dust cloud
- `FX_EchoPulse_Circular.png` — Three uneven cyan rings radiating outward
- `FX_EchoReflection_WithBarrier_REFERENCE.png` — Cyan reflected sound visualization with vertical reflective object
- `FX_EchoWave_Directional.png` — Cyan directional sound arcs
- `FX_Interaction_ImpactBurst.png` — Jagged cyan impact burst

### 06_UI
- `UI_Ability_EchoPulse_Icon.png` — Yellow sound pulse ability button
- `UI_Ability_EchoSight_Icon.png` — Blue eye ability button
- `UI_Ability_Locked_Icon.png` — Gray locked ability button
- `UI_Artifact_Counter_0of1_BakedText.png` — Artifact counter: 0/1
- `UI_Bottom_HUD_Bar_WithBakedIcons_REFERENCE.png` — Full dark bottom HUD with slots, bat logo, artifact counter, settings
- `UI_Dialogue_ThreeDots_Icon.png` — Cream chat bubble with three dots
- `UI_InventorySlot_Empty_01.png` — Empty cream item slot position 1
- `UI_InventorySlot_Empty_02.png` — Empty cream item slot position 2
- `UI_InventorySlot_Empty_03.png` — Empty cream item slot position 3
- `UI_InventorySlot_Empty_04.png` — Empty cream item slot position 4
- `UI_InventorySlot_Empty_05.png` — Empty cream item slot position 5
- `UI_Level_Title_BakedText.png` — Level header: REVERBAT / LEVEL 01 — THE CRYPT
- `UI_Objective_Panel_BakedText.png` — Objective instruction card
- `UI_Settings_Gear.png` — Settings gear icon

### 07_Extras
- `Collectible_BloodDrop_Red.png` — Single red blood artifact droplet
- `Crest_Bat_Shield_Purple.png` — Purple shield crest with bat
- `Emphasis_Lines_Pink.png` — Three pink comic emphasis lines
- `Emphasis_Lines_Yellow.png` — Three yellow comic emphasis lines
- `Key_Golden.png` — Yellow key
- `LockPlate_Keyhole_Gold.png` — Gold square with keyhole
- `Mouse_Cartoon_Gray.png` — Tiny gray mouse
- `Ornament_Corner_Left.png` — Purple corner flourished frame left
- `Ornament_Corner_Right.png` — Purple corner flourished frame right
- `Ornament_Divider_GoldFlower.png` — Horizontal flourish and golden flower center
- `Ornament_Swirl_Top.png` — Long purple ornamental scroll flourish
- `Sparkle_Stars_Gold.png` — Two gold cartoon stars
- `SpeechBubble_Blank_Cream.png` — Empty hand-drawn dialogue balloon


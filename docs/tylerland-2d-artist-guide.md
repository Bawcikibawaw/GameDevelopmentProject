# 🎨 Tylerland: Animal Kingdom — 100% 2D Hand-Drawn Art Specification & Artist Checklist

> **Art Direction Philosophy**: 
> *Tylerland: Animal Kingdom* is a **purely 2D hand-drawn horror game** (zero 3D models or primitive meshes). The visual style blends grungy 90s animatronic horror, hand-inked comic textures, gritty surveillance CRT monitors, and stark lighting contrast (dark shadows vs. harsh flashlight beams).

> [!IMPORTANT]
> **🚨 CRITICAL DESIGN RULE: FIXED POSITIONS PER PHASE & DAMAGE RELOCATION**
> 1. **NO CAMERA-ZOOMING OR CREEPING TO THE LENS**: Animatronics do **NOT** show they are coming by getting larger or creeping closer to the camera lens. In every room phase, the animatronic sprite sits in a **fixed, designated anchor position** (e.g., Spot A $\rightarrow$ Spot B $\rightarrow$ Doorway).
> 2. **DAMAGE CHANGES FORM AND LOCATION**: When an animatronic is hit by a counter-weapon or takes damage, **both its FORM changes** (cracked chassis, exposed skull, Nightmare transformation) **AND its LOCATION changes** (it immediately relocates from the doorway back to its retreat room stage)!

---

## 📐 Universal Technical Specifications

| Parameter | Specification | Details / Purpose |
| :--- | :--- | :--- |
| **Engine Standard** | Unity 6 (URP 2D) | Rendered using `SpriteRenderer` and URP 2D Unlit/Lit shaders |
| **Pixels Per Unit (PPU)** | **100 PPU** | 100 pixels in Photoshop/ClipStudio = 1.0 meter in the Unity world |
| **File Format** | **PNG (32-bit RGBA)** | Must include transparent alpha channels (especially for cutouts and overlays) |
| **Color Space** | **sRGB** | Point/Bilinear filter mode with clean pixel alpha edges |
| **Base Canvas Resolution** | **1920 × 1080 (16:9 Full HD)** | Master canvas standard for rooms, feeds, and full-screen UI |

---

## 🗺️ Roadmap Section 4: Office & Surveillance Systems

### 4.1 Security Office Environment (100% 2D Sprites)

All 4 cardinal walls surrounding the player are 2D billboard sprites. The player rotates the 3D camera to face each wall.

| Roadmap Section | Asset Name | Dimensions | Variations / States | Description & Placement Rules |
| :--- | :--- | :--- | :--- | :--- |
| **§4.1** | **Office Wall — Left (Erle Side)** | 1920 × 700 px (10m × 3.5m) | 1 version | Pizzeria wallpaper with Sheriff/Western decor. **Must have a transparent doorway cutout in the center** (approx. 400 × 560 px) for the corridor opening. |
| **§4.1** | **Office Wall — Center (Tyler Side)** | 1920 × 700 px (10m × 3.5m) | 1 version | Lead Singer / Main Stage crimson wall. **Must have a transparent doorway cutout in the center** (approx. 400 × 560 px). |
| **§4.1** | **Office Wall — Right (Orville Side)** | 1920 × 700 px (10m × 3.5m) | 1 version | Clinical/Aquatic medical bay wall. **Must have a transparent doorway cutout in the center** (approx. 400 × 560 px). |
| **§4.1** | **Office Wall — Rear (Sandy Side)** | 1920 × 700 px (10m × 3.5m) | 1 version | Grungy utility/maintenance wall behind player. **Must have a transparent doorway cutout in the center** (approx. 400 × 560 px). |
| **§4.1** | **Security Desk & Fixed Computer (Tyler Wall Side)** | 600 × 380 px | 2 states: Screen Idle, Screen Static | **CRITICAL PLACEMENT RULE**: This desk and CRT computer monitor sit **exclusively on the Tyler animatronic side (Center view)**, positioned on the **lower-right side of the room** so that **the center doorway is 100% completely visible and unobstructed**! The player clicks this computer screen with the mouse to open the cameras. |
| **§4.1** | **Near-Door Physical Wall Button Panels** | 160 × 240 px (per panel) | 4 Door states (Open, Closed, Lockout, Jammed) + 2 Light states (On, Off) | Mounted on the wall right beside every door frame for mouse point-and-click control: <br>• **Door Button**: Large industrial slam-button (Green when open, Red when shut, sparking when jammed). <br>• **Flashlight Button**: Rocker switch with bulb icon (Dark gray off, glowing yellow on). |
| **§4.1** | **Office Floor** | 1024 × 1024 px (Seamless Tile) | 1 version | Classic dirty black-and-white checkered linoleum tiles with grime and water stains. |
| **§4.1** | **Office Ceiling** | 1024 × 1024 px (Seamless Tile) | 1 version | Stained acoustic ceiling tiles, hanging wires, and a flickering fluorescent light housing. |

---

### 4.2 The 4 Camera Room Feeds (Surveillance Feeds)

The security tablet monitors 4 dedicated rooms. Each camera feed requires the empty room background plus progressive animatronic poses.

| Roadmap Section | Room / Channel | Dimensions | Required Variations | Visual Description |
| :--- | :--- | :--- | :--- | :--- |
| **§4.2** | **CAM 01 — Erle the Eagle (Sheriff Station)** | 1920 × 1080 px | • Empty Room <br>• Stage 1 (Perched on desk with badge) <br>• Stage 2 (Standing, wings twitching) <br>• Stage 3 (Crouching at doorway) | Western-themed security station, wanted posters, jail bars, gold/amber hues. |
| **§4.2** | **CAM 02 — Tyler the Lion (Main Show Stage)** | 1920 × 1080 px | • Empty Stage <br>• Stage 1 (On stage with microphone) <br>• Stage 2 (Stepped off stage into dark) <br>• Stage 3 (Peeking from corridor arch) | Grand pizzeria show stage, red velvet curtains, party tables with party hats. |
| **§4.2** | **CAM 02 — Smashed Lens (Nightmare Tyler Destroyed)** | 1920 × 1080 px | 1 broken feed state | Shattered CRT glass with exposed wires and heavy black static. Nightmare Tyler destroys this camera permanently when hit by the shotgun! |
| **§4.2** | **CAM 03 — Orville the Orca (Medical Bay)** | 1920 × 1080 px | • Empty Bay <br>• Stage 1 (Floating in dark medical tank) <br>• Stage 2 (Giant face pressed directly against glass/lens) <br>• Stage 3 (Podium empty, water dripping) | Gloomy aquatic veterinary ward, tile walls, examination table, clinical cyan lighting. |
| **§4.2** | **CAM 04 — Sandy the Snake (Maintenance & Ball Pit)** | 1920 × 1080 px | • Empty Pit <br>• Stage 1 (Coiled in ceiling pipes) <br>• Stage 2 (Slithering down into plastic balls) <br>• Stage 3 (Rearing out of pit towards camera) <br>• **Nightmare Shed Decoy Shell** (Hollow, lifeless husk left in pit!) | Industrial basement maintenance room filled with a filthy ball pit, rusted pipes, green emergency lighting. |
| **§4.2** | **Surveillance UI & CRT Overlays** | 1920 × 1080 px | Overlay PNGs | • CRT curved screen glass reflection & scanlines <br>• Animated static noise texture sheet (4 frames) <br>• Blinking red "● REC" dot <br>• 4-Room Tablet Radar Map graphic with button frames |

---

### 4.3 Door Shutters & Corridors (4 Directions)

Behind every wall cutout sits a dark corridor backdrop. When the shutter drops, it seals the doorway.

| Roadmap Section | Asset Name | Dimensions | Required Variations | Description |
| :--- | :--- | :--- | :--- | :--- |
| **§4.3** | **4 Corridor Backdrops (Left, Center, Right, Rear)** | 800 × 1200 px (each) | 2 versions per corridor: <br>1. **Dark / Unlit** (Deep silhouette darkness) <br>2. **Lit by Flashlight** (Illuminated hallway) | 1-point perspective corridors leading away into darkness. The lit versions reveal dirty walls, wires, and any animatronic standing at the doorway! |
| **§4.3** | **4 Door Shutters (Heavy Security Rollers)** | 400 × 600 px (each) | 3 versions: <br>1. Normal Closed (Heavy steel slats + yellow hazard stripes) <br>2. Jammed/Dented (Bloody dents, smoke) <br>3. Sparks (Lockout electric failure) | The physical moving shutter that slams down to block attacks. |

---

## 🦁 Roadmap Section 5: Animatronics, Blindspots & Jumpscares

### 5.1-5.6 The 4 Animatronics (Doorway Blindspots & Stages)

When animatronics reach your door, they stand right in the doorway cutout in front of the corridor backdrop.

| Section | Character & Role | Blindspot Door Sprites (700 × 1000 px) | Nightmare Transformation | Special Weapon Counter |
| :--- | :--- | :--- | :--- | :--- |
| **§5.2 / §5.6** | **Tyler the Lion** <br>*(Center Door / CAM 02)* | • Normal Silhouette (Dark) <br>• Normal Illuminated (Full detail) <br>• Damage Lvl 1, 2, 3 (Jaw dislocated, frame exposed) | **Nightmare Tyler**: Shattered head casing, demonic red pupils, elongated fangs, exposed hydraulic spine. | **Shotgun** (Triggers Nightmare & camera destruction) |
| **§5.2 / §5.6** | **Orville the Orca** <br>*(Right Door / CAM 03)* | • Normal Silhouette (Dark) <br>• Normal Illuminated (Full detail) <br>• Damage Lvl 1, 2, 3 (Cracked dorsal fin, bleeding blowhole) | **Nightmare Orville**: Triple row of needle teeth, pitch-black oil tears, bioluminescent veins, towering bulk. | **Pneumatic Harpoon** (Impales chassis) |
| **§5.2 / §5.3** | **Sandy the Snake** <br>*(Rear Door / CAM 04)* | • Normal Silhouette (Dark) <br>• Normal Illuminated (Full detail) <br>• Damage Lvl 1, 2, 3 (Peeling rubber skin, exposed wiring) <br>• **Shed Decoy Shell** (Empty hollow skin) | **Nightmare Sandy**: Split serpentine jaws, multiple glowing eyes, barbed metallic scales, hooded cobra crest. | **Spiked Mace** (Crushes serpentine skull) |
| **§5.2 / §5.6** | **Erle the Eagle** <br>*(Left Door / CAM 01)* | • Good Cop Mode (Calm posture) <br>• Bad Cop Mode (Aggressive hunched posture) <br>• Damage Lvl 1, 2, 3 (Broken beak, missing eye) | **Nightmare Erle**: Razor talons extending 1 meter, shredded feathers revealing skeletal steel wings, cracked sheriff badge embedded in chest. | **Electric Stun Gun** (Overloads battery capacitors) |
| **§5.7** | **True Final Boss (Night 6)** | • Multi-Door Stalker Sprites <br>• 3 Combat Phases | **Amalgamation**: Eldritch fusion of Lion mane, Orca jaw, Eagle talons, and Snake coils. | Requires surviving all 4 Nightmares in one run. |

---

### 5.4 2D Jumpscare Animation Sequences

Full-screen jumpscares play immediately when an animatronic reaches 0 seconds on its reaction timer and the door is open.

| Section | Jumpscare Sequence | Resolution | Frames | Motion / Keyframe Description |
| :--- | :--- | :--- | :--- | :--- |
| **§5.4** | **Tyler the Lion Jumpscare** | 1920 × 1080 px | 6 – 8 frames | Lunges forward out of the dark, jaw snaps shut on the camera lens, screen shakes with violent blood splatter. |
| **§5.4** | **Nightmare Tyler Jumpscare** | 1920 × 1080 px | 6 – 8 frames | Exposed robotic skull slams into view, glowing red optics flare, screen inverts colors with violent static. |
| **§5.4** | **Orville the Orca Jumpscare** | 1920 × 1080 px | 6 – 8 frames | Massive aquatic maw rises from below, clamps entire screen in razor teeth with splashing murky water. |
| **§5.4** | **Sandy the Snake Jumpscare** | 1920 × 1080 px | 6 – 8 frames | Fast serpentine strike from the ceiling/behind, fangs bite directly into player's eyes, venom drops sizzle on lens. |
| **§5.4** | **Erle the Eagle Jumpscare** | 1920 × 1080 px | 6 – 8 frames | Talons rake diagonally across the glass, screeching beak tears into the camera screen. |
| **§5.4** | **Boss Amalgamation Jumpscare** | 1920 × 1080 px | 8 – 10 frames | Chaos of multiple jaws, talons, and coils crushing the player simultaneously into pitch blackness. |

---

### 5.5 Special Counter-Weapons (Q Key Items)

| Section | Weapon Asset | Dimensions | Icon & In-Hand Sprites Needed |
| :--- | :--- | :--- | :--- |
| **§5.5** | **12-Gauge Security Shotgun** | 512 × 512 px (Icon) <br>800 × 500 px (In-Hand HUD) | • Shop / Inventory Icon <br>• In-hand firing muzzle flash frame (used when pressing Q at Center Door) |
| **§5.5** | **High-Voltage Electric Stun Gun** | 512 × 512 px (Icon) <br>800 × 500 px (In-Hand HUD) | • Shop / Inventory Icon <br>• In-hand arc discharge electricity frame (Left Door) |
| **§5.5** | **Pneumatic Harpoon Cannon** | 512 × 512 px (Icon) <br>800 × 500 px (In-Hand HUD) | • Shop / Inventory Icon <br>• In-hand launched spear cable frame (Right Door) |
| **§5.5** | **Heavy Spiked Industrial Mace** | 512 × 512 px (Icon) <br>800 × 500 px (In-Hand HUD) | • Shop / Inventory Icon <br>• In-hand swinging steel impact frame (Rear Door) |

---

## 💰 Roadmap Section 6: Economy, Shops & Minigames

| Roadmap Section | Screen / System | Canvas Size | Key Visual Elements Needed |
| :--- | :--- | :--- | :--- |
| **§6.1** | **Night End Salary Screen** | 1920 × 1080 px | Stained clipboard with daily shift breakdown, bloodstained dollar bills, red "REPAIR EXPENSES DEDUCTED" rubber stamp graphic. |
| **§6.2** | **Normal Roguelite Shop UI** | 1920 × 1080 px | Friendly/grungy vending kiosk interface, price tags, reroll button, sold-out stickers, 12 item upgrade icons (Door Reinforcement, High-Capacity Battery, Motion Radar, etc.). |
| **§6.3** | **Soul Shop & Soul Coins** | 1920 × 1080 px | Demonic occult vending machine, glowing purple/gold Soul Coins, unlockable cursed relics. |
| **§6.4** | **Minigame 1: Wire Splicing** | 1024 × 768 px | Open junction box with severed red, yellow, blue, and green wires + electrical spark effects. |
| **§6.4** | **Minigame 2: Pressure Levers** | 1024 × 768 px | Industrial steam boiler panel with 3 brass lever switches and twitching analog pressure gauges. |
| **§6.4** | **Minigame 3: Memory Keypad** | 1024 × 768 px | Retro keypad with 4 glowing animal face buttons that light up in sequence. |

---

## 📜 Roadmap Section 7: Presentation, Atmosphere & HUD

| Roadmap Section | Item | Dimensions | Description |
| :--- | :--- | :--- | :--- |
| **§7.3** | **Environmental Storytelling Posters** | 300 × 500 px (each) | • "Tyler & Friends Live!" concert poster (vintage 1994) <br>• "Report All Bites" OSHA safety poster <br>• Disturbed child crayon drawings of the animatronics <br>• Stained sticky notes with employee scribbles and lore clues |
| **§7.4** | **Diegetic HUD Elements** | Various UI PNGs | • Digital LED clock numbers (12 AM – 6 AM) <br>• Battery bar gauge & lightning bolt icon <br>• Door durability integrity meter <br>• Pulsing warning arrows (Left, Top, Right, Bottom) for doorway threats <br>• Vintage intercom / speaker box icon for phone calls |

---

## 🎬 Roadmap Section 3: Core Run & Game Flow Screens

| Roadmap Section | Screen | Dimensions | Visual Description |
| :--- | :--- | :--- | :--- |
| **§3.1** | **Main Menu Screen** | 1920 × 1080 px | Grungy game title logo (*Tylerland: Animal Kingdom*), dark office silhouette, flickering CRT logo, Night 1-6 selector cards. |
| **§3.1** | **Night Intro Screen** | 1920 × 1080 px | Pitch-black transition screen with ticking grandfather clock animation: *"12:00 AM — NIGHT X"*. |
| **§3.1** | **Game Over Screen** | 1920 × 1080 px | CRT static blizzard with *"YOU ARE DEAD"* text, newspaper clipping mentioning the night guard's disappearance. |
| **§3.1** | **6 AM Shift Complete (Victory)** | 1920 × 1080 px | Clock ticking from 5:59 to 6:00 AM with school bell chimes, confetti graphics, and warm dawn morning light peeking through window. |

---

## 📋 Artist Delivery Checklist Summary

- [ ] **4 Office Walls** (10m × 3.5m, transparent doorway cutouts in center)
- [ ] **1 Security Desk & Computer** (Designed for the lower-right of the Tyler wall)
- [ ] **4 Wall Button Panels** (Door Open/Close + Light On/Off states)
- [ ] **1 Floor & 1 Ceiling Texture** (Checkerboard linoleum & industrial tiles)
- [ ] **4 Corridor Backdrops** (Each in Dark and Flashlight-Lit states = 8 total)
- [ ] **4 Door Shutters** (Normal Closed, Jammed/Broken, Sparks = 12 total)
- [ ] **4 Camera Room Backgrounds** (Empty state when animatronic leaves = 4 total)
- [ ] **Animatronic Camera Stages** (3-4 progressive stages per animatronic = 15 total)
- [ ] **Sandy Shed Decoy Shell** (Empty husk for CAM 04 ball pit)
- [ ] **CAM 02 Smashed Feed** (Broken CRT visual for Nightmare Tyler)
- [ ] **Doorway Blindspot Sprites** (Normal, Damaged, and Nightmare for all 4 = 16 total)
- [ ] **2D Jumpscare Sequences** (6-8 frames for Tyler, Orville, Sandy, Erle, Boss = ~40 frames)
- [ ] **4 Special Counter-Weapons** (Icons and in-hand firing sprites)
- [ ] **Economy & Shop Screens** (Salary check, normal shop, soul shop, 3 minigames)
- [ ] **HUD & Atmosphere** (Posters, CRT scanlines, static sheet, battery/door gauges)
- [ ] **Core Flow Screens** (Menu, 12 AM Intro, Game Over, 6 AM Victory)

# Pocket Drift: Overdrive — Master 3D Asset Checklist & AI Generation Workflows

This guide lists **every 3D model required to build Pocket Drift: Overdrive**, complete with exact file formats, polygon budgets for PC + Pixel 10 Pro Fold, pivot-point rules, and specific prompts/workflows for creating them in **ChatGPT/Codex** (via Blender Python `bpy` scripts), **Google Antigravity using Gemini** (via Blender MCP or procedural Three.js/GLB exporters), and **Dedicated AI 3D Mesh Generators**.

---

## 1. File Formats, Polycounts & Unity Import Standards

To ensure high frame rates (120 FPS on PC and 60–120 FPS on the Pixel 10 Pro Fold's inner screen), every 3D asset must follow these technical specifications:

| Asset Category | Preferred File Format | Secondary Format | Polycount Budget (Triangles) | Texture Resolution (PBR) | Pivot Point & Orientation Rule |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **RC Vehicle Chassis** | **`.FBX`** (Binary, Embed Media ON) | `.GLB` | `4,500 – 7,500 tris` | `2048x2048` (Albedo, Normal, Metallic/Smoothness, Emission) | Pivot at bottom-center between axles (`0,0,0`); Forward = **`+Z`**, Up = **`+Y`**. **Wheels must be separate child meshes!** |
| **RC Vehicle Wheels** | **`.FBX`** | `.GLB` | `400 – 800 tris` per wheel | Shared with Car Chassis `2048x2048` atlas | Pivot at the **exact geometric center of the wheel hub** so spinning on the X-axis doesn't wobble. |
| **Modular Weapons & Pickups** | **`.FBX`** | `.GLB` | `600 – 1,500 tris` | `1024x1024` PBR | Pivot at the base attachment clamp (`0,0,0`) so it snaps cleanly to `RoofWeaponMount` or `RearWeaponMount`. |
| **Hero Track Hazards (Moving)** | **`.FBX`** (Supports rigging/animation) | `.GLB` | `1,500 – 4,000 tris` | `2048x2048` PBR | Pivot at center of mass (for physics spheres like Bowling Balls) or hinge base (for Pin Sweepers). |
| **Static Track Props & Ramps** | **`.FBX`** or **`.GLB`** | `.OBJ` | `500 – 3,000 tris` | `2048x2048` or Megascans Tiling Material | Pivot at bottom-surface contact plane (`Y = 0`) for effortless snapping onto the workbench/rooftop. |

---

## 2. How to Generate These 3D Models Using Your AI Toolset

You have three distinct ways to create these models using your workflow:

### Workflow A: Using ChatGPT / Codex (Blender Python `bpy` Script Generation)
* **Best For:** Hard-surface mechanical props, ramps, modular weapons, track barriers, and low/mid-poly kitbashed RC frames with mathematically perfect pivot points and hierarchy separation.
* **How It Works:**
  1. Open **Blender** (free) and switch to the **Scripting** workspace tab.
  2. Copy the **ChatGPT / Codex Blender Python Prompt** for any asset below and paste it into ChatGPT/Codex.
  3. Paste the generated Python script into Blender, click **Run Script**, and it will procedurally construct the 3D mesh, assign materials, set the exact pivot points, and **automatically export a Unity-ready `.FBX` file** (`Apply Scalings: FBX All`, Forward: `-Z`, Up: `Y`).

### Workflow B: Using Google Antigravity with Gemini (Agentic 3D Pipeline)
* **Best For:** Automated batch generation of `.GLB` / `.FBX` assets directly into your Unity `Assets/Models/` folder, or running an interactive **Three.js / Procedural Mesh Generator** or **Blender MCP (Model Context Protocol)** server inside Antigravity.
* **How It Works in Antigravity:**
  1. Open your Unity project directory inside **Antigravity** with the **Gemini Agent** active.
  2. **Option 1 (Headless Blender CLI Execution):** Ask Gemini in Antigravity to write the Blender `.py` generator script *and* execute it directly in the terminal via `blender --background --python generate_asset.py` so the `.FBX` appears straight inside `Assets/Models/` without you even opening Blender!
  3. **Option 2 (Browser-Based `.GLB` Studio):** Ask Gemini in Antigravity to spin up a local Three.js `GLTFExporter` tool that procedurally builds and previews the 3D model in Antigravity's preview pane with a one-click **"Download Unity .GLB"** button.

### Workflow C: Text-to-3D AI Mesh Generators (Meshy.ai / Tripo3D / Rodin) + Gemini/Codex Cleanup
* **Best For:** High-detail organic or sculpted shells (e.g., crushed soda cans, spilled nacho cheese, ornate RC car bodies).
* **How It Works:** Run the descriptive visual prompt to get a `.FBX` or `.GLB`, then use a Codex/Gemini Blender script to auto-decimate the mesh down to mobile polycounts and bake clean PBR textures.

---

## 3. Master Universal Prompt Wrapper for ChatGPT/Codex & Antigravity (Gemini)

Whenever you want **ChatGPT/Codex** or **Antigravity (Gemini)** to build a 3D model via Blender Python, paste this **Master System Prompt** first, followed by the specific **Asset Specification Block** from Section 4:

```
SYSTEM ROLE (FOR CHATGPT/CODEX OR ANTIGRAVITY GEMINI AGENT):
You are an expert Technical 3D Artist and Blender 4.x Python (`bpy`) Automation Engineer building Unity URP game assets for "Pocket Drift: Overdrive" (a micro-scale RC arcade combat racer).

TASK:
Write a complete, standalone, error-free Blender Python (`bpy`) script that procedurally models, UV-unwraps, assigns PBR Principled BSDF materials (Base Color, Metallic, Roughness, Emission), sets Unity-compliant pivot points, and exports the requested 3D asset as an `.fbx` file.

CRITICAL TECHNICAL RULES:
1. Start the script by clearing existing mesh objects (`bpy.ops.wm.read_factory_settings(use_empty=True)`).
2. Model using clean bevels, boolean modifiers (applied cleanly), cylinder/curve extrusions, and kitbash sub-components so the object looks like a detailed miniature/macro-scale game prop, NOT a primitive placeholder block.
3. Ensure Forward is along the Y-axis in Blender (which exports to Unity +Z Forward) and Up is Z-axis (Unity +Y Up).
4. Apply all location/rotation/scale transforms (`bpy.ops.object.transform_apply`) while keeping the requested Pivot Point origin intact.
5. End the script with `bpy.ops.export_scene.fbx(filepath=..., apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y')`.
6. If running inside Google Antigravity, also provide the terminal command (`blender -b -P script.py`) to auto-generate the `.fbx` directly into `Assets/Models/`.
```

---

## 4. Complete 3D Asset List & Individual Generation Prompts

### CATEGORY 1: Playable & AI RC Vehicles (4 Models — Export as `.FBX`)

*CRITICAL VEHICLE HIERARCHY RULE:* Every car `.FBX` **must** contain 5 separate mesh objects inside one parent: `Chassis_Body`, `Wheel_FL`, `Wheel_FR`, `Wheel_RL`, and `Wheel_RR`.

#### 1. `RC_Car_Scrapper.fbx` (Balanced Kitbashed Rally Buggy)
* **Target Polycount:** `5,500 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `RC_Car_Scrapper.fbx` with 5 separated mesh objects (`Chassis_Body`, `Wheel_FL`, `Wheel_FR`, `Wheel_RL`, `Wheel_RR`). The chassis is an aggressive micro-scale RC rally buggy with a tubular external roll cage, angled brushed-aluminum hood panels, wide riveted fender flares, an oversized dual-plane carbon-fiber rear drift wing, twin rear copper battery packs with glowing cyan LED strips, and an antenna whip on the roof. The 4 wheels must have deep-dish metallic gold rims and wide grooved drift tires with pivots centered at each wheel hub."

#### 2. `RC_Car_VoltDozer.fbx` (Heavy Armored Muscle Rammer)
* **Target Polycount:** `6,200 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `RC_Car_VoltDozer.fbx` with 5 separated mesh objects (`Chassis_Body` and 4 independent wheels). Design a heavy, wide-stance armored RC muscle car built for knocking rivals off tables. Include a massive front ramming plough kitbashed from a metallic wrench jaw, matte-black armored window louvers, hazard-yellow racing stripes, and three glowing blue cylindrical capacitor cells protruding from the trunk. Wheels must have wide slick rubber tires and heavy industrial steel rims."

#### 3. `RC_Car_NeonViper.fbx` (Cyberpunk Micro-Tuner Speedster)
* **Target Polycount:** `5,800 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `RC_Car_NeonViper.fbx` with 5 separated mesh objects (`Chassis_Body` and 4 independent wheels). Design a low-slung cyberpunk JDM drift RC car with extreme widebody aero splitters, a semi-translucent dark smoked polycarbonate shell material (`Alpha: 0.75`, `Roughness: 0.1`) revealing an internal green PCB circuit board and glowing magenta neon underglow tubes, plus dual mini cooling fans mounted in the rear bumper."

#### 4. `RC_Recovery_ClawDrone.fbx` (Out-of-Bounds Respawn Drone)
* **Target Polycount:** `3,200 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `RC_Recovery_ClawDrone.fbx` with separated child meshes for `Drone_Frame`, `Rotor_FL`, `Rotor_FR`, `Rotor_RL`, `Rotor_RR`, and `Claw_Grappler`. Design a miniature hobbyist carbon-fiber quadcopter with hazard-orange motor guards, a glowing green camera lens on the front, and an articulated 3-prong chrome arcade claw hanging beneath the belly to hoist fallen RC cars back onto the track."

---

### CATEGORY 2: Modular Weapons, Traps & Pickups (7 Models — Export as `.FBX` or `.GLB`)

#### 5. `Pickup_WeaponCapsule.fbx` (Floating Track Item Box)
* **Target Polycount:** `900 tris` | **Format:** `.FBX` / `.GLB`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Pickup_WeaponCapsule.fbx`. Model a miniature toy vending-machine gachapon sphere split into two halves: a clear glossy plastic top dome (`Transmission: 0.9`, `Roughness: 0.05`) and a vibrant neon-orange bottom hemisphere, with a floating metallic gear and lightning bolt emblem inside the center. Pivot at `0,0,0` center so it can rotate and bob in Unity."

#### 6. `Pickup_NitrousCO2Cartridge.fbx` (Speed Boost Canister)
* **Target Polycount:** `750 tris` | **Format:** `.FBX` / `.GLB`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Pickup_NitrousCO2Cartridge.fbx`. Model a miniature metallic 12g CO2 soda charger bulb modified into an RC boost pickup—brushed chrome steel body wrapped with two glowing electric-cyan emissive bands and a brass nozzle cap."

#### 7. `Weapon_EMPMine_9Volt.fbx` (Rear-Dropped Trap)
* **Target Polycount:** `1,100 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Weapon_EMPMine_9Volt.fbx`. Model an improvised miniature EMP mine kitbashed from a rectangular 9-volt battery laying flat, a crimped metal bottle cap bolted to the top, copper magnet wire coiled around the perimeter, and a bright red emissive LED dome in the center. Pivot at bottom base (`Z=0`)."

#### 8. `Weapon_KineticGrappleTurret.fbx` (Roof-Mounted Harpoon + Hook Mesh)
* **Target Polycount:** `1,400 tris` | **Format:** `.FBX` (Include `Turret_Base` and detachable `Grapple_Hook_Projectile`)
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Weapon_KineticGrappleTurret.fbx` containing two objects: `Turret_Base` and `Grapple_Hook_Projectile`. The turret base is a miniature roof-mounted winch crafted from a brass sewing bobbin, aluminum brackets, and a spring launcher. The projectile is a 3-pronged grappling hook fashioned from bent silver paperclips soldered together."

#### 9. `Weapon_RomanCandleSalvo.fbx` (Roof-Mounted Firework Launcher)
* **Target Polycount:** `1,200 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Weapon_RomanCandleSalvo.fbx`. Model a trio of miniature cardboard Roman candle firework tubes (red, yellow, and blue spiral stripe materials) strapped onto an aluminum RC roof bracket with matte-black electrical tape, charred muzzle tips, and a glowing orange fuse."

#### 10. `Weapon_ThumbtackCluster.fbx` (Rear-Dropped Tire Hazard)
* **Target Polycount:** `800 tris` | **Format:** `.FBX` / `.GLB`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Weapon_ThumbtackCluster.fbx`. Model 5 oversized brass drawing thumbtacks and colorful plastic push-pins scattered in a small circular cluster with their sharp steel needle points facing upward (`+Z` in Blender / `+Y` in Unity). Pivot at floor level."

#### 11. `Weapon_AcousticBassBlaster.fbx` (Roof-Mounted Shockwave Subwoofer)
* **Target Polycount:** `1,300 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Weapon_AcousticBassBlaster.fbx`. Model a miniature oversized subwoofer speaker cone housed in a heavy anodized purple and carbon-fiber roll cage that mounts to an RC car roof, complete with a ribbed rubber surround, chrome dust cap, and glowing violet LED ring."

---

### CATEGORY 3: Track 1 Props — "The Messy Garage Workbench" (6 Models — `.FBX` / `.GLB`)

#### 12. `Track1_WorkbenchTable_Base.fbx` (Main Driving Arena & Drop-Off Ledges)
* **Target Polycount:** `2,500 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Track1_WorkbenchTable_Base.fbx`. Model a massive macro-scale wooden garage workbench top (`120m x 80m` in Unity scale so RC cars feel tiny) made of thick beveled oak planks with a raised steel backstop pegboard wall along the north edge, a heavy cast-iron bench vise clamped to the southwest corner, and open drop-off cliff edges along the south and east sides."

#### 13. `Prop_GiantWrench_BridgeAndAnchor.fbx` (Chicane Barrier / Grapple Anchor)
* **Target Polycount:** `1,600 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_GiantWrench_BridgeAndAnchor.fbx`. Model a giant forged chrome-vanadium combination wrench (open jaw on one end, 12-point box ring on the other) scaled up to `18 meters` long with a slight arch so RC cars can either use it as a curved wall-bank or grapple-swing around its heavy head."

#### 14. `Prop_SolderingIron_And_Cord.fbx` (Heated Track Barrier)
* **Target Polycount:** `2,200 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_SolderingIron_And_Cord.fbx`. Model an oversized hobbyist soldering iron resting in a bent sheet-metal stand with a glowing cherry-red heated metal tip (`Emission: Orange-Red`), a yellow cleaning sponge tray, and a thick black rubber power cord snaking across the floor in an S-curve to form a natural drift chicane wall."

#### 15. `Prop_OpenTackleBox_Ramp.fbx` (Multi-Tier Jump Ramp)
* **Target Polycount:** `2,400 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_OpenTackleBox_Ramp.fbx`. Model an overturned green hammered-metal cantilever tool box with an angled lid (`22-degree slope`) that forms a wide drive-up jump ramp for RC cars, complete with scattered hex nuts and washers along the sides."

#### 16. `Prop_CrushedSodaCan_Spill.fbx` (Obstacle + Sticky Puddle Mesh)
* **Target Polycount:** `1,800 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_CrushedSodaCan_Spill.fbx` with two sub-meshes: `Can_Body` and `Soda_Puddle`. Model a dented red-and-silver aluminum soda can lying on its side with the pull-tab open, and a flat organic glossy translucent amber puddle mesh spreading out from the opening (`0.01m` thick) for a sticky slow-zone trigger."

#### 17. `Prop_HardwareScatter_Pack.fbx` (Physics Knockable Screws, Nuts & AA Batteries)
* **Target Polycount:** `1,200 tris total` | **Format:** `.FBX` (4 separate meshes in 1 file: `Hex_Nut`, `Phillips_Screw`, `Washer`, `AA_Battery`)
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_HardwareScatter_Pack.fbx` containing 4 separated physics-ready props spaced 3 meters apart: a threaded steel `Phillips_Screw`, a heavy hexagonal `Hex_Nut`, a flat `Metal_Washer`, and a cylindrical gold-and-black `AA_Battery`. Center each object's pivot at its own center of mass so we can attach Unity `Rigidbody` components and scatter them across corners to fly everywhere when cars drift through them."

---

### CATEGORY 4: Track 2 Props — "Midnight Strike" Neon Bowling Alley (4 Models — `.FBX`)

#### 18. `Prop_NeonBowlingPin.fbx` (Physics Knockdown Pin)
* **Target Polycount:** `1,100 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_NeonBowlingPin.fbx`. Model a regulation-profile bowling pin scaled to `8 meters` tall, featuring a glossy white UV-coated plastic material and two bright fluorescent magenta-orange emissive neck stripes. Place the pivot near the lower belly center of mass so it wobbles and topples realistically when hit by a boosting RC car or Roman Candle."

#### 19. `Prop_CosmicBowlingBall.fbx` (Rolling Giant Hazard Sphere)
* **Target Polycount:** `1,400 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_CosmicBowlingBall.fbx`. Model a giant `6-meter` diameter smooth bowling ball with three beveled finger holes drilled into the top and a hyper-glossy metallic purple-and-cyan galaxy material (`Roughness: 0.05`, `Clearcoat: 1.0`). Pivot at the exact geometric center (`0,0,0`)."

#### 20. `Prop_PinSetterSweeperBar.fbx` (Animated Lane Gate Hazard)
* **Target Polycount:** `1,900 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_PinSetterSweeperBar.fbx`. Model the mechanical horizontal metal sweeper rake from a bowling alley lane—featuring a wide hazard-striped steel beam, hydraulic side arms, and glowing neon cyan LED strips underneath."

#### 21. `Prop_SpilledNachoTray_Ramp.fbx` (Concourse Jump Ramp)
* **Target Polycount:** `1,600 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_SpilledNachoTray_Ramp.fbx`. Model a giant folded paper snack tray tipped upside-down at a `20-degree` ramp angle, surrounded by triangular tortilla chips and a glossy yellow cheese puddle."

---

### CATEGORY 5: Track 3 Props — "Storm Drain Circuit" Rain-Soaked Rooftop (4 Models — `.FBX`)

#### 22. `Prop_HVAC_DuctBridge_Kit.fbx` (Modular Elevated Metal Shortcut & Vent Ramp)
* **Target Polycount:** `2,000 tris` | **Format:** `.FBX` (3 modular pieces: `Duct_Straight`, `Duct_90Turn`, `Duct_SlopedVentRamp`)
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_HVAC_DuctBridge_Kit.fbx` containing 3 modular galvanized sheet-metal air conditioning duct segments (`Duct_Straight`, `Duct_90Turn`, `Duct_SlopedVentRamp`). Include riveted flange seams, wet brushed aluminum PBR materials (`Metallic: 0.9`, `Roughness: 0.25`), and no side guardrails so cars can battle for position on top of the ducts."

#### 23. `Prop_RooftopWindTurbine_Vent.fbx` (Spinning Wind-Push Hazard)
* **Target Polycount:** `2,200 tris` | **Format:** `.FBX` (Separate `Base_Collar` and `Spinning_Louvers_Dome`)
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_RooftopWindTurbine_Vent.fbx` with a static `Base_Collar` and a rotatable child `Spinning_Louvers_Dome`. Model a bulbous stainless-steel industrial rooftop whirlybird ventilator dome with curved aerodynamic fins that can spin continuously on the Y-axis in Unity."

#### 24. `Prop_CrackedTerracottaPot_Chicane.fbx` (Mossy Rooftop Barrier)
* **Target Polycount:** `1,800 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_CrackedTerracottaPot_Chicane.fbx`. Model an overturned giant clay plant pot broken into a main barrel barrier and two jagged terracotta shards, with a mound of dark wet soil and mossy stones spilling out to form a banked corner berm."

#### 25. `Prop_IndustrialGlassSkylight_Jump.fbx` (Lit Rooftop Gap Ramp)
* **Target Polycount:** `1,700 tris` | **Format:** `.FBX`
* **Prompt for ChatGPT/Codex/Antigravity (Gemini):**
  > "Generate `Prop_IndustrialGlassSkylight_Jump.fbx`. Model a pitched rectangular rooftop skylight with a weathered dark iron frame, angled glass panes (`25-degree` launch slope on both sides), and a warm amber emissive interior core so it glows through the rain in Unity URP."
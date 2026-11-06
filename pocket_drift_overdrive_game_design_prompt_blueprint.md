# Pocket Drift: Overdrive — Master Unity & AI Creation Blueprint

## 1. Recommended Unity Add-ons & Plugins (For Elite Drifting & Visuals)
Before feeding the code prompts into Codex, setting up your Unity project (Universal Render Pipeline - URP recommended for PC + Android Fold scaling) with the right tools will make the drifting feel and look studio-grade:

### Physics & Arcade Drifting Add-ons:
1. **Arcade Car Physics (or Edy’s Vehicle Physics / NWH Vehicle Physics 2):** 
   * *Why:* Unity’s built-in `WheelCollider` is notoriously finicky for snappy, *Micro Machines* / *Art of Rally* style drifting. 
   * *Pro Designer Recommendation:* For the absolute snappiest arcade drift feel that is easiest for **Codex** to script and modify, we will instruct Codex to write a **Sphere-Rigidbody Arcade Controller** (the industry secret used in *Mario Kart* and *Art of Rally* where an invisible rolling sphere handles physics and the car mesh rotates/tilts visually on top of it).
2. **More Mountains - Feel (MMFeedbacks):**
   * *Why:* The #1 plugin in Unity for "game juice." It links directly into your drift script to trigger camera shake, chromatic aberration on boost, controller/phone haptics, FOV kick, and squash-and-stretch on vehicle landings.

### Visual Polish Add-ons (Making Drift Look Amazing):
1. **Unity Trail Renderer + Particle System (URP) OR "Skidmarks" Asset:**
   * *Why:* Custom shader-based tire marks that stay on the workbench surface, combined with volumetric-style flipbook smoke particles that change color (White $\rightarrow$ Orange $\rightarrow$ Blue/Purple) as your drift boost charges up.
2. **Cinemachine (Unity Registry):**
   * *Why:* Essential for an isometric/top-down camera that dynamically zooms out at high speeds, looks ahead into the direction of your drift, and automatically adjusts its orthographic/perspective FOV when switching between a 16:9 PC monitor and the **Pixel 10 Pro Fold's** near-square (`1:1` / `6:5`) inner display.
3. **Quixel Bridge / Fab (Megascans to Unity URP):**
   * *Why:* Import 4K/8K surface imperfections (scratched wood, spilled motor oil, rusted bolts) and pair them with a **Tilt-Shift Post-Processing Volume** (Depth of Field) so the cars look truly miniature.

---

## 2. Master Codex / AI Coding Prompt for Unity (C#)
*Copy and paste the prompt below directly into your AI coding assistant (Codex, Cursor, GitHub Copilot, or ChatGPT) to generate the foundational C# architecture for your Unity project.*

```text
ROLE & OBJECTIVE:
You are an expert Senior Unity C# Gameplay & Physics Programmer. We are building "Pocket Drift: Overdrive", a micro-scale isometric arcade combat racer in Unity 6 (URP) targeting PC first, with seamless cross-platform deployment to Android (specifically optimized for the Pixel 10 Pro Fold's near-square inner display and standard outer display).

Write clean, modular, heavily commented, production-ready C# scripts using Unity's New Input System and Rigidbody physics. Avoid Unity's default WheelColliders; instead, implement a snappy "Rolling Sphere + Visual Car Root" arcade physics architecture (similar to Art of Rally and Mario Kart) for responsive drifting and knockback combat.

SYSTEM ARCHITECTURE TO GENERATE:

1. `ArcadeDriftController.cs` (Core Driving & Drift Physics):
   - Architecture: Use a detached Physics Sphere Rigidbody for movement/collisions and sync a `VisualCarRoot` transform to its position.
   - Raycast Ground Detection: Cast a ray downward to align the `VisualCarRoot` normal with ramps, books, and uneven workbench surfaces, applying extra downward gravity when airborne for snappy landings.
   - Snappy Steering & Drift State Machine:
     - Normal Driving: Responsive high-grip turning.
     - Drift State: Triggered when holding the Drift input while steering above a minimum speed threshold. Reduce lateral friction/grip, kick out the rear angle of the `VisualCarRoot` smoothly using `Mathf.Lerp`, and lock in an inward drift bias while allowing counter-steering.
   - Tiered Drift Boost System:
     - Accumulate `driftCharge` based on drift angle and duration.
     - 3 Charge Tiers (Tier 1: Sparks/Yellow, Tier 2: Flame/Orange, Tier 3: Overdrive/Blue).
     - Releasing the Drift button triggers a speed burst (`AddForce` impulse + temporary max speed multiplier) proportional to the achieved tier.
   - Visual Hooks: Expose UnityEvents or delegates for `OnDriftStart`, `OnDriftTierChanged(int tier)`, `OnBoostTriggered`, and `OnCarBumped` to drive TrailRenderers (skidmarks), particle systems (tire smoke/sparks), and car body roll/tilt.

2. `CrossPlatformInputManager.cs` (PC to Pixel 10 Pro Fold Transition):
   - Use Unity's InputSystem to support Keyboard (WASD + Space for Drift + Shift/Click for Weapon) and Gamepad seamlessly.
   - Include a Touch Input mode designed for foldable screens:
     - Left Thumb Zone: Virtual joystick or split left/right steer buttons.
     - Right Thumb Zone: Large tactile "Hold to Drift / Release to Boost" zone and a "Fire Modular Weapon" button above it.
   - Automatically detect input device switching at runtime.

3. `FoldableCameraAdapter.cs` (Dynamic Aspect Ratio & Look-Ahead):
   - Works alongside Cinemachine (or as a standalone smooth isometric camera controller).
   - Tracks the car from a high isometric angle (e.g., 45-55 degree pitch) with a dynamic "Velocity + Drift Direction Look-Ahead" offset so players can see around tight hairpin corners on a workbench.
   - Dynamic Aspect Ratio Scaling: Continuously monitor `Screen.width / (float)Screen.height`. Automatically adjust the Camera's FOV or Orthographic Distance so that transitioning between a 16:9 PC monitor, a 20:9 folded phone outer screen, and the Pixel 10 Pro Fold's near-square (~1:1) unfolded inner display preserves horizontal track visibility and gives unfolded players bonus vertical sightlines.

4. `CombatWeaponSystem.cs` & `KnockbackPhysics.cs` (Modular Pickups & Ring-Outs):
   - Implement an interface/base class `IWeaponPickup` with a modular slot on the car.
   - Code 3 initial weapons:
     a) `EMPMine`: Dropped behind the car; triggers an AoE blast that temporarily disables rival drift/boost systems and spins out their VisualCarRoot.
     b) `OilSlick`: Spawns a hazard zone collider that forces any car entering it into an uncontrolled zero-traction spin for 1.5 seconds.
     c) `KineticGrapple`: Fires a forward raycast/projectile that latches onto a rival car or heavy environment prop (like a giant wrench), pulling the player forward into a slingshot boost while yanking the target backward.
   - Knockback & Ring-Out System: Calculate collision relative velocity between cars. Apply exaggerated arcade knockback forces (`ForceMode.Impulse`) to push rivals off the edge of the table. Include a `TableEdgeFallTrigger` that detects when a car falls off the workbench and respawns them via a drone/crane drop after a brief penalty.

DELIVERABLE INSTRUCTIONS:
Please generate each script completely without placeholder comments like "// TODO". Provide step-by-step instructions on how to assemble the Player Car Prefab hierarchy in the Unity Editor (Sphere Rigidbody vs. Visual Mesh, TrailRenderer placement, and Center of Mass tuning).
```

---

## 3. Complete Graphics & Asset Checklist + AI Generation Prompts
Below is the complete list of visual assets you need to build the first playable vertical slice of **Pocket Drift: Overdrive**, paired with copy-paste prompts for **3D AI Generators** (Meshy, Tripo3D, Rodin) and **2D Image/UI/Texture Generators** (Midjourney v6, Leonardo.ai, Flux).

### A. 3D Vehicle Models (Use in Meshy.ai / Tripo3D / Rodin)
*Tip: Generate cars WITHOUT wheels attached, or separate the wheels in Blender so Unity can spin and turn the front wheels independently during a drift.*

1. **Starter Car: "The Scrapper" (Modified RC Rally Buggy)**
   * **Prompt:** `3D game asset of a miniature RC armored drift rally car, toy scale, kitbashed from brushed aluminum, carbon fiber chassis, exposed copper wiring, roll cage, oversized rear drift spoiler, aggressive wide fender flares, stylized realism, clean topology, isometric game ready, isolated on neutral background --no motion blur`
2. **Heavy Bruiser: "Volt Dozer" (Armored Muscle RC)**
   * **Prompt:** `3D game asset of a miniature RC armored muscle car built for ramming, heavy reinforced steel bumper made from a repurposed wrench head, glowing blue battery cells mounted in the trunk, wide slick drift tires, matte black and hazard yellow paint with micro scratches, stylized realism, game ready asset`
3. **Speedster: "Neon Viper" (Cyberpunk Micro-Tuner)**
   * **Prompt:** `3D game asset of a sleek miniature RC JDM drift car, widebody kit, translucent plastic shell showing internal circuit board and neon pink LEDs, dual mini turbo fans in the rear, low profile stance, glossy finish with decal stickers, stylized arcade racer aesthetic`

---

### B. 3D Modular Weapon Pickups & Hazards (Use in Meshy.ai / Tripo3D)
1. **EMP Mine (9-Volt Battery Trap)**
   * **Prompt:** `3D game prop of an improvised miniature EMP mine crafted from a crushed 9-volt battery, copper coils wrapped around a bottle cap, and a blinking red LED diode on top, toy scale, PBR textures, stylized realism`
2. **Kinetic Grapple Launcher (Roof-Mounted Attachment)**
   * **Prompt:** `3D game asset of a miniature roof-mounted RC car weapon turret that fires a magnetic grappling hook made from bent paperclips and a sewing bobbin wrapped in braided wire, metallic PBR materials, game-ready prop`
3. **Boost Canister Pickup (CO2 / Nitrous Cartridge)**
   * **Prompt:** `3D game asset of a glowing miniature metallic CO2 soda charger cartridge modified into an RC car nitrous boost pickup, neon cyan glowing rings, floating pickup item, stylized realistic PBR`

---

### C. Environment Props: "The Messy Garage Workbench" Track
*While you can pull raw wood and metal textures from Quixel Megascans, these hero props define the track boundaries and ramps.*

1. **Track Barrier: Giant Soldering Iron & Power Cord**
   * **Prompt:** `3D game environment prop of an oversized vintage soldering iron with a thick coiled rubber power cord and a metal stand with a yellow sponge, macro photography scale, worn metal and melted solder droplets, high detail PBR`
2. **Jump Ramp: Open Tackle Box & Stacked Hardcover Manuals**
   * **Prompt:** `3D game prop of an open vintage metal tool tackle box with spilled brass screws and washers forming a sloped jump ramp beside a stack of grease-stained car repair manuals, macro scale, realistic PBR textures`
3. **Stage Hazard: Spilled Soda Can (Sticky Slow-Zone / Splash)**
   * **Prompt:** `3D game prop of a crushed retro red-and-silver aluminum soda can lying on its side with a glossy puddle of fizzy liquid spilling out, giant macro scale relative to a toy car, high-res realistic textures`

---

### D. 2D Textures, Decals & VFX Flipbooks (Use in Midjourney / Flux / Leonardo)
1. **Track Surface Decal: Oil Slick & Grease Stains**
   * **Prompt:** `Top-down orthographic game texture of a dark rainbow iridescent motor oil puddle splattered on wood, black background for alpha transparency, seamless edges, 4k PBR decal texture --tile`
2. **Drift Tire Smoke & Sparks VFX Texture Sheet**
   * **Prompt:** `2D game VFX sprite sheet, 4x4 grid of stylized thick puffy cartoon-realistic tire smoke clouds on a solid black background, soft edges, volumetric lighting look, high contrast for Unity particle system`
3. **Workbench Wood Surface (If generating custom Seamless PBR)**
   * **Prompt:** `Top-down seamless tileable texture of a heavily used garage workbench made of thick oak planks, covered in tiny saw marks, circular coffee mug rings, pencil measurements, and dried paint splatters, 8k macro photography, flat lighting --tile`

---

### E. UI, HUD & Key Art Prompts (Designed for PC + Pixel 10 Pro Fold)
1. **Tactile Drift & Boost Meter HUD (Corner UI Element)**
   * **Prompt:** `2D game UI HUD element for an arcade RC drift racing game, curved tachometer and segmented boost bar styled like a high-tech RC radio transmitter LCD screen with carbon fiber bezel and glowing neon orange and cyan segments, clean vector-sharp edges, isolated on dark background`
2. **Foldable Touch Controls (Thumb Zone Overlays)**
   * **Prompt:** `Minimalist translucent sci-fi arcade mobile game touch button icons: a circular drift tire icon with glowing sparks, a grappling hook target icon, and a directional steering pad, sleek semi-transparent glassmorphism UI style, isolated on black background`
3. **Main Menu Key Art / Steam & Play Store Capsule**
   * **Prompt:** `Dynamic isometric action shot of two miniature armored RC drift cars powersliding around a giant rusted wrench and a spilled soda can on a sunlit dusty garage workbench, glowing blue drift sparks flying from the rear tires, one car firing a miniature grappling hook, tilt-shift macro photography depth of field, vibrant arcade lighting, Art of Rally meets Micro Machines aesthetic, Unreal/Unity 8k render`

---

## 4. Recommended Next Steps in Unity
1. **Create a New Project:** Select **Universal 3D (URP)** in Unity 6 (or Unity 2022.3 LTS).
2. **Set Up the Aspect Ratio Simulator:** In the Unity `Game` view, add two custom resolutions right away:
   * **PC:** `2560 x 1440` (16:9)
   * **Pixel 10 Pro Fold (Unfolded Inner Screen):** `2076 x 2152` (Near-Square)
3. **Run the Codex Prompt:** Paste Section 2 into Codex/Cursor to generate your 4 core scripts, test the Sphere-Rigidbody drifting on a simple scaled-up Cube ("Workbench"), and tune the `Drift Grip` and `Boost Impulse` variables until the handling feels addictive!
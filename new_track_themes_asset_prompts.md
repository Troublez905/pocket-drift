# Pocket Drift: Overdrive — Track Design & Environment Asset Prompts

To complement **Track 1: The Messy Garage Workbench**, this blueprint outlines the layout mechanics, environmental hazards, Unity lighting setups, and AI asset generation prompts for **Track 2: Neon Bowling Alley** and **Track 3: Rain-Soaked Rooftop**.

---

## Track 2: "Midnight Strike" (The Neon Bowling Alley)

### 1. Level Design & Core Mechanics
* **Surface Physics Zones:**
  * **Oiled Hardwood Lanes:** Ultra-low friction zones where drifts can be held across huge distances to charge Tier 3 Overdrive boosts effortlessly.
  * **Glow-in-the-Dark Carpet (Arcade Concourse):** High-grip neon geometric carpet where tight technical hairpins take place around arcade cabinets and spilled nacho trays.
* **Dynamic Track Hazards:**
  * **Runaway 16-Pound Bowling Balls:** Enormous glossy spheres roll down the lanes or return tracks, acting as moving walls that flatten or launch any RC car in their path.
  * **The Pin-Setter Sweeper:** A giant mechanical bar lowers onto the lane every 20 seconds—players must either drift underneath it before it closes or hit the side gutter-ramps to jump over it.
  * **Ring-Out Zones:** Falling into the deep ball-return pit or off the edge of the concourse snack tables triggers a Drone Respawn.

### 2. Unity URP Lighting & Shader Setup
* **Blacklight / UV Aesthetic:** Use a darkened ambient skybox with vibrant Point and Spot Lights (Neon Magenta, Electric Cyan, UV Purple, and Laser Green).
* **Custom URP Emission Map:** Give the carpet textures and bowling pins high-contrast Emission masks so they glow under Unity's `Bloom` post-processing without hurting mobile GPU performance on the Pixel 10 Pro Fold.
* **Planar Reflections (PC) / Reflection Probes (Fold):** Make the waxed hardwood bowling lanes reflect the neon lights and drift sparks like a mirror.

### 3. AI Asset Prompts (3D Props & 2D Textures)

#### 3D Models (Meshy.ai / Tripo3D / Rodin)
1. **Giant Glow Bowling Pin (Destructible Physics Obstacle)**
   * **Prompt:** `3D game prop of a regulation white bowling pin with two neon red neck stripes and a fluorescent UV-reactive coating, glossy plastic finish with minor scuff marks from bowling balls, macro scale, clean topology PBR`
2. **Swirling Cosmic Bowling Ball (Moving Hazard)**
   * **Prompt:** `3D game asset of a heavy bowling ball with a marbled galaxy swirl pattern of neon pink, deep purple, and metallic gold glitter, three finger holes, ultra-glossy reflective surface, game ready sphere prop`
3. **Spilled Nacho Tray & Jalapeños (Ramp / Sticky Hazard)**
   * **Prompt:** `3D game prop of a crinkled red-and-white checkered paper food tray tipped over on its side, spilling glossy yellow melted nacho cheese and tortilla chips that form a small jump ramp for a toy car, macro scale PBR`
4. **Rental Bowling Shoe Barrier (Chicane Obstacle)**
   * **Prompt:** `3D game prop of a giant retro red, blue, and tan leather rental bowling shoe lying on its side with glowing neon laces untied and sprawling across the floor, macro photography scale, high detail PBR`

#### 2D Seamless Textures (Midjourney / Flux)
1. **90s UV Arcade Carpet Texture**
   * **Prompt:** `Top-down seamless tileable texture of a classic 1990s bowling alley blacklight carpet, dark navy fabric covered in glowing neon magenta, cyan, and yellow geometric triangles, squiggles, and planets, 8k PBR texture --tile`
2. **Waxed Maple Bowling Lane Planks**
   * **Prompt:** `Top-down seamless tileable texture of polished maple wood bowling lane strips with dark wood-grain inlay arrows and glossy urethane wax reflection, 8k macro surface texture, flat lighting --tile`

---

## Track 3: "Storm Drain Circuit" (The Rain-Soaked Urban Rooftop)

### 1. Level Design & Core Mechanics
* **Surface Physics Zones:**
  * **Wet Tar & Gravel:** Medium-grip gritty surface that kicks up water spray instead of dust during drifts.
  * **Sheet-Metal HVAC Ducts:** Elevated, narrow metallic shortcut bridges with zero guardrails—high risk of being rammed off by a rival, but offers direct lines and magnetic grapple anchors.
  * **Hydroplane Puddles:** Shallow standing water pools that slightly tug steering toward the puddle side and cool down overheated boost engines.
* **Dynamic Track Hazards:**
  * **Spinning Industrial Exhaust Fans:** Giant rooftop ventilation turbines that generate a continuous `WindZone` force—pushing cars sideways toward the roof ledge unless players drift-boost through the gust.
  * **Lightning Rod Strikes:** Telegraph circles appear on wet metal plates 1.5 seconds before a localized lightning arc strikes, acting as a natural EMP blast.

### 2. Unity URP Lighting & Weather Setup
* **Atmospheric Storm Lighting:** Cool slate-blue directional moonlight paired with warm orange/amber glow coming from rooftop skylights and neon street signs below the edge of the building.
* **GPU Rain & Splash Particles:** A lightweight camera-attached rain particle system that always follows the `FoldableCameraAdapter` view box so you only render raindrops currently visible on screen.

### 3. AI Asset Prompts (3D Props & 2D Textures)

#### 3D Models (Meshy.ai / Tripo3D / Rodin)
1. **Galvanized HVAC Air Duct & Vent Ramp**
   * **Prompt:** `3D game environment prop of a modular galvanized steel rooftop HVAC air conditioning duct with riveted seams, a sloped vent hood that acts as a jump ramp, wet metallic reflections and rust streaks, PBR game asset`
2. **Spinning Rooftop Turbine Ventilator (Wind Hazard)**
   * **Prompt:** `3D game prop of a bulbous stainless steel spinning rooftop wind turbine ventilator dome, wet weathered metal with neon graffiti tags on the base, macro scale relative to an RC car, clean geometry PBR`
3. **Shattered Terracotta Plant Pot & Mossy Soil (Corner Barrier)**
   * **Prompt:** `3D game prop of an overturned cracked terracotta rooftop garden pot spilling dark wet potting soil, small pebbles, and bright green moss across concrete, macro scale, photorealistic stylized PBR`
4. **Illuminated Glass Skylight (Jump-Over Gap / Light Source)**
   * **Prompt:** `3D game environment prop of an angled industrial rooftop glass skylight with a rusted iron frame, glowing warm amber interior light shining through raindrops on the glass panes, PBR textures`

#### 2D Seamless Textures (Midjourney / Flux)
1. **Wet Rooftop Tar & Gravel Surface**
   * **Prompt:** `Top-down seamless tileable texture of wet dark asphalt rooftop roofing felt embedded with tiny wet river pebbles, miniature rainwater puddles, and green moss in the cracks, 8k macro PBR texture --tile`
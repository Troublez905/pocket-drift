# Pocket Drift: Overdrive — Expanded Weapons & Modular Combat Blueprint

This document provides the deep-dive game design, Codex C# implementation prompts, and 3D/2D AI graphic generation prompts for the modular weapon pickup system and ring-out physics.

---

## 1. Weapon Tier & Slot Architecture

Every RC car features two universal attachment points on its `VisualCarRoot`:
* **Roof Mount (`Transform roofWeaponSlot`):** Used for forward-firing or aimed projectiles (Grappling Hooks, Roman Candle Launchers, Shockwave Horns).
* **Rear Bumper Mount (`Transform rearUtilitySlot`):** Used for dropped traps and defensive payloads (EMP Mines, Oil Slicks, Thumbtack Strips).

---

## 2. Codex Prompt: Expanded Weapon & Ring-Out Scripts

*Paste this prompt into Codex/Cursor after generating your base controller to expand your combat arsenal and add interactive hazard physics.*

```
ROLE & OBJECTIVE:
You are a Senior Unity C# Gameplay Programmer working on "Pocket Drift: Overdrive". Building upon our `ArcadeDriftController` (Sphere-Rigidbody architecture) and `IWeaponPickup` interface, generate complete, production-ready C# scripts for an expanded modular weapon arsenal, visual attachment swapping, and our "Ring-Out & Crane Respawn" loop.

SCRIPTS TO GENERATE:

1. `WeaponInventoryManager.cs`:
   - Holds references to `Transform roofMountPoint` and `Transform rearMountPoint` on the `VisualCarRoot`.
   - When the car triggers a `WeaponPickupCrate`, instantiate the corresponding 3D weapon visual prefab onto the appropriate mount point with a quick scale-bounce animation (`AnimationCurve`).
   - Exposes `FireCurrentWeapon()` to `CrossPlatformInputManager`, supporting charge-and-release aiming (for projectiles/grapples) or instant activation (for traps).

2. `KineticGrappleWeapon.cs`:
   - Fires a high-speed hook projectile forward using a `LineRenderer` rendered as a taut braided wire.
   - If it hits an enemy car: Pulls the player toward the enemy (adding a forward `ForceMode.VelocityChange` boost) while pulling the enemy backward and reducing their angular drag for 0.8s.
   - If it hits a static `HeavyEnvironmentProp` (tagged "GrappleAnchor", like a giant wrench or bowling pin): Acts as a swing-tether or high-speed slingshot around tight hairpin turns, calculating a tangential velocity boost when the player releases the button.

3. `RomanCandleSalvo.cs` (Offensive Roof Weapon):
   - Fires 3 miniature firework projectiles in a staggered burst.
   - Projectiles bounce once off walls/props (`PhysicsMaterial` with high bounciness) and explode on car impact or after 1.8 seconds.
   - Explosion applies an upward and outward `AddExplosionForce` to knock rival spheres toward table edges.

4. `ThumbtackStripTrap.cs` (Defensive Rear Drop):
   - Drops a cluster of 5 brass thumbtacks behind the car.
   - Any rival hitting the trigger gets "Punctured Tires" for 2.5 seconds: their max speed drops by 30%, they emit constant orange rim-sparks, and their steering pulls erratically to one side.

5. `AcousticBassBlaster.cs` (Close-Range Ring-Out Pulse):
   - Roof-mounted miniature subwoofer that emits a 360-degree shockwave (`Physics.OverlapSphere`).
   - Pushes all nearby rival cars and loose physics props away with massive knockback scaled by proximity, ideal for knocking cars off the edge during side-by-side drifting.

6. `DroneRespawnManager.cs` (Ring-Out & Recovery Loop):
   - Triggered when a car enters the `TableEdgeFallTrigger` below the track.
   - Immediately switches the Cinemachine camera to a dramatic top-down angle watching the car tumble off the abyss, then spawns a miniature "Claw Drone" above the last valid grounded `Spline/Waypoint` node.
   - Smoothly lowers the car back onto the track over 1.5 seconds, grants 2 seconds of blinking invulnerability (ignoring collisions with rival cars via Layer overrides), and allows the player to press Accelerate on the exact frame of touchdown for a "Drop-Boost" start.

Provide clean, self-contained C# files with serialized tuning fields in the Inspector.
```

---

## 3. 3D & VFX AI Prompts for Expanded Weapons

### 3D Weapon Models (Meshy.ai / Tripo3D / Rodin)

1. **Weapon Crate Pickup (Translucent Toy Capsule)**
   * **Prompt:** `3D game pickup item, a miniature spherical plastic vending machine capsule half clear plastic and half neon orange, glowing holographic question mark and weapon gear inside, arcade racer style, PBR materials, isolated`

2. **Roman Candle Firework Launcher (Roof Mount)**
   * **Prompt:** `3D game asset of a miniature RC car roof-mounted weapon made from three colorful paper firework tubes strapped together with black electrical tape and a sparkler fuse igniter, scorch marks on the nozzle, toy scale, stylized realism PBR`

3. **Thumbtack Strip Trap (Rear Drop Hazard)**
   * **Prompt:** `3D game prop of five shiny brass drawing thumbtacks pushed through a torn strip of cardboard with sharp metal points facing up, macro scale relative to a toy car, realistic metallic PBR textures`

4. **Acoustic Bass Blaster (Subwoofer Turret)**
   * **Prompt:** `3D game asset of a miniature oversized car audio subwoofer speaker cone mounted in a kitbashed aluminum cage with heavy copper magnets and neon purple LED rings, mounted on top of an RC car, stylized realistic PBR`

5. **Recovery Claw Drone (Respawn Prop)**
   * **Prompt:** `3D game asset of a miniature quadcopter drone built from hobbyist carbon fiber parts, glowing yellow rotors, and a three-pronged metallic arcade claw hanging underneath to pick up toy RC cars, stylized realism, game ready`

### 2D VFX & Decal Prompts (Midjourney / Flux)

1. **EMP Electrical Arc Overlay (VFX Flipbook)**
   * **Prompt:** `2D game VFX sprite sheet, 4x4 grid of electric cyan and white lightning arcs and short-circuit sparks on a solid black background, sharp glowing edges, high contrast for Unity particle shader`

2. **Acoustic Shockwave Distortion Ring**
   * **Prompt:** `Top-down circular shockwave ring texture for Unity particle system, glowing neon purple and white concentric soundwave ripples on a pure black background, clean alpha mask`
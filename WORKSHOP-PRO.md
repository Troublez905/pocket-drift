# Workshop Circuit art rebuild

## Direction

Miniature motorsport on a real workshop bench: warm walnut, charcoal racing
surface, ivory markings, red curbs, brushed alloy, and restrained light accents.
Large tools establish scale; road markings and car colors establish readability.
The racing line stays clear of decorative tools. The broad asymmetric loop has
continuous-curvature sweepers with different turn radii for drift timing.
The 6.3-unit road uses a centerline with minimum curvature radius 5.13; this
keeps its 3.58-unit curbs and 3.9-unit barrier offsets from folding inward.

## Deliverables

- `PocketDrift_UnityProject/Assets/Scenes/WorkshopPro.unity`: generated arena.
- `PocketDrift_UnityProject/Assets/PocketDrift/ArtPro`: four FBX models and URP materials.
- Scrapper 07: red Italian mid-engine GT silhouette inspired by Ferrari.
- Volt Dozer 23: ivory German fastback inspired by Porsche 911 race cars.
- Neon Viper 81: teal Japanese long-hood coupe inspired by Toyota Supra.
- Original stylized bodies, not exact manufacturer models or branded replicas.
- Sculpted shoulders, actual wheel-arch openings, multi-spoke wheels, brake
  calipers, LED signatures, diffusers, splitters and rear wings.
- Separate wheel pivots and roof/rear weapon sockets on each car.
- Workbench, cutting mat, toolbox, screwdriver, hardware, start gantry,
  checkered line, safety blocks, painted track and alternating curbs.
- Revised sweeping circuit, pit garages, tiered grandstands, floodlight towers,
  directional markings, corner identifiers and collidable trackside safety rails.

## Rebuild

1. Run Blender in background with `--factory-startup --python Blender/generate_workshop_pro.py`.
2. In Unity use `Tools > Pocket Drift > Build Workshop Pro`.
3. Open `WorkshopPro` and press Play, or launch from the main menu.

The original models and WorkbenchArena scene are retained. The builder creates a
separate scene and routes the main menu to it. Generated art uses distinct asset
paths. Rebuilding replaces the generated WorkshopPro scene, so save manual edits
to a separate scene before rebuilding.

## Verification boundary

The final level-design review checked both 176-segment curb loops: no
self-intersections or inner/outer crossings. Pit structures clear the outside
rails by approximately 0.63 units. The revised starting grid follows the actual
22-waypoint curve, uses 3.4-unit row spacing and fits all four GT footprints
inside the road with at least 1.14 units of sampled corner clearance.

Unity 6000.4.0f1 compiled and built the scene with four racers and 22 checkpoints.
Automated validation entered Play Mode with simulation paused, checked material
support and FBX axis alignment, and captured the scene through URP. This is a
render/scene smoke check, not a driving test. `Blender/workshop-pro-unity.png` is
the in-engine overview; `Blender/workshop-pro-cars.png` is a Blender studio render.

SRP batching is disabled in WorkshopPipeline: enabling it produced incorrect
material colors in this editor/GPU configuration. Re-enable only after testing
on the target hardware; mobile draw-call performance is not yet profiled.

Blender previews show generated geometry, not gameplay. Unity import/build and
render validation are separate from a human driving test. Driving feel, lap
completion, combat, fall recovery, and mobile performance require Play Mode and
device testing before this should be considered a finished level.

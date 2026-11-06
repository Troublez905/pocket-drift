import bpy
import bmesh
import math
import os
from mathutils import Vector, Matrix

def clear_temp_objects(prefix="TEMP_"):
    for obj in list(bpy.data.objects):
        if obj.name.startswith(prefix):
            bpy.data.objects.remove(obj, do_unlink=True)

def get_or_create_material(name, base_color, metallic=0.0, roughness=0.5, emission_color=None, emission_strength=0.0):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = next((n for n in nodes if n.type == "BSDF_PRINCIPLED"), None)
    if not bsdf:
        bsdf = nodes.new(type="ShaderNodeBsdfPrincipled")
    
    bsdf.inputs["Base Color"].default_value = base_color
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = metallic
    if "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = roughness
    if emission_color:
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = emission_color
        elif "Emission" in bsdf.inputs:
            bsdf.inputs["Emission"].default_value = emission_color
        if "Emission Strength" in bsdf.inputs:
            bsdf.inputs["Emission Strength"].default_value = emission_strength
    return mat

def create_cylinder(bm, radius, depth, segments=16, matrix=Matrix(), cap_ends=True, cap_tris=False, **kwargs):
    return bmesh.ops.create_cone(
        bm,
        cap_ends=True,
        cap_tris=False,
        segments=segments,
        radius1=radius,
        radius2=radius,
        depth=depth,
        matrix=matrix
    )

def apply_transforms(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.select_set(False)

def export_object_to_fbx(root_obj, filepath):
    # Deselect all
    bpy.ops.object.select_all(action='DESELECT')
    
    # Select root and all children recursively
    def select_recursive(o):
        o.select_set(True)
        for child in o.children:
            select_recursive(child)
            
    select_recursive(root_obj)
    bpy.context.view_layer.objects.active = root_obj
    
    bpy.ops.export_scene.fbx(
        filepath=filepath,
        use_selection=True,
        apply_scale_options='FBX_SCALE_ALL',
        axis_forward='-Z',
        axis_up='Y',
        bake_space_transform=False,
        object_types={'EMPTY', 'MESH'}
    )
    bpy.ops.object.select_all(action='DESELECT')
    print(f"[EXPORT] Saved {filepath}")

# ==============================================================================
# BUILDER: RC_Car_VoltDozer (Heavy Muscle Rammer)
# ==============================================================================
def build_volt_dozer(export_dir):
    # Materials
    mat_armor = get_or_create_material("Volt_Mat_ArmorBlack", (0.08, 0.08, 0.09, 1.0), metallic=0.7, roughness=0.35)
    mat_hazard = get_or_create_material("Volt_Mat_HazardYellow", (0.95, 0.75, 0.05, 1.0), metallic=0.2, roughness=0.4)
    mat_steel = get_or_create_material("Volt_Mat_ForgedSteel", (0.55, 0.55, 0.58, 1.0), metallic=0.9, roughness=0.2)
    mat_glow = get_or_create_material("Volt_Mat_BlueCapacitor", (0.05, 0.4, 0.9, 1.0), metallic=0.1, roughness=0.1, emission_color=(0.1, 0.6, 1.0, 1.0), emission_strength=4.0)
    mat_rubber = get_or_create_material("Volt_Mat_TireRubber", (0.05, 0.05, 0.05, 1.0), metallic=0.0, roughness=0.8)

    root = bpy.data.objects.new("RC_Car_VoltDozer", None)
    bpy.context.scene.collection.objects.link(root)
    root.empty_display_size = 0.5
    root.empty_display_type = 'PLAIN_AXES'

    # Chassis Mesh
    mesh_chassis = bpy.data.meshes.new("VoltDozer_Chassis")
    obj_chassis = bpy.data.objects.new("Chassis_Body", mesh_chassis)
    bpy.context.scene.collection.objects.link(obj_chassis)
    obj_chassis.parent = root

    bm = bmesh.new()
    # Main muscular wedge body
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((1.35, 2.5, 0.42, 1.0)) @ Matrix.Translation((0, 0, 0.38)))
    
    # Roof cabin
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((1.1, 1.2, 0.35, 1.0)) @ Matrix.Translation((0, -0.15, 0.68)))

    # Heavy Front Ramming Plough (Wrench Jaw Bumper)
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((1.55, 0.35, 0.45, 1.0)) @ Matrix.Translation((0, 1.35, 0.32)))
    # Ram spikes / wedges
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=6, radius1=0.15, radius2=0.0, depth=0.45, matrix=Matrix.Translation((-0.55, 1.6, 0.32)) @ Matrix.Rotation(math.radians(-90), 4, 'X'))
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=6, radius1=0.15, radius2=0.0, depth=0.45, matrix=Matrix.Translation((0.55, 1.6, 0.32)) @ Matrix.Rotation(math.radians(-90), 4, 'X'))

    # 3 Protruding Trunk Capacitor Cells
    for i, x_pos in enumerate([-0.35, 0.0, 0.35]):
        create_cylinder(bm, cap_ends=True, segments=12, radius=0.12, depth=0.55, matrix=Matrix.Translation((x_pos, -1.05, 0.62)) @ Matrix.Rotation(math.radians(25), 4, 'X'))

    bm.to_mesh(mesh_chassis)
    bm.free()

    obj_chassis.data.materials.append(mat_armor)
    obj_chassis.data.materials.append(mat_hazard)
    obj_chassis.data.materials.append(mat_steel)
    obj_chassis.data.materials.append(mat_glow)
    apply_transforms(obj_chassis)

    # 4 Wheels (Separated, centered pivots)
    wheel_coords = [
        ("Wheel_FL", (-0.72,  0.82, 0.32)),
        ("Wheel_FR", ( 0.72,  0.82, 0.32)),
        ("Wheel_RL", (-0.76, -0.82, 0.36)),
        ("Wheel_RR", ( 0.76, -0.82, 0.36)),
    ]
    for w_name, (wx, wy, wz) in wheel_coords:
        w_mesh = bpy.data.meshes.new(f"Volt_{w_name}_Mesh")
        w_obj = bpy.data.objects.new(w_name, w_mesh)
        bpy.context.scene.collection.objects.link(w_obj)
        w_obj.parent = root
        w_obj.location = Vector((wx, wy, wz))

        bm_w = bmesh.new()
        radius = 0.36 if "R" in w_name[6] else 0.32
        width = 0.28
        create_cylinder(bm_w, cap_ends=True, segments=18, radius=radius, depth=width, matrix=Matrix.Rotation(math.radians(90), 4, 'Y'))
        # Steel Hub
        create_cylinder(bm_w, cap_ends=True, segments=12, radius=radius * 0.55, depth=width * 1.05, matrix=Matrix.Rotation(math.radians(90), 4, 'Y'))
        bm_w.to_mesh(w_mesh)
        bm_w.free()

        w_obj.data.materials.append(mat_rubber)
        w_obj.data.materials.append(mat_steel)
        apply_transforms(w_obj)

    # Weapon Sockets
    s_roof = bpy.data.objects.new("Socket_Roof_Weapon", None)
    s_roof.parent = root
    s_roof.location = Vector((0.0, 0.88, -0.1))
    s_roof.empty_display_size = 0.2
    bpy.context.scene.collection.objects.link(s_roof)

    s_rear = bpy.data.objects.new("Socket_Rear_Utility", None)
    s_rear.parent = root
    s_rear.location = Vector((0.0, 0.35, -1.35))
    s_rear.empty_display_size = 0.2
    bpy.context.scene.collection.objects.link(s_rear)

    export_path = os.path.join(export_dir, "RC_Car_VoltDozer.fbx")
    export_object_to_fbx(root, export_path)
    return root

# ==============================================================================
# BUILDER: RC_Car_NeonViper (Cyberpunk Micro-Tuner)
# ==============================================================================
def build_neon_viper(export_dir):
    mat_shell = get_or_create_material("Viper_Mat_SmokedPolycarb", (0.1, 0.12, 0.15, 0.85), metallic=0.8, roughness=0.15)
    mat_pcb = get_or_create_material("Viper_Mat_GreenCircuit", (0.05, 0.5, 0.2, 1.0), metallic=0.3, roughness=0.4, emission_color=(0.1, 0.8, 0.3, 1.0), emission_strength=1.5)
    mat_neon = get_or_create_material("Viper_Mat_MagentaUnderglow", (0.9, 0.05, 0.7, 1.0), metallic=0.0, roughness=0.1, emission_color=(1.0, 0.08, 0.85, 1.0), emission_strength=5.0)
    mat_tires = get_or_create_material("Viper_Mat_DriftSlicks", (0.06, 0.06, 0.06, 1.0), metallic=0.0, roughness=0.6)
    mat_rims = get_or_create_material("Viper_Mat_ChrGoldRims", (0.85, 0.65, 0.15, 1.0), metallic=0.95, roughness=0.1)

    root = bpy.data.objects.new("RC_Car_NeonViper", None)
    bpy.context.scene.collection.objects.link(root)
    root.empty_display_size = 0.5
    root.empty_display_type = 'PLAIN_AXES'

    mesh_chassis = bpy.data.meshes.new("NeonViper_Chassis")
    obj_chassis = bpy.data.objects.new("Chassis_Body", mesh_chassis)
    bpy.context.scene.collection.objects.link(obj_chassis)
    obj_chassis.parent = root

    bm = bmesh.new()
    # Sleek low-slung wedge chassis
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((1.3, 2.4, 0.3, 1.0)) @ Matrix.Translation((0, 0, 0.25)))
    
    # Aggressive widebody aero front splitter
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((1.45, 0.45, 0.06, 1.0)) @ Matrix.Translation((0, 1.25, 0.14)))
    
    # Side skirts
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((0.12, 1.5, 0.06, 1.0)) @ Matrix.Translation((-0.72, 0, 0.14)))
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((0.12, 1.5, 0.06, 1.0)) @ Matrix.Translation(( 0.72, 0, 0.14)))

    # Aerodynamic cockpit
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((0.95, 1.15, 0.28, 1.0)) @ Matrix.Translation((0, -0.15, 0.5)))

    # Dual Rear Mini Cooling Fan pods
    create_cylinder(bm, cap_ends=True, segments=12, radius=0.15, depth=0.18, matrix=Matrix.Translation((-0.35, -1.22, 0.28)) @ Matrix.Rotation(math.radians(90), 4, 'X'))
    create_cylinder(bm, cap_ends=True, segments=12, radius=0.15, depth=0.18, matrix=Matrix.Translation(( 0.35, -1.22, 0.28)) @ Matrix.Rotation(math.radians(90), 4, 'X'))

    # Underglow neon tube strips
    create_cylinder(bm, cap_ends=True, segments=8, radius=0.03, depth=1.6, matrix=Matrix.Translation((-0.55, 0, 0.12)) @ Matrix.Rotation(math.radians(90), 4, 'X'))
    create_cylinder(bm, cap_ends=True, segments=8, radius=0.03, depth=1.6, matrix=Matrix.Translation(( 0.55, 0, 0.12)) @ Matrix.Rotation(math.radians(90), 4, 'X'))

    bm.to_mesh(mesh_chassis)
    bm.free()

    obj_chassis.data.materials.append(mat_shell)
    obj_chassis.data.materials.append(mat_pcb)
    obj_chassis.data.materials.append(mat_neon)
    apply_transforms(obj_chassis)

    # 4 Wheels
    wheel_coords = [
        ("Wheel_FL", (-0.68,  0.78, 0.25)),
        ("Wheel_FR", ( 0.68,  0.78, 0.25)),
        ("Wheel_RL", (-0.72, -0.78, 0.26)),
        ("Wheel_RR", ( 0.72, -0.78, 0.26)),
    ]
    for w_name, (wx, wy, wz) in wheel_coords:
        w_mesh = bpy.data.meshes.new(f"Viper_{w_name}_Mesh")
        w_obj = bpy.data.objects.new(w_name, w_mesh)
        bpy.context.scene.collection.objects.link(w_obj)
        w_obj.parent = root
        w_obj.location = Vector((wx, wy, wz))

        bm_w = bmesh.new()
        create_cylinder(bm_w, cap_ends=True, segments=20, radius=0.28, depth=0.24, matrix=Matrix.Rotation(math.radians(90), 4, 'Y'))
        # Deep dish rim
        create_cylinder(bm_w, cap_ends=True, segments=12, radius=0.18, depth=0.26, matrix=Matrix.Rotation(math.radians(90), 4, 'Y'))
        bm_w.to_mesh(w_mesh)
        bm_w.free()

        w_obj.data.materials.append(mat_tires)
        w_obj.data.materials.append(mat_rims)
        apply_transforms(w_obj)

    # Sockets
    s_roof = bpy.data.objects.new("Socket_Roof_Weapon", None)
    s_roof.parent = root
    s_roof.location = Vector((0.0, 0.65, -0.1))
    s_roof.empty_display_size = 0.2
    bpy.context.scene.collection.objects.link(s_roof)

    s_rear = bpy.data.objects.new("Socket_Rear_Utility", None)
    s_rear.parent = root
    s_rear.location = Vector((0.0, 0.28, -1.25))
    s_rear.empty_display_size = 0.2
    bpy.context.scene.collection.objects.link(s_rear)

    export_path = os.path.join(export_dir, "RC_Car_NeonViper.fbx")
    export_object_to_fbx(root, export_path)
    return root

# ==============================================================================
# BUILDER: RC_Recovery_ClawDrone
# ==============================================================================
def build_claw_drone(export_dir):
    mat_carbon = get_or_create_material("Drone_Mat_CarbonFiber", (0.1, 0.1, 0.1, 1.0), metallic=0.6, roughness=0.35)
    mat_orange = get_or_create_material("Drone_Mat_HazardOrange", (0.95, 0.4, 0.05, 1.0), metallic=0.1, roughness=0.4)
    mat_chrome = get_or_create_material("Drone_Mat_ChromeClaw", (0.85, 0.85, 0.88, 1.0), metallic=0.95, roughness=0.15)
    mat_lens = get_or_create_material("Drone_Mat_GreenSensor", (0.05, 0.9, 0.3, 1.0), metallic=0.1, roughness=0.1, emission_color=(0.1, 1.0, 0.4, 1.0), emission_strength=4.0)

    root = bpy.data.objects.new("RC_Recovery_ClawDrone", None)
    bpy.context.scene.collection.objects.link(root)
    root.empty_display_size = 0.5
    root.empty_display_type = 'PLAIN_AXES'

    # Drone Frame
    mesh_frame = bpy.data.meshes.new("Drone_Frame_Mesh")
    obj_frame = bpy.data.objects.new("Drone_Frame", mesh_frame)
    bpy.context.scene.collection.objects.link(obj_frame)
    obj_frame.parent = root

    bm = bmesh.new()
    # Fuselage center pod
    bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((0.55, 0.75, 0.35, 1.0)) @ Matrix.Translation((0, 0, 0.5)))
    # Front camera lens
    create_cylinder(bm, cap_ends=True, segments=12, radius=0.12, depth=0.15, matrix=Matrix.Translation((0, 0.42, 0.5)) @ Matrix.Rotation(math.radians(90), 4, 'X'))

    # 4 Carbon arm struts
    arm_coords = [(-0.6, 0.6), (0.6, 0.6), (-0.6, -0.6), (0.6, -0.6)]
    for ax, ay in arm_coords:
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((0.1, 0.9, 0.06, 1.0)) @ Matrix.Translation((ax * 0.5, ay * 0.5, 0.5)) @ Matrix.Rotation(math.atan2(ay, ax) - math.pi/2, 4, 'Z'))
        # Motor housing at end of each arm
        create_cylinder(bm, cap_ends=True, segments=12, radius=0.15, depth=0.18, matrix=Matrix.Translation((ax, ay, 0.55)))

    bm.to_mesh(mesh_frame)
    bm.free()
    obj_frame.data.materials.append(mat_carbon)
    obj_frame.data.materials.append(mat_orange)
    obj_frame.data.materials.append(mat_lens)
    apply_transforms(obj_frame)

    # 4 Rotors
    for i, (rx, ry) in enumerate(arm_coords):
        r_name = f"Rotor_{'FL' if i==0 else 'FR' if i==1 else 'RL' if i==2 else 'RR'}"
        r_mesh = bpy.data.meshes.new(f"{r_name}_Mesh")
        r_obj = bpy.data.objects.new(r_name, r_mesh)
        bpy.context.scene.collection.objects.link(r_obj)
        r_obj.parent = root
        r_obj.location = Vector((rx, ry, 0.66))

        bm_r = bmesh.new()
        # Propeller blades
        bmesh.ops.create_cube(bm_r, size=1.0, matrix=Matrix.Diagonal((0.08, 0.7, 0.02, 1.0)))
        bm_r.to_mesh(r_mesh)
        bm_r.free()
        r_obj.data.materials.append(mat_orange)
        apply_transforms(r_obj)

    # Claw Grappler (Hanging underneath)
    claw_mesh = bpy.data.meshes.new("Claw_Grappler_Mesh")
    claw_obj = bpy.data.objects.new("Claw_Grappler", claw_mesh)
    bpy.context.scene.collection.objects.link(claw_obj)
    claw_obj.parent = root
    claw_obj.location = Vector((0, 0, 0.15))

    bm_c = bmesh.new()
    # Central winch collar
    create_cylinder(bm_c, cap_ends=True, segments=12, radius=0.15, depth=0.2)
    # 3 curved claws
    for angle in [0, 120, 240]:
        rad = math.radians(angle)
        cx = math.cos(rad) * 0.22
        cy = math.sin(rad) * 0.22
        bmesh.ops.create_cube(bm_c, size=1.0, matrix=Matrix.Diagonal((0.06, 0.06, 0.35, 1.0)) @ Matrix.Translation((cx, cy, -0.15)) @ Matrix.Rotation(math.radians(25), 4, 'Y') @ Matrix.Rotation(rad, 4, 'Z'))
    bm_c.to_mesh(claw_mesh)
    bm_c.free()
    claw_obj.data.materials.append(mat_chrome)
    apply_transforms(claw_obj)

    export_path = os.path.join(export_dir, "RC_Recovery_ClawDrone.fbx")
    export_object_to_fbx(root, export_path)
    return root

# Main Entry
export_dir = r"C:\Users\ghost\Desktop\Ideas-Brainstorms\0-PocketDrift\PocketDrift_Exports"
os.makedirs(export_dir, exist_ok=True)

# Build VoltDozer spaced at (6, 0, 0)
vd = build_volt_dozer(export_dir)
vd.location = Vector((6, 0, 0))

# Build NeonViper spaced at (12, 0, 0)
nv = build_neon_viper(export_dir)
nv.location = Vector((12, 0, 0))

# Build ClawDrone spaced at (0, 6, 0)
cd = build_claw_drone(export_dir)
cd.location = Vector((0, 6, 0))

print("ALL 3 MODELS (VoltDozer, NeonViper, ClawDrone) BUILT AND EXPORTED TO:", export_dir)

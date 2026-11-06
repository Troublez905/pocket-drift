"""Reproducible Workshop Circuit art. Run with Blender --background --python."""
import bpy
import math
import os
import json
import sys
from mathutils import Vector

BASE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(BASE, 'PocketDrift_UnityProject', 'Assets', 'PocketDrift', 'ArtPro')
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)

PALETTE = {
    'Graphite': ('202B32', .25, .45), 'Rubber': ('10181D', 0, .8),
    'Alloy': ('9AAEB5', .8, .27), 'Orange': ('F47732', .25, .3),
    'Yellow': ('F3C64C', .2, .3), 'Teal': ('36B9AD', .3, .25),
    'Ivory': ('E9E3D1', 0, .5), 'Red': ('D74C43', .1, .4),
    'Glass': ('183D4D', .55, .2), 'Wood': ('846043', 0, .72),
    'WoodLight': ('967050', 0, .72), 'Road': ('35434A', 0, .88),
    'Mat': ('305951', 0, .85), 'Copper': ('B97B4E', .65, .35),
    'Lamp': ('C0F1EF', .1, .22)
}
mats = {}
for name, (hexcode, metal, rough) in PALETTE.items():
    rgb = tuple(int(hexcode[i:i+2], 16) / 255 for i in (0, 2, 4))
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bs = mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = (*rgb, 1)
    bs.inputs['Metallic'].default_value = metal
    bs.inputs['Roughness'].default_value = rough
    mats[name] = mat

def v(p):
    return Vector((p[0], -p[2], p[1]))

def root(name):
    o = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(o)
    return o

def finish(o, name, parent, material):
    o.name = name
    o.parent = parent
    o.data.materials.append(mats[material])
    return o

def box(name, p, size, material, parent, bevel=.04):
    bpy.ops.mesh.primitive_cube_add(size=1, location=v(p))
    o = bpy.context.object
    o.scale = (size[0], size[2], size[1])
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new('Machined edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 3
        bpy.ops.object.modifier_apply(modifier=mod.name)
        mod = o.modifiers.new('Weighted highlights', 'WEIGHTED_NORMAL')
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o, name, parent, material)

def cylinder(name, p, radius, depth, material, parent, axis='Y', vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=v(p))
    o = bpy.context.object
    if axis == 'X':
        o.rotation_euler[1] = math.pi / 2
    elif axis == 'Z':
        o.rotation_euler[0] = math.pi / 2
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bevel = o.modifiers.new('Edge lip', 'BEVEL')
    bevel.width = min(.025, radius * .12)
    bevel.segments = 2
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return finish(o, name, parent, material)

def beam(name, a, b, radius, material, parent):
    a, b = v(a), v(b)
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=radius, depth=(b-a).length, location=(a+b)/2)
    o = bpy.context.object
    o.rotation_euler = (b-a).to_track_quat('Z', 'Y').to_euler()
    return finish(o, name, parent, material)

def shell(name, sections, material, parent):
    # Sections: longitudinal position, half-width, bottom and top height.
    verts = []
    for z, w, lo, hi in sections:
        verts += [v((-w,lo,z)), v((w,lo,z)), v((w,hi,z)), v((-w,hi,z))]
    faces = [(3,2,1,0)]
    for i in range(len(sections)-1):
        for j in range(4):
            faces.append((4*i+j, 4*i+(j+1)%4, 4*(i+1)+(j+1)%4, 4*(i+1)+j))
    faces.append(tuple(range(len(verts)-4,len(verts))))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    o = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(o)
    finish(o,name,parent,material)
    bpy.context.view_layer.objects.active = o
    mod = o.modifiers.new('Body edge highlights','BEVEL')
    mod.width = .045
    mod.segments = 3
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

def label(text, p, size, material, parent, flat=True):
    curve = bpy.data.curves.new('Lettering','FONT')
    curve.body = text
    curve.align_x = 'CENTER'
    curve.size = size
    curve.extrude = .001
    o = bpy.data.objects.new('Mark_'+text, curve)
    bpy.context.collection.objects.link(o)
    o.location = v(p)
    if not flat:
        o.rotation_euler[0] = math.pi/2
    o.parent = parent
    curve.materials.append(mats[material])
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    bpy.ops.object.convert(target='MESH')
    o.select_set(False)
    return o

def export(r):
    if '--render-only' in sys.argv:
        return
    bpy.ops.object.select_all(action='DESELECT')
    for o in [r] + list(r.children_recursive):
        o.select_set(True)
    bpy.context.view_layer.objects.active = r
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,r.name+'.fbx'), use_selection=True,
        object_types={'MESH','EMPTY'}, axis_forward='-Z', axis_up='Y',
        apply_unit_scale=True, bake_space_transform=True, add_leaf_bones=False, bake_anim=False)

def car(name, color, style, number):
    r = root(name)
    width = .67 if style == 'muscle' else .59
    box('Undertray',(0,.22,0),(1.35,.16,2.35),'Graphite',r)
    shell('Sculpted body', [(-1.15,width*.8,.25,.49),(-.85,width,.25,.62),(.55,width,.25,.58),(1.16,width*.8,.29,.42)],color,r)
    if style == 'buggy':
        shell('Cockpit',[(-.5,.4,.6,.89),(-.25,.4,.6,1.03),(.38,.32,.57,.83)],'Glass',r)
        for x in (-.43,.43):
            beam('Roll cage',(x,.6,-.62),(x,.99,-.25),.04,'Alloy',r)
            beam('Roll cage',(x,.99,-.25),(x,.67,.48),.04,'Alloy',r)
        beam('Roof crossbar',(-.43,.99,-.25),(.43,.99,-.25),.04,'Alloy',r)
        for x in (-.3,-.1,.1,.3):
            cylinder('Rally spotlight',(x,.83,.47),.075,.075,'Lamp',r,'Z')
    else:
        high = .94 if style == 'muscle' else .8
        shell('Tinted canopy',[(-.65,.44,.59,.66),(-.3,.4,.59,high),(.25,.38,.57,high),(.65,.4,.54,.59)],'Glass',r)
        box('Roof panel',(0,high+.018,-.03),(.79,.055,.51),color,r)
        for x in (-.2,.2):
            box('Hood racing stripe',(x,.595,.78),(.13,.012,.46),'Graphite',r,.003)
        if style == 'muscle':
            box('Supercharger',(0,.69,.68),(.34,.22,.42),'Alloy',r)
            box('Intake opening',(0,.72,.9),(.26,.1,.025),'Rubber',r)
    for x in (-.72,.72):
        for z in (-.77,.77):
            wheelname = 'Wheel_'+('F' if z>0 else 'R')+('L' if x<0 else 'R')
            wheel = cylinder(wheelname,(x,.29,z),.30,.24,'Rubber',r,'X',32)
            hub = cylinder('Machined hub',(x+math.copysign(.13,x),.29,z),.18,.025,'Alloy',r,'X')
            # Keep wheel pieces under the steering pivot with their world transforms intact.
            world = hub.matrix_world.copy()
            hub.parent = wheel
            hub.matrix_world = world
            cylinder('Axle cap',(x+math.copysign(.15,x),.29,z),.075,.028,color,r,'X',12)
            box('Wide fender',(x*.88,.63,z),(.36,.12,.69),color,r)
    for x in (-.42,.42):
        box('Headlight',(x,.44,1.13),(.23,.10,.05),'Lamp',r,.02)
        box('Tail light',(x,.46,-1.13),(.23,.08,.05),'Red',r,.01)
        box('Wing upright',(x,.72,-.96),(.055,.38,.09),'Graphite',r)
    box('Rear aerofoil',(0,.93,-1.04),(1.55,.08,.34),'Graphite',r)
    box('Front splitter',(0,.25,1.15),(1.4,.07,.21),'Graphite',r)
    box('Front grille',(0,.38,1.17),(.58,.11,.04),'Rubber',r)
    for x in (-.6,.6):
        cylinder('Exhaust',(x,.35,-1.22),.065,.2,'Alloy',r,'Z')
    label(number,(0,.63,.68),.29,'Ivory',r)
    for name,p in [('Socket_Roof_Weapon',(0,1.05,0)),('Socket_Rear_Utility',(0,.4,-1.25))]:
        s = root(name)
        s.parent = r
        s.location = v(p)
    export(r)
    return r

def gt_car(name, color, style, number):
    r = root(name)
    # Curved cross sections give the coachwork real shoulders rather than box faces.
    def coachwork(name, sections, mat):
        verts=[]; faces=[]
        ring=[(-1,0),(-1,.5),(-.92,.85),(-.7,1),(.7,1),(.92,.85),(1,.5),(1,0)]
        for z,w,lo,hi in sections:
            verts.extend(v((x*w,lo+h*(hi-lo),z)) for x,h in ring)
        faces.append(tuple(reversed(range(8))))
        for i in range(len(sections)-1):
            for j in range(8): faces.append((i*8+j,i*8+(j+1)%8,(i+1)*8+(j+1)%8,(i+1)*8+j))
        faces.append(tuple(range(len(verts)-8,len(verts))))
        mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts,[],faces); mesh.update()
        o=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(o)
        finish(o,name,r,mat)
        for p in mesh.polygons: p.use_smooth=True
        return o
    if style=='italian':
        body=[(-1.38,.52,.22,.47),(-1.15,.67,.2,.57),(-.8,.68,.2,.64),(-.3,.60,.21,.59),(.3,.57,.2,.52),(.85,.65,.2,.53),(1.25,.59,.24,.39),(1.42,.45,.27,.33)]
        cabin=[(-.72,.38,.55,.59),(-.4,.40,.55,.89),(.05,.37,.51,.92),(.55,.33,.48,.54)]
    elif style=='german':
        body=[(-1.3,.48,.22,.49),(-1.05,.69,.2,.63),(-.65,.68,.2,.66),(0,.59,.2,.59),(.65,.65,.2,.61),(1.05,.57,.24,.49),(1.3,.40,.27,.37)]
        cabin=[(-.98,.34,.58,.63),(-.55,.41,.57,.95),(-.15,.42,.55,1.02),(.23,.38,.53,.96),(.66,.33,.51,.59)]
    else:
        body=[(-1.35,.50,.21,.5),(-1.03,.68,.2,.65),(-.65,.67,.2,.63),(0,.58,.2,.58),(.65,.64,.2,.55),(1.22,.57,.24,.46),(1.43,.43,.27,.36)]
        cabin=[(-.85,.34,.59,.62),(-.5,.40,.57,.92),(-.1,.39,.55,.96),(.32,.34,.52,.9),(.70,.31,.5,.56)]
    coachwork('Sculpted GT coachwork',body,color)
    coachwork('Panoramic glazing',cabin,'Glass')
    roofheight=1.025 if style=='german' else .965 if style=='japanese' else .925
    coachwork('Painted roof',[(-.42,.31,roofheight-.04,roofheight),(-.15,.33,roofheight-.025,roofheight+.018),(.12,.30,roofheight-.04,roofheight)],color)
    box('Carbon flat floor',(0,.18,0),(1.27,.06,2.7),'Graphite',r,.02)
    # Wheel openings are cut into the actual body, not painted onto fender blocks.
    bodymesh=r.children[0]
    for x in (-.64,.64):
        for z in (-.83,.83):
            bpy.ops.mesh.primitive_cylinder_add(vertices=40,radius=.345,depth=.6,location=v((x,.29,z)),rotation=(0,math.pi/2,0))
            cutter=bpy.context.object
            bpy.context.view_layer.objects.active=bodymesh
            mod=bodymesh.modifiers.new('Wheel arch','BOOLEAN'); mod.operation='DIFFERENCE'; mod.object=cutter
            bpy.ops.object.modifier_apply(modifier=mod.name)
            bpy.data.objects.remove(cutter,do_unlink=True)
            wheel=cylinder('Wheel_'+('F' if z>0 else 'R')+('L' if x<0 else 'R'),(x,.29,z),.29,.22,'Rubber',r,'X',48)
            face=x+math.copysign(.115,x)
            cylinder('Brake rotor',(face,.29,z),.21,.018,'Graphite',r,'X',40)
            cylinder('Center lock',(face+math.copysign(.025,x),.29,z),.045,.045,'Copper',r,'X')
            for j in range(10):
                a=j*math.tau/10
                beam('Forged wheel spoke',(face,.29,z),(face,.29+math.sin(a)*.20,z+math.cos(a)*.20),.016,'Alloy',r)
            box('Brake caliper',(face,.29,z+.15),(.025,.13,.065),'Red',r,.015)
    for side in (-1,1):
        x=side*.5
        if style=='german':
            lamp=cylinder('Round projector housing',(x,.53,1.04),.125,.05,'Graphite',r,'Z',40)
            cylinder('Round LED signature',(x,.53,1.075),.097,.02,'Lamp',r,'Z',40)
        else:
            lamp=box('Swept LED headlight',(x,.48,1.12),(.24,.035,.19),'Lamp',r,.025)
            lamp.rotation_euler[2]=side*.22
        box('Rear light blade',(side*.42,.52,-1.29),(.35,.045,.025),'Red',r,.01)
        box('Front intake',(side*.43,.32,1.27),(.25,.13,.04),'Rubber',r,.03)
        box('Side skirt',(side*.65,.23,0),(.10,.09,1.15),'Graphite',r,.02)
        box('Side cooling duct',(side*.61,.45,-.38),(.025,.18,.30),'Rubber',r,.04)
        beam('Swan neck wing mount',(side*.39,.58,-1.02),(side*.39,.97,-1.11),.025,'Alloy',r)
        box('Wing endplate',(side*.77,.98,-1.16),(.035,.21,.37),color,r,.015)
        cylinder('Titanium exhaust',(side*.26,.29,-1.39),.06,.13,'Alloy',r,'Z')
        for z in (.67,.76,.85): box('Fender louvre',(side*.59,.63,z),(.10,.012,.025),'Graphite',r,.004)
    box('Carbon rear wing',(0,.99,-1.15),(1.58,.055,.32),'Graphite',r,.02)
    box('Front splitter',(0,.22,1.30),(1.33,.045,.28),'Graphite',r,.02)
    box('Diffuser',(0,.23,-1.25),(1.15,.09,.31),'Graphite',r,.015)
    for x in (-.4,-.2,0,.2,.4): box('Diffuser strake',(x,.2,-1.3),(.025,.14,.29),'Graphite',r,.005)
    for x in (-.12,.12):
        stripe=box('Endurance hood stripe',(x,.565,.70),(.085,.008,.55),'Ivory',r,.002)
    label(number,(0,roofheight+.025,-.08),.27,'Ivory',r)
    for socket,p in [('Socket_Roof_Weapon',(0,roofheight+.12,0)),('Socket_Rear_Utility',(0,.4,-1.4))]:
        s=root(socket); s.parent=r; s.location=v(p)
    export(r)
    return r

cars = [gt_car('Pro_Scrapper','Red','italian','07'), gt_car('Pro_VoltDozer','Ivory','german','23'), gt_car('Pro_NeonViper','Teal','japanese','81')]
for r in cars:
    for o in [r]+list(r.children_recursive): o.hide_render = True

# Continuous curvature prevents inner curbs folding over at tight spline knots.
points=[]
for i in range(176):
    t=3.85-i*math.tau/176
    points.append(Vector((20*math.cos(t),11*math.sin(t)+1.5*math.cos(2*t))))
track=root('Pro_WorkshopCircuit')
box('Table slab',(0,-.575,0),(56,1.1,40),'Wood',track,.22)
for i in range(14):
    box('Wood plank',(i*4-26,-.025,0),(3.96,.05,39.8),'WoodLight' if i%3==0 else 'Wood',track,.01)
for x in (-26,26):
    box('Steel table trim',(x,-.3,0),(.18,.55,40),'Graphite',track)
for z in (-20,20):
    box('Steel table trim',(0,-.3,z),(56,.55,.18),'Graphite',track)

def ribbon(name, inner, outer, material, y, indices=None):
    verts=[]; faces=[]
    for i,p in enumerate(points):
        tangent=(points[(i+1)%len(points)]-points[(i-1)%len(points)]).normalized()
        normal=Vector((-tangent.y,tangent.x))
        for offset in (inner,outer):
            q=p+normal*offset
            verts.append(v((q.x,y,q.y)))
    for i in (indices if indices is not None else range(len(points))):
        j=(i+1)%len(points)
        faces.append((2*i,2*j,2*j+1,2*i+1))
    mesh=bpy.data.meshes.new(name); mesh.from_pydata(verts,[],faces); mesh.update()
    o=bpy.data.objects.new(name,mesh); bpy.context.collection.objects.link(o)
    finish(o,name,track,material)

ribbon('Racing surface',-3.15,3.15,'Road',.035)
for side in (-1,1):
    ribbon('Edge paint',side*2.98,side*3.08,'Ivory',.045)
    for parity,mat in [(0,'Ivory'),(1,'Red')]:
        ribbon('Striped curbing',side*3.15,side*3.58,mat,.05,[i for i in range(len(points)) if (i//2)%2==parity])

box('Cutting mat',(0,.045,0),(19,.035,10),'Mat',track,.08)
for x in range(-9,10): box('Mat grid',(x,.067,0),(.018,.004,9.5),'WoodLight',track,0)
for z in range(-4,5): box('Mat grid',(0,.067,z),(18.5,.004,.018),'WoodLight',track,0)
label('POCKET / DRIFT',(0,.08,-1.2),1.15,'Ivory',track)
label('WORKSHOP CIRCUIT  //  01',(0,.08,-2.4),.38,'Ivory',track)

# Giant bench tools supply miniature-scale cues, kept inside the infield.
box('Toolbox',(6,1.15,1.2),(5.2,2.2,2.8),'Red',track,.18)
box('Toolbox lid',(6,2.29,1.2),(5.35,.18,2.95),'Graphite',track,.08)
for y in (.5,1.05,1.6):
    box('Drawer seam',(6,y,-.215),(4.8,.04,.03),'Graphite',track,.005)
    box('Drawer pull',(6,y+.18,-.28),(1.2,.10,.12),'Alloy',track)
beam('Handle',(5,2.65,1.2),(7,2.65,1.2),.12,'Alloy',track)
for x in (5,7): beam('Handle support',(x,2.3,1.2),(x,2.65,1.2),.12,'Alloy',track)
beam('Screwdriver shaft',(-7,.32,-1),(-2,.32,1),.15,'Alloy',track)
beam('Screwdriver handle',(-8,.32,-1.4),(-5.8,.32,-.52),.4,'Orange',track)
for x,z in [(-6,2),(-3,3),(2,3)]:
    cylinder('Oversize bolt',(x,.35,z),.42,.7,'Alloy',track,vertices=6)
    cylinder('Bolt shaft',(x,.8,z),.21,.5,'Alloy',track)
for z in (-16.8,16.8):
    for x in range(-22,23,4):
        box('Safety block',(x,.4,z),(3.6,.8,.65),'Graphite',track,.12)
        box('Safety reflector',(x,.83,z),(1.7,.045,.5),'Yellow',track,.01)

# Start line lies on the left straight, with generous overhead clearance.
p=points[0]; tangent=(points[1]-points[-1]).normalized(); n=Vector((-tangent.y,tangent.x))
for row in range(2):
    for col in range(10):
        q=p+n*((col-4.5)*.6)+tangent*((row-.5)*.5)
        tile=box('Start checker',(q.x,.055,q.y),(.6,.015,.5),'Ivory' if (row+col)%2 else 'Graphite',track,0)
        tile.rotation_euler[2]=math.atan2(-n.y,n.x)
for side in (-1,1):
    q=p+n*side*4.1
    box('Gantry pillar',(q.x,2.1,q.y),(.35,4.2,.35),'Graphite',track)
qa=p+n*4.1; qb=p-n*4.1
beam('Start gantry',(qa.x,4.2,qa.y),(qb.x,4.2,qb.y),.18,'Yellow',track)
for x in range(-12,17,7):
    box('Pit garage',(x,1.0,-18.5),(5.8,2,2.8),'Ivory',track,.12)
    box('Pit roof',(x,2.12,-18.5),(6.15,.22,3.0),'Graphite',track,.08)
    box('Garage opening',(x,.9,-17.05),(4.9,1.6,.05),'Rubber',track,.015)
    box('Team fascia',(x,1.95,-16.98),(5.5,.25,.08),'Red' if x<0 else 'Teal',track,.01)
for x in range(-16,19,3):
    for row in range(3):
        box('Grandstand terrace',(x,.3+row*.38,16.5+row*.75),(2.95,.30,.72),'Graphite',track,.03)
        for seat in range(5):
            box('Grandstand seat',(x-1.15+seat*.55,.52+row*.38,16.5+row*.75),(.40,.13,.45),'Red' if row%2==0 else 'Ivory',track,.04)
for x,z in [(-24,12),(24,12),(-24,-12),(24,-12)]:
    beam('Floodlight mast',(x,0,z),(x,5,z),.065,'Alloy',track)
    box('Floodlight bank',(x,5,z),(1.4,.3,.35),'Graphite',track)
    for dx in (-.45,0,.45): box('Floodlight lens',(x+dx,4.92,z-.2),(.32,.19,.035),'Lamp',track,.02)
for i in range(0,len(points),8):
    p=points[i]; tangent=(points[(i+1)%len(points)]-points[(i-1)%len(points)]).normalized()
    n=Vector((-tangent.y,tangent.x))
    for side in (-1,1):
        q=p+n*(side*3.9)
        marker=box('Circuit safety rail',(q.x,.22,q.y),(.13,.4,1.3),'Ivory',track,.045)
        marker.rotation_euler[2]=math.atan2(-tangent.x,-tangent.y)
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from track_finish import finish_track
finish_track(track, points, box, label)
export(track)
with open(os.path.join(OUT,'circuit.json'),'w') as f:
    json.dump({'points':[{'x':float(p.x),'z':float(p.y)} for p in points[::8]],'palette':{k:{'hex':h,'metal':m,'rough':r} for k,(h,m,r) in PALETTE.items()}},f,indent=2)

# Render the actual generated geometry for review, rather than a concept image.
scene=bpy.context.scene
scene.render.engine='CYCLES'
scene.cycles.samples=32
scene.cycles.use_denoising=True
scene.world=bpy.data.worlds.new('Workshop studio')
scene.world.color=(.24,.24,.24)
def area(name,pos,power,size):
    data=bpy.data.lights.new(name,'AREA'); data.energy=power; data.shape='DISK'; data.size=size
    o=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(o); o.location=v(pos)
    o.rotation_euler=(Vector((0,0,0))-o.location).to_track_quat('-Z','Y').to_euler()
area('Large softbox',(0,35,5),18000,30)
area('Cool fill',(-20,18,-15),7000,20)
data=bpy.data.cameras.new('Overview'); camera=bpy.data.objects.new('Overview',data); bpy.context.collection.objects.link(camera)
camera.location=v((38,48,-48)); camera.rotation_euler=(v((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler()
data.type='ORTHO'; data.ortho_scale=69; scene.camera=camera
scene.render.resolution_x=1600; scene.render.resolution_y=1100; scene.render.resolution_percentage=100
scene.render.filepath=os.path.join(BASE,'Blender','workshop-pro-overview.png')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(BASE,'Blender','WorkshopPro.blend'))
bpy.ops.render.render(write_still=True)
for o in [track]+list(track.children_recursive): o.hide_render=True
for i,r in enumerate(cars):
    for o in [r]+list(r.children_recursive): o.hide_render=False
    r.location=v(((i-1)*3.1,0,0))
box('Studio floor',(0,-.15,0),(200,.2,200),'Graphite',None)
camera.location=v((7,7,10)); camera.rotation_euler=(v((0,.3,0))-camera.location).to_track_quat('-Z','Y').to_euler()
data.ortho_scale=11.5
scene.render.filepath=os.path.join(BASE,'Blender','workshop-pro-cars.png')
bpy.ops.render.render(write_still=True)
print('WORKSHOP_PRO_GENERATED')

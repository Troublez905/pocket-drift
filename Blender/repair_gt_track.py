"""Run Blender --background --factory-startup --threads 4 --python this_file."""
import os
import math
import json
import bpy
from mathutils import Vector

HERE=os.path.dirname(os.path.abspath(__file__))
source=open(os.path.join(HERE,'generate_workshop_pro.py'),encoding='utf-8').read()
namespace={'__file__':os.path.join(HERE,'generate_workshop_pro.py'),'__name__':'track_repair'}
exec(source.split('cars = [gt_car(')[0],namespace)
bpy.ops.wm.open_mainfile(filepath=os.path.join(HERE,'WorkshopPro.blend'))
namespace['mats']={name:bpy.data.materials[name] for name in namespace['PALETTE']}
track=bpy.data.objects['Pro_WorkshopCircuit']
if not track.get('pit_clearance_fixed',False):
    for obj in track.children_recursive:
        if obj.name.startswith(('Pit garage','Pit roof','Garage opening','Team fascia')):
            obj.location.y += .8
            if obj.name.startswith('Pit roof'): obj.scale.y *= 3.0/3.2
    track['pit_clearance_fixed']=True
box=namespace['box']; beam=namespace['beam']; label=namespace['label']; v=namespace['v']
points=[]
for i in range(176):
    t=3.85-i*math.tau/176
    points.append(Vector((20*math.cos(t),11*math.sin(t)+1.5*math.cos(2*t))))
remove=('Racing surface','Edge paint','Striped curbing','Circuit safety rail',
        'Start checker','Gantry pillar','Start gantry','Direction chevron')
for obj in list(track.children_recursive):
    if obj.name.startswith(remove): bpy.data.objects.remove(obj,do_unlink=True)

def ribbon(name,inner,outer,material,height,indices=None):
    verts=[];faces=[]
    for i,p in enumerate(points):
        tangent=(points[(i+1)%176]-points[(i-1)%176]).normalized()
        normal=Vector((-tangent.y,tangent.x))
        for offset in (inner,outer):
            q=p+normal*offset;verts.append(v((q.x,height,q.y)))
    for i in indices if indices is not None else range(176):
        j=(i+1)%176;faces.append((2*i,2*j,2*j+1,2*i+1))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    namespace['finish'](obj,name,track,material)

ribbon('Racing surface',-3.15,3.15,'Road',.035)
for side in (-1,1):
    ribbon('Edge paint',side*2.98,side*3.08,'Ivory',.045)
    for parity,mat in [(0,'Ivory'),(1,'Red')]:
        ribbon('Striped curbing',side*3.15,side*3.58,mat,.05,[i for i in range(176) if i//2%2==parity])
for i in range(0,176,8):
    p=points[i];t=(points[(i+1)%176]-points[(i-1)%176]).normalized();n=Vector((-t.y,t.x))
    for side in (-1,1):
        q=p+n*side*3.9
        rail=box('Circuit safety rail',(q.x,.22,q.y),(.13,.4,1.3),'Ivory',track,.025)
        rail.rotation_euler.z=math.atan2(-t.x,-t.y)
p=points[0];t=(points[1]-points[-1]).normalized();n=Vector((-t.y,t.x))
for row in range(2):
    for col in range(10):
        q=p+n*((col-4.5)*.6)+t*((row-.5)*.5)
        tile=box('Start checker',(q.x,.055,q.y),(.6,.015,.5),'Ivory' if (row+col)%2 else 'Graphite',track,0)
        tile.rotation_euler.z=math.atan2(-n.y,n.x)
for side in (-1,1):
    q=p+n*side*4.1
    box('Gantry pillar',(q.x,2.1,q.y),(.35,4.2,.35),'Graphite',track,.03)
qa=p+n*4.1;qb=p-n*4.1
beam('Start gantry',(qa.x,4.2,qa.y),(qb.x,4.2,qb.y),.18,'Yellow',track)
helper={'__name__':'track_finish'}
exec(open(os.path.join(HERE,'track_finish.py'),encoding='utf-8').read(),helper)
helper['finish_track'](track,points,box,label)
namespace['export'](track)
with open(os.path.join(namespace['OUT'],'circuit.json'),'w') as f:
    json.dump({'points':[{'x':p.x,'z':p.y} for p in points[::8]]},f,indent=2)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(HERE,'WorkshopPro.blend'))
print('GT_TRACK_REPAIR_EXPORTED',flush=True)
scene=bpy.context.scene
scene.cycles.samples=16
scene.render.filepath=os.path.join(HERE,'workshop-pro-overview.png')
bpy.ops.render.render(write_still=True)
print('GT_TRACK_REPAIR_RENDERED',flush=True)

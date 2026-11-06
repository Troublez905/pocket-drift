"""Final circuit readability pass; call finish_track(track, points, box, label).

Coordinates follow generate_workshop_pro: Blender (x,-z,y), Unity (x,y,z).
Call after construction and before export(track). Does not reset the scene.
"""
import bpy
import math
from mathutils import Vector


def finish_track(track, points, box, label):
    # The infield bend approaches z=2.85; the old mat obscures its racing surface.
    for obj in list(track.children_recursive):
        if obj.name.startswith('Cutting mat'):
            obj.dimensions.y = 5.0
        elif obj.name.startswith('Mat grid'):
            if obj.dimensions.y > 4:
                obj.dimensions.y = 4.5
            elif abs(obj.location.y) > 2:
                bpy.data.objects.remove(obj, do_unlink=True)

    # Sparse directional arrows establish travel direction without painting a lane.
    for i in (16, 48, 96, 144):
        p = points[i % len(points)]
        t = (points[(i+1) % len(points)] - points[(i-1) % len(points)]).normalized()
        n = Vector((-t.y, t.x))
        tip = p + t * .65
        for side in (-1, 1):
            tail = p - t * .45 + n * side * .52
            middle = (tip + tail) * .5
            delta = tip - tail
            mark = box('Direction chevron', (middle.x,.058,middle.y),
                       (.10,.008,delta.length), 'Ivory', track, 0)
            mark.rotation_euler.z = math.atan2(-delta.x,-delta.y)

    # Landmark corner identifiers sit outside the curb, never on the racing line.
    for idx, text in ((32,'01'), (64,'02'), (96,'03'), (128,'04')):
        p=points[idx % len(points)]
        t=(points[(idx+1)%len(points)]-points[(idx-1)%len(points)]).normalized()
        n=Vector((-t.y,t.x))
        q=p+n*4.5
        label(text,(q.x,.065,q.y),.55,'Ivory',track)

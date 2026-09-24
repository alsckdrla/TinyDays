"""Read-only Blender audit: additive sitting work must preserve legacy authored data."""
import bpy,hashlib,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
names=['Adult_Idle_Biped','Adult_Walk_Biped','Adult_Idle_Quadruped','Adult_Hop_Quadruped','Adult_Run_Biped']
if '--idle' in sys.argv:names+=['Adult_Sit_Down','Adult_Stand_Up','Adult_Sit_Hold']
def digest(value):return hashlib.sha256(repr(value).encode()).hexdigest()
def read(path):
    bpy.ops.wm.open_mainfile(filepath=str(path));result={}
    for name in names:
        action=bpy.data.actions[name];curves=[]
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for c in bag.fcurves:
                            curves.append((c.data_path,c.array_index,[(tuple(p.co),tuple(p.handle_left),tuple(p.handle_right),p.interpolation) for p in c.keyframe_points]))
        result[name]=digest(sorted(curves))
    result['geometry']=digest([(o.name,[tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons]) for o in sorted(bpy.data.objects,key=lambda o:o.name) if o.type=='MESH'])
    result['bind_bones']=digest([(b.name,tuple(b.head_local),tuple(b.tail_local)) for b in bpy.data.objects['AdultRig'].data.bones])
    return result
before=read(root/('Logs/BeforeV093/AdultRabbitMotion.blend' if '--idle' in sys.argv else 'Logs/BeforeV089/AdultRabbitMotion.blend'));after=read(root/'ArtSource/AdultRabbitMotion.blend')
for name in before:
    print('PRESERVATION',name,'PASS' if before[name]==after[name] else 'FAIL',after[name])
if before!=after:raise RuntimeError('Legacy data changed')

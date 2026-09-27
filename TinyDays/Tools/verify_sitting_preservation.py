"""Read-only Blender audit: additive sitting work must preserve legacy authored data."""
import bpy,hashlib,sys
if '--v0105' in sys.argv:sys.argv.append('--v0104')
if '--v0104' in sys.argv:sys.argv.append('--v0103')
if '--v0103' in sys.argv:sys.argv.append('--v0102')
if '--v0102' in sys.argv:sys.argv.append('--v0100')
if '--v0101' in sys.argv:sys.argv.append('--v0100')
if '--v0100' in sys.argv:sys.argv.append('--v098')
from pathlib import Path
root=Path(__file__).resolve().parents[1]
names=['Adult_Idle_Biped','Adult_Walk_Biped','Adult_Idle_Quadruped','Adult_Hop_Quadruped','Adult_Run_Biped']
if any(x in sys.argv for x in ('--idle','--v094','--v095','--v096','--v097','--v098')):names+=['Adult_Sit_Down','Adult_Stand_Up','Adult_Sit_Hold']
if any(x in sys.argv for x in ('--v094','--v095','--v096','--v097','--v098')):names+=['Adult_Breathe_Stand','Adult_Breathe_Sit']
if any(x in sys.argv for x in ('--v096','--v097','--v098')):names+=['Adult_Sit_Down_Supported','Adult_Stand_Up_Supported']
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
                            if '--v094' in sys.argv and name.startswith('Adult_Breathe_') and c.data_path=='pose.bones["Spine"].location':continue
                            curves.append((c.data_path,c.array_index,[(tuple(p.co),tuple(p.handle_left),tuple(p.handle_right),p.interpolation) for p in c.keyframe_points]))
        result[name]=digest(sorted(curves))
    result['geometry']=digest([(o.name,[tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons]) for o in sorted(bpy.data.objects,key=lambda o:o.name) if o.type=='MESH'])
    result['bind_bones']=digest([(b.name,tuple(b.head_local),tuple(b.tail_local)) for b in bpy.data.objects['AdultRig'].data.bones])
    return result
if '--v0100' in sys.argv:names+=['Adult_Sigh_Stand','Adult_Sigh_Sit']
if '--v0102' in sys.argv:names+=['Adult_Fidget_Ankles']
if '--v0105' in sys.argv:names+=['Adult_Fidget_Weight']
before=read(root/('Logs/BeforeV0105/AdultRabbitMotion.blend' if '--v0105' in sys.argv else 'Logs/BeforeV0104/AdultRabbitMotion.blend' if '--v0104' in sys.argv else 'Logs/BeforeV0103/AdultRabbitMotion.blend' if '--v0103' in sys.argv else 'Logs/BeforeV0102/AdultRabbitMotion.blend' if '--v0102' in sys.argv else 'Logs/BeforeV0101/AdultRabbitMotion.blend' if '--v0101' in sys.argv else 'Logs/BeforeV0100/AdultRabbitMotion.blend' if '--v0100' in sys.argv else 'Logs/BeforeV098/AdultRabbitMotion.blend' if '--v098' in sys.argv else 'Logs/BeforeV097/AdultRabbitMotion.blend' if '--v097' in sys.argv else 'Logs/BeforeV096/AdultRabbitMotion.blend' if '--v096' in sys.argv else 'Logs/BeforeV095/AdultRabbitMotion.blend' if '--v095' in sys.argv else 'Logs/BeforeV094/AdultRabbitMotion.blend' if '--v094' in sys.argv else 'Logs/BeforeV093/AdultRabbitMotion.blend' if '--idle' in sys.argv else 'Logs/BeforeV089/AdultRabbitMotion.blend'));after=read(root/'ArtSource/AdultRabbitMotion.blend')
for name in before:
    print('PRESERVATION',name,'PASS' if before[name]==after[name] else 'FAIL',after[name])
if before!=after:raise RuntimeError('Legacy data changed')

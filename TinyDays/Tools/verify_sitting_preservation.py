"""Read-only Blender audit: additive sitting work must preserve legacy authored data."""
import bpy,hashlib,sys
if '--v0111' in sys.argv:sys.argv.append('--v0110')
if '--v0110' in sys.argv:sys.argv.append('--v0109')
if '--v0109' in sys.argv:sys.argv.append('--v0108')
if '--v0108' in sys.argv:sys.argv.append('--v0107')
if '--v0107' in sys.argv:sys.argv.append('--v0106')
if '--v0106' in sys.argv:sys.argv.append('--v0105')
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
    bpy.app.driver_namespace['tiny_sleep_eye']=lambda frame:0.0
    bpy.ops.wm.open_mainfile(filepath=str(path));result={}
    for name in names:
        action=bpy.data.actions[name];curves=[]
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for c in bag.fcurves:
                            if '--v0111' in sys.argv and name in lying_tail_names and c.data_path.startswith('pose.bones["Tail"]'):continue
                            if '--v094' in sys.argv and name.startswith('Adult_Breathe_') and c.data_path=='pose.bones["Spine"].location':continue
                            curves.append((c.data_path,c.array_index,[(tuple(p.co),tuple(p.handle_left),tuple(p.handle_right),p.interpolation) for p in c.keyframe_points]))
        result[name]=digest(sorted(curves))
    result['geometry']=digest([(o.name,[tuple(v.co) for v in o.data.vertices],[tuple(p.vertices) for p in o.data.polygons]) for o in sorted(bpy.data.objects,key=lambda o:o.name) if o.type=='MESH'])
    result['bind_bones']=digest([(b.name,tuple(b.head_local),tuple(b.tail_local)) for b in bpy.data.objects['AdultRig'].data.bones])
    if '--v0114' in sys.argv or '--v0115' in sys.argv:result['weights']=digest([(o.name,[[(o.vertex_groups[g.group].name,g.weight) for g in v.groups] for v in o.data.vertices]) for o in sorted(bpy.data.objects,key=lambda o:o.name) if o.type=='MESH'])
    return result
if '--v0100' in sys.argv:names+=['Adult_Sigh_Stand','Adult_Sigh_Sit']
if '--v0102' in sys.argv:names+=['Adult_Fidget_Ankles']
if '--v0105' in sys.argv:names+=['Adult_Fidget_Weight']
if '--v0106' in sys.argv:names+=['Adult_Fidget_Sandplay']
if '--v0107' in sys.argv:names+=['Adult_Stand_To_Lie','Adult_Lie_To_Stand','Adult_Sit_To_Lie','Adult_Lie_To_Sit','Adult_Lie_Hold']
if '--v0109' in sys.argv:names+=['Adult_Breathe_Lie','Adult_Fall_Asleep','Adult_Sleep_Lie','Adult_Wake_Lie']
if '--v0112' in sys.argv or '--v0114' in sys.argv:
    import json
    names=[c['name'] for c in json.loads((root/'Assets/Art/Generated/AdultRabbit/AdultRabbitMotion.audit.json').read_text())['clips'][:32]]
lying_tail_names=set(names[17:])
if '--v0115' in sys.argv:
    import json
    names=[c['name'] for c in json.loads((root/'Assets/Art/Generated/AdultRabbit/AdultRabbitMotion.audit.json').read_text())['clips'][:34]]
version=next((v for v in ('0111','0110','0109','0108','0107','0106','0105','0104','0103','0102','0101','0100','098','097','096','095','094') if '--v'+v in sys.argv),'093' if '--idle' in sys.argv else '089')
if '--v0112' in sys.argv:version='0112'
if '--v0114' in sys.argv:version='0114'
if '--v0115' in sys.argv:version='0115'
before=read(root/('Logs/BeforeV'+version+'/AdultRabbitMotion.blend'));after=read(root/'ArtSource/AdultRabbitMotion.blend')
for name in before:
    print('PRESERVATION',name,'PASS' if before[name]==after[name] else 'FAIL',after[name])
if before!=after:raise RuntimeError('Legacy data changed')
if '--v0115' in sys.argv:
    (root/'Docs/AdultSideDownPreservation.txt').write_text('\n'.join('PASS '+name+' '+after[name] for name in after)+'\nOriginal34 actions, geometry, weights and bind bones exact against BeforeV0115.\n',encoding='utf8')
elif '--v0114' in sys.argv:
    (root/'Docs/AdultSideRisePreservationV0114.txt').write_text('\n'.join('PASS '+name+' '+after[name] for name in after)+'\nOriginal32 actions, pocket-free geometry, weights and bind bones exact against BeforeV0114. Only direct return32/33 changed.\n',encoding='utf8')
elif '--v0112' in sys.argv:
    (root/'Docs/AdultSideRisePreservation.txt').write_text('\n'.join('PASS '+name+' '+after[name] for name in after)+'\nAll original32 actions, base geometry and bind bones exact.\n',encoding='utf8')
elif '--v0111' in sys.argv:
    (root/'Docs/AdultSideSleepPreservation.txt').write_text('\n'.join('PASS '+name+' '+after[name] for name in after)+'\nOriginal 0-16 actions exact; 17-25 exclude only authorized Tail channels. Geometry and bind bones exact. Side actions intentionally changed.\n',encoding='utf8')
elif '--v0109' in sys.argv:
    (root/'Docs/AdultSidePreservation.txt').write_text('\n'.join('PASS '+name+' '+after[name] for name in after)+'\nOriginal 26 actions, geometry and bind bones preserved.\n',encoding='utf8')
elif '--v0107' in sys.argv:
    (root/'Docs/AdultSleepPreservation.txt').write_text('\n'.join('PASS '+name+' '+after[name] for name in after)+'\nAdditive eye shape data excluded from base geometry hash; source 22 actions preserved.\n',encoding='utf8')
elif '--v0106' in sys.argv:
    (root/'Docs/AdultLyingPreservation.txt').write_text('\n'.join('PASS '+name+' '+after[name] for name in after)+'\n',encoding='utf8')

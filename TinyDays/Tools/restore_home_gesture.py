"""Recover an unused door clip present in the old FBX but orphaned in its blend."""
import bpy,sys,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from adult_rabbit_source import ROOT,action_signature,rig_signature
backup=ROOT/'ArtSource/Backups/CanonicalRabbit20261002'
target=ROOT/'ArtSource/AdultRabbitHome.blend'
bpy.ops.wm.open_mainfile(filepath=str(target))
if bpy.data.actions.get('Adult_Door_Interact'):
    report_path=ROOT/'Docs/CanonicalRabbitVerification.json'
    report=json.loads(report_path.read_text(encoding='utf8'))
    home=next(e for e in report['exports'] if e['file']=='AdultRabbitHome')
    assert home['actionHash'] in (action_signature(),action_signature(('Adult_Door_Interact',))), 'Existing curves changed'
    home.update(actionCount=len(bpy.data.actions),actionHash=action_signature(),unusedDoorGesture='Recovered from original FBX; retained with fake user')
    report_path.write_text(json.dumps(report,indent=2),encoding='utf8')
    print('HOME_GESTURE_ALREADY_RETAINED',flush=True)
    sys.exit(0)
before=action_signature();bind=rig_signature()
objects=set(bpy.data.objects);actions=set(bpy.data.actions)
bpy.ops.import_scene.fbx(filepath=str(backup/'AdultRabbitHome.fbx'),use_anim=True)
added=set(bpy.data.actions)-actions
gesture=next(a for a in added if a.name.endswith('Adult_Door_Interact'))
for o in set(bpy.data.objects)-objects:bpy.data.objects.remove(o,do_unlink=True)
for a in added-{gesture}:bpy.data.actions.remove(a)
# Check the pre-existing authored actions before retaining the recovered clip.
gesture.name='Adult_Door_Interact';gesture.use_fake_user=True
assert before==action_signature(('Adult_Door_Interact',)), 'Existing 36 action curves changed'
rig=bpy.data.objects['AdultRig']
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type in {'MESH','ARMATURE'}:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Art/Generated/RabbitHome/AdultRabbitHome.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=.125,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
assert bind==rig_signature()
assert before==action_signature(('Adult_Door_Interact',))
bpy.ops.wm.save_as_mainfile(filepath=str(target))
report_path=ROOT/'Docs/CanonicalRabbitVerification.json'
report=json.loads(report_path.read_text(encoding='utf8'))
home=next(e for e in report['exports'] if e['file']=='AdultRabbitHome')
home['actionCount']=len(bpy.data.actions);home['actionHash']=action_signature();home['unusedDoorGesture']='Recovered from original FBX; saved with fake user to prevent orphan loss'
report_path.write_text(json.dumps(report,indent=2),encoding='utf8')
print('HOME_GESTURE_RECOVERED',len(bpy.data.actions),flush=True)

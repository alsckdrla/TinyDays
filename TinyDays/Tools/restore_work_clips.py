"""Preserve legacy exported work clips that had no blend fake-user retention."""
import bpy,sys,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from adult_rabbit_source import ROOT,action_signature,rig_signature
backup=ROOT/'ArtSource/Backups/CanonicalRabbit20261002'
report_path=ROOT/'Docs/CanonicalRabbitVerification.json'
report=json.loads(report_path.read_text(encoding='utf8'))
def body_action(action):
    if not action:return False
    for slot in action.slots:
        for layer in action.layers:
            for strip in layer.strips:
                bag=strip.channelbag(slot)
                if bag and any(c.data_path.startswith('pose.bones[') for c in bag.fcurves):return True
    return False
for stem,folder,expected in [('AdultRabbitWater','RabbitWater',['Adult_Water_Carry_Idle','Adult_Water_Carry_Walk','Adult_Water_Pour']),('AdultRabbitBench','RabbitBench',['Adult_Bench_Sit','Adult_Bench_Breathe','Adult_Bench_Stand'])]:
    target=ROOT/'ArtSource'/f'{stem}.blend'
    bpy.ops.wm.open_mainfile(filepath=str(target))
    missing=[n for n in expected if not body_action(bpy.data.actions.get(n))]
    before=action_signature(missing);bind=rig_signature()
    for n in missing:
        if bpy.data.actions.get(n):bpy.data.actions.remove(bpy.data.actions[n])
    print('RESTORE_MISSING',stem,missing,flush=True)
    if missing:
        objects=set(bpy.data.objects);actions=set(bpy.data.actions)
        bpy.ops.import_scene.fbx(filepath=str(backup/f'{stem}.fbx'),use_anim=True)
        added=set(bpy.data.actions)-actions
        retained={n:next(a for a in added if a.name.endswith(n) and body_action(a)) for n in missing}
        for o in set(bpy.data.objects)-objects:bpy.data.objects.remove(o,do_unlink=True)
        for a in added-set(retained.values()):bpy.data.actions.remove(a)
        for n,a in retained.items():a.name=n;a.use_fake_user=True
    for n in expected:bpy.data.actions[n].use_fake_user=True
    assert before==action_signature(missing) and bind==rig_signature()
    hidden=[(o,o.hide_get()) for o in bpy.data.objects]
    bpy.ops.object.select_all(action='DESELECT')
    for o in bpy.data.objects:
        if o.type=='ARMATURE' or o.name in ('Head','BodyTorso','BodyArms','BodyLegs','BodyFeet','BodyHands','BodyTail','Top','Bottom','Shoes','Neckwear','Backpack'):o.hide_set(False);o.select_set(True)
    bpy.context.view_layer.objects.active=bpy.data.objects['AdultRig']
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Art/Generated'/folder/f'{stem}.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=.25,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
    for o,h in hidden:o.hide_set(h)
    assert before==action_signature(missing) and bind==rig_signature()
    bpy.ops.wm.save_as_mainfile(filepath=str(target))
    row=next(e for e in report['exports'] if e['file']==stem)
    row.update(actionCount=len(bpy.data.actions),actionHash=action_signature(),recoveredClips=missing)
report_path.write_text(json.dumps(report,indent=2),encoding='utf8')
print('WORK_CLIPS_RESTORED',flush=True)

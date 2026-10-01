"""Append only direct-return actions; all existing32 actions are preserved."""
import bpy,sys,ast,json
from pathlib import Path
from mathutils import Matrix,Vector
import math
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'Tools'))
bpy.app.driver_namespace['tiny_sleep_eye']=lambda frame:0.0
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitMotion.blend'))
rig=bpy.data.objects['AdultRig'];base={b.name:b.matrix_local.copy() for b in rig.data.bones}
shape=bpy.data.objects['Head'].data.shape_keys
if shape and shape.animation_data:shape.animation_data_clear()
tree=ast.parse((ROOT/'Tools/append_sleep_review.py').read_text())
for node in tree.body:
    if isinstance(node,ast.FunctionDef) and node.name in ('assign','limb','key'):exec(compile(ast.Module(body=[node],type_ignores=[]),'<helpers>','exec'))
from adult_rabbit_side_rise import build,CLIPS
result=build(rig,base,assign,limb,key)
rig.animation_data.action=bpy.data.actions[CLIPS[0][0]];bpy.context.scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'Logs/SideRiseDraft.blend'))
if '--draft' not in sys.argv:
    bpy.ops.object.select_all(action='DESELECT')
    for ob in bpy.data.objects:
        if ob.type in {'MESH','ARMATURE'}:ob.hide_set(False);ob.select_set(True)
    bpy.context.view_layer.objects.active=rig;out=ROOT/'Assets/Art/Generated/AdultRabbit'
    bpy.ops.export_scene.fbx(filepath=str(out/'AdultRabbitMotion.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=.125,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
    from adult_rabbit_sleep import install_preview_driver
    install_preview_driver();bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitMotion.blend'))
    report=json.loads((out/'AdultRabbitMotion.audit.json').read_text());report['clips']=[r for r in report['clips'] if r['name'] not in {c[0] for c in CLIPS}]
    for name,duration,loop in CLIPS:report['clips'].append({'name':name,'frames':round(duration*30),'duration':duration,'loop':loop,'family':'AdultStandard_v2'})
    (out/'AdultRabbitMotion.audit.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('DIRECT_SIDE_ART_OK',result,flush=True)

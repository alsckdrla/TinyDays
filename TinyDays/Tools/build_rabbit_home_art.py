"""Export a separate review FBX with one reusable door reach gesture.

The established 36 actions and the motion review FBX are never overwritten.
"""
import ast
import bpy
import json
import math
import sys
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Art/Generated/RabbitHome'
OUT.mkdir(parents=True, exist_ok=True)
bpy.app.driver_namespace['tiny_sleep_eye'] = lambda frame: 0.0
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'ArtSource/AdultRabbitMotion.blend'))
sys.path.insert(0,str(ROOT/'Tools'))
from adult_rabbit_source import apply_source
apply_source()
rig = bpy.data.objects['AdultRig']
original = {action.name for action in bpy.data.actions}
assert len([name for name in original if name.startswith('Adult_')]) == 36, original
base = {bone.name: bone.matrix_local.copy() for bone in rig.data.bones}
tree = ast.parse((ROOT / 'Tools/append_sleep_review.py').read_text(encoding='utf-8'))
for node in tree.body:
    if isinstance(node, ast.FunctionDef) and node.name in ('assign', 'limb', 'key'):
        exec(compile(ast.Module(body=[node], type_ignores=[]), '<pose helpers>', 'exec'))

rig.animation_data.action = bpy.data.actions['Adult_Breathe_Stand']
bpy.data.actions['Adult_Door_Interact'].use_fake_user = True
bpy.context.scene.frame_set(1)
bpy.context.view_layer.update()
standing = {bone.name: bone.matrix.copy() for bone in rig.pose.bones}
action = bpy.data.actions.new('Adult_Door_Interact')
rig.animation_data.action = action

def smooth(x):
    x = max(0.0, min(1.0, x))
    return x*x*(3-2*x)

for frame in range(1, 38):
    t = (frame-1)/30.0
    reach = smooth((t-.07)/.27) * (1-smooth((t-.82)/.27))
    pose = {name: matrix.copy() for name, matrix in standing.items()}
    # The same authored gesture can be mirrored by the review controller's
    # handle contact target. Rest of the body stays on AdultStandard_v2.
    for suffix in ('L',):
        hand = pose['Hand_'+suffix].translation
        target = hand + Vector((0, -.145*reach, .26*reach))
        limb(pose, 'UpperArm_'+suffix, 'Forearm_'+suffix, 'Hand_'+suffix,
             target, (1, -.2, .15))
    assign(pose)
    key(frame)

rig.animation_data.action = bpy.data.actions['Adult_Breathe_Stand']
bpy.context.scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.data.objects:
    if obj.type in {'MESH', 'ARMATURE'}:
        obj.hide_set(False)
        obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'AdultRabbitHome.fbx'),
    use_selection=True, object_types={'MESH', 'ARMATURE'}, apply_unit_scale=True,
    axis_forward='-Z', axis_up='Y', add_leaf_bones=False, bake_anim=True,
    bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_step=.125, bake_anim_simplify_factor=0, mesh_smooth_type='FACE')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitHome.blend'))
assert original.issubset({action.name for action in bpy.data.actions})
(OUT/'AdultRabbitHome.audit.json').write_text(json.dumps({
    'family':'AdultStandard_v2', 'preservedActions':36,
    'newAction':'Adult_Door_Interact', 'duration':1.2,
    'source':'ArtSource/AdultRabbitHome.blend'}, indent=2), encoding='utf-8')
print('RABBIT_HOME_ART_OK', flush=True)

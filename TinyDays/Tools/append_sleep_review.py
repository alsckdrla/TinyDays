"""Incremental additive build, preserving the existing 22 animation curves."""
import bpy, math, json, sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from mathutils import Vector, Matrix
from adult_rabbit_sleep import add_eyes, build, CLIPS, install_preview_driver
ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbit.blend'))
add_eyes()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbit.blend'))
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type in {'MESH','ARMATURE'}:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=bpy.data.objects['AdultRig']
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Art/Generated/AdultRabbit/AdultRabbit.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,mesh_smooth_type='FACE')
bpy.app.driver_namespace['tiny_sleep_eye']=lambda frame:0.0
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitMotion.blend'))
rig=bpy.data.objects['AdultRig'];base={b.name:b.matrix_local.copy() for b in rig.data.bones}
shape=bpy.data.objects['Head'].data.shape_keys
if shape and shape.animation_data:shape.animation_data_clear()
def assign(m):
    for b in rig.pose.bones:b.matrix_basis=Matrix.Identity(4)
    bpy.context.view_layer.update()
    for b in rig.pose.bones:b.matrix=m[b.name];bpy.context.view_layer.update()
def limb(m,a,b,c,target,pole):
    start=m[a].translation;delta=target-start
    l1=rig.data.bones[a].length;l2=rig.data.bones[b].length
    assert delta.length<l1+l2+.0005,('Unreachable',a,delta.length,l1+l2)
    d=max(.0001,min(delta.length,l1+l2-.0001));u=delta.normalized();x=(l1*l1-l2*l2+d*d)/(2*d)
    v=Vector(pole);v=(v-u*v.dot(u)).normalized();middle=start+u*x+v*math.sqrt(max(0,l1*l1-x*x))
    for n,h,t in [(a,start,middle),(b,middle,target)]:
        bone=rig.data.bones[n];m[n]=(bone.tail_local-bone.head_local).rotation_difference(t-h).to_matrix().to_4x4()@bone.matrix_local;m[n].translation=h
    m[c]=base[c].copy();m[c].translation=target
def key(frame):
    for b in rig.pose.bones:
        for channel in ('location','rotation_quaternion','scale'):b.keyframe_insert(channel,frame=frame,group=b.name)
build(rig,base,assign,limb,key)
rig.animation_data.action=bpy.data.actions['Adult_Walk_Biped'];bpy.context.scene.frame_set(1)
bpy.ops.object.select_all(action='DESELECT');rig.hide_set(False);rig.select_set(True)
for o in bpy.data.objects:
    if o.type=='MESH':o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
out=ROOT/'Assets/Art/Generated/AdultRabbit'
bpy.ops.export_scene.fbx(filepath=str(out/'AdultRabbitMotion.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=.25,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
install_preview_driver()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitMotion.blend'))
report=json.loads((out/'AdultRabbitMotion.audit.json').read_text())
report['clips']=[r for r in report['clips'] if r['name'] not in {c[0] for c in CLIPS}]
for name,seconds,loop in CLIPS:report['clips'].append({'name':name,'frames':round(seconds*30),'duration':seconds,'loop':loop,'family':'AdultStandard_v2'})
(out/'AdultRabbitMotion.audit.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('SLEEP_ART_OK',flush=True)

"""Separate bench task source; established character/actions are read-only inputs."""
import bpy, math, ast, json
from pathlib import Path
from mathutils import Matrix, Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Art/Generated/RabbitBench';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitMotion.blend'))
import sys
sys.path.insert(0,str(ROOT/'Tools'))
from adult_rabbit_source import apply_source
apply_source()
for o in bpy.data.objects:
 if o.type=='MESH' and o.data.shape_keys:
  o.data.shape_keys.animation_data_clear()
  for k in o.data.shape_keys.key_blocks:k.value=0
rig=bpy.data.objects['AdultRig'];base={b.name:b.matrix_local.copy() for b in rig.data.bones}
tree=ast.parse((ROOT/'Tools/append_sleep_review.py').read_text(encoding='utf-8'))
for node in tree.body:
 if isinstance(node,ast.FunctionDef) and node.name in ('assign','limb','key'):
  exec(compile(ast.Module(body=[node],type_ignores=[]),'<bench pose helpers>','exec'))
def smooth(x):
 x=max(0,min(1,x));return x*x*x*(x*(x*6-15)+10)
def sample(name,t):
 rig.animation_data.action=bpy.data.actions[name];bpy.context.scene.frame_set(1+int(t*30),subframe=(t*30)%1)
 bpy.context.view_layer.update();return {b.name:b.matrix.copy() for b in rig.pose.bones}
stand=sample('Adult_Idle_Biped',0)
breath0=sample('Adult_Breathe_Stand',0)
def rotate(p,name,angle):
 pivot=p[name].translation;d=Matrix.Translation(pivot)@Matrix.Rotation(math.radians(angle),4,'X')@Matrix.Translation(-pivot)
 for b in [rig.data.bones[name]]+list(rig.data.bones[name].children_recursive):p[b.name]=d@p[b.name]
def pose(amount,lean,breath,lift):
 p={n:m.copy() for n,m in stand.items()}
 # Settle onto the front of the seat first; slide the hips back gently only
 # while lifting the feet. This keeps the grounded thighs ahead of the board.
 offset=Vector((0,.12*amount+.08*lift,(.3936-stand['Pelvis'].translation.z)*amount))
 for b in [rig.data.bones['Pelvis']]+list(rig.data.bones['Pelvis'].children_recursive):p[b.name].translation+=offset
 rotate(p,'Spine',lean);rotate(p,'Head',-.55*lean)
 # Preserve the established chest-only breathing deformation, with fixed hips/feet.
 delta=breath['Spine'].translation-breath0['Spine'].translation
 for b in [rig.data.bones['Spine']]+list(rig.data.bones['Spine'].children_recursive):p[b.name].translation+=delta*amount
 for suffix,side in [('L',1),('R',-1)]:
  target=stand['Foot_'+suffix].translation.copy()
  target.z+=.0536*lift
  limb(p,'Thigh_'+suffix,'Shin_'+suffix,'Foot_'+suffix,target,(0,-1,0))
  p['Foot_'+suffix]=stand['Foot_'+suffix].copy()
  p['Foot_'+suffix].translation=target
  hand=stand['Hand_'+suffix].translation.lerp(Vector((side*.18,.04,.5336)),amount)
  hand.z+=delta.z*amount*.3
  limb(p,'UpperArm_'+suffix,'Forearm_'+suffix,'Hand_'+suffix,hand,(side,-.35,-.2))
 return p
clips=[('Adult_Bench_Sit',1.8,False),('Adult_Bench_Breathe',4,True),('Adult_Bench_Stand',1.8,False)]
for name,seconds,loop in clips:
 poses=[]
 for i in range(round(seconds*30)+1):
  t=i/30
  if 'Breathe' in name:a=1;lift=1;lean=0;breath=sample('Adult_Breathe_Stand',t%4)
  elif 'Sit' in name:a=smooth(t/1.45);lift=smooth((t-1.45)/.35);lean=12*math.sin(math.pi*t/1.8)**2;breath=breath0
  else:
   a=1-smooth(max(0,t-.35)/1.45);lift=1-smooth(t/.35);lean=15*math.sin(math.pi*t/1.8)**2;breath=breath0
  p=pose(a,lean,breath,lift)
  envelope=a if loop else math.sin(math.pi*t/seconds)**2
  for n,amplitude in [('Ear_L',.25),('Ear_R',.22),('NeckSocket',.18),('BackSocket',.14)]:rotate(p,n,amplitude*math.sin(math.tau*t/4-.3)*envelope)
  poses.append(p)
 action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
 for i,p in enumerate(poses):assign(p);key(i+1)
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for curve in bag.fcurves:
     for k in curve.keyframe_points:k.interpolation='LINEAR'
rig.animation_data.action=bpy.data.actions['Adult_Bench_Breathe'];bpy.context.scene.frame_set(1)
# Bench preview: seat beneath hips, no backrest; geometry exported separately.
wood=bpy.data.materials.new('BenchWood');wood.diffuse_color=(.43,.27,.13,1)
parts=[]
def box(name,location,scale):
 bpy.ops.mesh.primitive_cube_add(size=1,location=location);o=bpy.context.object;o.name=name;o.dimensions=scale
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(wood);parts.append(o)
for y in [.10,.22,.34]:box('Seat board',(0,y,.275),(1.5,.12,.05))
for x in [-.58,.58]:
 for y in [.10,.34]:box('Bench leg',(x,y,.125),(.085,.085,.25))
 box('Bench cross rail',(x,.22,.20),(.085,.36,.07))
box('Long rail',(0,.22,.14),(1.24,.06,.065))
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();bench=bpy.context.object;bench.name='WoodBench'
# Export with local origin at floor and bench center in plan.
bpy.context.scene.cursor.location=(0,.22,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');bench.location=(0,0,0)
bpy.ops.export_scene.fbx(filepath=str(OUT/'WoodBench.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
bench.location=(0,.22,0)
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
 if o!=bench and o.type in {'MESH','ARMATURE'}:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'AdultRabbitBench.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=.25,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=121
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitBench.blend'))
(OUT/'AdultRabbitBench.audit.json').write_text(json.dumps({'family':'AdultStandard_v2','originalActions':36,'clips':clips,'seatHeight':.30,'width':1.5,'depth':.36,'pelvisHeight':.3936,'seatBackOffset':.20,'soleLift':.0536,'benchTriangles':sum(len(p.vertices)-2 for p in bench.data.polygons)},indent=2))
print('BENCH_ART_OK',flush=True)

"""Separate water-task source. Never writes the established motion/home sources."""
import bpy, math, ast, json, hashlib
from pathlib import Path
from mathutils import Matrix, Vector
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/Art/Generated/RabbitWater';OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitMotion.blend'))
for o in bpy.data.objects:
 if o.type=='MESH' and o.data.shape_keys:
  o.data.shape_keys.animation_data_clear()
  for k in o.data.shape_keys.key_blocks:k.value=0
rig=bpy.data.objects['AdultRig'];base={b.name:b.matrix_local.copy() for b in rig.data.bones}
tree=ast.parse((ROOT/'Tools/append_sleep_review.py').read_text(encoding='utf-8'))
for node in tree.body:
 if isinstance(node,ast.FunctionDef) and node.name in ('assign','limb','key'):
  exec(compile(ast.Module(body=[node],type_ignores=[]),'<pose helpers>','exec'))
def smooth(u):
 u=max(0,min(1,u));return u*u*u*(u*(u*6-15)+10)
def amount(t):return smooth(t/1.2)*(1-smooth((t-4.4)/1.6))
def sample(name,t):
 rig.animation_data.action=bpy.data.actions[name];bpy.context.scene.frame_set(1+int(t*30),subframe=(t*30)%1)
 bpy.context.view_layer.update();return {b.name:b.matrix.copy() for b in rig.pose.bones}
def rotate(pose,name,degrees):
 p=pose[name].translation;d=Matrix.Translation(p)@Matrix.Rotation(math.radians(degrees),4,'X')@Matrix.Translation(-p)
 for b in [rig.data.bones[name]]+list(rig.data.bones[name].children_recursive):pose[b.name]=d@pose[b.name]
clips=[('Adult_Water_Carry_Idle',4,True),('Adult_Water_Carry_Walk',.8,True),('Adult_Water_Pour',6,False)]
for name,seconds,loop in clips:
 frames=round(seconds*30)
 poses=[sample('Adult_Walk_Biped' if 'Walk' in name else 'Adult_Breathe_Stand',(i/30)%(.8 if 'Walk' in name else 4)) for i in range(frames+1)]
 if 'Pour' in name:poses=[sample('Adult_Breathe_Stand',0) for i in range(frames+1)]
 action=bpy.data.actions.new(name);rig.animation_data.action=action
 for i,p in enumerate(poses):
  t=i/30;a=amount(t) if 'Pour' in name else 0
  rotate(p,'Spine',-5+13*a);rotate(p,'Head',7*a)
  center=p['Spine'].translation+Vector((0,-.34,-.10+.05*a))
  tilt=Matrix.Rotation(math.radians(25*a),4,'X')
  for suffix,side in [('L',1),('R',-1)]:
   target=center+tilt@Vector((side*.12,.08,.305))
   limb(p,'UpperArm_'+suffix,'Forearm_'+suffix,'Hand_'+suffix,target,(side,-.15,-.6))
   p['Hand_'+suffix]=tilt@base['Hand_'+suffix];p['Hand_'+suffix].translation=target
  assign(p);key(i+1)
 # Imported curves use dense keys; linear segments avoid overshoot between samples.
 for layer in action.layers:
  for strip in layer.strips:
   for bag in strip.channelbags:
    for curve in bag.fcurves:
     for point in curve.keyframe_points:point.interpolation='LINEAR'
rig.animation_data.action=bpy.data.actions['Adult_Water_Carry_Idle'];bpy.context.scene.frame_set(1)
# Compact closed low-poly can. Local origin is the body center; front is -Y.
blue=bpy.data.materials.new('WaterBlue');blue.diffuse_color=(.13,.40,.65,1)
dark=bpy.data.materials.new('WaterDark');dark.diffuse_color=(.045,.15,.26,1)
verts=[];faces=[];mats=[]
def face(f,mat=0):faces.append(f);mats.append(mat)
def tube(points,radii,sides=8,mat=0):
 start=len(verts)
 for j,p in enumerate(points):
  p=Vector(p);d=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])
  d.normalize();u=d.cross(Vector((0,0,1)))
  if u.length<.1:u=d.cross(Vector((0,1,0)))
  u.normalize();v=d.cross(u)
  for k in range(sides):verts.append(tuple(p+radii[j]*(u*math.cos(k*math.tau/sides)+v*math.sin(k*math.tau/sides))))
 for j in range(len(points)-1):
  for k in range(sides):face((start+j*sides+k,start+j*sides+(k+1)%sides,start+(j+1)*sides+(k+1)%sides,start+(j+1)*sides+k),mat)
 face(tuple(start+k for k in reversed(range(sides))),mat);face(tuple(start+(len(points)-1)*sides+k for k in range(sides)),mat)
# Body gently rounded at base/top, top recess separate dark disc.
tube([(0,0,z) for z in [-.15,-.13,.11,.145,.15]],[.145,.18,.19,.165,.11],12)
tube([(0,0,.1505),(0,0,.151)],[.102,.102],12,1)
tube([(.17*math.cos(t),.08,.14+.15*math.sin(t)) for t in [j*math.pi/12 for j in range(13)]],[.026]*13,6)
tube([(0,-.13,-.02),(0,-.27,.04),(0,-.40,.14)],[.055,.040,.04],8)
tube([(0,-.40,.14),(0,-.435,.16)],[.085,.085],12)
for j in range(7):
 a=j*math.tau/6;r=.047 if j<6 else 0
 x=r*math.cos(a);z=.16+r*math.sin(a)
 tube([(x,-.453,z),(x,-.455,z)],[.009,.009],5,1)
mesh=bpy.data.meshes.new('WateringCan');mesh.from_pydata(verts,[],faces);mesh.materials.append(blue);mesh.materials.append(dark)
can=bpy.data.objects.new('WateringCan',mesh);bpy.context.collection.objects.link(can)
for polygon,mat in zip(mesh.polygons,mats):polygon.material_index=mat;polygon.use_smooth=mat==0
triangles=sum(len(f)-2 for f in faces);assert triangles<=1000,triangles
bpy.ops.object.select_all(action='DESELECT');can.select_set(True);bpy.context.view_layer.objects.active=can
bpy.ops.export_scene.fbx(filepath=str(OUT/'WateringCan.fbx'),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,mesh_smooth_type='FACE')
can.location=rig.pose.bones['Spine'].matrix.translation+Vector((0,-.34,-.10))
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
 if o!=can and o.type in {'MESH','ARMATURE'}:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'AdultRabbitWater.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=.25,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
bpy.context.scene.frame_start=1;bpy.context.scene.frame_end=121
preview=bpy.data.texts.get('WaterPreview.py') or bpy.data.texts.new('WaterPreview.py')
preview.clear();preview.write('''# Run this text once to enable the optional can preview (no preference changes).
import bpy, math
from mathutils import Matrix, Vector
def water_preview(scene):
 r=bpy.data.objects.get('AdultRig');c=bpy.data.objects.get('WateringCan')
 if not r or not c:return
 t=(scene.frame_current_final-1)/30
 def ease(x):
  x=max(0,min(1,x));return x*x*x*(x*(x*6-15)+10)
 a=ease(t/1.2)*(1-ease((t-4.4)/1.6)) if r.animation_data.action and r.animation_data.action.name=='Adult_Water_Pour' else 0
 c.matrix_world=Matrix.Translation(r.pose.bones['Spine'].matrix.translation+Vector((0,-.34,-.10+.05*a)))@Matrix.Rotation(math.radians(25*a),4,'X')
bpy.app.handlers.frame_change_post[:]=[h for h in bpy.app.handlers.frame_change_post if h.__name__!='water_preview']
bpy.app.handlers.frame_change_post.append(water_preview)
water_preview(bpy.context.scene)
''')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitWater.blend'))
(OUT/'AdultRabbitWater.audit.json').write_text(json.dumps({'family':'AdultStandard_v2','preservedSourceActions':36,'clips':clips,'canTriangles':triangles,'canMaterials':2,'grips':[[.12,.08,.305],[-.12,.08,.305]],'spout':[0,-.455,.16]},indent=2))
print('WATER_ART_OK',triangles,flush=True)

"""Append two work clips, preserving existing saved water actions and geometry."""
import bpy, math, ast, json, sys, shutil, hashlib
from pathlib import Path
from mathutils import Matrix, Vector
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Tools'))
from adult_rabbit_source import action_signature, rig_signature
NAMES=('Adult_Water_Pickup','Adult_Water_Putdown')
def smooth(x):
    x=max(0,min(1,x));return x*x*x*(10+x*(-15+6*x))
def build():
    source=ROOT/'ArtSource/AdultRabbitWater.blend'
    backup=ROOT/'ArtSource/Backups/WaterFlow20261002';backup.mkdir(parents=True,exist_ok=True)
    for path in (source,ROOT/'Assets/Art/Generated/RabbitWater/AdultRabbitWater.fbx'):
        target=backup/path.name
        if not target.exists():shutil.copy2(path,target)
    bpy.ops.wm.open_mainfile(filepath=str(source))
    global rig,base
    rig=bpy.data.objects['AdultRig'];base={b.name:b.matrix_local.copy() for b in rig.data.bones}
    before=action_signature(NAMES);bind=rig_signature()
    tree=ast.parse((ROOT/'Tools/append_sleep_review.py').read_text(encoding='utf-8'))
    for node in tree.body:
        if isinstance(node,ast.FunctionDef) and node.name in ('assign','limb','key'):
            exec(compile(ast.Module(body=[node],type_ignores=[]),'<work helpers>','exec'),globals())
    def sample(name):
        rig.animation_data.action=bpy.data.actions[name];bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
        return {b.name:b.matrix.copy() for b in rig.pose.bones}
    stand=sample('Adult_Breathe_Stand');carry=sample('Adult_Water_Carry_Idle')
    def rotate(p,name,angle):
        center=p[name].translation;d=Matrix.Translation(center)@Matrix.Rotation(math.radians(angle),4,'X')@Matrix.Translation(-center)
        for b in [rig.data.bones[name]]+list(rig.data.bones[name].children_recursive):p[b.name]=d@p[b.name]
    for name in NAMES:
        old=bpy.data.actions.get(name)
        if old:bpy.data.actions.remove(old)
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        for frame in range(55):
            t=frame/30;pick=name==NAMES[0]
            lift=smooth((t-.75)/1.05) if pick else 1-smooth(t/1.05)
            bend=(smooth(t/.65)*(1-smooth((t-.75)/1.05))) if pick else smooth(t/1.05)*(1-smooth((t-1.15)/.65))
            p={}
            for n in stand:
                q=stand[n].to_quaternion().slerp(carry[n].to_quaternion(),lift)
                p[n]=q.to_matrix().to_4x4();p[n].translation=stand[n].translation.lerp(carry[n].translation,lift)
                p[n].translation+=Vector((0,-.045*bend,-.36*bend))
            rotate(p,'Spine',32*bend);rotate(p,'Head',-12*bend)
            # Feet are world support points, not descendants of the lowering hip.
            for side in ('L','R'):
                foot=stand['Foot_'+side].translation
                limb(p,'Thigh_'+side,'Shin_'+side,'Foot_'+side,foot,(0,-1,0))
                p['Foot_'+side]=stand['Foot_'+side].copy()
            can=Vector((0,-.36,.1525)).lerp(p['Spine'].translation+Vector((0,-.34,-.10)),lift)
            contact=smooth((t-.3)/.35) if pick else 1-smooth((t-1.15)/.65)
            for side,sign in (('L',1),('R',-1)):
                target=p['Hand_'+side].translation.lerp(can+Vector((sign*.12,.08,.305)),contact)
                # The free hand follows a reachable arc; full handle contact is never clipped.
                delta=target-p['UpperArm_'+side].translation
                length=rig.data.bones['UpperArm_'+side].length+rig.data.bones['Forearm_'+side].length
                if contact<.999 and delta.length>length-.001:target=p['UpperArm_'+side].translation+delta.normalized()*(length-.001)
                try:limb(p,'UpperArm_'+side,'Forearm_'+side,'Hand_'+side,target,(sign,-.15,-.6))
                except AssertionError:
                    print('PICKUP_REACH',name,frame,'lift',lift,'bend',bend,'shoulder',p['UpperArm_'+side].translation,'target',target,flush=True);raise
            assign(p);key(frame+1)
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for curve in bag.fcurves:
                        for point in curve.keyframe_points:point.interpolation='LINEAR'
    assert before==action_signature(NAMES),'Existing water/body curves changed'
    assert bind==rig_signature(),'Bind changed'
    rig.animation_data.action=bpy.data.actions[NAMES[0]];bpy.context.scene.frame_set(1)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.data.objects:
        if obj.type in ('MESH','ARMATURE') and obj.name!='WateringCan':obj.hide_set(False);obj.select_set(True)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Art/Generated/RabbitWater/AdultRabbitWater.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=.25,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
    bpy.context.scene.frame_end=55
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    (ROOT/'Docs/WaterPickupPreservation.json').write_text(json.dumps({'oldCurvesPreserved':True,'bindPreserved':True,'addedClips':NAMES,'duration':1.8,'frames':54,'canonicalSHA256':hashlib.sha256((ROOT/'ArtSource/AdultRabbit - 01.blend').read_bytes()).hexdigest()},indent=2))
    print('WATER_PICKUP_ART_OK',flush=True)
def install_preview():
    source=ROOT/'ArtSource/AdultRabbitWater.blend'
    bpy.ops.wm.open_mainfile(filepath=str(source))
    text=bpy.data.texts.get('WaterPreview.py') or bpy.data.texts.new('WaterPreview.py')
    text.clear();text.write('''# Run once for work-prop preview; no auto-execution preference changes.
import bpy, math
from mathutils import Matrix, Vector
def water_preview(scene):
    r=bpy.data.objects.get('AdultRig');c=bpy.data.objects.get('WateringCan')
    if not r or not c:return
    name=r.animation_data.action.name if r.animation_data and r.animation_data.action else ''
    t=(scene.frame_current_final-1)/30
    def ease(x):
        x=max(0,min(1,x));return x*x*x*(10+x*(-15+6*x))
    held=r.pose.bones['Spine'].matrix.translation+Vector((0,-.34,-.10))
    if name in ('Adult_Water_Pickup','Adult_Water_Putdown'):
        lift=ease((t-.75)/1.05) if name=='Adult_Water_Pickup' else 1-ease(t/1.05)
        c.matrix_world=Matrix.Translation(Vector((0,-.36,.1525)).lerp(held,lift))
    else:
        a=ease(t/1.2)*(1-ease((t-4.4)/1.6)) if name=='Adult_Water_Pour' else 0
        c.matrix_world=Matrix.Translation(held+Vector((0,0,.05*a)))@Matrix.Rotation(math.radians(25*a),4,'X')
bpy.app.handlers.frame_change_post[:]=[h for h in bpy.app.handlers.frame_change_post if h.__name__!='water_preview']
bpy.app.handlers.frame_change_post.append(water_preview)
water_preview(bpy.context.scene)
''')
    if bpy.data.objects['AdultRig'].animation_data.action.name=='Adult_Water_Pickup':bpy.data.objects['WateringCan'].location=Vector((0,-.36,.1525))
    bpy.ops.wm.save_as_mainfile(filepath=str(source));print('WATER_WORK_PREVIEW_INSTALLED',flush=True)
if __name__=='__main__':
    if '--preview-only' in sys.argv:install_preview()
    else:build();install_preview()

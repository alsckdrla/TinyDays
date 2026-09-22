"""First AdultStandard_v2 locomotion review: four looping clips, no farm replacement."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix, Euler

ROOT=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbit.blend'))
scene=bpy.context.scene;scene.render.fps=30
rig=bpy.data.objects['AdultRig'];rig.hide_set(False);rig.hide_render=False
objects=[o for o in bpy.data.objects if o.type=='MESH']
for o in objects:o.hide_set(False);o.hide_render=False
rest={b.name:(b.location.copy(),b.rotation_quaternion.copy(),b.scale.copy()) for b in rig.pose.bones}
base={b.name:b.matrix.copy() for b in rig.pose.bones}

def joint(a,c,l1,l2,pole):
    delta=c-a;d=max(.0001,min(delta.length,l1+l2-.0001));u=delta.normalized();x=(l1*l1-l2*l2+d*d)/(2*d)
    v=Vector(pole);v=(v-u*v.dot(u)).normalized();return a+u*x+v*math.sqrt(max(0,l1*l1-x*x))
def segment(name,a,b):
    bone=rig.data.bones[name];m=(bone.tail_local-bone.head_local).rotation_difference(b-a).to_matrix().to_4x4()@bone.matrix_local;m.translation=a;return m
def limb(m,upper,lower,end,target,pole):
    a=m[upper].translation;b=joint(a,target,rig.data.bones[upper].length,rig.data.bones[lower].length,pole)
    m[upper]=segment(upper,a,b);m[lower]=segment(lower,b,target);m[end]=base[end].copy();m[end].translation=target
def rotate_group(m,names,pivot,angle):
    transform=Matrix.Translation(pivot)@Matrix.Rotation(angle,4,'X')@Matrix.Translation(-pivot)
    for n in names:m[n]=transform@m[n]
def quad_pose(p,hop):
    m={n:v.copy() for n,v in base.items()};pivot=m['Spine'].translation.copy()
    rotate_group(m,[n for n in m if n!='Root'],pivot,1.18)
    dz=-.31+(.13*max(0,math.sin(2*math.pi*p)) if hop else .006*math.sin(2*math.pi*p))
    for n in m:
        if n!='Root':m[n].translation.z+=dz
    for side in ('L','R'):
        x=m['Thigh_'+side].translation.x
        limb(m,'Thigh_'+side,'Shin_'+side,'Foot_'+side,Vector((x,.10,.11)),(0,-1,0))
        x=m['UpperArm_'+side].translation.x
        limb(m,'UpperArm_'+side,'Forearm_'+side,'Hand_'+side,Vector((x,-.47,.10)),(0,1,0))
    return m
def assign(m):
    for b in rig.pose.bones:b.matrix_basis=Matrix.Identity(4)
    bpy.context.view_layer.update()
    for b in rig.pose.bones:b.matrix=m[b.name];bpy.context.view_layer.update()

def reset():
    for b in rig.pose.bones:
        l,r,s=rest[b.name];b.location=l;b.rotation_mode='QUATERNION';b.rotation_quaternion=r;b.scale=s
def rot(name,x=0,y=0,z=0):
    b=rig.pose.bones[name];b.rotation_mode='QUATERNION';b.rotation_quaternion=Euler((x,y,z),'XYZ').to_quaternion()
def key(frame):
    for b in rig.pose.bones:
        b.keyframe_insert('location',frame=frame,group=b.name)
        b.keyframe_insert('rotation_quaternion',frame=frame,group=b.name)
        b.keyframe_insert('scale',frame=frame,group=b.name)

def track(values,p):
    count=len(values);x=(p%1)*count;i=int(x);t=x-i;t=t*t*(3-2*t)
    return values[i]*(1-t)+values[(i+1)%count]*t

def elbow_angle(side):
    shoulder=rig.pose.bones['UpperArm_'+side].head
    elbow=rig.pose.bones['Forearm_'+side].head
    wrist=rig.pose.bones['Hand_'+side].head
    return math.degrees((shoulder-elbow).angle(wrist-elbow))

def bend_elbow(side,target):
    low,high=-2.5,0
    for _ in range(20):
        mid=(low+high)/2;rot('Forearm_'+side,mid);bpy.context.view_layer.update()
        if elbow_angle(side)<target:low=mid
        else:high=mid
    assert abs(elbow_angle(side)-target)<.1,(side,elbow_angle(side),target)

# Contact, recoil, passing and high point for left then right support.
walk_z=[0,-.030,0,.030,0,-.030,0,.030]
walk_side=[-.014,-.020,-.010,.008,.014,.020,.010,-.008]
walk_twist=[-.055,-.038,0,.040,.055,.038,0,-.040]
walk_foot_y=[-.23,-.12,.02,.15,.23,.12,-.02,-.15]
walk_foot_z=[.125,.125,.125,.125,.125,.165,.235,.185]
walk_phases=['Contact L','Recoil L','Passing L','High Point L','Contact R','Recoil R','Passing R','High Point R']

# Stance is linear in-place travel: adding 0.92m/cycle of forward motion locks the foot.
# Sole offsets come from the actual shoe geometry rather than the ankle height alone.
shoe=bpy.data.objects['Shoes']
ankle_ground={}
for side_name in ('L','R'):
    group=shoe.vertex_groups['Foot_'+side_name].index
    sole=min(v.co.z for v in shoe.data.vertices if any(g.group==group and g.weight>.99 for g in v.groups))
    ankle_ground[side_name]=base['Foot_'+side_name].translation.z-sole+.0025

def walk_target(side_name,phase):
    if phase<=.5:
        y=-.23+.92*phase;lift=0
    else:
        t=(phase-.5)*2
        # Hermite return matches stance velocity at lift-off and landing.
        y=(2*t**3-3*t*t+1)*.23+(t**3-2*t*t+t)*.46+(-2*t**3+3*t*t)*(-.23)+(t**3-t*t)*.46
        lift=.105*math.sin(math.pi*t)**2
    return Vector((base['Foot_'+side_name].translation.x,y,ankle_ground[side_name]+lift))

specs=[('Adult_Idle_Biped',108),('Adult_Walk_Biped',24),('Adult_Idle_Quadruped',90),('Adult_Hop_Quadruped',30)]
report={'bodyFamily':'AdultStandard_v2','reference':'ref_walking_ani_01.jpg; ref_ani.mp4 is an incomplete 104-byte fragment',
        'scope':'Disney-inspired biped idle/walk revision; quadruped clips preserved, farm replacement and transitions excluded.',
        'walkPhases':walk_phases,'walkVirtualDistance':.92,'virtualSpeedMetersPerSecond':1.15,'clips':[]}
for name,frames in specs:
    old=bpy.data.actions.get(name)
    if old:bpy.data.actions.remove(old)
    action=bpy.data.actions.new(name);action.use_fake_user=True
    rig.animation_data_create();rig.animation_data.action=action
    for f in range(frames+1):
        scene.frame_set(f+1);reset();bpy.context.view_layer.update();p=f/frames;a=2*math.pi*p
        pelvis=rig.pose.bones['Pelvis'];spine=rig.pose.bones['Spine'];head=rig.pose.bones['Head']
        if name=='Adult_Idle_Biped':
            # Asymmetric weight, slow breath, one small curious head response.
            breath=.5-.5*math.cos(a);shift=math.sin(a);curious=math.sin(math.pi*max(0,min(1,(p-.48)/.24)))**2 if .48<p<.72 else 0
            pelvis.location.x=-.012+.010*shift;pelvis.location.z=.011*breath
            rot('Pelvis',0,0,-.026+.012*shift);rot('Spine',.015*breath,0,.035-.016*shift)
            rot('Head',-.012*breath,.035*curious,-.018+.012*shift)
            rot('UpperArm_L',-.08,0,-.04);rot('Forearm_L',-.20)
            rot('UpperArm_R',.04,0,.025);rot('Forearm_R',-.12)
            rot('Ear_L',.040*math.sin(a-.55)-.035*curious);rot('Ear_R',.034*math.sin(a-.82)-.025*curious)
            rot('NeckSocket',-.018*math.sin(a-.32),0,-.012*shift);rot('BackSocket',.012*math.sin(a-.46),0,.008*shift)
        elif name=='Adult_Walk_Biped':
            # Eight readable poses: foot targets produce planted contact/recoil and a lifted passing leg.
            m={n:v.copy() for n,v in base.items()};dz=track(walk_z,p);side=track(walk_side,p);twist=track(walk_twist,p)
            for n in m:
                if n!='Root':m[n].translation+=Vector((side,0,dz))
            for side_name,offset in [('L',0),('R',.5)]:
                phase=(p+offset)%1;x=m['Thigh_'+side_name].translation.x
                target=Vector((x,track(walk_foot_y,phase),track(walk_foot_z,phase)))
                limb(m,'Thigh_'+side_name,'Shin_'+side_name,'Foot_'+side_name,target,(0,-1,0))
            assign(m);rot('Pelvis',0,0,-twist*.45);rot('Spine',.025*math.sin(2*a),0,twist)
            rot('Head',-.020*math.sin(2*a),0,-twist*.55)
            # Bent elbows and delayed wrists, opposite to the legs.
            for side_name,phase in [('L',0),('R',math.pi)]:
                swing=math.cos(a+phase);lag=math.cos(a+phase-.32)
                rot('UpperArm_'+side_name,.42*swing*(1.3 if swing>0 else 1),0,.035*(1 if side_name=='L' else -1))
                bend_elbow(side_name,95+5*lag);rot('Hand_'+side_name,.12*lag)
                rot('Ear_'+side_name,.085*math.sin(a+phase-.48))
            rot('NeckSocket',-.045*math.sin(a-.38),0,-.025*math.sin(a-.28));rot('BackSocket',.035*math.sin(a-.55),0,.018*math.sin(a-.42))
            # Solve legs after pelvis/spine changes so body sway cannot displace planted feet.
            bpy.context.view_layer.update()
            solved={b.name:b.matrix.copy() for b in rig.pose.bones}
            for side_name,offset in [('L',0),('R',.5)]:
                limb(solved,'Thigh_'+side_name,'Shin_'+side_name,'Foot_'+side_name,walk_target(side_name,(p+offset)%1),(0,-1,0))
            assign(solved)
        else:
            assign(quad_pose(p,name=='Adult_Hop_Quadruped'))
            rot('Head',-.06*math.sin(a));rot('Ear_L',.08*math.sin(a-.4));rot('Ear_R',.07*math.sin(a-.65))
        key(f+1)
    for slot in action.slots:
        for layer in action.layers:
            for strip in layer.strips:
                bag=strip.channelbag(slot)
                if bag:
                    for curve in bag.fcurves:
                        for point in curve.keyframe_points:point.interpolation='LINEAR' if name=='Adult_Walk_Biped' else 'BEZIER'
    report['clips'].append({'name':name,'frames':frames,'duration':frames/30,'loop':True,
        'style':'8-pose Disney-inspired cute walk' if name=='Adult_Walk_Biped' else 'asymmetric breathing idle' if name=='Adult_Idle_Biped' else 'preserved v0.70 quadruped'})

rig.animation_data.action=bpy.data.actions['Adult_Walk_Biped'];scene.frame_set(2);scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=rig
out=ROOT/'Assets/Art/Generated/AdultRabbit';out.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(out/'AdultRabbitMotion.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},
    apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,
    bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitMotion.blend'))
(out/'AdultRabbitMotion.audit.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('ADULT_RABBIT_MOTION_ART_OK')

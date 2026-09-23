"""AdultStandard_v2 locomotion: biped idle/walk/run and two preserved quadruped drafts."""
import bpy, math, json
import numpy as np
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

run_spec=json.loads((ROOT/'Assets/Resources/AdultRunTiming.json').read_text(encoding='utf8'))
run_phases=list(zip(run_spec['names'],run_spec['phases']))
def pose_curve(knots,p):
    for (a,x),(b,y) in zip(knots,knots[1:]):
        if p<=b:
            t=max(0,min(1,(p-a)/(b-a)));return x+(y-x)*t*t*(3-2*t)
    return knots[-1][1]

def run_foot(side_name,phase):
    # Fold the recovering heel behind the body before driving the knee forward.
    pitch=pose_curve([(0,-.15),(.10,0),(.22,.08),(run_spec['stance'],.72),(.56,1.05),(.68,.7),(.82,.08),(.94,-.25),(1,-.15)],phase)
    rotation=Matrix.Rotation(pitch,4,'X')
    group=shoe.vertex_groups['Foot_'+side_name].index
    points=[v.co-base['Foot_'+side_name].translation for v in shoe.data.vertices if any(g.group==group and g.weight>.99 for g in v.groups)]
    bottom=min((rotation.to_3x3()@p).z for p in points)
    distance=run_spec['speed']*run_spec['duration']
    stance=run_spec['stance'];start=-.23;end=start+distance*stance
    # Periodic Hermite recovery matches planted velocity at both boundaries.
    knots=[(stance,end,distance),(.425,.30,0),(.56,.31,0),(.68,.23,0),(.82,0,-2.4),(.94,start-.018,0),(1,start,distance)]
    y=start+distance*phase
    if phase>stance:
        for (a,x,dx),(b,z,dz) in zip(knots,knots[1:]):
            if phase<=b:
                t=(phase-a)/(b-a);h=b-a
                y=(2*t**3-3*t*t+1)*x+(t**3-2*t*t+t)*h*dx+(-2*t**3+3*t*t)*z+(t**3-t*t)*h*dz
                break
    lift=0 if phase<=stance else pose_curve([(stance,0),(.425,.08),(.56,.11),(.68,.11),(.82,.045),(.925,.03),(1,0)],phase)
    return Vector((base['Foot_'+side_name].translation.x,y,.0025-bottom+lift)),rotation

def prepare_run_height():
    # Fit a single periodic C-infinity height curve to the WHOLE reach envelope.
    # Never clip its height separately at each animation frame.
    envelope=[]
    for i in range(352):
        p=i/352;a=2*math.pi*p
        m={n:v.copy() for n,v in base.items()}
        for n in m:
            if n!='Root':m[n].translation+=Vector((.012*math.sin(a),-.018,0))
        assign(m);rot('Pelvis',.045,0,-.028*math.cos(a));bpy.context.view_layer.update()
        ceiling=float('inf')
        for side,phase in [('L',p),('R',(p+.5)%1)]:
            target,_=run_foot(side,phase)
            hip=rig.pose.bones['Thigh_'+side].matrix.translation
            delta=hip-target;reach=rig.data.bones['Thigh_'+side].length+rig.data.bones['Shin_'+side].length-.0001
            ceiling=min(ceiling,math.sqrt(max(.0001,reach*reach-delta.x*delta.x-delta.y*delta.y))-delta.z)
        envelope.append((p,ceiling))
    amplitude=.079/2
    # Compression follows contact; the crest MUST occur at the flight midpoint.
    # Solve only the mean height. Do not trade flight timing for extra height.
    apex=(run_spec['stance']+.5)/2
    phase=apex-.25
    offset=min(limit+amplitude*math.cos(4*math.pi*(p-phase)) for p,limit in envelope)-.004
    return offset,phase,amplitude

run_height_offset,run_height_phase,run_height_amplitude=prepare_run_height()

def smooth_periodic_body_curve(curve):
    # Uniform periodic cubic spline: solve tangents so first and second
    # derivatives agree at every knot, including the repeated cycle boundary.
    points=curve.keyframe_points;n=len(points)-1
    if n<3:return
    h=points[1].co.x-points[0].co.x
    y=np.array([points[i].co.y for i in range(n)])
    matrix=4*np.eye(n)
    for i in range(n):matrix[i,(i-1)%n]=1;matrix[i,(i+1)%n]=1
    slopes=np.linalg.solve(matrix,3*(np.roll(y,-1)-np.roll(y,1))/h)
    for i,point in enumerate(points):
        point.interpolation='BEZIER';point.handle_left_type='FREE';point.handle_right_type='FREE'
        slope=float(slopes[i%n]);x,z=point.co
        point.handle_left=(x-h/3,z-slope*h/3);point.handle_right=(x+h/3,z+slope*h/3)

specs=[('Adult_Idle_Biped',108),('Adult_Walk_Biped',24),('Adult_Idle_Quadruped',90),('Adult_Hop_Quadruped',30),('Adult_Run_Biped',round(run_spec['duration']*30))]
report={'bodyFamily':'AdultStandard_v2','reference':'ref_walking_ani_01.jpg; ref_ani.mp4 is an incomplete 104-byte fragment',
        'scope':'Biped run added; existing idle/walk and quadruped drafts preserved; no farm replacement.',
        'runSpeedMetersPerSecond':run_spec['speed'],'runStanceFraction':run_spec['stance'],'runReference':'ref_run_ani_01.jpg','runPhases':run_phases,
        'walkPhases':walk_phases,'walkVirtualDistance':.92,'virtualSpeedMetersPerSecond':1.15,'clips':[],
        'runHeightCurve':{'offset':run_height_offset,'phase':run_height_phase,'amplitude':run_height_amplitude,'reachMargin':.004}}
for name,frames in specs:
    old=bpy.data.actions.get(name)
    if old:bpy.data.actions.remove(old)
    action=bpy.data.actions.new(name);action.use_fake_user=True
    rig.animation_data_create();rig.animation_data.action=action
    # Four samples per frame preserve toe roll and heel fold at any authored duration.
    sample_count=frames*4 if name=='Adult_Run_Biped' else frames
    for sample in range(sample_count+1):
        f=sample*frames/sample_count
        scene.frame_set(int(f)+1,subframe=f-int(f));reset();bpy.context.view_layer.update();p=f/frames;a=2*math.pi*p
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
        elif name=='Adult_Run_Biped':
            m={n:v.copy() for n,v in base.items()}
            dz=run_height_offset-run_height_amplitude*math.cos(4*math.pi*(p-run_height_phase))
            for n in m:
                if n!='Root':m[n].translation+=Vector((.012*math.sin(a),-.018,dz))
            # Slight extra waist lean; counter-rotate the head to retain forward gaze.
            run_lean=math.radians(4)
            assign(m);rot('Pelvis',.045,0,-.028*math.cos(a));rot('Spine',.11+run_lean,0,.07*math.cos(a));rot('Head',-.085-run_lean,0,-.04*math.cos(a))
            for side_name,phase in [('L',0),('R',math.pi)]:
                swing=math.cos(a+phase)
                rot('UpperArm_'+side_name,.68*swing,0,.035*(1 if side_name=='L' else -1))
                bend_elbow(side_name,92+6*math.cos(a+phase-.25));rot('Hand_'+side_name,.13*math.cos(a+phase-.38))
                rot('Ear_'+side_name,.11*math.sin(2*a-.55)+.035*math.sin(a+phase))
            rot('NeckSocket',-.065*math.sin(2*a-.4),0,-.025*math.sin(a));rot('BackSocket',.045*math.sin(2*a-.55),0,.025*math.sin(a-.4))
            bpy.context.view_layer.update();solved={b.name:b.matrix.copy() for b in rig.pose.bones}
            targets={side_name:run_foot(side_name,(p+offset)%1) for side_name,offset in [('L',0),('R',.5)]}
            # The precomputed periodic curve already fits both legs. A failed fit
            # is a generation error, not permission to reintroduce a sharp dip.
            drop=0
            for side_name in ('L','R'):
                delta=solved['Thigh_'+side_name].translation-targets[side_name][0]
                reach=rig.data.bones['Thigh_'+side_name].length+rig.data.bones['Shin_'+side_name].length-.0001
                drop=max(drop,delta.z-math.sqrt(max(.0001,reach*reach-delta.x*delta.x-delta.y*delta.y)))
            report['runMaximumPelvisFit']=max(report.get('runMaximumPelvisFit',0),drop)
            if drop>.0005:raise RuntimeError(f'Run height envelope violation {drop:.6f}m at {p:.6f}')
            for side_name,offset in [('L',0),('R',.5)]:
                target,rotation=targets[side_name]
                limb(solved,'Thigh_'+side_name,'Shin_'+side_name,'Foot_'+side_name,target,(0,-1,0))
                solved['Foot_'+side_name]=rotation@base['Foot_'+side_name];solved['Foot_'+side_name].translation=target
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
                        for point in curve.keyframe_points:point.interpolation='LINEAR' if name in ('Adult_Walk_Biped','Adult_Run_Biped') else 'BEZIER'
                        if name=='Adult_Run_Biped' and any('"'+bone+'"' in curve.data_path for bone in ('Pelvis','Spine','Head')):
                            smooth_periodic_body_curve(curve)
    report['clips'].append({'name':name,'frames':frames,'duration':frames/30,'loop':True,
        'style':'cute biped run with short flight' if name=='Adult_Run_Biped' else '8-pose Disney-inspired cute walk' if name=='Adult_Walk_Biped' else 'asymmetric breathing idle' if name=='Adult_Idle_Biped' else 'preserved v0.70 quadruped'})

rig.animation_data.action=bpy.data.actions['Adult_Walk_Biped'];scene.frame_set(2);scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in objects:o.select_set(True)
bpy.context.view_layer.objects.active=rig
out=ROOT/'Assets/Art/Generated/AdultRabbit';out.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(out/'AdultRabbitMotion.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},
    apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,
    bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=.25,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitMotion.blend'))
(out/'AdultRabbitMotion.audit.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('ADULT_RABBIT_MOTION_ART_OK')

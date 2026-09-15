"""Stage 2-3: elastic locomotion, independently timed transitions and locomotion bridges.
Reads RabbitStudy.blend without saving it; owns RabbitMotion.blend/.fbx/.audit.json.
Unity owns forward translation. Special actions await transition review.
"""
import bpy, math, json, hashlib
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT/'ArtSource/RabbitStudy.blend'
OUT = ROOT/'Assets/Art/Generated'
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
scene = bpy.context.scene
scene.render.fps = 30
rig = bpy.data.objects['RabbitRig']
body = bpy.data.objects['RabbitBody']
names = [b.name for b in rig.data.bones]
base = {}
for label in ['Biped', 'Quadruped']:
    rig.animation_data.action = bpy.data.actions['Pose_'+label]
    scene.frame_set(1); bpy.context.view_layer.update()
    base[label] = {b.name:b.matrix.copy() for b in rig.pose.bones}

def assign(matrices):
    for pb in rig.pose.bones: pb.matrix_basis = Matrix.Identity(4)
    bpy.context.view_layer.update()
    for pb in rig.pose.bones:
        pb.matrix = matrices[pb.name]
        bpy.context.view_layer.update()

def joint(a,c,l1,l2,pole):
    delta=c-a; d=delta.length
    assert d < l1+l2+.001, 'Unreachable limb: '+str(d)
    u=delta.normalized();d=max(.0001,min(d,l1+l2-.00001))
    x=(l1*l1-l2*l2+d*d)/(2*d)
    v=Vector(pole);v=(v-u*v.dot(u)).normalized()
    return a+u*x+v*math.sqrt(max(0,l1*l1-x*x))

def segment(name,a,b):
    rest=rig.data.bones[name]
    q=(rest.tail_local-rest.head_local).rotation_difference(b-a)
    m=q.to_matrix().to_4x4() @ rest.matrix_local
    m.translation=a
    return m

def limb(m,start,middle,end,ankle,pole,reference):
    a=m[start].translation
    b=joint(a,ankle,rig.data.bones[start].length,rig.data.bones[middle].length,pole)
    m[start]=segment(start,a,b);m[middle]=segment(middle,b,ankle)
    m[end]=reference[end].copy();m[end].translation=ankle

def smooth(t): return t*t*(3-2*t)

def foot_track(phase,start,stance,stride,lift):
    p=(phase-start)%1
    if p <= stance:
        return stride*(p-stance/2),0,True
    u=(p-stance)/(1-stance)
    # Hermite tangents match the backward stance speed at both contacts.
    y=(2*u**3-3*u*u+1)*(stride*stance/2)+(-2*u**3+3*u*u)*(-stride*stance/2)
    y+=(u**3-2*u*u+u + u**3-u*u)*stride*(1-stance)
    return y,lift*math.sin(math.pi*u)**2,False

def bump(p,start,end):
    if not start<p<end:return 0
    return math.sin(math.pi*(p-start)/(end-start))**2

def rotate_group(m,group,pivot,angle):
    transform=Matrix.Translation(pivot)@Matrix.Rotation(angle,4,'X')@Matrix.Translation(-pivot)
    for n in group:m[n]=transform@m[n]

def follow_through(m,head_angle,ear_angle,tail_angle):
    rotate_group(m,['Head','Ear_L','Ear_R'],m['Head'].translation.copy(),head_angle)
    for side in ['L','R']:
        n='Ear_'+side
        rotate_group(m,[n],m[n].translation.copy(),ear_angle*(1 if side=='L' else .8))
    rotate_group(m,['Tail'],m['Tail'].translation.copy(),tail_angle)

def pose(kind,p):
    quadruped=kind in ('Idle_Quadruped','Hop_Quadruped')
    ref=base['Quadruped' if quadruped else 'Biped']
    m={n:a.copy() for n,a in ref.items()}
    walk=kind=='Walk_Biped';hop=kind=='Hop_Quadruped'
    q=(p+.39)%.5
    flight=bump(q,.40,.50) if walk else bump(p,.15,.48) if hop else 0
    if walk:dz=-.045-.022*bump(q,0,.18)+.090*flight
    elif hop:dz=.003+.115*flight-.018*bump((p-.75)%1,0,.4)-.012*bump(p,.48,.70)
    else:dz=(-.017 if not quadruped else .001)+.005*math.sin(2*math.pi*p)
    for n in names:
        if n!='Root':m[n].translation+=Vector((0,0,dz))
    contacts={}
    for side,offset in [('L',0),('R',.5)]:
        n='Foot_'+side;ankle=ref[n].translation.copy()
        if walk:
            dy,lift,contact=foot_track((p+offset-.11)%1,0,.40,.36,.105)
            ankle+=Vector((0,dy,lift+.090*flight));contacts[n]=contact
        elif hop:
            dy,lift,contact=foot_track(p,.75,.40,.30,.095)
            ankle+=Vector((0,dy,lift+.090*flight));contacts[n]=contact
        else:contacts[n]=True
        limb(m,'Thigh_'+side,'Shin_'+side,n,ankle,(0,-1,0),ref)
        if quadruped:
            n='Hand_'+side;ankle=ref[n].translation.copy()
            if hop:
                dy,lift,contact=foot_track(p,.48,.32,.30,.080)
                ankle+=Vector((0,dy,lift+.090*flight));contacts[n]=contact
            else:contacts[n]=True
            limb(m,'UpperArm_'+side,'Forearm_'+side,n,ankle,(0,1,0),ref)
        elif walk:
            rotate_group(m,['UpperArm_'+side,'Forearm_'+side,'Hand_'+side],
                         m['UpperArm_'+side].translation.copy(),.32*math.cos(2*math.pi*(p+offset-.11)))
    if walk:follow_through(m,.040*math.sin(4*math.pi*(p-.04)),.14*math.sin(4*math.pi*(p-.09)),.12*math.sin(4*math.pi*(p-.12)))
    elif hop:follow_through(m,.070*math.sin(2*math.pi*(p-.05)),.19*math.sin(2*math.pi*(p-.11)),.16*math.sin(2*math.pi*(p-.15)))
    else:follow_through(m,.008*math.sin(2*math.pi*p),.04*math.sin(2*math.pi*(p-.08)),.015*math.sin(2*math.pi*(p-.12)))
    return m,contacts

basic_pose=pose

def mix_matrix(a,b,t):
    ap,aq,asc=a.decompose();bp,bq,bsc=b.decompose()
    return Matrix.LocRotScale(ap.lerp(bp,t),aq.slerp(bq,t),asc.lerp(bsc,t))

def blend_body(a,b,t):
    m={n:mix_matrix(a[n],b[n],t) for n in names}
    # Keep body attachments local to their moving parent instead of interpolating
    # shoulder/hip positions through the interior of the turning torso.
    for n in ['Head','Tail','UpperArm_L','UpperArm_R','Thigh_L','Thigh_R']:
        m[n]=m['Spine']@mix_matrix(a['Spine'].inverted()@a[n],b['Spine'].inverted()@b[n],t)
    for n in ['Ear_L','Ear_R']:
        m[n]=m['Head']@mix_matrix(a['Head'].inverted()@a[n],b['Head'].inverted()@b[n],t)
    return m

def span(p,start,end): return smooth(max(0,min(1,(p-start)/(end-start))))

def step_point(a,b,p,start,end,lift):
    t=span(p,start,end)
    point=a.lerp(b,t)
    point.z+=lift*math.sin(math.pi*t)**2
    return point,not(start<p<end)

def transition(p,rising=False):
    a=basic_pose('Idle_Quadruped' if rising else 'Idle_Biped',0)[0]
    b=basic_pose('Idle_Biped' if rising else 'Idle_Quadruped',0)[0]
    if p<=0:return a,{'Foot_L':True,'Foot_R':True,**({'Hand_L':True,'Hand_R':True} if rising else {})}
    if p>=1:return b,{'Foot_L':True,'Foot_R':True,**({} if rising else {'Hand_L':True,'Hand_R':True})}
    # Each direction has its own support order and landing response.
    t=span(p,.38,.96) if rising else span(p,0,.48)
    m=blend_body(a,b,t);contacts={}
    if rising:
        dz=-.018*bump(p,.35,.53)+.014*bump(p,.55,.75)-.012*bump(p,.82,1)
        tilt=-.045*bump(p,.40,.67)
        legs=[('L',.02,.20),('R',.20,.38)];hand_start=.32;hand_end=.86
    else:
        dz=-.015*bump(p,.48,.62)
        tilt=.070*bump(p,.53,.82)
        legs=[('L',.60,.77),('R',.77,.94)];hand_start=.06;hand_end=.50
    # Tilt around the forepaw support region: the rump rises without dragging paws.
    pivot=base['Quadruped']['Hand_L'].translation.copy();pivot.x=0
    rotate_group(m,[n for n in names if n!='Root'],pivot,tilt)
    for n in names:
        if n!='Root':m[n].translation.z+=dz
    for side,start,end in legs:
        n='Foot_'+side
        ankle,support=step_point(a[n].translation,b[n].translation,p,start,end,.065)
        ref={n:mix_matrix(a[n],b[n],span(p,start,end))}
        limb(m,'Thigh_'+side,'Shin_'+side,n,ankle,(0,-1,0),ref);contacts[n]=support
        n='Hand_'+side
        ankle,_=step_point(a[n].translation,b[n].translation,p,hand_start,hand_end,.045)
        ref={n:mix_matrix(a[n],b[n],span(p,hand_start,hand_end))}
        limb(m,'UpperArm_'+side,'Forearm_'+side,n,ankle,(0,1,0),ref)
        contacts[n]=p<=hand_start if rising else p>=hand_end
    response=(-bump(p,.40,.60)+.65*bump(p,.62,.88)) if rising else (-bump(p,.49,.65)+.7*bump(p,.65,.92))
    follow_through(m,.055*response,.17*response,.12*response)
    return m,contacts

def bridge(name,p):
    walk=name.startswith('Walk');starting=name.endswith('Start')
    idle='Idle_Biped' if walk else 'Idle_Quadruped';moving='Walk_Biped' if walk else 'Hop_Quadruped'
    a=basic_pose(idle if starting else moving,0)[0]
    b=basic_pose(moving if starting else idle,0)[0]
    distance=(.0684 if starting else .1116) if walk else .09
    # Cubic cumulative travel matches zero speed at idle and 0.3m/s at motion.
    travel=distance*smooth(p)+(.18*(p**3-p*p) if starting else .18*(p**3-2*p*p+p))
    if p<=0:return a,basic_pose(idle if starting else moving,0)[1],travel
    if p>=1:return b,basic_pose(moving if starting else idle,0)[1],travel
    m=blend_body(a,b,smooth(p));contacts={}
    response=-.022*bump(p,.04,.40) if starting else -.020*bump(p,.42,.72)+.010*bump(p,.72,.95)
    for n in names:
        if n!='Root':m[n].translation.z+=response
    for side in ['L','R']:
        n='Foot_'+side;start=a[n].translation.copy();finish=b[n].translation-Vector((0,distance,0))
        if walk:
            if starting and side=='R':point=start.copy();support=True
            elif starting:
                point,_=step_point(start,finish,p,.10,1,.085);support=p<=.10
            elif side=='L':
                point,_=step_point(start,finish,p,0,.38,.050);support=p>=.38
            else:point,support=step_point(start,finish,p,.40,.90,.065)
        else:
            point,support=step_point(start,finish,p,.18 if starting else .51,.68 if starting else .92,.045)
        point.y+=travel
        ref={n:mix_matrix(a[n],b[n],smooth(p))}
        limb(m,'Thigh_'+side,'Shin_'+side,n,point,(0,-1,0),ref);contacts[n]=support
        if not walk:
            n='Hand_'+side;start=a[n].translation.copy();finish=b[n].translation-Vector((0,distance,0))
            if starting:
                point,_=step_point(start,finish,p,.70,1,.02);support=p<=.70
            else:
                point,_=step_point(start,finish,p,0,.48,.015);support=p>=.48
            point.y+=travel
            ref={n:mix_matrix(a[n],b[n],smooth(p))}
            limb(m,'UpperArm_'+side,'Forearm_'+side,n,point,(0,1,0),ref);contacts[n]=support
    follow_through(m,response*2,response*6,response*4)
    return m,contacts,travel

def sample_motion(name,p,distance):
    if name=='To_Quadruped':
        m,c=transition(p);return m,c,0
    if name=='To_Biped':
        m,c=transition(p,rising=True);return m,c,0
    if name.endswith(('_Start','_Stop')):return bridge(name,p)
    m,c=basic_pose(name,p);return m,c,distance*p

specs=[('Idle_Biped',90,0),('Idle_Quadruped',90,0),('Walk_Biped',36,.36),('Hop_Quadruped',30,.30),
       ('To_Quadruped',36,0),('To_Biped',42,0),('Walk_Start',18,.0684),('Walk_Stop',18,.1116),
       ('Hop_Start',18,.09),('Hop_Stop',18,.09)]
following={'To_Quadruped':'Idle_Quadruped','To_Biped':'Idle_Biped','Walk_Start':'Walk_Biped',
           'Walk_Stop':'Idle_Biped','Hop_Start':'Hop_Quadruped','Hop_Stop':'Idle_Quadruped'}
report={'source_sha256':hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        'blender':bpy.app.version_string,'bones':names,'clips':[],
        'scope':'Elastic motion revision: all ten clips await user review. Special actions not created.',
        'connections':[['Idle_Biped','Walk_Start'],['Walk_Start','Walk_Biped'],['Walk_Biped','Walk_Stop'],
                       ['Walk_Stop','To_Quadruped'],['To_Quadruped','Idle_Quadruped'],['Idle_Quadruped','Hop_Start'],
                       ['Hop_Start','Hop_Quadruped'],['Hop_Quadruped','Hop_Stop'],['Hop_Stop','To_Biped'],['To_Biped','Idle_Biped']]}
for name,frames,distance in specs:
    rig.animation_data.action=None
    action=bpy.data.actions.new(name);action.use_fake_user=True
    rig.animation_data.action=action
    record={'name':name,'duration':frames/30,'distance':distance,'loop':name not in following,
            'next':following.get(name,name),'samples':[],'travel':[]}
    for f in range(frames+1):
        scene.frame_set(f+1)
        try:matrices,contacts,travel=sample_motion(name,f/frames,distance)
        except AssertionError as e:raise AssertionError((name,f,frames,str(e))) from e
        record['travel'].append({'time':f/30,'distance':travel})
        assign(matrices)
        for pb in rig.pose.bones:
            pb.rotation_mode='QUATERNION'
            for prop in ['location','rotation_quaternion','scale']:
                pb.keyframe_insert(prop,frame=f+1,group=pb.name)
        if True:  # Sample every frame, including short aerial and contact intervals.
            bpy.context.view_layer.update()
            deps=bpy.context.evaluated_depsgraph_get()
            ob=body.evaluated_get(deps);mesh=ob.to_mesh()
            points=[ob.matrix_world@v.co for v in mesh.vertices]
            lo=[min(v[i] for v in points) for i in range(3)]
            hi=[max(v[i] for v in points) for i in range(3)]
            assert all(math.isfinite(v) for v in lo+hi)
            assert lo[2]>-.045, (name,f,'floor penetration',lo[2])
            assert hi[2]<2.25, (name,f,'unexpected height',hi[2])
            record['samples'].append({'time':f/30,'travel':travel,'min':lo,'max':hi,
                'contactNames':[n for n,v in contacts.items() if v],
                'joints':[{'name':n,'position':list(rig.pose.bones[n].head)} for n in ['Head','Spine','Hand_L','Hand_R','Foot_L','Foot_R']]})
            ob.to_mesh_clear()
    # Bake densely with linear interpolation: no Bezier overshoot between contacts.
    for slot in action.slots:
        for layer in action.layers:
            for strip in layer.strips:
                bag=strip.channelbag(slot)
                if bag:
                    for curve in bag.fcurves:
                        for key in curve.keyframe_points:key.interpolation='LINEAR'
    if record['loop']:
        record['loop_matrix_error']=max(max(abs(a-b) for ra,rb in zip(basic_pose(name,0)[0][n],basic_pose(name,1)[0][n]) for a,b in zip(ra,rb)) for n in names)
        assert record['loop_matrix_error']<.00001
    report['clips'].append(record)

rig.animation_data.action=bpy.data.actions['Walk_Biped'];scene.frame_set(1)
scene.frame_start=1;scene.frame_end=37
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'RabbitMotion.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},
    apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
    bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/RabbitMotion.blend'))
(OUT/'RabbitMotion.audit.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==report['source_sha256']
print('RABBIT_STAGE23_ART_OK')

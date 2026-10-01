"""Left-side rest. Authored support envelope; original rig and meshes preserved."""
import bpy, math, json, sys, numpy as np
from mathutils import Matrix, Vector, Quaternion
from mathutils.bvhtree import BVHTree
from pathlib import Path
CLIPS=[('Adult_Lie_To_Left',2.4,False),('Adult_Left_To_Lie',2.4,False),('Adult_Breathe_Left',4,True),
       ('Adult_Fall_Asleep_Left',3,False),('Adult_Sleep_Left',6,True),('Adult_Wake_Left',1.5,False)]
# Body-specific support targets are separate from the shared transition timing.
SUPPORT={'left_hand':(-.12,.935,.097),'right_hand':(-.335,.64,.37),'head_lean_degrees':4,'head_nod_degrees':-3}
# The preserved rig's labels predate its -Y facing convention: anatomical
# left is -X (legacy suffix R), anatomical right is +X (legacy suffix L).
ANATOMICAL_LEFT='R'
ANATOMICAL_RIGHT='L'
def ease(x):
    x=max(0,min(1,x));return x*x*x*(10+x*(-15+6*x))

def build(rig,base,assign,limb,key):
    from adult_rest_tail import fix_existing
    fix_existing(rig,base)
    scene=bpy.context.scene;rig.animation_data.action=bpy.data.actions['Adult_Lie_Hold'];scene.frame_set(1);bpy.context.view_layer.update()
    rest={b.name:b.matrix.copy() for b in rig.pose.bones};inv={n:v.inverted() for n,v in base.items()}
    descendants={b.name:[b.name]+[c.name for c in b.children_recursive] for b in rig.data.bones}
    geometry={};skin_geometry={}
    for ob in bpy.data.objects:
        if ob.type!='MESH':continue
        pts=np.array([tuple(v.co)+(1,) for v in ob.data.vertices]);weights={}
        for v in ob.data.vertices:
            for w in v.groups:
                name=ob.vertex_groups[w.group].name
                if name not in weights:weights[name]=np.zeros(len(pts))
                weights[name][v.index]=w.weight
        skin_geometry[ob.name]=(pts,weights)
        if ob.name not in ('Backpack','Top','Neckwear','BodyTail'):geometry[ob.name]=(pts,weights)
    def floor(m,name):
        pts,weights=geometry[name]
        return float(sum((pts@np.array(m[n]@inv[n]).T)[:,2]*w for n,w in weights.items()).min())
    def rotate(m,n,angle,axis='X'):
        p=m[n].translation.copy();q=Matrix.Translation(p)@Matrix.Rotation(angle,4,axis)@Matrix.Translation(-p)
        for child in descendants[n]:m[child]=q@m[child]
    def pose(u,pitch,returning=False):
        m={n:v.copy() for n,v in rest.items()}
        # Knees gather before the torso follows; return has its own overlap.
        legs=ease(min(1,u*1.25)) if not returning else ease(u)
        arms=ease(min(1,u*1.45)) if not returning else ease(min(1,u*1.12))
        for side,hip,knee in [(ANATOMICAL_LEFT,-55,115),(ANATOMICAL_RIGHT,-65,125)]:
            rotate(m,'Thigh_'+side,math.radians(hip)*legs)
            rotate(m,'Shin_'+side,math.radians(knee)*legs)
            rotate(m,'Foot_'+side,math.radians(-50)*legs)
            if side==ANATOMICAL_LEFT:
                foot=m['Foot_R'].copy();target=foot.translation+Vector(((.036+.0022*(math.degrees(pitch)-13))*legs,0,0))
                pole=m['Shin_R'].translation-m['Thigh_R'].translation
                limb(m,'Thigh_R','Shin_R','Foot_R',target,pole)
                foot.translation=target;m['Foot_R']=foot
        rotate(m,'Spine',math.radians(-4)*math.sin(math.pi*u)**2,'Y')
        rotate(m,'Head',math.radians(-2)*math.sin(math.pi*u)**2,'Y')
        roll=ease(u);pivot=rest['Pelvis'].translation
        q=Matrix.Translation(pivot)@Matrix.Rotation(pitch*roll,4,'X')@Matrix.Rotation(-math.pi/2*roll,4,'Y')@Matrix.Translation(-pivot)
        for n in m:
            if n!='Root':m[n]=q@m[n]
        rotate(m,'Head',math.radians(SUPPORT['head_lean_degrees'])*roll,'Y')
        rotate(m,'Head',math.radians(SUPPORT['head_nod_degrees'])*roll,'X')
        m['Tail']=m['Pelvis']@base['Pelvis'].inverted()@base['Tail']
        final_q=Matrix.Translation(pivot)@Matrix.Rotation(pitch,4,'X')@Matrix.Rotation(-math.pi/2,4,'Y')@Matrix.Translation(-pivot)
        final_body={n:final_q@v if n!='Root' else v for n,v in rest.items()}
        ground=.002-floor(final_body,'BodyTorso')
        for side in (ANATOMICAL_LEFT,ANATOMICAL_RIGHT):
            upper,lower,hand=('UpperArm_'+side,'Forearm_'+side,'Hand_'+side)
            target=Vector(SUPPORT['left_hand'] if side==ANATOMICAL_LEFT else SUPPORT['right_hand']);target.z-=ground
            target=q@final_q.inverted()@target
            target=m[hand].translation.lerp(target,arms)
            if side==ANATOMICAL_LEFT:
                # Carry the hand around the cheek, not straight through it.
                target+=q.to_3x3()@final_q.inverted().to_3x3()@Vector((-.08*math.sin(math.pi*arms),0,0))
            oldhand=m[hand].copy()
            pole=(m[lower].translation-m[upper].translation).normalized().lerp(Vector((-1,.1,-.95) if side==ANATOMICAL_LEFT else (-.3,-1,-.1)),arms)
            limb(m,upper,lower,hand,target,pole)
            wrist=m[lower].to_quaternion()@base[lower].to_quaternion().inverted()@base[hand].to_quaternion()
            m[hand]=oldhand.to_quaternion().slerp(wrist,arms).to_matrix().to_4x4();m[hand].translation=target
            if side==ANATOMICAL_LEFT:
                rotation=Matrix.Rotation(1.2*arms,4,'Z')@Matrix.Rotation(-.55*arms,4,'X')@m[hand];rotation.translation=target;m[hand]=rotation
            else:
                rotation=Matrix.Rotation(.4*arms,4,'Z')@m[hand];rotation.translation=target;m[hand]=rotation
        return m
    def skin_points(m,name):
        pts,weights=skin_geometry[name]
        return sum((pts@np.array(m[n]@inv[n]).T)[:,:3]*w[:,None] for n,w in weights.items())
    pillow_indices={name:np.where(sum(w for n,w in skin_geometry[name][1].items() if n in ('Forearm_R','Hand_R'))>.5)[0] for name in ('Top','BodyHands')}
    def pillow_gap(m):
        hp=skin_points(m,'Head');tree=BVHTree.FromPolygons([Vector(p) for p in hp],[tuple(p.vertices) for p in bpy.data.objects['Head'].data.polygons])
        closest={}
        for name,indices in pillow_indices.items():
            distances=[];ps=skin_points(m,name)
            for index in indices:
                p=Vector(ps[index]);hit,normal,face,d=tree.find_nearest(p)
                distances.append(d*(1 if (p-hit).dot(normal)>=0 else -1))
            closest[name]=min(distances)
        return closest
    # Raise the head only as far as the visible left forearm needs: it is no
    # longer forced to touch the floor through its supporting arm.
    lo,hi=math.radians(13),math.radians(19)
    for _ in range(20):
        pitch=(lo+hi)/2;gaps=pillow_gap(pose(1,pitch))
        if min(gaps.values())<.0015:lo=pitch
        else:hi=pitch
    pitch=(lo+hi)/2
    print('PILLOW_GAPS',pillow_gap(pose(1,pitch)),flush=True)
    endpoint=pose(1,pitch);print('SIDE_PITCH',math.degrees(pitch),'FLOORS',{n:floor(endpoint,n) for n in geometry},flush=True)
    # Offline smooth envelope clears the rotating silhouette without runtime clamps.
    us=np.linspace(0,1,161);paths={}
    for returning in (False,True):
        heights=np.array([.002-min(floor(pose(float(u),pitch,returning),n) for n in geometry) for u in us])
        heights[0]=0
        polynomial=np.polynomial.Chebyshev.fit(us,heights,18)
        residual=max(heights-polynomial(us))+.0004
        def height(u,poly=polynomial,residual=residual,end=float(heights[-1])):
            # Exact resting endpoints and zero endpoint speed from outer easing.
            raw=float(poly(u));raw-=(1-u)*float(poly(0))+u*(float(poly(1))-end)
            return raw+residual*math.sin(math.pi*u)**.5
        paths[returning]=height
    def lifted(u,returning=False):
        m=pose(u,pitch,returning);z=paths[returning](u)
        for n in m:
            if n!='Root':m[n].translation.z+=z
        return m
    end=lifted(1)
    print('SIDE_REST_FLOORS',{n:floor(end,n) for n in geometry},flush=True)
    if '--inspect-side' in sys.argv:
        for n in ('Pelvis','Spine','Head','UpperArm_R','Forearm_R','Hand_R','UpperArm_L','Forearm_L','Hand_L'):
            print('JOINT',n,tuple(end[n].translation),flush=True)
        pts,weights=geometry['Head'];hp=sum((pts@np.array(end[n]@inv[n]).T)[:,:3]*w[:,None] for n,w in weights.items())
        print('HEAD_LOW',hp[np.argsort(hp[:,2])[:15]].tolist(),flush=True)
        raise SystemExit(0)
    cached={}
    for name,duration,loop in CLIPS:
        samples=[]
        clock=0.0
        for i in range(round(duration*120)+1):
            t=i/120;p=t/duration
            sleeping=1.0 if name=='Adult_Sleep_Left' else 0.0
            if name in ('Adult_Fall_Asleep_Left','Adult_Wake_Left'):
                decay=(1+2*t/.8)*math.exp(-2*t/.8)
                sleeping=1-decay if name=='Adult_Fall_Asleep_Left' else decay
                if i:clock+=(1/120)/(4+2*sleeping)
                p=clock
            if loop or name in ('Adult_Fall_Asleep_Left','Adult_Wake_Left'):
                m={n:v.copy() for n,v in end.items()}
                breath=.5-.5*math.cos(2*math.pi*(p+.04*(1-math.cos(2*math.pi*p))))
                # Chest opens along its own ventral direction; head remains supported.
                pivot=m['Head'].translation.copy();axis=Vector((0,math.sin(pitch),-math.cos(pitch)))
                q=Matrix.Translation(pivot)@Matrix.Rotation(.0335*(1+sleeping/3)*breath,4,axis)@Matrix.Translation(-pivot)
                for n in descendants['Spine']:m[n]=q@m[n]
                for n in ('Head','Ear_L','Ear_R'):m[n]=end[n].copy()
                # The pillow hand stays planted; chest breathing is absorbed at
                # the elbow rather than sliding the supporting forearm wholesale.
                limb(m,'UpperArm_R','Forearm_R','Hand_R',end['Hand_R'].translation,
                     end['Forearm_R'].translation-end['UpperArm_R'].translation)
                m['Hand_R']=end['Hand_R'].copy()
            else:
                returning=name=='Adult_Left_To_Lie'
                u=ease(p) if not returning else 1-ease(p)
                m=lifted(u,returning)
                if i==0:m={n:v.copy() for n,v in (end if returning else rest).items()}
                if i==round(duration*120):m={n:v.copy() for n,v in (rest if returning else end).items()}
            samples.append(m)
        cached[name]=samples
    worst=min((floor(m,n),name,i/120,n) for name,samples in cached.items() for i,m in enumerate(samples) for n in geometry)
    print('SIDE_FLOOR_WORST',worst,flush=True)
    for name,duration,loop in CLIPS:
        old=bpy.data.actions.get(name)
        if old:bpy.data.actions.remove(old)
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        for i,m in enumerate(cached[name]):assign(m);key(1+i/4)
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for curve in bag.fcurves:
                            for point in curve.keyframe_points:point.interpolation='LINEAR'
    if worst[0]<-.0005:raise RuntimeError(('Side floor clearance',worst))
    from adult_rabbit_sleep import eye_weight
    dest=Path(__file__).resolve().parents[1]/'Assets/Art/Generated/AdultRabbit/SleepTiming.json'
    timing=json.loads(dest.read_text(encoding='utf8'));timing['clips']=[c for c in timing['clips'] if c['clip']<29]
    for index,(name,duration,loop) in enumerate(CLIPS[3:],29):
        source={29:23,30:24,31:25}[index]
        timing['clips'].append({'clip':index,'name':name,'duration':duration,'loop':loop,'eyes':[eye_weight(source,i/120) for i in range(round(duration*120)+1)]})
    data=json.dumps(timing);dest.write_text(data,encoding='utf8')
    text=bpy.data.texts.get('SleepCurves.json') or bpy.data.texts.new('SleepCurves.json');text.clear();text.write(data)

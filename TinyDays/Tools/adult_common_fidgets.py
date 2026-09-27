"""Body-family idle gestures, with authored shoe contact pivots and no scale keys."""
import bpy, math
from mathutils import Matrix, Vector
from adult_common_sigh import ease

STANDING_SHIFT=.040
STANDING_FOOT_OFFSET=.020

def pulse(t,start,peak,end):
    if t<=start or t>=end:return 0
    return ease((t-start)/(peak-start)) if t<peak else 1-ease((t-peak)/(end-peak))

def around(point,angle,axis):
    return Matrix.Translation(point)@Matrix.Rotation(angle,4,axis)@Matrix.Translation(-point)

def ankle(t,side):
    return sum(pulse(t,.35+.5*i,.65+.5*i,.95+.5*i) for i in range(6) if i%2==(0 if side=='L' else 1))

def shift(t):
    knots=[(0,0),(.4,-1),(6.1,-1),(6.5,1),(12.2,1),(13,0)]
    for (a,x),(b,y) in zip(knots,knots[1:]):
        if t<=b:return x+(y-x)*ease((t-a)/(b-a))
    return 0

def foot_motion(t,side):
    outward,restore,taps=(.4,5.75,1.75) if side=='L' else (6.5,11.85,7.85)
    outside=ease((t-outward)/.35)-ease((t-restore)/.35)
    yaw=8*(ease((t-outward-.04)/.27)-ease((t-restore-.04)/.27))
    lift=.003*(pulse(t,outward,outward+.175,outward+.35)+pulse(t,restore,restore+.175,restore+.35))
    toe=sum(pulse(t,taps+.5*i,taps+.5*i+.28,taps+.5*i+.45) for i in range(5))
    return yaw,lift,outside,toe

def standing_frame(rest,t):
    """Separate hip loading from rib-cage counter-turn; never scale a joint."""
    w=shift(t);env=ease(t/.4)*(1-ease((t-12.2)/.8))
    left=1 if rest['Thigh_L'].translation.x>0 else -1
    settle=.0015*math.sin(2*math.pi*t/4)*env
    delta=Vector((left*(STANDING_SHIFT*w+settle),0,0))
    turn=around(rest['Pelvis'].translation,math.radians(5*w),'Z')@around(rest['Pelvis'].translation,math.radians(-standing_roll(w)*left),'Y')
    return Matrix.Translation(delta)@turn

def standing_roll(w):
    # The preserved breathing pose is not exactly symmetric. Respect its bind
    # and feet instead of forcing equal tilts that overextend the right leg.
    return 4.5*w+1.5*w*w

def standing_foot(rest,heels,lengths,t,side):
    n='Foot_'+side;sign=1 if rest[n].translation.x>0 else -1
    yaw,lift,outside,toe=foot_motion(t,side)
    foot=around(heels[side],math.radians(yaw)*sign,'Z')@around(heels[side],-math.radians(10)*toe,'X')@rest[n]
    foot.translation.x+=sign*STANDING_FOOT_OFFSET*outside
    foot.translation.z+=lift+.0015*outside
    return foot

def standing_heights(rig,rest,heels,lengths):
    """Height-first authored path: reach failures must fail, never lower it."""
    return [.009*ease((i/120)/.4)*(1-ease((i/120-12.2)/.8)) for i in range(13*120+1)]

def build(rig,assign,limb,key):
    scene=bpy.context.scene;shoe=bpy.data.objects['Shoes'];max_reach=0
    upper=[b.name for b in rig.data.bones if b.name=='Spine' or any(a.name=='Spine' for a in b.parent_recursive)]
    heads=[b.name for b in rig.data.bones if b.name=='Head' or any(a.name=='Head' for a in b.parent_recursive)]
    for seated,name,duration in [(True,'Adult_Fidget_Ankles',4),(False,'Adult_Fidget_Weight',13)]:
        rig.animation_data.action=bpy.data.actions['Adult_Breathe_Sit' if seated else 'Adult_Breathe_Stand'];scene.frame_set(1);bpy.context.view_layer.update()
        rest={b.name:b.matrix.copy() for b in rig.pose.bones};heels={};shoe_lengths={}
        for side in ('L','R'):
            n='Foot_'+side;group=shoe.vertex_groups[n].index;skin=rest[n]@rig.data.bones[n].matrix_local.inverted()
            points=[skin@v.co for v in shoe.data.vertices if any(g.group==group and g.weight>.99 for g in v.groups)]
            bottom=min(p.z for p in points);candidates=[p for p in points if p.z<bottom+.0001]
            # Blender forward is -Y: the rear-most lowest sole point is the heel.
            heel=max(candidates,key=lambda p:p.y);heels[side]=heel.copy()
            shoe_lengths[side]=max(p.y for p in points)-min(p.y for p in points)
        heights=standing_heights(rig,rest,heels,shoe_lengths) if not seated else None
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        def weight(t):return shift(t)
        for sample in range(duration*120+1):
            t=sample/120;m={n:v.copy() for n,v in rest.items()};w=weight(t) if not seated else 0
            if not seated:
                left=1 if rest['Thigh_L'].translation.x>0 else -1
                turn=Matrix.Translation(Vector((0,0,-float(heights[sample]))))@standing_frame(rest,t)
                for n in m:
                    if n!='Root':m[n]=turn@m[n]
                counter=around(m['Spine'].translation,math.radians(-3*w),'Z')@around(m['Spine'].translation,math.radians((standing_roll(w)-2*w)*left),'Y')
                for n in upper:m[n]=counter@m[n]
            breath=.003*math.sin(2*math.pi*t/4)*ease(t/.35)*(1-ease((t-duration+.35)/.35))
            for n in upper:m[n].translation.z+=breath
            lag=weight(max(0,t-2/30))-w if not seated else .2*(ankle(t,'L')-ankle(t,'R'))
            head_turn=around(m['Head'].translation,math.radians(lag*(2 if not seated else .3)),'Z' if not seated else 'X')
            for n in heads:m[n]=head_turn@m[n]
            for side in ('L','R'):
                n='Foot_'+side
                amount=ankle(t,side)
                side_sign=1 if rest[n].translation.x>0 else -1
                yaw,lift,outside,toe=foot_motion(t,side) if not seated else (0,0,0,0)
                angle=-math.radians(12)*amount if seated else math.radians(yaw)*side_sign
                foot=around(heels[side],angle,'X' if seated else 'Z')@rest[n]
                if not seated:
                    foot=standing_foot(rest,heels,shoe_lengths,t,side)
                target=foot.translation.copy();a=m['Thigh_'+side].translation
                max_reach=max(max_reach,(target-a).length-rig.data.bones['Thigh_'+side].length-rig.data.bones['Shin_'+side].length)
                pole=rest['Shin_'+side].translation-rest['Thigh_'+side].translation if seated else Vector((0,-1,0))
                limb(m,'Thigh_'+side,'Shin_'+side,n,target,pole)
                m[n]=foot
                shoulder=m['UpperArm_'+side].translation.copy()
                arm=around(shoulder,math.radians(lag*.5),'X')
                for prefix in ('UpperArm_','Forearm_','Hand_'):m[prefix+side]=arm@m[prefix+side]
                if not seated:
                    # Support-side hand rests beside the waist; opposite arm remains loose.
                    hand_weight=max(0,shift(max(0,t-.06))*(1 if side=='L' else -1))
                    hand_weight*=1-ease((t-12.1)/.9)
                    current=m['Hand_'+side].translation.copy()
                    waist=m['Spine'].translation+Vector((side_sign*.235,-.105,.045))
                    target=current.lerp(waist,hand_weight)
                    wrist=m['Hand_'+side].copy()
                    old_pole=m['Forearm_'+side].translation-shoulder
                    pole=old_pole.normalized().lerp(Vector((side_sign,.35,-.2)).normalized(),hand_weight)
                    limb(m,'UpperArm_'+side,'Forearm_'+side,'Hand_'+side,target,pole)
                    wrist.translation=target;m['Hand_'+side]=wrist
            if sample in (0,duration*120):m={n:v.copy() for n,v in rest.items()}
            assign(m);key(sample/4+1)
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for c in bag.fcurves:
                            for p in c.keyframe_points:p.interpolation='LINEAR'
        print('COMMON_FIDGET',name,duration,'seconds; heel pivots',heels,flush=True)
        if not seated:print('CONTRAPPOSTO_SHOE_LENGTHS',shoe_lengths,'max reach error',max_reach,flush=True)
    if max_reach>.0001:raise RuntimeError(('Fidget leg reach exceeded',max_reach))

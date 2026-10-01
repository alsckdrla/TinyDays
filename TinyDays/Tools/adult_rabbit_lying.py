"""Additive supine transitions. Metres, -Y forward, Z up; no mesh/rig edits.

The seated/standing portions sample preserved authored clips. Reclining and
getting up have independent timing and hand paths, not a reversed clip.
"""
import bpy, math, json, numpy as np
from mathutils import Matrix, Vector
from pathlib import Path

CLIPS=[('Adult_Stand_To_Lie',3),('Adult_Lie_To_Stand',3.2),
       ('Adult_Sit_To_Lie',2),('Adult_Lie_To_Sit',2.2),('Adult_Lie_Hold',1)]

def ease(x):
    x=max(0,min(1,x));return x*x*x*(10+x*(-15+6*x))

def build(rig,base,assign,limb,key):
    scene=bpy.context.scene
    def sample(name,t):
        rig.animation_data.action=bpy.data.actions[name]
        f=1+t*30;scene.frame_set(int(f),subframe=f%1);bpy.context.view_layer.update()
        return {b.name:b.matrix.copy() for b in rig.pose.bones}
    seated=sample('Adult_Breathe_Sit',0)
    standing=sample('Adult_Breathe_Stand',0)
    inv={n:b.matrix_local.inverted() for n,b in rig.data.bones.items()}
    upper=[b.name for b in rig.data.bones if b.name=='Spine' or any(a.name=='Spine' for a in b.parent_recursive)]
    heads=['Head','Ear_L','Ear_R']
    def turn(m,names,pivot,angle):
        q=Matrix.Translation(pivot)@Matrix.Rotation(angle,4,'X')@Matrix.Translation(-pivot)
        for n in names:m[n]=q@m[n]
    def points(obj,bone):
        o=bpy.data.objects[obj];g=o.vertex_groups[bone].index
        return [v.co.copy() for v in o.data.vertices if any(w.group==g and w.weight>.99 for w in v.groups)]
    shoes={s:points('Shoes','Foot_'+s) for s in ('L','R')}
    hands={s:points('BodyHands','Hand_'+s) for s in ('L','R')}
    torso=bpy.data.objects['BodyTorso']
    torso_points=np.array([tuple(v.co)+(1,) for v in torso.data.vertices])
    torso_weights={}
    for v in torso.data.vertices:
        for w in v.groups:
            n=torso.vertex_groups[w.group].name
            if n not in torso_weights:torso_weights[n]=np.zeros(len(torso.data.vertices))
            torso_weights[n][v.index]=w.weight
    def torso_floor(m):
        return min(sum((torso_points@np.array(m[n]@inv[n]).T)[:,2]*weights for n,weights in torso_weights.items()))
    worst=[0,None]
    def solve(m,a,b,c,target,pole):
        error=(target-m[a].translation).length-rig.data.bones[a].length-rig.data.bones[b].length
        if error>worst[0]:worst[:]=[error,(a,tuple(target),tuple(m[a].translation))]
        limb(m,a,b,c,target,pole)
    def blend(a,b,w):
        # Hierarchical rotation interpolation preserves each limb's local
        # segment length while overlapping the seated and reclining phases.
        out={}
        for bone in rig.data.bones:
            n=bone.name;parent=bone.parent.name if bone.parent else None
            x=a[parent].inverted()@a[n] if parent else a[n]
            y=b[parent].inverted()@b[n] if parent else b[n]
            xl,xr,xs=x.decompose();yl,yr,ys=y.decompose()
            pos=xl.lerp(yl,w)
            if xl.length>1e-6 and abs(xl.length-yl.length)<.0001:
                pos=pos.normalized()*((1-w)*xl.length+w*yl.length)
            local=Matrix.LocRotScale(pos,xr.slerp(yr,w),xs.lerp(ys,w))
            out[n]=out[parent]@local if parent else local
        return out
    def recline(u,rising=False):
        # Pelvis settles, spine rolls back and head follows the chest. Each
        # channel spans the action rather than stopping at each storyboard pose.
        p=ease(u)
        body=ease((u-(.03 if rising else .08))/(.90 if rising else .80))
        head=ease((u-(.24 if rising else .14))/(.72 if rising else .82))
        m={n:v.copy() for n,v in base.items()}
        pelvis=Vector((0,.055,.0735)).lerp(Vector((0,.055,.172)),ease((u-.35)/.60))
        # A broad, pre-authored rolling-support envelope clears the buttock as
        # its surface rotates onto the floor. Never clamp height frame by frame.
        pelvis.z+=.065*math.sin(math.pi*u)**2
        pa=math.radians(-80)*p
        transform=Matrix.Translation(pelvis)@Matrix.Rotation(pa,4,'X')@Matrix.Translation(-base['Pelvis'].translation)
        for n in m:
            if n!='Root':m[n]=transform@m[n]
        lean=.04*(1-body)+math.radians(-78.15)*body
        turn(m,upper,m['Spine'].translation.copy(),lean-pa)
        head_angle=.008*(1-head)+math.radians(-91)*head
        turn(m,heads,m['Head'].translation.copy(),head_angle-lean)
        # Preserve the pelvis-relative tail attachment; floor overlap is allowed.
        m['Tail']=m['Pelvis']@base['Pelvis'].inverted()@base['Tail']
        for side,sign in [('L',1),('R',-1)]:
            pitch=-.48+(math.radians(-80)+.48)*ease((u-.08)/.75)
            rot=Matrix.Rotation(pitch,4,'X')
            fm=rot@base['Foot_'+side];fm.translation=Vector()
            z=.0045-min((fm@inv['Foot_'+side]@v).z for v in shoes[side])
            dest=Vector((sign*.16,-.335+.015*p,z))
            delta=dest-m['Thigh_'+side].translation
            solve(m,'Thigh_'+side,'Shin_'+side,'Foot_'+side,dest,(0,delta.z,-delta.y))
            fm.translation=dest;m['Foot_'+side]=fm
            # Palm to floor, elbow-supported recline, then fold onto abdomen.
            # Reverse transition uses a longer push before releasing support.
            reach=ease((u-.15)/(.50 if not rising else .52))*(1-ease((u-(.72 if not rising else .74))/(.28 if not rising else .26)))
            support=Vector((sign*.29,.23,.0))
            handrot=Matrix.Rotation(math.radians(-90)*ease((u-.72)/.28),4,'X')@base['Hand_'+side]
            handrot.translation=Vector()
            floorz=.002-min((handrot@inv['Hand_'+side]@v).z for v in hands[side])
            support.z=floorz
            belly=Vector((sign*.048,.28+(.025 if side=='L' else 0),.355+(.012 if side=='L' else 0)))
            start=seated['Hand_'+side].translation
            destination=start.lerp(belly,ease(u/.85)).lerp(support,reach)
            shoulder=m['UpperArm_'+side].translation
            pole=Vector((sign,0,-.25))
            solve(m,'UpperArm_'+side,'Forearm_'+side,'Hand_'+side,destination,pole)
            handrot.translation=destination;m['Hand_'+side]=handrot
        if u<=0:return {n:v.copy() for n,v in seated.items()}
        return m
    # Cache preserved samples before assigning newly-created actions.
    cached={}
    for name,duration in CLIPS:
        poses=[]
        for i in range(round(duration*120)+1):
            t=i/120
            if name=='Adult_Stand_To_Lie':
                a=sample('Adult_Sit_Down_Supported',min(1.6,t*1.6/1.5))
                b=recline(max(0,(t-1)/2))
                m=blend(a,b,ease((t-1)/.5)) if t<1.5 else b
                if t<.25:m=blend(standing,m,ease(t/.25))
            elif name=='Adult_Lie_To_Stand':
                a=recline(max(0,1-t/1.7),True)
                b=sample('Adult_Stand_Up_Supported',max(0,(t-1.2)*1.8/2))
                m=blend(a,b,ease((t-1.2)/.5)) if t>1.2 else a
                if t>2.95:m=blend(m,standing,ease((t-2.95)/.25))
            elif name=='Adult_Sit_To_Lie':m=recline(t/2)
            elif name=='Adult_Lie_To_Sit':m=recline(1-t/2.2,True)
            else:m=recline(1)
            poses.append(m)
        cached[name]=poses
    print('LYING_MAX_REACH',worst,flush=True)
    floor_worst=min((torso_floor(m),name,i/120) for name,poses in cached.items() for i,m in enumerate(poses))
    print('LYING_TORSO_FLOOR',floor_worst,flush=True)
    tail_points=np.array([tuple(v.co)+(1,) for v in bpy.data.objects['BodyTail'].data.vertices])
    tail_floor=min(float((tail_points@np.array(m['Tail']@inv['Tail']).T)[:,2].min()) for poses in cached.values() for m in poses)
    print('LYING_TAIL_FLOOR',tail_floor,flush=True)
    if floor_worst[0]<-.0005:raise RuntimeError('Anatomical body below floor (short tail ground overlap allowed)')
    for name,duration in CLIPS:
        old=bpy.data.actions.get(name)
        if old:bpy.data.actions.remove(old)
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        for i,m in enumerate(cached[name]):
            assign(m)
            if name!='Adult_Lie_Hold':
                p=i/(len(cached[name])-1)
                for bone,strength in [('Ear_L',.025),('Ear_R',.020),('NeckSocket',.018)]:
                    pb=rig.pose.bones[bone]
                    pb.rotation_quaternion=pb.rotation_quaternion@Matrix.Rotation(strength*math.sin(2*math.pi*p)*math.sin(math.pi*p)**2,4,'X').to_quaternion()
            key(i/4+1)
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for curve in bag.fcurves:
                            for point in curve.keyframe_points:point.interpolation='LINEAR'
    # Fail explicitly instead of stretching an arm/leg to an impossible target.
    if worst[0]>.0005:raise RuntimeError(('Supine target outside limb reach',worst))

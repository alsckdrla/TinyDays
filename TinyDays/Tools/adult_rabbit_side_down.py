"""Direct left-side entries. Anatomical left is legacy R; no reversed clips.

Reuse a few proven anatomical support poses, not their animation curves. The
descent timing, hand paths and contact windows are authored independently.
"""
import bpy, math, numpy as np
from mathutils import Matrix, Vector, Quaternion
from adult_rabbit_side_rise import ease

def spline(times,values,t):
    """Shape-preserving Hermite: no unrequested dip between support poses."""
    values=np.asarray(values);i=max(0,min(len(times)-2,int(np.searchsorted(times,t)-1)))
    h=np.diff(times);slopes=np.diff(values,axis=0)/h[:,None];tangents=[np.zeros_like(values[0])]
    for j in range(1,len(times)-1):
        a,b=slopes[j-1],slopes[j];same=a*b>0;w1=2*h[j]+h[j-1];w2=h[j]+2*h[j-1]
        d=np.zeros_like(a);d[same]=(w1+w2)/(w1/a[same]+w2/b[same]);tangents.append(d)
    tangents.append(np.zeros_like(values[-1]));u=max(0,min(1,(t-times[i])/h[i]))
    return (2*u**3-3*u*u+1)*values[i]+(u**3-2*u*u+u)*h[i]*tangents[i]+(-2*u**3+3*u*u)*values[i+1]+(u**3-u*u)*h[i]*tangents[i+1]

CLIPS=[('Adult_Stand_To_Left',3.2,False),('Adult_Sit_To_Left',2.4,False)]

def build(rig,base,assign,limb,key):
    scene=bpy.context.scene; inv={n:m.inverted() for n,m in base.items()}
    def sample(name,t=0):
        rig.animation_data.action=bpy.data.actions[name]
        scene.frame_set(1+int(t*30),subframe=(t*30)%1);bpy.context.view_layer.update()
        return {b.name:b.matrix.copy() for b in rig.pose.bones}
    stand=sample('Adult_Breathe_Stand');sit=sample('Adult_Breathe_Sit');side=sample('Adult_Breathe_Left')
    kneel=sample('Adult_Left_To_Stand',1.7)
    support=sample('Adult_Left_To_Sit',1.35)
    elbow=sample('Adult_Left_To_Sit',.55)
    def copy(m):return {n:v.copy() for n,v in m.items()}
    def rotate(m,n,degrees,axis='X'):
        p=m[n].translation;delta=Matrix.Translation(p)@Matrix.Rotation(math.radians(degrees),4,axis)@Matrix.Translation(-p)
        for b in [rig.data.bones[n]]+list(rig.data.bones[n].children_recursive):m[b.name]=delta@m[b.name]
    def move(m,d):
        for n in m:
            if n!='Root':m[n].translation+=d
    def solve(m,a,b,c,target,pole):
        old={n:m[n].copy() for n in (a,b,c)};limb(m,a,b,c,target,pole)
        for n,end in ((a,b),(b,c)):
            q=old[n].to_quaternion();direction=(m[end].translation-m[n].translation).normalized()
            result=((q@Vector((0,1,0))).rotation_difference(direction)@q).to_matrix().to_4x4()
            result.translation=m[n].translation;m[n]=result
        m[c]=old[c];m[c].translation=target
    geometry={};rigid={}
    for ob in bpy.data.objects:
        if ob.type!='MESH':continue
        pts=np.array([tuple(v.co)+(1,) for v in ob.data.vertices]);weights={}
        for v in ob.data.vertices:
            for g in v.groups:
                n=ob.vertex_groups[g.group].name
                if n not in weights:weights[n]=np.zeros(len(pts))
                weights[n][v.index]=g.weight
        geometry[ob.name]=(pts,weights)
        for n,w in weights.items():rigid[ob.name,n]=[Vector(p[:3]) for p in pts[w>.99]]
    def floor(m,name):
        pts,weights=geometry[name]
        return float(sum((pts@np.array(m[n]@inv[n]).T)[:,2]*w for n,w in weights.items()).min())
    # Lowering and support are distinct from rising: chest opens toward the
    # destination, the free right arm reaches across before the left forearm.
    left_anchor=support['Hand_R'].copy()
    rotate(support,'Spine',-7);rotate(support,'Head',4)
    solve(support,'UpperArm_R','Forearm_R','Hand_R',left_anchor.translation,(-1,0,.2));support['Hand_R']=left_anchor
    # The right palm assists the descent, then releases as the left forearm
    # takes the load. Position it within the real arm's reach on this body.
    right_support=copy(support)
    rotate(right_support,'Spine',18,'Y');rotate(right_support,'Spine',8);rotate(right_support,'Head',-5)
    rot=base['Hand_L'].copy();rot.translation=Vector()
    shoulder=right_support['UpperArm_L'].translation
    p=Vector((shoulder.x,shoulder.y,.002-min((rot@inv['Hand_L']@v).z for v in rigid['BodyHands','Hand_L'])))
    solve(right_support,'UpperArm_L','Forearm_L','Hand_L',p,(1,-.2,.1));right_support['Hand_L']=rot;right_support['Hand_L'].translation=p
    rotate(elbow,'Head',4,'Y')
    prepare=copy(stand);move(prepare,Vector((.012,0,-.045)))
    rotate(prepare,'Spine',7);rotate(prepare,'Head',-3)
    for s in ('L','R'):solve(prepare,'Thigh_'+s,'Shin_'+s,'Foot_'+s,stand['Foot_'+s].translation,(0,-1,.3))
    # Whole-action interpolation carries velocity through intermediate poses.
    def authored(poses,times,t):
        m={}
        for b in rig.data.bones:
            n=b.name;p=b.parent.name if b.parent else None
            local=[v[p].inverted()@v[n] if p else v[n] for v in poses]
            coords=[np.array(v.translation) for v in local];q0=local[0].to_quaternion();angles=[]
            for v in local:
                q=q0.inverted()@v.to_quaternion()
                if q.w<0:q.negate()
                axis,angle=q.to_axis_angle();angles.append(np.array(axis)*angle)
            r=Vector(spline(times,angles,t));q=q0@Quaternion(r.normalized() if r.length>1e-8 else Vector((1,0,0)),r.length)
            pos=Vector(spline(times,coords,t));lengths=[Vector(v).length for v in coords]
            if max(lengths)-min(lengths)<.0001 and lengths[0]>.0001:pos=pos.normalized()*lengths[0]
            v=Matrix.LocRotScale(pos,q,Vector((1,1,1)));m[n]=m[p]@v if p else v
        m['Tail']=m['Pelvis']@inv['Pelvis']@base['Tail'];return m
    specs=[([stand,prepare,kneel,right_support,elbow,side],[0,.35,1.05,1.85,2.45,3.2]),
           ([sit,support,elbow,side],[0,.55,1.35,2.4])]
    report={}
    for (name,duration,_),(poses,times) in zip(CLIPS,specs):
        values=[authored(poses,times,i/240) for i in range(round(duration*240)+1)]
        # Carry the chest sideways while the hands exchange load. Otherwise
        # releasing a forward brace briefly straightens the spine and lifts the
        # head before the roll catches up. Rotate the torso, never translate or
        # stretch the head/neck independently.
        for i,m in enumerate(values):
            t=i/240;start,peak,end=(1.7,2.12,2.8) if duration>3 else (.5,.9,1.75)
            weight=ease((t-start)/(peak-start))*(1-ease((t-peak)/(end-peak)))
            rotate(m,'Spine',(-26 if duration>3 else -16)*weight,'Y')
        poles={}
        for suffix in ('L','R'):
            directions=[]
            for pose in poses:
                h=pose['Thigh_'+suffix].translation;axis=(pose['Foot_'+suffix].translation-h).normalized()
                v=pose['Shin_'+suffix].translation-h;v-=axis*v.dot(axis)
                directions.append(np.array(v.normalized()))
            poles[suffix]=directions
        # Keep the right foot planted while kneeling; the later support transfer
        # releases it. The left hand is planted during the seated side support.
        windows=[('Foot_L',.0,1.12,stand),('Foot_R',.0,.24,stand),('Hand_L',1.72,1.80,right_support)] if duration>3 else [('Hand_R',.49,.61,support)]
        for i,m in enumerate(values):
            t=i/240
            for s in ('L','R'):
                for a,b,c,obj in [('Thigh_','Shin_','Foot_','Shoes'),('UpperArm_','Forearm_','Hand_','BodyHands')]:
                    a+=s;b+=s;c+=s;target=Vector(spline(times,[np.array(p[c].translation) for p in poses],t))
                    for bone,start,end,anchor in windows:
                        if bone==c:
                            w=ease((t-start+.16)/.16)*(1-ease((t-end)/.2))
                            target=target.lerp(anchor[c].translation,w)
                            rot=m[c].to_quaternion().slerp(anchor[c].to_quaternion(),w)
                            m[c]=rot.to_matrix().to_4x4();m[c].translation=target
                    rotation=m[c].copy();rotation.translation=Vector()
                    sole=min((rotation@inv[c]@v).z for v in rigid[obj,c])
                    target.z=max(target.z,.0015-sole)
                    start=m[a].translation;limit=rig.data.bones[a].length+rig.data.bones[b].length-(.008 if c.startswith('Hand_') else .0003)
                    delta=target-start
                    # Projection keeps bone lengths; failed contacts are reported,
                    # never silently declared planted after reaching a timer.
                    if delta.length>limit:target=start+delta.normalized()*limit
                    pole=Vector(spline(times,poles[s],t)) if c.startswith('Foot_') else m[b].translation-start
                    if c.startswith('Hand_'):
                        weight=.85*ease(t/.3)*(1-ease((t-duration+.35)/.35))
                        pole=pole.normalized().lerp(Vector((1 if s=='L' else -1,-.2,.05)).normalized(),weight)
                    solve(m,a,b,c,target,pole)
        # A smooth precomputed clearance envelope, not a per-frame runtime snap.
        names=('Head','BodyTorso','BodyLegs','BodyHands','Shoes')
        need=np.array([max(0,.0015-min(floor(m,n) for n in names)) for m in values])
        lift=np.zeros(len(values));ts=np.arange(len(values))/240
        for i in np.where(need>0)[0]:
            bell=np.maximum(0,1-np.abs(ts-ts[i])/.28)
            lift=np.maximum(lift,(need[i]+.001)*(.5-.5*np.cos(np.pi*bell)))
        for _ in range(6):lift=np.convolve(np.pad(lift,(2,2),mode='edge'),np.array([1,4,6,4,1])/16,mode='valid')
        for i,m in enumerate(values):
            w=ease(i/48)*ease((len(values)-1-i)/48);move(m,Vector((0,0,float(lift[i])*w)))
            t=i/240
            if duration>3:
                w=ease((t-.7)/.3)*(1-ease((t-1.1)/.4))
                if w:
                    h=m['Thigh_R'].translation;k=Vector((-.123,-.06,.0435))
                    l1=rig.data.bones['Thigh_R'].length;l2=rig.data.bones['Shin_R'].length
                    foot=m['Foot_R'].copy();move(m,(k+(h-k).normalized()*l1-h)*w)
                    target=foot.translation.lerp(Vector((k.x,k.y+math.sqrt(l2*l2-(.1295-k.z)**2),.1295)),w)
                    pole=(m['Shin_R'].translation-m['Thigh_R'].translation).lerp(k-m['Thigh_R'].translation,w)
                    solve(m,'Thigh_R','Shin_R','Foot_R',target,pole)
                    foot.translation=target;m['Foot_R']=foot
            for bone,start,end,anchor in windows:
                w=ease((t-start+.16)/.16)*(1-ease((t-end)/.2))
                if not w and not (bone.startswith('Hand_') and end<t<end+.37):continue
                s=bone[-1];hand=bone.startswith('Hand_');a=('UpperArm_' if hand else 'Thigh_')+s;b=('Forearm_' if hand else 'Shin_')+s
                target=m[bone].translation.lerp(anchor[bone].translation,w)
                q=m[bone].to_quaternion().slerp(anchor[bone].to_quaternion(),w)
                if hand:
                    # Detach vertically before sweeping the palm sideways.
                    # This gives 30fps import interpolation real clearance too.
                    release=max(0,t-end)
                    target.z+=.025*ease(release/.07)*(1-ease((release-.12)/.25))
                delta=target-m[a].translation;limit=rig.data.bones[a].length+rig.data.bones[b].length-(.008 if hand else .0002)
                if delta.length>limit:
                    if not hand:
                        # Lower the pelvis along the reachable sphere while
                        # preserving the planted foot; do not lengthen the leg.
                        move(m,delta.normalized()*(delta.length-limit))
                    elif w<.999:target=m[a].translation+delta.normalized()*limit
                solve(m,a,b,bone,target,m[b].translation-m[a].translation)
                m[bone]=q.to_matrix().to_4x4();m[bone].translation=target
            m['Tail']=m['Pelvis']@inv['Pelvis']@base['Tail']
        values[0]=copy(poses[0]);values[-1]=copy(side)
        minimum=min((floor(m,n),i/240,n) for i,m in enumerate(values) for n in names)
        report[name]={'floor':minimum,'clearanceLift':float(max(lift))}
        print('SIDE_DOWN_GEOMETRY',name,report[name],flush=True)
        old=bpy.data.actions.get(name)
        if old:bpy.data.actions.remove(old)
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action;previous={}
        for i,m in enumerate(values):
            assign(m)
            for b in rig.pose.bones:
                q=b.rotation_quaternion.copy()
                if b.name in previous and previous[b.name].dot(q)<0:q.negate();b.rotation_quaternion=q
                previous[b.name]=q
            key(1+i/8)
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for curve in bag.fcurves:
                            for p in curve.keyframe_points:p.interpolation='LINEAR'
    return report

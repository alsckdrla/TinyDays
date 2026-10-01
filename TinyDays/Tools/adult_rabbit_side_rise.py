"""Additive direct side returns, -Y forward/Z up. Anatomical left = legacy R."""
import bpy, math, numpy as np
from mathutils import Matrix, Vector, Quaternion
CLIPS=[('Adult_Left_To_Sit',2.4,False),('Adult_Left_To_Stand',3.2,False)]
# Contact targets are body-specific; timing remains shared by the body family.
SUPPORT={'right_palm':(.24,-.12,.075),'left_palm':(-.24,-.12,.075),
         'kneel_pelvis':(.012,.12,.24)}
def ease(x):
    x=max(0,min(1,x));return x*x*x*(10+x*(-15+6*x))
def spline(times,values,t):
    i=max(0,min(len(times)-2,int(np.searchsorted(times,t)-1)));a,b=times[i:i+2];u=max(0,min(1,(t-a)/(b-a)))
    slopes=[]
    for j in (i,i+1):
        slopes.append(np.zeros_like(values[j]) if j in (0,len(times)-1) else (values[j+1]-values[j-1])/(times[j+1]-times[j-1]))
    return (2*u**3-3*u*u+1)*values[i]+(u**3-2*u*u+u)*(b-a)*slopes[0]+(-2*u**3+3*u*u)*values[i+1]+(u**3-u*u)*(b-a)*slopes[1]
def build(rig,base,assign,limb,key):
    scene=bpy.context.scene;inv={n:m.inverted() for n,m in base.items()}
    def sample(name,t=0):
        rig.animation_data.action=bpy.data.actions[name];f=1+t*30;scene.frame_set(int(f),subframe=f%1);bpy.context.view_layer.update()
        return {b.name:b.matrix.copy() for b in rig.pose.bones}
    side=sample('Adult_Breathe_Left');sit=sample('Adult_Breathe_Sit');stand=sample('Adult_Breathe_Stand')
    descendants={b.name:[b.name]+[c.name for c in b.children_recursive] for b in rig.data.bones}
    def rotate(m,n,angle,axis='X'):
        p=m[n].translation.copy();q=Matrix.Translation(p)@Matrix.Rotation(math.radians(angle),4,axis)@Matrix.Translation(-p)
        for child in descendants[n]:m[child]=q@m[child]
    def translate(m,delta):
        for n in m:
            if n!='Root':m[n].translation+=delta
    def blend(a,b,w):
        out={}
        for bone in rig.data.bones:
            n=bone.name;p=bone.parent.name if bone.parent else None
            x=a[p].inverted()@a[n] if p else a[n];y=b[p].inverted()@b[n] if p else b[n]
            xl,xr,xs=x.decompose();yl,yr,ys=y.decompose();v=xl.lerp(yl,w)
            if abs(xl.length-yl.length)<.0001 and xl.length>.0001:v=v.normalized()*xl.length
            local=Matrix.LocRotScale(v,xr.slerp(yr,w),Vector((1,1,1)));out[n]=out[p]@local if p else local
        return out
    rigid={}
    geometry={}
    for ob in bpy.data.objects:
        if ob.type!='MESH':continue
        weights={};pts=np.array([tuple(v.co)+(1,) for v in ob.data.vertices])
        for v in ob.data.vertices:
            for w in v.groups:
                n=ob.vertex_groups[w.group].name
                if n not in weights:weights[n]=np.zeros(len(pts))
                weights[n][v.index]=w.weight
        geometry[ob.name]=(pts,weights)
        for n in weights:
            indices=np.where(weights[n]>.99)[0]
            if len(indices):rigid[(ob.name,n)]=[Vector(pts[i][:3]) for i in indices]
    def floor(m,name):
        pts,w=geometry[name];return float(sum((pts@np.array(m[n]@inv[n]).T)[:,2]*v for n,v in w.items()).min())
    def contact(m,obj,bone):
        points=rigid[(obj,bone)];return min(points,key=lambda v:(m[bone]@inv[bone]@v).z)
    def solve(m,a,b,c,target,pole):
        # Preserve authored axial roll. Rebuilding from the bind direction has
        # an antipodal singularity when a folded shin points back/upward.
        prior={n:m[n].copy() for n in (a,b)}
        limb(m,a,b,c,target,pole)
        for n,end in ((a,b),(b,c)):
            direction=(m[end].translation-m[n].translation).normalized()
            original=prior[n].to_quaternion()
            swing=(original@Vector((0,1,0))).rotation_difference(direction)
            result=(swing@original).to_matrix().to_4x4();result.translation=m[n].translation;m[n]=result
    def palm(suffix,target):
        bone='Hand_'+suffix;rotation=base[bone].copy();rotation.translation=Vector()
        target=Vector(target);target.z=.002-min((rotation@inv[bone]@v).z for v in rigid[('BodyHands',bone)])
        return target,rotation
    kneel={n:v.copy() for n,v in stand.items()};translate(kneel,Vector(SUPPORT['kneel_pelvis'])-kneel['Pelvis'].translation)
    rotate(kneel,'Spine',35);rotate(kneel,'Head',-20)
    # Left knee is below its hip, foot folded behind. Lengths derive from bind.
    h=kneel['Thigh_R'].translation;l1=rig.data.bones['Thigh_R'].length;l2=rig.data.bones['Shin_R'].length
    knee=Vector((h.x,h.y-math.sqrt(l1*l1-(h.z-.099)**2),.099))
    foot=Vector((h.x,knee.y+math.sqrt(l2*l2-(.1295-knee.z)**2),.1295))
    solve(kneel,'Thigh_R','Shin_R','Foot_R',foot,knee-h)
    kneel['Foot_R']=stand['Foot_R'].copy();kneel['Foot_R'].translation=foot
    solve(kneel,'Thigh_L','Shin_L','Foot_L',stand['Foot_L'].translation,(0,-1,.3))
    kneel['Foot_L']=stand['Foot_L'].copy()
    # Ground push ends before the high half-kneel: hands release as pelvis rises.
    # Leaned seated push is distinct from the kneeling rise.
    push={n:v.copy() for n,v in sit.items()};rotate(push,'Spine',55);rotate(push,'Head',-35)
    translate(push,Vector((-.035,.0,0)))
    for suffix in ('L','R'):
        target,rotation=palm(suffix,(.24 if suffix=='L' else -.24,-.29,.075))
        solve(push,'UpperArm_'+suffix,'Forearm_'+suffix,'Hand_'+suffix,target,(1 if suffix=='L' else -1,-.2,.1))
        push['Hand_'+suffix]=rotation;push['Hand_'+suffix].translation=target
    sit_push={n:v.copy() for n,v in sit.items()};rotate(sit_push,'Spine',27);rotate(sit_push,'Spine',-12,'Y');rotate(sit_push,'Head',-15)
    translate(sit_push,Vector((-.02,0,0)))
    for suffix in ('R',):
        target,rotation=palm(suffix,(.24 if suffix=='L' else -.24,-.04,.075))
        solve(sit_push,'UpperArm_'+suffix,'Forearm_'+suffix,'Hand_'+suffix,target,(1 if suffix=='L' else -1,-.2,.1))
        sit_push['Hand_'+suffix]=rotation;sit_push['Hand_'+suffix].translation=target
    lifted=blend(side,push,.18);rotate(lifted,'Head',-6,'Y')
    sit_lifted=blend(side,sit_push,.18);rotate(sit_lifted,'Head',-6,'Y')
    def authored(poses,times,t):
        m={}
        for bone in rig.data.bones:
            n=bone.name;p=bone.parent.name if bone.parent else None
            locals=[v[p].inverted()@v[n] if p else v[n] for v in poses]
            coords=[];rots=[];q0=locals[0].to_quaternion()
            for v in locals:
                coords.append(np.array(v.translation));q=q0.inverted()@v.to_quaternion()
                if q.w<0:q.negate()
                axis,angle=q.to_axis_angle();rots.append(np.array(axis)*angle)
            pos=Vector(spline(times,coords,t));r=Vector(spline(times,rots,t))
            q=q0@Quaternion(r.normalized() if r.length>1e-8 else Vector((1,0,0)),r.length)
            lengths=[Vector(v).length for v in coords]
            if max(lengths)-min(lengths)<.0001 and lengths[0]>.0001:pos=pos.normalized()*lengths[0]
            local=Matrix.LocRotScale(pos,q,Vector((1,1,1)));m[n]=m[p]@local if p else local
        m['Tail']=m['Pelvis']@inv['Pelvis']@base['Tail'];return m
    # Different timing/paths for sitting and kneeling rise; no reversed action.
    specs=[([side,sit_lifted,sit_push,sit],[0,.42,1.35,2.4]),
           ([side,lifted,push,kneel,stand],[0,.42,1.1,1.75,3.2])]
    cached={};report={}
    for (name,duration,loop),(poses,times) in zip(CLIPS,specs):
        # Blend normalized bending directions, not knee points that can pass
        # through the hip-to-ankle line and reverse the IK plane.
        poles={}
        for suffix in ('L','R'):
            directions=[]
            for pose in poses:
                hip=pose['Thigh_'+suffix].translation;axis=(pose['Foot_'+suffix].translation-hip).normalized()
                v=pose['Shin_'+suffix].translation-hip;v-=axis*v.dot(axis)
                directions.append(np.array(v.normalized()))
            poles[suffix]=directions
        values=[authored(poses,times,i/240) for i in range(round(duration*240)+1)]
        for i,m in enumerate(values):
            t=i/240
            for suffix in ('L','R'):
                bone='Foot_'+suffix;positions=[np.array(p[bone].translation) for p in poses]
                # Settle both seated feet before the body reaches its final pose.
                # The authored spline already has zero velocity at its endpoint.
                foot_t=(min(duration,t+.10*ease((t-1.8)/.5)) if name.endswith('Sit')
                        else min(duration,t+.30*ease((t-2.1)/.8)) if suffix=='L' else t)
                target=Vector(spline(times,positions,foot_t))
                gather=math.sin(math.pi*max(0,min(1,(t-1.1)/.65)))**2 if name.endswith('Stand') and suffix=='R' else 0
                target.z+=.045*gather;target.x-=.015*gather
                settle_lift=-.003*math.sin(math.pi*max(0,min(1,(t-1.3)/.7)))**2 if name.endswith('Stand') and suffix=='R' else 0
                target.z+=settle_lift
                rot=m[bone].copy();rot.translation=Vector()
                sole=min((rot@inv[bone]@v).z for v in rigid[('Shoes',bone)])
                # Visible soles travel above the ground; no hidden knee twist.
                target.z=max(target.z,.002-sole)
                hip=m['Thigh_'+suffix].translation;limit=rig.data.bones['Thigh_'+suffix].length+rig.data.bones['Shin_'+suffix].length-.001
                d=target-hip;horizontal=Vector((d.x,d.y,0));allowed=math.sqrt(max(0,limit*limit-d.z*d.z))
                if horizontal.length>allowed:
                    horizontal=horizontal.normalized()*allowed;target.x=hip.x+horizontal.x;target.y=hip.y+horizontal.y
                old=m[bone].copy();solve(m,'Thigh_'+suffix,'Shin_'+suffix,bone,target,
                    Vector(spline(times,poles[suffix],t)))
                old.translation=target;m[bone]=old
            for suffix in ('L','R'):
                bone='Hand_'+suffix;target=Vector(spline(times,[np.array(p[bone].translation) for p in poses],t))
                old=m[bone].copy();rot=old.copy();rot.translation=Vector()
                target.z=max(target.z,.002-min((rot@inv[bone]@v).z for v in rigid[('BodyHands',bone)]))
                elbow=Vector(spline(times,[np.array(p['Forearm_'+suffix].translation) for p in poses],t))
                shoulder=m['UpperArm_'+suffix].translation;limit=rig.data.bones['UpperArm_'+suffix].length+rig.data.bones['Forearm_'+suffix].length-.001
                target.z=max(target.z,shoulder.z-limit+.001)
                delta=target-shoulder;horizontal=Vector((delta.x,delta.y,0));allowed=math.sqrt(max(0,limit*limit-delta.z*delta.z))
                if horizontal.length>allowed:
                    horizontal=horizontal.normalized()*allowed;target.x=shoulder.x+horizontal.x;target.y=shoulder.y+horizontal.y
                solve(m,'UpperArm_'+suffix,'Forearm_'+suffix,bone,target,elbow-m['UpperArm_'+suffix].translation)
                old.translation=target;m[bone]=old
        # Offline body clearance is fitted once for the whole action, not clamped
        # during playback. Endpoints match the original references exactly.
        need=np.array([max(0,.0015-min(floor(m,n) for n in ('BodyTorso','Head','BodyLegs','BodyHands','Shoes'))) for m in values])
        wi=int(np.argmax(need));print('CLEARANCE_NEED',name,wi/240,float(need[wi]),{n:floor(values[wi],n) for n in ('BodyTorso','Head','BodyLegs','BodyHands','Shoes')},flush=True)
        # Smooth wide bell envelopes over any clearance deficit clusters.
        lift=np.zeros(len(values));dt=1/240
        for i in np.where(need>0)[0]:
            radius=.32;ts=np.arange(len(values))*dt-i*dt
            bump=np.where(abs(ts)<radius,.5+.5*np.cos(np.pi*np.minimum(abs(ts)/radius,1)),0)
            lift=np.maximum(lift,(need[i]+.001)*bump)
        # Smooth the envelope twice with a short binomial kernel.
        kernel=np.array([1,4,6,4,1])/16
        for _ in range(5):lift=np.convolve(np.pad(lift,(2,2),mode='edge'),kernel,mode='valid')
        if name.endswith('Stand'):lift*=1.925
        lift*=np.array([ease(i/60)*ease((len(values)-1-i)/60) for i in range(len(values))])
        for i,m in enumerate(values):
            t=i/240;feet={s:m['Foot_'+s].copy() for s in ('L','R')};translate(m,Vector((0,0,float(lift[i]))))
            # Reconcile the ground contacts after the authored body envelope.
            for suffix in ('L','R'):
                original=feet[suffix];k=m['Shin_'+suffix].translation
                hip=m['Thigh_'+suffix].translation;delta=original.translation-hip
                limit=rig.data.bones['Thigh_'+suffix].length+rig.data.bones['Shin_'+suffix].length-.001
                horizontal=Vector((delta.x,delta.y,0));allowed=math.sqrt(max(0,limit*limit-delta.z*delta.z))
                if horizontal.length>allowed:
                    v=horizontal.normalized()*allowed;original.translation=Vector((hip.x+v.x,hip.y+v.y,original.translation.z))
                solve(m,'Thigh_'+suffix,'Shin_'+suffix,'Foot_'+suffix,original.translation,k-m['Thigh_'+suffix].translation)
                m['Foot_'+suffix]=original
            if name.endswith('Stand'):
                support=ease((t-1.05)/.6)*(1-ease((t-1.75)/.15))
                if support:
                    h=m['Thigh_R'].translation;l1=rig.data.bones['Thigh_R'].length;l2=rig.data.bones['Shin_R'].length
                    # Fixed knee contact; pelvis follows the thigh-length sphere.
                    # Do not let a grounded knee slide with the rising pelvis.
                    knee_z=.044;k=Vector((-.123,-.06,knee_z))
                    fixed_front=m['Foot_L'].copy();fixed_back=m['Foot_R'].copy()
                    projected=k+(h-k).normalized()*l1
                    translate(m,(projected-h)*support);h=m['Thigh_R'].translation
                    solve(m,'Thigh_L','Shin_L','Foot_L',fixed_front.translation,m['Shin_L'].translation-m['Thigh_L'].translation)
                    m['Foot_L']=fixed_front
                    goal=Vector((k.x,k.y+math.sqrt(l2*l2-(.1295-knee_z)**2),.1295))
                    foot=fixed_back;target=foot.translation.lerp(goal,support)
                    current=m['Shin_R'].translation-h;pole=current.lerp(k-h,support)
                    solve(m,'Thigh_R','Shin_R','Foot_R',target,pole)
                    foot.translation=target;m['Foot_R']=foot
            cs,ce=(1.31,1.4) if name.endswith('Sit') else (1.07,1.14)
            cw=ease((t-(cs-.1))/.1)*(1-ease((t-ce)/.15))
            rotate(m,'Spine',3*cw);rotate(m,'Head',-2*cw)
            for suffix in ('L','R'):
                anchor=sit_push if name.endswith('Sit') else push
                if name.endswith('Sit') and suffix=='L':continue
                start,end=(1.31,1.4) if name.endswith('Sit') else (1.07,1.14)
                w=ease((t-(start-.12))/.12)*(1-ease((t-end)/.18))
                if w:
                    bone='Hand_'+suffix;old=m[bone].copy();target=old.translation.lerp(anchor[bone].translation,w)
                    rot=old.to_quaternion().slerp(anchor[bone].to_quaternion(),w);rm=rot.to_matrix().to_4x4()
                    target.z=max(target.z,.002-min((rm@inv[bone]@v).z for v in rigid[('BodyHands',bone)]))
                    shoulder=m['UpperArm_'+suffix].translation
                    limit=rig.data.bones['UpperArm_'+suffix].length+rig.data.bones['Forearm_'+suffix].length-.001
                    delta=target-shoulder;horizontal=Vector((delta.x,delta.y,0));allowed=math.sqrt(max(0,limit*limit-delta.z*delta.z))
                    if horizontal.length>allowed:
                        horizontal=horizontal.normalized()*allowed;target.x=shoulder.x+horizontal.x;target.y=shoulder.y+horizontal.y
                    solve(m,'UpperArm_'+suffix,'Forearm_'+suffix,bone,target,m['Forearm_'+suffix].translation-m['UpperArm_'+suffix].translation)
                    m[bone]=rm;m[bone].translation=target
        endpoint=sit if name.endswith('Sit') else stand
        for i,m in enumerate(values):
            w=ease((i/240-(duration-.3))/.3)
            if w:values[i]=blend(m,endpoint,w)
        values[0]={n:v.copy() for n,v in side.items()};values[-1]={n:v.copy() for n,v in endpoint.items()}
        minimum=min((floor(m,n),i/240,n) for i,m in enumerate(values) for n in ('BodyTorso','Head','BodyLegs','BodyHands','Shoes'))
        head_step=max((values[i]['Head'].translation-values[i-1]['Head'].translation).length for i in range(1,len(values)))
        print('DIRECT_FLOOR',name,minimum,'MAX_LIFT',float(max(lift)),flush=True)
        print('DIRECT_HEAD_STEP',name,head_step,flush=True)
        if minimum[0]<-.0005:raise RuntimeError(('Direct return floor',minimum))
        cached[name]=values;report[name]={'maxLift':float(max(lift)),'floor':minimum}
    for name,duration,loop in CLIPS:
        old=bpy.data.actions.get(name)
        if old:bpy.data.actions.remove(old)
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        previous={}
        for i,m in enumerate(cached[name]):
            assign(m)
            for bone in rig.pose.bones:
                q=bone.rotation_quaternion.copy()
                if bone.name in previous and previous[bone.name].dot(q)<0:q.negate();bone.rotation_quaternion=q
                previous[bone.name]=q
            key(1+i/8)
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for curve in bag.fcurves:
                            for point in curve.keyframe_points:point.interpolation='LINEAR'
    return report

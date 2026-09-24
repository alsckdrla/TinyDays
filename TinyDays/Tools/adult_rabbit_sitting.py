"""Non-looping floor transitions; source geometry and AdultStandard_v2 stay intact."""
import bpy, math
from mathutils import Vector, Matrix

def build(rig, base, assign, limb, key):
    scene=bpy.context.scene
    # Phase, pelvis height/back shift, spine lean, ankle fore/aft, toe pitch,
    # hand lateral/fore-aft/height. Blender forward is -Y.
    stand=(.53,0,0,-.02,0,.348,-.065,.665)
    seated=(.0715,.055,.04,-.335,-.48,.24,-.18,.35)
    # Independent seconds-based tracks: intermediate poses are pass-throughs.
    # Flat tracks below represent intentional contacts, not synchronized stops.
    down=[[(0,.53),(.12,.53),(1.1,.0715),(1.6,.0715)],
          [(0,0),(.15,0),(.95,.055),(1.6,.055)],
          [(0,0),(.18,.035),(.90,.54),(1.12,.50),(1.6,.04)],
          [(0,-.02),(.65,-.02),(1.35,-.335),(1.6,-.335)],
          [(0,0),(.65,0),(1.35,-.48),(1.6,-.48)],
          [(0,.348),(.25,.32),(1.02,.24),(1.14,.24),(1.6,.24)],
          [(0,-.065),(.3,-.23),(1.02,-.10),(1.14,-.10),(1.6,-.18)],
          [(0,.665),(.2,.70),(1.02,.143),(1.14,.143),(1.6,.35)]]
    up=[[(0,.0715),(.08,.0715),(.30,.082),(.55,.085),(1.5,.53),(1.8,.53)],
        [(0,.055),(.4,.10),(.65,.10),(1.5,0),(1.8,0)],
        [(0,.04),(.40,.60),(.55,.58),(1.5,.02),(1.8,0)],
        [(0,-.335),(.25,-.335),(.65,-.02),(1.8,-.02)],
        [(0,-.48),(.25,-.48),(.65,0),(1.8,0)],
        [(0,.24),(.35,.24),(.55,.24),(1.55,.348),(1.8,.348)],
        [(0,-.18),(.35,-.10),(.55,-.10),(1.55,-.065),(1.8,-.065)],
        [(0,.35),(.35,.143),(.55,.143),(.9,.42),(1.4,.665),(1.8,.665)]]
    shoe=bpy.data.objects['Shoes']
    points={s:[v.co-base['Foot_'+s].translation for v in shoe.data.vertices
               if any(g.group==shoe.vertex_groups['Foot_'+s].index and g.weight>.99 for g in v.groups)] for s in ('L','R')}
    def smooth(t):return t*t*t*(10+t*(-15+6*t))
    def track(knots,time):
        def acceleration(i):
            if i==0 or i==len(knots)-1:return 0
            a,x=knots[i-1];b,y=knots[i];c,z=knots[i+1]
            left=(y-x)/(b-a);right=(z-y)/(c-b)
            # True holds have zero boundary acceleration. Moving channels share
            # their estimated acceleration instead of resetting it at every key.
            if abs(left)<1e-9 or abs(right)<1e-9:return 0
            return 2*(right-left)/(c-a)
        def slope(i):
            if i==0 or i==len(knots)-1:return 0
            a,x=knots[i-1];b,y=knots[i];c,z=knots[i+1]
            left=(y-x)/(b-a);right=(z-y)/(c-b)
            return 2*left*right/(left+right) if left*right>0 else 0
        if time<=knots[0][0]:return knots[0][1]
        for i,((a,x),(b,y)) in enumerate(zip(knots,knots[1:])):
            if time<=b:
                t=(time-a)/(b-a);q=smooth(t)
                # Quintic Hermite with shared velocity AND acceleration.
                return x+(y-x)*q+(b-a)*(slope(i)*(t-6*t**3+8*t**4-3*t**5)+slope(i+1)*(-4*t**3+7*t**4-3*t**5))+(b-a)**2*(acceleration(i)*(.5*t**2-1.5*t**3+1.5*t**4-.5*t**5)+acceleration(i+1)*(.5*t**3-t**4+.5*t**5))
        return knots[-1][1]
    def params(tracks,time):return [track(k,time) for k in tracks]
    rig.animation_data.action=bpy.data.actions['Adult_Idle_Biped'];scene.frame_set(1);bpy.context.view_layer.update()
    idle={b.name:b.matrix_basis.copy() for b in rig.pose.bones}
    max_reach=0;worst=None
    for name,frames,knots in [('Adult_Sit_Down',48,down),('Adult_Stand_Up',54,up),('Adult_Sit_Hold',30,[[(0,v),(1,v)] for v in seated])]:
        old=bpy.data.actions.get(name)
        if old:bpy.data.actions.remove(old)
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        for sample in range(frames*4+1):
            p=sample/(frames*4);scene.frame_set(sample//4+1,subframe=(sample%4)/4)
            seconds=p*frames/30
            z,y,lean,fy,pitch,hx,hy,hz=params(knots,seconds)
            m={n:v.copy() for n,v in base.items()}
            for n in m:
                if n!='Root':m[n].translation+=Vector((0,y,z-.53))
            pivot=m['Spine'].translation.copy()
            upper=[b.name for b in rig.data.bones if b.name=='Spine' or any(a.name=='Spine' for a in b.parent_recursive)]
            transform=Matrix.Translation(pivot)@Matrix.Rotation(lean,4,'X')@Matrix.Translation(-pivot)
            for n in upper:m[n]=transform@m[n]
            delayed=params(knots,max(0,seconds-2/30))[2]
            follow=(delayed-lean)*smooth(min(1,(1-p)/.12))
            # Keep original endpoints but allow the head to participate in the
            # action instead of cancelling the same fraction at every instant.
            participation=math.sin(math.pi*p)**2
            counter=.8-.14*participation
            hp=m['Head'].translation.copy();turn=Matrix.Translation(hp)@Matrix.Rotation(-lean*counter+follow*.22,4,'X')@Matrix.Translation(-hp)
            for n in ('Head','Ear_L','Ear_R'):m[n]=turn@m[n]
            for side,sign in [('L',1),('R',-1)]:
                rotation=Matrix.Rotation(pitch,4,'X');bottom=min((rotation.to_3x3()@v).z for v in points[side])
                foot=Vector((sign*.16,fy,.0025-bottom))
                release=1.14 if name=='Adult_Sit_Down' else .55
                hand_time=seconds
                if name!='Adult_Sit_Hold' and side=='R' and seconds>release:
                    delay=1/30;end=frames/30
                    hand_time=release+max(0,seconds-release-delay)*(end-release)/(end-release-delay)
                shx,shy,shz=params(knots,hand_time)[5:]
                hand=Vector((sign*shx,shy,shz))
                if name=='Adult_Sit_Down' and .2<seconds<1.02:
                    u=(seconds-.2)/.82
                    hand.z+=.003*64*u**3*(1-u)**3
                if name!='Adult_Sit_Hold' and hand_time>release:
                    u=min(1,(hand_time-release)/(frames/30-release))
                    arc=64*u**3*(1-u)**3
                    hand+=Vector((sign*.012*arc,-.009*arc,.008*arc))
                for a,b,c,target,pole in [('Thigh_','Shin_','Foot_',foot,(0,-1,0)),('UpperArm_','Forearm_','Hand_',hand,(sign*.25,1,0))]:
                    if a=='Thigh_':
                        delta=target-m[a+side].translation
                        # Sagittal bend direction remains well-defined even when
                        # ankle and hip have the same height; no inward knee flip.
                        pole=(0,delta.z,-delta.y)
                    error=(target-m[a+side].translation).length-rig.data.bones[a+side].length-rig.data.bones[b+side].length
                    if error>max_reach:worst=(name,p,a,tuple(target),tuple(m[a+side].translation))
                    max_reach=max(max_reach,error)
                    limb(m,a+side,b+side,c+side,target,pole)
                m['Foot_'+side]=rotation@base['Foot_'+side];m['Foot_'+side].translation=foot
                # Keep hands rounded and directed down; actual palm contact is audited in Unity.
            # Match the existing idle endpoint exactly, with a gentle approach/release.
            blend=1-smooth(min(1,seconds/.2)) if name=='Adult_Sit_Down' else smooth(max(0,(seconds-1.55)/.25)) if name=='Adult_Stand_Up' else 0
            assign(m)
            lag=follow*(1-blend)
            for name_,strength in [('Ear_L',.35),('Ear_R',.30),('NeckSocket',.15),('BackSocket',.10)]:
                bone=rig.pose.bones[name_];bone.rotation_quaternion=bone.rotation_quaternion@Matrix.Rotation(lag*strength,4,'X').to_quaternion()
            bpy.context.view_layer.update()
            if blend:
                for bone in rig.pose.bones:
                    loc,rot,scale=bone.matrix_basis.decompose();il,ir,isc=idle[bone.name].decompose()
                    bone.matrix_basis=Matrix.LocRotScale(loc.lerp(il,blend),rot.slerp(ir,blend),scale.lerp(isc,blend))
                bpy.context.view_layer.update()
            # The inherited standing pose is just under the review floor by 1mm.
            # A constant 2mm clearance keeps the new transitions above it without
            # a frame-dependent pelvis clamp or changing existing locomotion.
            root=rig.pose.bones['Root'];matrix=root.matrix.copy();matrix.translation.z+=.002;root.matrix=matrix
            bpy.context.view_layer.update()
            key(sample/4+1)
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for curve in bag.fcurves:
                            for point in curve.keyframe_points:point.interpolation='LINEAR'
    print('SIT_MAX_UNREACHABLE_METERS',max_reach,worst,flush=True)
    if max_reach>.0005:raise RuntimeError('Sitting target exceeds original limb reach')

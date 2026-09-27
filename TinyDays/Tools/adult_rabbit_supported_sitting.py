"""Additive asymmetric variants; legacy actions are read, never edited.

Blender +X is anatomical left; forward is -Y. Contact windows are in seconds.
The right hand retains the legacy world-space palm support. The left hand
follows a knee-relative point, with a raised left knee during the low pose.
"""
import bpy, math
from mathutils import Vector, Matrix

def build(rig, base, assign, limb, key):
    scene=bpy.context.scene
    upper=[b.name for b in rig.data.bones if b.name=='Spine' or any(a.name=='Spine' for a in b.parent_recursive)]
    shoe=bpy.data.objects['Shoes']
    points={s:[v.co-base['Foot_'+s].translation for v in shoe.data.vertices
        if any(g.group==shoe.vertex_groups['Foot_'+s].index and g.weight>.99 for g in v.groups)] for s in ('L','R')}
    pants=bpy.data.objects['Bottom'];pants.data.calc_loop_triangles()
    triangles=[tuple(t.vertices) for t in pants.data.loop_triangles]
    influences=[[(pants.vertex_groups[g.group].name,g.weight) for g in v.groups if pants.vertex_groups[g.group].name in base] for v in pants.data.vertices]
    inverses={b.name:b.matrix_local.inverted() for b in rig.data.bones}
    hands=bpy.data.objects['BodyHands']
    palm_offset=min((v.co-base['Hand_L'].translation for v in hands.data.vertices if any(g.group==hands.vertex_groups['Hand_L'].index and g.weight>.99 for g in v.groups)),key=lambda v:v.z)
    def knee_surface(m,x,y):
        skin={n:m[n]@inverses[n] for n in m}
        vertices=[sum((skin[n]@v.co*w for n,w in weights),Vector()) for v,weights in zip(pants.data.vertices,influences)]
        heights=[]
        for ia,ib,ic in triangles:
            a,b,c=vertices[ia],vertices[ib],vertices[ic]
            den=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y)
            if abs(den)<1e-10:continue
            u=((b.y-c.y)*(x-c.x)+(c.x-b.x)*(y-c.y))/den
            v=((c.y-a.y)*(x-c.x)+(a.x-c.x)*(y-c.y))/den
            if u>=-.0001 and v>=-.0001 and u+v<=1.0001:heights.append(u*a.z+v*b.z+(1-u-v)*c.z)
        if not heights:raise RuntimeError('Knee palm target outside knee surface')
        return max(heights)
    def ease(t):
        t=max(0,min(1,t));return t*t*t*(10+t*(-15+6*t))
    def window(t,a,b,c,d):return ease((t-a)/(b-a))*(1-ease((t-c)/(d-c)))
    max_reach=0;worst=None
    for source,name,frames in [('Adult_Sit_Down','Adult_Sit_Down_Supported',48),('Adult_Stand_Up','Adult_Stand_Up_Supported',54)]:
        down=frames==48
        # Cache complete source matrices before assigning the new action.
        samples=[]
        rig.animation_data.action=bpy.data.actions[source]
        for i in range(frames*4+1):
            scene.frame_set(i//4+1,subframe=(i%4)/4);bpy.context.view_layer.update()
            samples.append({b.name:b.matrix.copy() for b in rig.pose.bones})
        old=bpy.data.actions.get(name)
        if old:bpy.data.actions.remove(old)
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        for i,original in enumerate(samples):
            t=i/120;m={n:v.copy() for n,v in original.items()}
            weight=window(t,.18,.65,1.12,1.6) if down else window(t,0,.32,.70,1.55)
            knee_weight=window(t,.48,.94,1.10,1.55) if down else window(t,.08,.35,.56,.78)
            touch=window(t,.50,.90,1.12,1.57) if down else window(t,.06,.30,.60,1.05)
            if weight>0 or touch>0:
                # Small lateral weight shift, chest roll and yaw. Feet and palm
                # remain independent targets rather than following this transform.
                for n in m:
                    if n!='Root':m[n].translation.x-=.014*weight
                pivot=m['Spine'].translation.copy()
                transform=Matrix.Translation(pivot)@Matrix.Rotation(-.14*weight,4,'Y')@Matrix.Rotation(.09*weight,4,'Z')@Matrix.Rotation(-.12*weight,4,'X')@Matrix.Translation(-pivot)
                for n in upper:m[n]=transform@m[n]
                for side,sign in [('L',1),('R',-1)]:
                    foot_matrix=original['Foot_'+side].copy();foot=foot_matrix.translation.copy()
                    if side=='L' and knee_weight>0:
                        flat=base['Foot_L'].copy();flat.translation=Vector((.16,-.24,.0045-min(v.z for v in points['L'])))
                        foot=foot.lerp(flat.translation,knee_weight)
                        foot_matrix=Matrix.LocRotScale(foot,foot_matrix.to_quaternion().slerp(flat.to_quaternion(),knee_weight),Vector((1,1,1)))
                        # Preserve clearance while intentionally changing ankle pitch.
                        rotation=foot_matrix.to_3x3()@base['Foot_L'].to_3x3().inverted()
                        foot.z=.0045-min((rotation@v).z for v in points['L']);foot_matrix.translation=foot
                    delta=foot-m['Thigh_'+side].translation
                    # The raised knee must bend forward even when ankle height
                    # exceeds hip height. Blend its pole before the low pose.
                    pole=Vector((0,delta.z,-delta.y))
                    if side=='L':pole=pole.lerp(Vector((0,-abs(delta.z),abs(delta.y))),knee_weight)
                    limb(m,'Thigh_'+side,'Shin_'+side,'Foot_'+side,foot,pole);m['Foot_'+side]=foot_matrix
                for side,sign in [('L',1),('R',-1)]:
                    hand=original['Hand_'+side].translation.copy()
                    if side=='L':
                        # Rounded palm bottom sits on the knee top. The target is
                        # relative to the moving knee, not frozen in world space.
                        target=m['Shin_L'].translation+Vector((0,-.025,.183))
                        if touch>0:
                            surface=knee_surface(m,target.x+palm_offset.x,target.y+palm_offset.y)
                            target.z=surface+.004-palm_offset.z
                        hand=hand.lerp(target,touch)
                        if down:hand.z+=.050*window(t,.18,.40,.60,.90)
                    error=(hand-m['UpperArm_'+side].translation).length-rig.data.bones['UpperArm_'+side].length-rig.data.bones['Forearm_'+side].length
                    if error>max_reach:max_reach=error;worst=(name,t,side)
                    pole=Vector((sign*.25,1,0))
                    if side=='L':pole=pole.lerp(Vector((.8,-.5,-.25)),touch)
                    limb(m,'UpperArm_'+side,'Forearm_'+side,'Hand_'+side,hand,pole)
                # All lengths are physical bone lengths, never scale compensation.
            assign(m);key(i/4+1)
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for curve in bag.fcurves:
                            for p in curve.keyframe_points:p.interpolation='LINEAR'
    print('SUPPORTED_SIT_MAX_UNREACHABLE_METERS',max_reach,worst,flush=True)
    if max_reach>.0005:raise RuntimeError('Supported sitting exceeds original arm reach')

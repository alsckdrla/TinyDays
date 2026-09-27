"""Seated hand-drawing gesture; actual rounded hand surface follows the floor.

No finger, scale or surface-mark animation. Existing actions are not edited.
"""
import bpy, math
from mathutils import Matrix, Vector
from adult_common_sigh import ease
from adult_common_fidgets import around

def envelope(t):
    return ease(t/1.2)*(1-ease((t-6.5)/1.5))

def path(t):
    # Continuous position/velocity/acceleration at circle/curve joins.
    if t<=3.2:
        u=ease((t-1.2)/2);a=2*math.pi*u
        return Vector((.025*math.sin(a),.025*(1-math.cos(a)),0))
    if t<=5.8:
        u=ease((t-3.2)/2.6);a=4*math.pi*u
        return Vector((.025*math.sin(a),.015*math.sin(2*a),0))
    u=ease((t-5.8)/.7)
    return Vector((.025*u,-.015*u,0))

def build(rig,assign,limb,key):
    scene=bpy.context.scene
    rig.animation_data.action=bpy.data.actions['Adult_Breathe_Sit'];scene.frame_set(1);bpy.context.view_layer.update()
    rest={b.name:b.matrix.copy() for b in rig.pose.bones}
    upper=[b.name for b in rig.data.bones if b.name=='Spine' or any(a.name=='Spine' for a in b.parent_recursive)]
    heads=[b.name for b in rig.data.bones if b.name=='Head' or any(a.name=='Head' for a in b.parent_recursive)]
    hand=bpy.data.objects['BodyHands'];n='Hand_R';g=hand.vertex_groups[n].index
    skin=rest[n]@rig.data.bones[n].matrix_local.inverted()
    points=[skin@v.co for v in hand.data.vertices if any(w.group==g and w.weight>.99 for w in v.groups)]
    tip=min(points,key=lambda p:p.z);offset=tip-rest[n].translation
    origin=Vector((-.33,-.15,.0015))
    action=bpy.data.actions.new('Adult_Fidget_Sandplay');action.use_fake_user=True;rig.animation_data.action=action
    reach_error=0
    for sample in range(961):
        t=sample/120;e=envelope(t);m={n:v.copy() for n,v in rest.items()}
        breathe=.5*math.sin(2*math.pi*t/4)*e
        turn=around(rest['Spine'].translation,math.radians(-18*e),'Y')@around(rest['Spine'].translation,math.radians(8*e+breathe),'X')
        for n in upper:m[n]=turn@m[n]
        # Watch the drawing first, then look left/right without moving the hand.
        look=ease((t-3.2)/.7)-2*ease((t-4.2)/1.1)+ease((t-5.4)/.6)
        pitch=16*e-10*(ease((t-3.2)/.7)-ease((t-5.4)/.6))
        watch=1-(ease((t-3.2)/.7)-ease((t-5.4)/.6))
        head=around(m['Head'].translation,math.radians(22*look-22*e*watch),'Z')@around(m['Head'].translation,math.radians(pitch),'X')
        for n in heads:m[n]=head@m[n]
        # A single small recovery arc clears the floor before returning to knee.
        target=rest['Hand_R'].translation.lerp(origin+path(min(t,6.5))-offset,e)
        target.z+=.035*math.sin(math.pi*ease((t-6.5)/1.5)) if t>6.5 else 0
        for side in ('R','L'):
            wrist=rest['Hand_'+side].copy()
            dest=target if side=='R' else wrist.translation
            a=m['UpperArm_'+side].translation
            reach_error=max(reach_error,(dest-a).length-rig.data.bones['UpperArm_'+side].length-rig.data.bones['Forearm_'+side].length)
            original=(m['Forearm_'+side].translation-a).normalized()
            pole=original.lerp(Vector((-.6,.5,-.1) if side=='R' else (.7,-.3,0)).normalized(),e)
            limb(m,'UpperArm_'+side,'Forearm_'+side,'Hand_'+side,dest,pole)
            wrist.translation=dest;m['Hand_'+side]=wrist
        if sample in (0,960):m={n:v.copy() for n,v in rest.items()}
        assign(m);key(sample/4+1)
    for slot in action.slots:
        for layer in action.layers:
            for strip in layer.strips:
                bag=strip.channelbag(slot)
                if bag:
                    for c in bag.fcurves:
                        for p in c.keyframe_points:p.interpolation='LINEAR'
    print('SANDPLAY_REACH_ERROR',reach_error,flush=True)
    if reach_error>.0001:raise RuntimeError(('Sandplay arm reach exceeded',reach_error))

"""Two additive five-second body-family sighs. No appendage dependency."""
import bpy, math
from mathutils import Vector, Matrix

def ease(t):
    t=max(0,min(1,t));return t*t*t*(10+t*(-15+6*t))

def curve(t,high,low):
    # One-second inhale/exhale, then a relaxed three-second recovery.
    if t<=1:return high*ease(t)
    if t<=2:return high+(low-high)*ease(t-1)
    return low*(1-ease((t-2)/3))

def build(rig,assign,limb,key):
    scene=bpy.context.scene
    upper=[b.name for b in rig.data.bones if b.name=='Spine' or any(a.name=='Spine' for a in b.parent_recursive)]
    head_group=[b.name for b in rig.data.bones if b.name=='Head' or any(a.name=='Head' for a in b.parent_recursive)]
    max_reach=0
    for seated,name,source in [(False,'Adult_Sigh_Stand','Adult_Breathe_Stand'),(True,'Adult_Sigh_Sit','Adult_Breathe_Sit')]:
        rig.animation_data.action=bpy.data.actions[source];scene.frame_set(1);bpy.context.view_layer.update()
        rest={b.name:b.matrix.copy() for b in rig.pose.bones}
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        for sample in range(601):
            t=sample/120;lift=curve(t,.024,-.020);lean=math.radians(curve(t,-5 if seated else -4,18 if seated else 12))
            forward=curve(t,0,.020)
            lagged=math.radians(curve(max(0,t-.075),-5 if seated else -4,18 if seated else 12))*(1-ease((t-4.7)/.3))
            m={n:v.copy() for n,v in rest.items()};pivot=m['Spine'].translation.copy()
            transform=Matrix.Translation(Vector((0,-forward,lift)))@Matrix.Translation(pivot)@Matrix.Rotation(lean,4,'X')@Matrix.Translation(-pivot)
            for n in upper:m[n]=transform@m[n]
            # Rotation only at the existing head joint: no neck length change.
            hp=m['Head'].translation.copy();counter=-lean if seated else -.65*lean+.12*(lagged-lean)
            # Keep inhalation unchanged; release gaze stabilization only on the
            # exhale. Existing head rotation, never a separate head translation.
            release=ease((t-1)/.35)
            head_pitch=math.radians(curve(max(0,t-2/30),0,8 if seated else 6))*(1-ease((t-4.7)/.3))
            counter=counter*(1-release)+(head_pitch-lean)*release
            turn=Matrix.Translation(hp)@Matrix.Rotation(counter,4,'X')@Matrix.Translation(-hp)
            for n in head_group:m[n]=turn@m[n]
            for side in ('L','R'):
                if seated:
                    target=rest['Hand_'+side].translation+Vector((0,-forward*.15,.15*curve(max(0,t-.075),.024,-.020)*(1-ease((t-4.7)/.3))))
                    a=m['UpperArm_'+side].translation
                    max_reach=max(max_reach,(target-a).length-rig.data.bones['UpperArm_'+side].length-rig.data.bones['Forearm_'+side].length)
                    limb(m,'UpperArm_'+side,'Forearm_'+side,'Hand_'+side,target,rest['Forearm_'+side].translation-rest['UpperArm_'+side].translation)
                    m['Hand_'+side]=rest['Hand_'+side].copy();m['Hand_'+side].translation=target
                else:
                    shoulder=m['UpperArm_'+side].translation.copy()
                    turn=Matrix.Translation(shoulder)@Matrix.Rotation(-lean*.45+(lagged-lean)*.5,4,'X')@Matrix.Translation(-shoulder)
                    for n in ('UpperArm_','Forearm_','Hand_'):m[n+side]=turn@m[n+side]
            if sample in (0,600):m={n:v.copy() for n,v in rest.items()}
            assign(m);key(sample/4+1)
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for c in bag.fcurves:
                            for p in c.keyframe_points:p.interpolation='LINEAR'
        print('COMMON_SIGH',name,'5s, 120Hz baked, fixed support',flush=True)
    if max_reach>.0005:raise RuntimeError(('Sigh arm reach',max_reach))

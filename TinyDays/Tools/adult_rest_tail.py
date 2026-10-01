"""Rabbit short tail: retain bind attachment to pelvis; ground overlap is allowed."""
import bpy

def fix_existing(rig,base):
    from adult_rabbit_lying import CLIPS as lying
    from adult_rabbit_sleep import CLIPS as sleep
    names=[row[0] for row in lying]+[row[0] for row in sleep]
    relative=base['Pelvis'].inverted()@base['Tail']
    for name in names:
        action=bpy.data.actions.get(name)
        if not action:continue
        end=float(action.frame_range[1])
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for curve in list(bag.fcurves):
                            if curve.data_path.startswith('pose.bones["Tail"]'):bag.fcurves.remove(curve)
        rig.animation_data.action=action
        for sample in range(round((end-1)*4)+1):
            frame=1+sample/4;bpy.context.scene.frame_set(int(frame),subframe=frame%1);bpy.context.view_layer.update()
            bone=rig.pose.bones['Tail'];bone.matrix=rig.pose.bones['Pelvis'].matrix@relative
            for channel in ('location','rotation_quaternion','scale'):bone.keyframe_insert(channel,frame=frame,group='Tail')

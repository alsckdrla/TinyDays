"""Additive AdultStandard_v2 body loops; secondary appendages are runtime profiles.

No changes to existing actions, bind skeleton, meshes, or scales.
"""
import bpy, math
from mathutils import Quaternion, Vector

def build(rig, key, limb, assign, periodic):
    scene=bpy.context.scene
    for name,source in [('Adult_Breathe_Stand','Adult_Stand_Up'),('Adult_Breathe_Sit','Adult_Sit_Hold')]:
        rig.animation_data.action=bpy.data.actions[source]
        scene.frame_set(55 if source=='Adult_Stand_Up' else 1)
        bpy.context.view_layer.update()
        if source=='Adult_Stand_Up':
            # The preserved legacy standing reference has an asymmetrically
            # raised right sole. Ground BOTH feet in the new loop only.
            pose={b.name:b.matrix.copy() for b in rig.pose.bones};shoe=bpy.data.objects['Shoes'];targets={}
            for side in ('L','R'):
                n='Foot_'+side;group=shoe.vertex_groups[n].index
                skin=pose[n]@rig.data.bones[n].matrix_local.inverted()
                bottom=min((skin@v.co).z for v in shoe.data.vertices if any(g.group==group and g.weight>.99 for g in v.groups))
                targets[side]=pose[n].translation.copy();targets[side].z+=.0025-bottom
            drop=0
            for side in ('L','R'):
                delta=pose['Thigh_'+side].translation-targets[side]
                reach=rig.data.bones['Thigh_'+side].length+rig.data.bones['Shin_'+side].length-.0001
                drop=max(drop,delta.z-math.sqrt(max(.0001,reach*reach-delta.x**2-delta.y**2)))
            for n in pose:
                if n!='Root':pose[n].translation.z-=drop
            for side in ('L','R'):
                rotation=pose['Foot_'+side].to_quaternion()
                limb(pose,'Thigh_'+side,'Shin_'+side,'Foot_'+side,targets[side],(0,-1,0))
                pose['Foot_'+side]=rotation.to_matrix().to_4x4();pose['Foot_'+side].translation=targets[side]
            assign(pose)
        rest={b.name:b.matrix_basis.copy() for b in rig.pose.bones}
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        for frame in range(121):
            phase=2*math.pi*frame/120
            for b in rig.pose.bones:b.matrix_basis=rest[b.name].copy()
            # Local chest displacement is parallel to rig/world up. Pelvis, feet
            # and seated support remain untouched. 7mm peak-to-peak, no scaling.
            spine=rig.pose.bones['Spine']
            parent_rotation=rig.pose.bones['Pelvis'].matrix.to_quaternion()
            local_up=parent_rotation.inverted()@Vector((0,0,1))
            spine.location+=local_up*(.0035*math.sin(phase))
            spine.rotation_quaternion=rest['Spine'].to_quaternion()@Quaternion((1,0,0),math.radians(.35)*math.sin(phase))
            head=rig.pose.bones['Head']
            head.rotation_quaternion=rest['Head'].to_quaternion()@Quaternion((1,0,0),math.radians(-.22)*math.sin(phase-.18))
            for side in ('L','R'):
                b=rig.pose.bones['UpperArm_'+side]
                b.rotation_quaternion=rest[b.name].to_quaternion()@Quaternion((1,0,0),math.radians(.25)*math.sin(phase-.12))
            key(frame+1)
        for layer in action.layers:
            for strip in layer.strips:
                for slot in action.slots:
                    bag=strip.channelbag(slot)
                    if bag:
                        for curve in bag.fcurves:
                            for p in curve.keyframe_points:p.interpolation='BEZIER';p.handle_left_type='AUTO';p.handle_right_type='AUTO'
                            if max(p.co.y for p in curve.keyframe_points)-min(p.co.y for p in curve.keyframe_points)>1e-8:periodic(curve)
        print('COMMON_BREATHING',name,'4 seconds, fixed support, no scale',flush=True)

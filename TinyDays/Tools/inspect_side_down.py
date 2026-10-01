"""Read-only draft geometry/contact/rotation report."""
import bpy,sys,numpy as np
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Logs/SideDownDraft.blend'))
rig=bpy.data.objects['AdultRig'];inv={b.name:b.matrix_local.inverted() for b in rig.data.bones}
def points(obj,bone=None):
    ob=bpy.data.objects[obj];out=[]
    for v in ob.data.vertices:
        weights=[(ob.vertex_groups[g.group].name,g.weight) for g in v.groups]
        if bone and sum(w for n,w in weights if n==bone)<.9:continue
        out.append(sum((rig.pose.bones[n].matrix@inv[n]@v.co*w for n,w in weights),Vector()))
    return out
for name,duration in [('Adult_Stand_To_Left',3.2),('Adult_Sit_To_Left',2.4)]:
    rig.animation_data.action=bpy.data.actions[name];previous={};worst=(0,0,'');last=None;headstep=0;low=99;rebound=(0,0)
    for i in range(round(duration*240)+1):
        t=i/240;scene=bpy.context.scene;f=1+t*30;scene.frame_set(int(f),subframe=f%1);bpy.context.view_layer.update()
        for s in ('L','R'):
            for prefix in ('Thigh_','Shin_','Foot_','UpperArm_','Forearm_','Hand_'):
                n=prefix+s;q=rig.pose.bones[n].matrix.to_quaternion()
                if n in previous:
                    dot=abs(q.dot(previous[n]));angle=np.degrees(2*np.arccos(min(1,dot)))
                    if angle>worst[0]:worst=(angle,t,n)
                previous[n]=q
        p=rig.pose.bones['Head'].matrix.translation
        if last is not None:headstep=max(headstep,(p-last).length)
        last=p.copy()
        low=min(low,p.z)
        if p.z-low>rebound[0]:rebound=(p.z-low,t)
        if any(abs(t-v)<1e-5 for v in ([.24,1.0,1.05,1.1,1.59,1.65,1.71,2.3,3.2] if duration>3 else [.49,.55,.61,1.35,2.4])):
            print('SUPPORT',name,t,{n:min(p.z for p in points('BodyHands',n)) for n in ('Hand_L','Hand_R')},'knee',rig.pose.bones['Shin_R'].matrix.translation[:],'elbow',rig.pose.bones['Forearm_R'].matrix.translation[:],flush=True)
    print('ROTATION',name,worst,'HEADSTEP',headstep,'REBOUND',rebound,flush=True)

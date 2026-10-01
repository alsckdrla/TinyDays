import bpy,sys,math,csv
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
source=ROOT/'ArtSource/AdultRabbitMotion.blend'
if '--draft' in sys.argv:source=ROOT/'Logs/SideRiseDraft.blend'
if '--before' in sys.argv:source=ROOT/'Logs/BeforeV0114/AdultRabbitMotion.blend'
bpy.app.driver_namespace['tiny_sleep_eye']=lambda frame:0.0
bpy.ops.wm.open_mainfile(filepath=str(source));rig=bpy.data.objects['AdultRig']
names=['Pelvis','Spine','Head','Thigh_L','Shin_L','Foot_L','Thigh_R','Shin_R','Foot_R']
rows=[]
for action,duration in [('Adult_Left_To_Sit',2.4),('Adult_Left_To_Stand',3.2)]:
    rig.animation_data.action=bpy.data.actions[action];prev={};peak={n:(0,0) for n in names}
    for i in range(round(duration*240)+1):
        t=i/240;f=1+t*30;bpy.context.scene.frame_set(int(f),subframe=f%1)
        row=[action,t]
        for n in names:
            b=rig.pose.bones[n];q=b.matrix.to_quaternion();step=math.degrees(prev[n].rotation_difference(q).angle) if n in prev else 0;step=min(step,360-step)
            if step>peak[n][0]:peak[n]=(step,t)
            prev[n]=q.copy();row.extend([*b.matrix.translation,step])
        rows.append(row)
    print('ROTATION_PEAK',action,peak,flush=True)
out=ROOT/'Logs'/('side-rotation-before.csv' if '--before' in sys.argv else 'side-rotation-after.csv')
with out.open('w',newline='') as file:
    writer=csv.writer(file);writer.writerow(['clip','time']+[n+'_'+v for n in names for v in ['x','y','z','step_deg']]);writer.writerows(rows)

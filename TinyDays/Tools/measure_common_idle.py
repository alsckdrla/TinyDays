"""Read-only 240Hz source-curve audit; Unity playback is checked separately."""
import bpy, json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/AdultRabbitMotion.blend'))
rig=bpy.data.objects['AdultRig'];scene=bpy.context.scene;result={}
for name in ('Adult_Breathe_Stand','Adult_Breathe_Sit'):
    rig.animation_data.action=bpy.data.actions[name]
    series={n:[] for n in ('Spine','Head')}
    for i in range(961):
        frame=1+i/8;scene.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update()
        for n in series:series[n].append(rig.pose.bones[n].matrix.translation.copy())
    result[name]={}
    for n,points in series.items():
        velocities=[(b-a)*240 for a,b in zip(points,points[1:])]
        accelerations=[(b-a)*240 for a,b in zip(velocities,velocities[1:])]
        seam=(points[0]-points[-1]).length
        velocity_seam=(velocities[0]-velocities[-1]).length
        result[name][n]={'vertical_range_mm':(max(p.z for p in points)-min(p.z for p in points))*1000,
            'position_seam_mm':seam*1000,'velocity_seam_m_s':velocity_seam,
            'peak_acceleration_m_s2':max(a.length for a in accelerations)}
        assert seam<.00001 and velocity_seam<.002,(name,n,seam,velocity_seam)
        assert max(a.length for a in accelerations)<.05,(name,n,'breathing acceleration spike')
(root/'Docs/AdultCommonIdleCurves.json').write_text(json.dumps(result,indent=2),encoding='utf8')
print(json.dumps(result,indent=2));print('COMMON_IDLE_CURVES_OK')

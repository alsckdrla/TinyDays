import bpy
from pathlib import Path
bpy.app.driver_namespace['tiny_sleep_eye']=lambda frame:0.0
bpy.ops.wm.open_mainfile(filepath=str(Path(__file__).resolve().parents[1]/'ArtSource/AdultRabbitMotion.blend'))
r=bpy.data.objects['AdultRig'];r.animation_data.action=bpy.data.actions['Adult_Breathe_Stand'];bpy.context.scene.frame_set(1)
print('RIG',r.matrix_world)
for b in r.pose.bones:
 print('BONE',b.name,tuple(round(v,4) for v in b.matrix.translation),'length',round(b.length,4),'parent',b.parent.name if b.parent else None)

"""Read-only source audit: fixed pelvis attachment, intentional tail-floor overlap."""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(root/'Tools'))
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/AdultRabbitMotion.blend'))
rig=bpy.data.objects['AdultRig'];reference=rig.data.bones['Pelvis'].matrix_local.inverted()@rig.data.bones['Tail'].matrix_local
from adult_rabbit_lying import CLIPS as lying
from adult_rabbit_sleep import CLIPS as sleep
from adult_rabbit_side import CLIPS as side
rows=[]
for name,*unused in lying+sleep+side:
    action=bpy.data.actions[name];rig.animation_data.action=action;duration=(action.frame_range[1]-1)/30;position=angle=0.0
    for f in range(round(duration*240)+1):
        bpy.context.scene.frame_set(1+f//8,subframe=f%8/8);bpy.context.view_layer.update()
        relative=rig.pose.bones['Pelvis'].matrix.inverted()@rig.pose.bones['Tail'].matrix
        position=max(position,(relative.translation-reference.translation).length)
        angle=max(angle,relative.to_quaternion().rotation_difference(reference.to_quaternion()).angle)
    rows.append({'clip':name,'positionErrorM':position,'angleErrorRadians':angle})
    assert position<.0001 and angle<.002,(name,position,angle)
(root/'Docs/AdultRestTailVerification.json').write_text(json.dumps({'sampleRate':240,'groundOverlapAllowed':True,'clips':rows},indent=2),encoding='utf8')
print('REST_TAIL_OK',max(r['positionErrorM'] for r in rows),flush=True)

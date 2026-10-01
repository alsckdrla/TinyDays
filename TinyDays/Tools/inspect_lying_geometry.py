"""Read-only geometry diagnostic for the supine authoring task."""
import bpy,sys
from pathlib import Path
from mathutils import Matrix, Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'ArtSource/AdultRabbitMotion.blend'))
r=bpy.data.objects['AdultRig'];r.animation_data.action=bpy.data.actions['Adult_Lie_Hold' if '--lying' in sys.argv else 'Adult_Breathe_Sit'];bpy.context.scene.frame_set(1)
bpy.context.view_layer.update()
for o in bpy.data.objects:
    if o.type!='MESH':continue
    pts=[]
    for v in o.data.vertices:
        p=Vector()
        for w in v.groups:
            n=o.vertex_groups[w.group].name
            if n in r.pose.bones:p+=(r.pose.bones[n].matrix@r.data.bones[n].matrix_local.inverted()@v.co)*w.weight
        pts.append(p)
    print(o.name,'bounds',[(min(p[i] for p in pts),max(p[i] for p in pts)) for i in range(3)])
for n in ['Pelvis','Spine','Head','Thigh_L','Shin_L','Foot_L','UpperArm_R','Forearm_R','Hand_R']:
    b=r.pose.bones[n];print(n,tuple(b.matrix.translation),'bind',tuple(b.bone.head_local),'length',b.bone.length)

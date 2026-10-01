import bpy, math
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Logs/LyingDraft.blend'))
s=bpy.context.scene;r=bpy.data.objects['AdultRig']
for o in bpy.data.objects:
    if o.type=='MESH':o.hide_render=o.name in ('Backpack','BodyTorso','BodyArms','BodyLegs','BodyFeet')
r.hide_render=True;r.animation_data.action=bpy.data.actions['Adult_Lie_Hold'];s.frame_set(1)
for o in bpy.data.objects:
    if o.type!='MESH' or o.hide_render:continue
    e=o.evaluated_get(bpy.context.evaluated_depsgraph_get());m=e.to_mesh()
    print('FLOOR',o.name,min((o.matrix_world@v.co).z for v in m.vertices));e.to_mesh_clear()
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.location.z=-.002
floor=bpy.context.object;floor.color=(.75,.85,.84,1)
s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='MATERIAL'
s.display.shading.show_shadows=True;s.display.shading.show_cavity=True;s.display.shading.background_type='WORLD';s.world.color=(.8,.85,.86)
s.render.resolution_x=900;s.render.resolution_y=700;s.render.resolution_percentage=100
bpy.ops.object.camera_add();cam=bpy.context.object;s.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=2.4
out=root/'Logs/LyingDraft';out.mkdir(exist_ok=True)
for label,position in [('Side',(3,-.2,1.0)),('Oblique',(2,-2,2)),('Head',(0,3,1.7))]:
    cam.location=position;cam.rotation_euler=(Vector((0,.25,.3))-cam.location).to_track_quat('-Z','Y').to_euler()
    s.render.filepath=str(out/(label+'.png'));bpy.ops.render.render(write_still=True)

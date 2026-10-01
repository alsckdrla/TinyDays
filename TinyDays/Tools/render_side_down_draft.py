import bpy
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Logs/SideDownDraft.blend'))
s=bpy.context.scene;r=bpy.data.objects['AdultRig']
for ob in bpy.data.objects:
    if ob.type=='MESH':ob.hide_render=ob.name in ('Backpack','BodyTorso','BodyArms','BodyLegs','BodyFeet')
r.hide_render=True
bpy.ops.mesh.primitive_plane_add(size=20);bpy.context.object.location.z=-.002
s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='MATERIAL'
s.display.shading.show_shadows=True;s.display.shading.show_cavity=True;s.world.color=(.8,.85,.86)
s.render.resolution_x=500;s.render.resolution_y=500;s.render.resolution_percentage=100
bpy.ops.object.camera_add();cam=bpy.context.object;s.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=2.6
out=root/'Logs/SideDownDraftImages';out.mkdir(exist_ok=True)
for name,times in [('Adult_Stand_To_Left',[.35,1.05,1.65,2.3,3.2]),('Adult_Sit_To_Left',[.55,1.35,2.4])]:
    r.animation_data.action=bpy.data.actions[name]
    for i,t in enumerate(times):
        f=1+t*30;s.frame_set(int(f),subframe=f%1)
        for view,pos in [('Front',(-2,-4,2)),('Side',(-4,0,1.5)),('Top',(-.1,.1,5))]:
            cam.location=pos;cam.rotation_euler=(Vector((0,.1,.65))-cam.location).to_track_quat('-Z','Y').to_euler()
            s.render.filepath=str(out/(name+'_'+str(i)+'_'+view+'.png'));bpy.ops.render.render(write_still=True)

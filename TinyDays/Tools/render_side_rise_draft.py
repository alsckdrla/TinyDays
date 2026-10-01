import bpy
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Logs/SideRiseDraft.blend'))
s=bpy.context.scene;r=bpy.data.objects['AdultRig']
for ob in bpy.data.objects:
    if ob.type=='MESH':ob.hide_render=ob.name in ('Backpack','BodyTorso','BodyArms','BodyLegs','BodyFeet')
r.hide_render=True
bpy.ops.mesh.primitive_plane_add(size=20);bpy.context.object.location.z=-.002
s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='MATERIAL'
s.display.shading.show_shadows=True;s.display.shading.show_cavity=True;s.world.color=(.8,.85,.86)
s.render.resolution_x=500;s.render.resolution_y=500;s.render.resolution_percentage=100
bpy.ops.object.camera_add();cam=bpy.context.object;s.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=2.6
out=root/'Docs/Captures/SideRiseDraft';out.mkdir(exist_ok=True)
for name,times in [('Adult_Left_To_Sit',[.42,1.35,1.7,2.4]),('Adult_Left_To_Stand',[1.1,1.75,2.4,3.2])]:
    r.animation_data.action=bpy.data.actions[name]
    for t in times:
        f=1+t*30;s.frame_set(int(f),subframe=f%1)
        for view,pos in [('Front',(-2,-4,2)),('Side',(-4,0,1.5))]:
            cam.location=pos;cam.rotation_euler=(Vector((0,.1,.65))-cam.location).to_track_quat('-Z','Y').to_euler()
            s.render.filepath=str(out/(name+'_'+str(t)+'_'+view+'.png'));bpy.ops.render.render(write_still=True)

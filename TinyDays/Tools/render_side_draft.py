import bpy,sys
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/('Logs/BeforeV0111/AdultRabbitMotion.blend' if '--before' in sys.argv or '--restore-v0110' in sys.argv else 'Logs/SideDraft.blend')))
s=bpy.context.scene;r=bpy.data.objects['AdultRig']
for ob in bpy.data.objects:
    if ob.type=='MESH':ob.hide_render=ob.name in ('Backpack','BodyTorso','BodyArms','BodyLegs','BodyFeet')
r.hide_render=True;r.animation_data.action=bpy.data.actions['Adult_Breathe_Left'];s.frame_set(1)
bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.location.z=-.002;bpy.context.object.color=(.75,.85,.84,1)
s.render.engine='BLENDER_WORKBENCH';s.display.shading.light='STUDIO';s.display.shading.color_type='MATERIAL'
s.display.shading.show_shadows=True;s.display.shading.show_cavity=True;s.display.shading.background_type='WORLD';s.world.color=(.8,.85,.86)
s.render.resolution_x=900;s.render.resolution_y=700;s.render.resolution_percentage=100
bpy.ops.object.camera_add();cam=bpy.context.object;s.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=2.3
out=root/('Docs/Captures/SideDirectionV0110' if '--restore-v0110' in sys.argv else 'Docs/Captures/SideDirectionV0111');out.mkdir(exist_ok=True)
for label,position in [('Side',(-3,.4,1)),('Oblique',(-2,-2,2)),('Head',(0,3,1.7)),('Top',(0,.4,4))]:
    cam.location=position;cam.rotation_euler=(Vector((.12,.4,.25))-cam.location).to_track_quat('-Z','Y').to_euler()
    s.render.filepath=str(out/(('Before_' if '--before' in sys.argv else 'After_')+label+'.png'));bpy.ops.render.render(write_still=True)
if '--before' not in sys.argv:
    cam.location=(-3,.4,1.4);cam.rotation_euler=(Vector((.12,.4,.25))-cam.location).to_track_quat('-Z','Y').to_euler()
    for suffix,label,color in [('R','Actual LEFT / legacy R',(1,.08,.04,1)),('L','Actual RIGHT / legacy L',(.04,.35,1,1))]:
        p=r.matrix_world@r.pose.bones['Forearm_'+suffix].head
        mat=bpy.data.materials.new(label);mat.diffuse_color=color
        bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=.025,location=p);bpy.context.object.data.materials.append(mat)
        bpy.ops.object.text_add(location=p+Vector((-.3,-.13,.14 if suffix=='L' else .035)))
        text=bpy.context.object;text.data.body=label;text.data.size=.055;text.rotation_euler=cam.rotation_euler;text.data.materials.append(mat)
    s.render.filepath=str(out/'ActualLeftMarker.png');bpy.ops.render.render(write_still=True)

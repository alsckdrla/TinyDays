"""Read-only Blender inspection renders; scene setup is never saved."""
import bpy, math, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
shrink='--shrink' in sys.argv
OUT=ROOT/('Docs/Captures/UserRabbitEyeShrink' if shrink else 'Docs/Captures/UserRabbitRoundEyes');OUT.mkdir(parents=True,exist_ok=True)
backup=ROOT/('ArtSource/Backups/AdultRabbit - 01.before-eye-shrink-20261002.blend' if shrink else 'ArtSource/Backups/AdultRabbit - 01.before-round-eyes-20261002.blend')
for stage,file in [('Before',backup),('After',ROOT/'ArtSource/AdultRabbit - 01.blend')]:
 bpy.ops.wm.open_mainfile(filepath=str(file));scene=bpy.context.scene
 scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=700;scene.render.resolution_y=700;scene.render.resolution_percentage=100
 scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
 shading=scene.display.shading;shading.light='STUDIO';shading.color_type='MATERIAL';shading.background_type='WORLD';scene.world.color=(.18,.22,.23)
 shading.show_shadows=True;shading.show_cavity=True;shading.cavity_type='BOTH';shading.show_specular_highlight=False
 camera=bpy.data.objects.new('Inspection camera',bpy.data.cameras.new('Inspection camera'));scene.collection.objects.link(camera);scene.camera=camera
 camera.data.type='ORTHO';camera.data.ortho_scale=1.05
 head=bpy.data.objects['Head'];closed=head.data.shape_keys.key_blocks['SleepEyesClosed']
 for view,angle in [('Front',0),('Oblique',30),('Side',75)]:
  a=math.radians(angle);camera.location=(3*math.sin(a),-3*math.cos(a),1.57)
  camera.rotation_euler=(Vector((0,0,1.49))-camera.location).to_track_quat('-Z','Y').to_euler()
  for amount in [0,.5,1]:
   closed.value=amount;bpy.context.view_layer.update()
   scene.render.filepath=str(OUT/f'{stage}_{view}_Closed{round(amount*100):03d}.png')
   bpy.ops.render.render(write_still=True)
print('USER_ROUND_EYES_RENDER_OK',flush=True)

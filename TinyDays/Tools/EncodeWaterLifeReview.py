"""Blender's bundled video encoder; no external ffmpeg/install or art edits."""
import bpy
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
for view in (90,130,180):
    frames=sorted((ROOT/f'Logs/RabbitWaterLifeFrames/{view}').glob('*.png'))
    assert frames, f'No frames for {view}'
    scene=bpy.context.scene
    if scene.sequence_editor:scene.sequence_editor_clear()
    editor=scene.sequence_editor_create()
    strip=editor.strips.new_image('Water routine',str(frames[0]),channel=1,frame_start=1)
    for frame in frames[1:]:strip.elements.append(frame.name)
    scene.frame_start=1;scene.frame_end=len(frames)
    scene.render.fps=30;scene.render.resolution_x=960;scene.render.resolution_y=720;scene.render.resolution_percentage=100
    scene.render.use_sequencer=True
    scene.render.image_settings.media_type='VIDEO'
    scene.render.image_settings.file_format='FFMPEG'
    scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264';scene.render.ffmpeg.constant_rate_factor='MEDIUM'
    scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
    scene.render.filepath=str(ROOT/f'Docs/Captures/RabbitWaterLife/Flow{view}_1x.mp4')
    bpy.ops.render.render(animation=True)
    print('WATER_LIFE_VIDEO_OK',view,len(frames),flush=True)

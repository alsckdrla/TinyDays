"""Encode completed renders only; retain older frames without appending their tail."""
import bpy
import re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
counts={}
for log in sorted((ROOT/'Logs').glob('*.log'),key=lambda p:p.stat().st_mtime,reverse=True):
    content=log.read_text(encoding='utf-8',errors='replace')
    if 'WATER_BENCH_LIFE_RENDER_OK' not in content:continue
    counts={int(view):int(count) for view,count in re.findall(r'WATER_BENCH_LIFE_RENDER (0|45|90) frames=(\d+)',content)}
    if len(counts)==3:break
assert len(counts)==3,'No completed three-view render log; refusing stale frame glob'
for view in (0,45,90):
    frames=[ROOT/f'Logs/RabbitWaterBenchLifeFrames/{view}/{i:04d}.png' for i in range(counts[view])]
    assert all(p.is_file() for p in frames),f'Missing current render frame for {view}'
    assert frames, f'No frames for {view}'
    scene=bpy.context.scene
    if scene.sequence_editor:scene.sequence_editor_clear()
    editor=scene.sequence_editor_create()
    strip=editor.strips.new_image('Water and bench routine',str(frames[0]),channel=1,frame_start=1)
    for frame in frames[1:]:strip.elements.append(frame.name)
    scene.frame_start=1;scene.frame_end=len(frames)
    scene.render.fps=30;scene.render.resolution_x=960;scene.render.resolution_y=720;scene.render.resolution_percentage=100
    scene.render.use_sequencer=True
    scene.render.image_settings.media_type='VIDEO';scene.render.image_settings.file_format='FFMPEG'
    scene.render.ffmpeg.format='MPEG4';scene.render.ffmpeg.codec='H264';scene.render.ffmpeg.constant_rate_factor='MEDIUM'
    scene.view_settings.view_transform='Standard';scene.view_settings.look='None'
    scene.render.filepath=str(ROOT/f'Docs/Captures/RabbitWaterBenchLife/Flow{view}_1x.mp4')
    bpy.ops.render.render(animation=True)
    print('WATER_BENCH_LIFE_VIDEO_OK',view,len(frames),flush=True)

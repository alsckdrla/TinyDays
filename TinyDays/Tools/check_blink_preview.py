"""Headless preview self-test; no art files are loaded or saved."""
import bpy
import runpy
from pathlib import Path
scope = runpy.run_path(str(Path(__file__).with_name('preview_adult_blink.py')))
scene = bpy.context.scene
scene.render.fps = 240
mesh = bpy.data.meshes.new('BlinkProbe')
mesh.from_pydata([(0,0,0),(1,0,0),(0,1,0)], [], [(0,1,2)])
obj = bpy.data.objects.new('BlinkProbe', mesh)
scene.collection.objects.link(obj)
obj.shape_key_add(name='Basis')
key = obj.shape_key_add(name='SleepEyesClosed')
seen = False
for frame in range(1,240*10):
    scene.frame_set(frame)
    assert 0 <= key.value <= 1
    seen |= key.value > .9
assert seen
scene['tiny_blink_sleep'] = True
scene['tiny_blink_sleep_weight'] = 1.0
scene.frame_set(240*11)
assert key.value == 1
scene['tiny_blink_sleep'] = False
scene['tiny_blink_sleep_weight'] = 0.0
scene.frame_set(240*11+1)
assert key.value == 0
print('BLINK_PREVIEW_OK: awake blink, sleep closure, fresh wake wait; no art writes')

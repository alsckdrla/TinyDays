"""Run in Blender's Text Editor; preview only, never save/bake canonical art.
Body/sleep animation is sampled first; this handler composes the eye channel.
Set tiny_blink_sleep=True during falling asleep/sleep/waking, False once awake.
"""
import bpy
INTERVALS = (3.2, 3.6, 4.0, 4.4)
def smooth(t):
    t = max(0.0, min(1.0, t))
    return t*t*t*(t*(t*6-15)+10)
def curve(t):
    if t < 0 or t >= .24: return 0.0
    if t < .08: return smooth(t/.08)
    if t < .11: return 1.0
    return 1-smooth((t-.11)/.13)
_clocks = {}
def schedule(state):
    while True:
        state['random'] = (state['random']*1664525+1013904223) & 0xffffffff
        index = (state['random'] >> 16) % 4
        if index != state['last']: break
    state['last'] = index
    state['next'] = state['clock']+INTERVALS[index]
def tiny_blink_preview(scene):
    identity = scene.as_pointer()
    state = _clocks.get(identity)
    if state is None or scene.frame_current < state['frame']:
        state = dict(random=int(scene.get('tiny_blink_seed', 1701)) & 0xffffffff,
                     clock=0.0, frame=scene.frame_start, last=-1, start=-1.0, suppressed=False, blink=0.0)
        schedule(state)
        _clocks[identity] = state
    dt = max(0, scene.frame_current-state['frame'])/(scene.render.fps/scene.render.fps_base)
    state['frame'] = scene.frame_current
    state['clock'] += dt
    sleep_function = bpy.app.driver_namespace.get('tiny_sleep_eye')
    sleep = float(sleep_function(scene.frame_current)) if sleep_function else float(scene.get('tiny_blink_sleep_weight', 0))
    actions = [obj.animation_data.action.name for obj in scene.objects
               if obj.type == 'ARMATURE' and obj.animation_data and obj.animation_data.action]
    inhibit = scene.get('tiny_blink_sleep', False) or any(
        any(tag in name for tag in ('Fall_Asleep', 'Sleep_Lie', 'Sleep_Left', 'Wake_Lie', 'Wake_Left')) for name in actions)
    if inhibit:
        import math
        state['suppressed'] = True
        state['start'] = -1.0
        state['blink'] *= math.exp(-dt*35)
    else:
        if state['suppressed']:
            state['suppressed'] = False
            schedule(state)
        while state['clock'] >= state['next']:
            state['start'] = state['next']
            now = state['clock']
            state['clock'] = state['next']
            schedule(state)
            state['clock'] = now
        state['blink'] = curve(state['clock']-state['start'])
    blink = state['blink']
    for obj in scene.objects:
        keys = getattr(getattr(obj, 'data', None), 'shape_keys', None)
        if keys and 'SleepEyesClosed' in keys.key_blocks:
            key = keys.key_blocks['SleepEyesClosed']
            # Driver source is retained in tiny_sleep_eye; preview handler is now the final compositor.
            if keys.animation_data:
                for driver in keys.animation_data.drivers:
                    if 'SleepEyesClosed' in driver.data_path: driver.mute = True
            key.value = sleep+(1-sleep)*blink
for handler in list(bpy.app.handlers.frame_change_post):
    if handler.__name__ == 'tiny_blink_preview': bpy.app.handlers.frame_change_post.remove(handler)
bpy.app.handlers.frame_change_post.append(tiny_blink_preview)

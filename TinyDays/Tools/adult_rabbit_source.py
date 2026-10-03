"""Canonical geometry overlay. Never saves or mutates the user's source file."""
import bpy, hashlib
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'ArtSource/AdultRabbit - 01.blend'

def digest(value):
    return hashlib.sha256(repr(value).encode()).hexdigest()

def rig_signature():
    r=bpy.data.objects['AdultRig']
    return digest((tuple(tuple(row) for row in r.matrix_world),[(b.name,b.parent.name if b.parent else None,tuple(tuple(row) for row in b.matrix_local),b.use_deform) for b in r.data.bones]))

def action_signature(excluded=()):
    result=[]
    for a in bpy.data.actions:
        if a.name in excluded:continue
        curves=[]
        for slot in a.slots:
            for layer in a.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for c in bag.fcurves:
                            curves.append((c.data_path,c.array_index,c.extrapolation,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right),k.interpolation,k.handle_left_type,k.handle_right_type) for k in c.keyframe_points]))
        result.append((a.name,tuple(a.frame_range),sorted(curves)))
    return digest(sorted(result))

def apply_source():
    """Overlay coordinates/shape coordinates only; keep authored rig and actions."""
    targets={o.name:o for o in bpy.data.objects if o.type=='MESH'}
    with bpy.data.libraries.load(str(SOURCE),link=False) as (available,loaded):
        loaded.objects=available.objects
    incoming={o.name.split('.')[0]:o for o in loaded.objects if o}
    # Library naming collisions use .001; match the original twelve module names.
    source_rig=next(o for o in loaded.objects if o and o.type=='ARMATURE')
    target=bpy.data.objects['AdultRig']
    assert [(b.name,b.parent.name if b.parent else None,b.matrix_local) for b in target.data.bones]==[(b.name,b.parent.name if b.parent else None,b.matrix_local) for b in source_rig.data.bones], 'Canonical bind differs'
    applied=[]
    for name,o in targets.items():
        src=incoming.get(name)
        if not src or src.type!='MESH':continue # Keep separate can/bench props.
        a,b=o.data,src.data
        assert len(a.vertices)==len(b.vertices) and [tuple(p.vertices) for p in a.polygons]==[tuple(p.vertices) for p in b.polygons], name+' topology changed'
        assert [g.name for g in o.vertex_groups]==[g.name for g in src.vertex_groups], name+' groups changed'
        assert [[(g.group,g.weight) for g in v.groups] for v in a.vertices]==[[(g.group,g.weight) for g in v.groups] for v in b.vertices], name+' weights changed'
        assert o.matrix_local==src.matrix_local, name+' transform changed'
        for v,w in zip(a.vertices,b.vertices):v.co=w.co
        if b.shape_keys:
            assert a.shape_keys and list(a.shape_keys.key_blocks.keys())==list(b.shape_keys.key_blocks.keys()),name+' shape keys differ'
            for k in b.shape_keys.key_blocks:
                for v,w in zip(a.shape_keys.key_blocks[k.name].data,k.data):v.co=w.co
        a.update();applied.append(name)
    assert len(applied)==12,applied
    for o in loaded.objects:
        if o:bpy.data.objects.remove(o,do_unlink=True)
    return applied

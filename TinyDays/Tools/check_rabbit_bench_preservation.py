"""Compare the previous separate bench source, never modifying either source."""
import bpy, json, hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def digest(value):return hashlib.sha256(repr(value).encode()).hexdigest()
def snapshot(path):
 bpy.ops.wm.open_mainfile(filepath=str(path))
 meshes={o.name:digest(([(tuple(v.co)) for v in o.data.vertices],[(tuple(p.vertices),p.material_index) for p in o.data.polygons],[(g.name,g.index) for g in o.vertex_groups], [[(g.group,g.weight) for g in v.groups] for v in o.data.vertices])) for o in bpy.data.objects if o.type=='MESH' and o.name!='WoodBench'}
 rig=bpy.data.objects['AdultRig']
 bones=digest([(b.name,b.parent.name if b.parent else None,[tuple(row) for row in b.matrix_local]) for b in rig.data.bones])
 actions={}
 for a in bpy.data.actions:
  if a.name.startswith('Adult_Bench_'):continue
  curves=[]
  for layer in a.layers:
   for strip in layer.strips:
    for bag in strip.channelbags:
     for c in bag.fcurves:curves.append((c.data_path,c.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right),k.interpolation) for k in c.keyframe_points]))
  actions[a.name]=digest(curves)
 return {'meshes':meshes,'bones':bones,'actions':actions}
before=snapshot(ROOT/'Logs/BeforeBenchV0122/AdultRabbitBench.blend')
after=snapshot(ROOT/'ArtSource/AdultRabbitBench.blend')
assert before==after,'Non-bench character mesh, weight, bind or original action changed'
report={'characterMeshesAndWeights':'identical','bindSkeleton':'identical','originalActions':len(after['actions']),'actionsIdentical':True}
(ROOT/'Docs/RabbitBenchPreservationV122.json').write_text(json.dumps(report,indent=2))
print('BENCH_PRESERVATION_OK',report)

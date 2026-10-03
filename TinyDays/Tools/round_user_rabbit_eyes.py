"""One-file user-model edit. Never rebuilds generated character/game assets."""
import bpy, json, hashlib, shutil, sys
from pathlib import Path
from mathutils import Vector
from mathutils.geometry import intersect_ray_tri

ROOT=Path(__file__).resolve().parents[1]
TARGET=ROOT/'ArtSource/AdultRabbit - 01.blend'
SHRINK='--shrink' in sys.argv
BACKUP=ROOT/('ArtSource/Backups/AdultRabbit - 01.before-eye-shrink-20261002.blend' if SHRINK else 'ArtSource/Backups/AdultRabbit - 01.before-round-eyes-20261002.blend')
REPORT=ROOT/('Docs/UserRabbitEyeShrinkVerification.json' if SHRINK else 'Docs/UserRabbitRoundEyesVerification.json')
assert not BACKUP.exists(), 'Backup already exists; inspect before running again'
BACKUP.parent.mkdir(parents=True,exist_ok=True)
shutil.copy2(TARGET,BACKUP)
bpy.ops.wm.open_mainfile(filepath=str(TARGET))
head=bpy.data.objects['Head'];mesh=head.data
keys=mesh.shape_keys.key_blocks
assert list(keys.keys())==['Basis','SleepEyesClosed']
basis=keys['Basis'];closed=keys['SleepEyesClosed']
adj={i:set() for i in range(len(mesh.vertices))}
for p in mesh.polygons:
 for i in p.vertices:adj[i].update(p.vertices)
unseen=set(adj);islands=[]
while unseen:
 i=unseen.pop();group={i};stack=[i]
 while stack:
  for n in adj[stack.pop()]&unseen:unseen.remove(n);group.add(n);stack.append(n)
 islands.append(group)
dark={i for p in mesh.polygons if mesh.materials[p.material_index].name=='Dark' for i in p.vertices}
eyes=[g for g in islands if g&dark]
assert len(eyes)==2
old={k.name:[v.co.copy() for v in k.data] for k in keys}
old_mesh=[v.co.copy() for v in mesh.vertices]
def invariant():
 records=[]
 for o in sorted(bpy.data.objects,key=lambda o:o.name):
  row=[o.name,o.type,[tuple(r) for r in o.matrix_local]]
  if o.type=='MESH':
   row.extend([[(tuple(p.vertices),p.material_index,p.use_smooth) for p in o.data.polygons],[[tuple((g.group,g.weight)) for g in v.groups] for v in o.data.vertices],[m.name for m in o.data.materials]])
   if o!=head:row.append([tuple(v.co) for v in o.data.vertices])
  if o.type=='ARMATURE':row.append([(b.name,b.parent.name if b.parent else None,[tuple(r) for r in b.matrix_local]) for b in o.data.bones])
  records.append(row)
 return hashlib.sha256(repr(records).encode()).hexdigest()
before=invariant();allowed=set();rows=[]
mesh.calc_loop_triangles()
for eye in sorted(eyes,key=lambda g:min(g)):
 ids=sorted(eye);assert len(ids)==74 and ids==list(range(ids[0],ids[-1]+1))
 center=(basis.data[ids[0]].co+basis.data[ids[-1]].co)*.5
 z=(basis.data[ids[0]].co-center).normalized()
 x=basis.data[ids[1]].co-center;x=(x-z*x.dot(z)).normalized()
 y=z.cross(x).normalized()
 values=[basis.data[i].co-center for i in ids]
 width=max(v.dot(x) for v in values)-min(v.dot(x) for v in values)
 height=max(v.dot(z) for v in values)-min(v.dot(z) for v in values)
 factor=.9 if SHRINK else height/width
 vertical=.9 if SHRINK else 1
 patch=[g for g in islands if len(g)==25 and ((sum((basis.data[i].co for i in g),Vector())/len(g))-center).length<.065]
 assert len(patch)==1;patch=patch[0];allowed.update(eye|patch)
 for key in keys:
  for i in eye:
   v=old[key.name][i]-center;key.data[i].co=old[key.name][i]+x*v.dot(x)*(factor-1)+z*v.dot(z)*(vertical-1)
 for i in eye:mesh.vertices[i].co=basis.data[i].co
 triangles=[t.vertices[:] for t in mesh.loop_triangles if all(i in eye for i in t.vertices)]
 for i in patch:
  local=old['Basis'][i]-center
  origin=center+x*local.dot(x)*(.9 if SHRINK else 1)+z*local.dot(z)*vertical-y
  hits=[]
  for tri in triangles:
   hit=intersect_ray_tri(*(basis.data[j].co for j in tri),y,origin,True)
   if hit is not None:hits.append(hit)
  assert hits, 'Highlight projection missed widened eye'
  new=min(hits,key=lambda p:(p-origin).length)-y*.0005
  basis.data[i].co=new;mesh.vertices[i].co=new
  closed.data[i].co=new+(old['SleepEyesClosed'][i]-old['Basis'][i])
 new_values=[basis.data[i].co-center for i in ids]
 new_width=max(v.dot(x) for v in new_values)-min(v.dot(x) for v in new_values)
 new_height=max(v.dot(z) for v in new_values)-min(v.dot(z) for v in new_values)
 assert abs(new_width-(width*.9 if SHRINK else height))<1e-6 and abs(new_height-height*vertical)<1e-6
 assert ((basis.data[ids[0]].co+basis.data[ids[-1]].co)*.5-center).length<1e-7
 assert max(abs((basis.data[i].co-old['Basis'][i]).dot(y)) for i in eye)<1e-7
 rows.append({'center':list(center),'beforeWidth':width,'beforeHeight':height,'afterHeight':new_height,'afterWidth':new_width,'horizontalScale':factor,'verticalScale':vertical,'highlightVertices':len(patch),'eyeVertices':len(eye)})
for key in keys:
 assert all(key.data[i].co==old[key.name][i] for i in range(len(mesh.vertices)) if i not in allowed), 'Non-eye shape geometry changed'
assert all(mesh.vertices[i].co==old_mesh[i] for i in range(len(mesh.vertices)) if i not in allowed)
assert invariant()==before,'Mesh topology/material/weight/bind/object invariant changed'
closed.value=0;head.active_shape_key_index=0
mesh.update()
bpy.ops.wm.save_as_mainfile(filepath=str(TARGET))
REPORT.write_text(json.dumps({'target':str(TARGET),'backup':str(BACKUP),'eyes':rows,'nonEyeGeometry':'identical in mesh and both shape keys','topologyWeightsMaterialsRigObjects':'identical','newVertices':0,'newMaterials':0,'animationsInUserFile':len(bpy.data.actions)},indent=2),encoding='utf-8')
print('USER_ROUND_EYES_OK',json.dumps(rows),flush=True)

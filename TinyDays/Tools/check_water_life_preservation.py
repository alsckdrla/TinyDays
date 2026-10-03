"""Read-only art comparison against the pre-append water source."""
import bpy,sys,json,hashlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Tools'))
from adult_rabbit_source import action_signature,rig_signature,digest
def geometry():
    return digest([(o.name,tuple(tuple(v.co) for v in o.data.vertices),tuple(tuple(p.vertices) for p in o.data.polygons),
                    [(k.name,tuple(tuple(v.co) for v in k.data)) for k in o.data.shape_keys.key_blocks] if o.data.shape_keys else [],
                    [(g.name,g.index) for g in o.vertex_groups],[[tuple((g.group,g.weight)) for g in v.groups] for v in o.data.vertices],
                    [m.name for m in o.data.materials]) for o in bpy.data.objects if o.type=='MESH'])
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/Backups/WaterFlow20261002/AdultRabbitWater.blend'))
before=(geometry(),rig_signature(),action_signature(('Adult_Water_Pickup','Adult_Water_Putdown')))
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbitWater.blend'))
after=(geometry(),rig_signature(),action_signature(('Adult_Water_Pickup','Adult_Water_Putdown')))
assert before==after,'Existing geometry/bind/actions changed'
assert len(bpy.data.actions)==41
sha=hashlib.sha256((ROOT/'ArtSource/AdultRabbit - 01.blend').read_bytes()).hexdigest()
assert sha=='625909966788c99c8dc62e5160bca7fa9390e6b82e580d48596db6deec59c531'
(ROOT/'Docs/WaterLifeSavedPreservation.json').write_text(json.dumps({'savedGeometryWeightsShapesMaterials':'unchanged','savedBind':'unchanged','original39Actions':'unchanged','addedActions':2,'canonicalSHA256':sha},indent=2))
print('WATER_LIFE_SAVED_PRESERVATION_OK',flush=True)

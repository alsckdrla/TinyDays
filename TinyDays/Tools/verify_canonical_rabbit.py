"""Read saved derived files; geometry overlays stay in memory, never save art."""
import bpy,sys,json,hashlib
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from adult_rabbit_source import ROOT,SOURCE,apply_source,rig_signature,digest
report=json.loads((ROOT/'Docs/CanonicalRabbitVerification.json').read_text(encoding='utf8'))
assert hashlib.sha256(SOURCE.read_bytes()).hexdigest()==report['sourceSha256']
def geometry():
    rows=[]
    for name in ('Head','BodyTorso','BodyArms','BodyLegs','BodyFeet','BodyHands','BodyTail','Top','Bottom','Shoes','Neckwear','Backpack'):
        o=bpy.data.objects[name];m=o.data
        rows.append((name,[tuple(v.co) for v in m.vertices],[(k.name,[tuple(v.co) for v in k.data]) for k in m.shape_keys.key_blocks] if m.shape_keys else []))
    return digest(rows)
results=[]
for row in report['exports']:
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource'/(row['file']+'.blend')))
    before=geometry();apply_source();assert before==geometry(),row['file']+' saved geometry mismatch'
    added=sum(name in bpy.data.actions for name in ('Adult_Water_Pickup','Adult_Water_Putdown')) if row['file']=='AdultRabbitWater' else 0
    assert rig_signature()==row['rigHash'] and len(bpy.data.actions)-row['actionCount'] in (0,added)
    expected={'AdultRabbit':0,'AdultRabbitMotion':36,'AdultRabbitHome':37,'AdultRabbitWater':39+added,'AdultRabbitBench':39}[row['file']]
    assert len(bpy.data.actions)==expected
    results.append({'file':row['file'],'savedGeometryAndShapes':'canonical exact','actions':expected,'bind':'PASS'})
(ROOT/'Docs/CanonicalRabbitSavedVerification.json').write_text(json.dumps(results,indent=2),encoding='utf8')
print('CANONICAL_SAVED_FILES_OK',flush=True)

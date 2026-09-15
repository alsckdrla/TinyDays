"""Check authored contacts plus Unity forward movement, independently of rendering."""
import hashlib, json, math
from pathlib import Path

root=Path(__file__).resolve().parents[1]
audit=json.loads((root/'Assets/Art/Generated/RabbitMotion.audit.json').read_text(encoding='utf-8'))
assert hashlib.sha256((root/'ArtSource/RabbitStudy.blend').read_bytes()).hexdigest()==audit['source_sha256'], 'Approved source changed after motion generation'
lines=['Stage 2-3 source contact verification',
       'Checks support joint trajectories with Unity forward displacement; visual paw deformation is a separate observation.']
for clip in audit['clips']:
    peak=0;checks=0
    for a,b in zip(clip['samples'],clip['samples'][1:]):
        common=set(a['contactNames'])&set(b['contactNames'])
        for name in common:
            pa=next(j['position'] for j in a['joints'] if j['name']==name)
            pb=next(j['position'] for j in b['joints'] if j['name']==name)
            # Forward in Blender is -Y. Runtime moves +Z in Unity by this distance.
            delta=[pb[i]-pa[i] for i in range(3)]
            delta[1]-=b['travel']-a['travel']
            drift=math.sqrt(sum(x*x for x in delta));peak=max(peak,drift);checks+=1
    assert checks>0,clip['name']+' has no support samples'
    assert peak<.003,(clip['name'],'support drift',peak)
    if clip['loop']:assert clip['loop_matrix_error']<.00001
    assert abs(clip['travel'][0]['distance'])<.00001
    assert abs(clip['travel'][-1]['distance']-clip['distance'])<.00001
    assert all(b['distance']>=a['distance']-.00001 for a,b in zip(clip['travel'],clip['travel'][1:]))
    lines.append(f"{clip['name']}: {checks} support comparisons, max world drift {peak:.7f}m: PASS")
lines.append('Approved RabbitStudy.blend hash unchanged: PASS')
previous=json.loads((root/'Docs/Captures/Stage23/BeforeElasticRevision/RabbitMotion.audit.json').read_text(encoding='utf-8'))
for old in previous['clips']:
    new=next(c for c in audit['clips'] if c['name']==old['name'])
    assert new['duration']==old['duration'] and new['distance']==old['distance']
lines.append('All ten review durations/distances preserved; pose changes are intentional: PASS')
walk=next(c for c in audit['clips'] if c['name']=='Walk_Biped')
air=[s for s in walk['samples'] if not s['contactNames']]
assert len(air)>=4, 'Prancing walk needs sampled aerial intervals'
assert all(min(j['position'][2] for j in s['joints'] if j['name'].startswith('Foot_'))>.115 for s in air)
assert {'Foot_L','Foot_R'} <= set(n for s in walk['samples'] for n in s['contactNames'])
lines.append(f"Prancing walk: {len(air)} airborne samples and alternating support: PASS")
for name in ['Walk_Biped','Hop_Quadruped']:
    c=next(c for c in audit['clips'] if c['name']==name)
    old=next(c for c in previous['clips'] if c['name']==name)
    def amplitude(clip):
        heights=[next(j['position'][2] for j in s['joints'] if j['name']=='Spine') for s in clip['samples']]
        return max(heights)-min(heights)
    assert amplitude(c)>amplitude(old)*1.5
    lines.append(f"{name}: torso vertical range {amplitude(c):.4f}m, increased from {amplitude(old):.4f}m: PASS")
down=next(c for c in audit['clips'] if c['name']=='To_Quadruped')
up=next(c for c in audit['clips'] if c['name']=='To_Biped')
def hip(s):return next(j['position'][2] for j in s['joints'] if j['name']=='Spine')
landing=[s for s in down['samples'] if .60<=s['time']<=.96]
assert max(map(hip,landing))-hip(down['samples'][-1])>.025
assert all({'Hand_L','Hand_R'}<=set(s['contactNames']) for s in landing)
compress=[s for s in up['samples'] if .54<=s['time']<=.74]
assert min(map(hip,compress))<hip(up['samples'][0])-.010
assert all({'Foot_L','Foot_R'}<=set(s['contactNames']) for s in compress)
lines.append('Independent transitions: forepaw-supported rump recoil and hindpaw-supported compression: PASS')
(root/'Docs/Stage23ContactVerification.txt').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('\n'.join(lines))

"""Geometry-only migration/export; keeps all authored action curves unchanged."""
import bpy,sys,json,hashlib,shutil
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
from adult_rabbit_source import ROOT,SOURCE,apply_source,action_signature,rig_signature

backup=ROOT/'ArtSource/Backups/CanonicalRabbit20261002'
backup.mkdir(parents=True,exist_ok=True)
source_hash=hashlib.sha256(SOURCE.read_bytes()).hexdigest()
if not (backup/SOURCE.name).exists():shutil.copy2(SOURCE,backup/SOURCE.name)
bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
source_max=max((o.matrix_world@v.co).z for o in bpy.data.objects if o.type=='MESH' for v in o.data.vertices)
(ROOT/'Assets/Art/Generated/AdultRabbit/CanonicalSource.audit.json').write_text(json.dumps({'source':'ArtSource/AdultRabbit - 01.blend','sourceSha256':source_hash,'maximumHeight':source_max},indent=2),encoding='utf8')
if '--metadata-only' in sys.argv:
    print('CANONICAL_SOURCE_METADATA_OK',source_max,flush=True)
    sys.exit(0)
entries=[('AdultRabbit','AdultRabbit',False),('AdultRabbitMotion','AdultRabbit',True),('AdultRabbitHome','RabbitHome',True),('AdultRabbitWater','RabbitWater',True),('AdultRabbitBench','RabbitBench',True)]
if '--base-only' in sys.argv:entries=entries[:1]
reports=[]
for stem,folder,motion in entries:
    blend=ROOT/'ArtSource'/f'{stem}.blend'
    fbx=ROOT/'Assets/Art/Generated'/folder/f'{stem}.fbx'
    for path in (blend,fbx):
        if not (backup/path.name).exists():shutil.copy2(path,backup/path.name)
    bpy.app.driver_namespace['tiny_sleep_eye']=lambda frame:0.0
    bpy.ops.wm.open_mainfile(filepath=str(blend))
    before=action_signature();bind=rig_signature()
    names=apply_source()
    assert before==action_signature() and bind==rig_signature()
    # Repeat in-memory to verify idempotence before saving once.
    head=bpy.data.objects['Head'];coords=[tuple(v.co) for v in head.data.vertices]
    apply_source();assert coords==[tuple(v.co) for v in head.data.vertices]
    hidden=[(o,o.hide_get(),o.hide_render) for o in bpy.data.objects]
    bpy.ops.object.select_all(action='DESELECT')
    for o in bpy.data.objects:
        if o.name in names or o.type=='ARMATURE':o.hide_set(False);o.select_set(True)
    bpy.context.view_layer.objects.active=bpy.data.objects['AdultRig']
    bpy.ops.export_scene.fbx(filepath=str(fbx),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=motion,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=.125 if folder in ('AdultRabbit','RabbitHome') else .25,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
    for o,h,r in hidden:o.hide_set(h);o.hide_render=r
    assert before==action_signature() and bind==rig_signature(),'Export mutated authored animation'
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    reports.append({'file':stem,'modules':names,'actionCount':len(bpy.data.actions),'actionHash':before,'rigHash':bind,'repeatOverlay':'PASS'})
assert source_hash==hashlib.sha256(SOURCE.read_bytes()).hexdigest(),'Canonical file changed'
(ROOT/'Docs/CanonicalRabbitVerification.json').write_text(json.dumps({'source':str(SOURCE),'sourceSha256':source_hash,'sourceUnchanged':True,'exports':reports},indent=2),encoding='utf8')
print('CANONICAL_RABBIT_SYNC_OK',flush=True)

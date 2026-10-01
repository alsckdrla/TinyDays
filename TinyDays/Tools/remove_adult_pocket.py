"""One-time mesh-only migration; preserves every authored action and surviving vertex."""
import bpy,bmesh,hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
BACKUP=ROOT/'Logs/BeforePocketRemoval';BACKUP.mkdir(parents=True,exist_ok=True)
OUT=ROOT/'Assets/Art/Generated/AdultRabbit'
def digest(x):return hashlib.sha256(repr(x).encode()).hexdigest()
def actions():
    result=[]
    for a in bpy.data.actions:
        curves=[]
        for slot in a.slots:
            for layer in a.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for c in bag.fcurves:curves.append((c.data_path,c.array_index,[(tuple(k.co),tuple(k.handle_left),tuple(k.handle_right),k.interpolation) for k in c.keyframe_points]))
        result.append((a.name,sorted(curves)))
    return digest(sorted(result))
def vertex(o,v):return (tuple(v.co),tuple(sorted((o.vertex_groups[g.group].name,g.weight) for g in v.groups)))
def geometry(o,excluded=set()):
    verts=[vertex(o,v) for v in o.data.vertices if v.index not in excluded]
    faces=[(tuple(vertex(o,o.data.vertices[i]) for i in p.vertices),p.material_index,p.use_smooth) for p in o.data.polygons if not set(p.vertices)&excluded]
    return digest((verts,faces))
report=[]
for stem in ('AdultRabbit','AdultRabbitMotion'):
    source=ROOT/'ArtSource'/f'{stem}.blend'
    if not (BACKUP/source.name).exists():shutil.copy2(source,BACKUP/source.name)
    bpy.app.driver_namespace['tiny_sleep_eye']=lambda frame:0.0
    bpy.ops.wm.open_mainfile(filepath=str(source))
    top=bpy.data.objects['Top'];mesh=top.data
    adjacency={v.index:set() for v in mesh.vertices}
    for edge in mesh.edges:
        a,b=edge.vertices;adjacency[a].add(b);adjacency[b].add(a)
    unseen=set(adjacency);candidates=[]
    while unseen:
        component=set();stack=[next(iter(unseen))]
        while stack:
            i=stack.pop()
            if i in component:continue
            component.add(i);stack.extend(adjacency[i]-component)
        unseen-=component
        if all(.09<mesh.vertices[i].co.x<.21 and -.19<mesh.vertices[i].co.y<-.14 and .58<mesh.vertices[i].co.z<.70 for i in component):candidates.append(component)
    print('POCKET_COMPONENTS',[(len(c),[tuple(mesh.vertices[i].co) for i in sorted(c)][:3]) for c in candidates],flush=True)
    assert candidates and sum(len(c) for c in candidates)<100,(stem,'Unexpected pocket geometry')
    removed=set.union(*candidates)
    assert all(vertex(top,mesh.vertices[i])[1]==(('Spine',1.0),) for i in removed),'Pocket weight mismatch'
    expected=geometry(top,removed);before_actions=actions()
    others={o.name:geometry(o) for o in bpy.data.objects if o.type=='MESH' and o!=top}
    rig=bpy.data.objects['AdultRig'];bind=digest([(b.name,tuple(v for row in b.matrix_local for v in row),b.parent.name if b.parent else None) for b in rig.data.bones])
    mesh.calc_loop_triangles();old_tri=len(mesh.loop_triangles)
    bm=bmesh.new();bm.from_mesh(mesh);bm.verts.ensure_lookup_table();bmesh.ops.delete(bm,geom=[bm.verts[i] for i in removed],context='VERTS');bm.to_mesh(mesh);bm.free();mesh.update()
    assert geometry(top)==expected,'Surviving garment changed'
    assert actions()==before_actions,'Animation changed'
    assert all(geometry(bpy.data.objects[n])==h for n,h in others.items()),'Other mesh changed'
    assert bind==digest([(b.name,tuple(v for row in b.matrix_local for v in row),b.parent.name if b.parent else None) for b in rig.data.bones]),'Bind changed'
    mesh.calc_loop_triangles();new_tri=len(mesh.loop_triangles)
    hidden=[(o,o.hide_get()) for o in bpy.data.objects]
    bpy.ops.object.select_all(action='DESELECT')
    for o in bpy.data.objects:
        if o.type in {'MESH','ARMATURE'}:o.hide_set(False);o.select_set(True)
    bpy.context.view_layer.objects.active=rig
    motion=stem.endswith('Motion')
    bpy.ops.export_scene.fbx(filepath=str(OUT/f'{stem}.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=motion,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_step=.125,bake_anim_simplify_factor=0,mesh_smooth_type='FACE')
    for o,state in hidden:o.hide_set(state)
    assert actions()==before_actions
    bpy.ops.wm.save_as_mainfile(filepath=str(source))
    if not motion:
        path=OUT/'AdultRabbit.audit.json';audit=json.loads(path.read_text());entry=next(m for m in audit['modules'] if m['name']=='Top');entry['vertices']=len(mesh.vertices);entry['triangles']=new_tri;audit['trianglesAllModules']=sum(m['triangles'] for m in audit['modules']);path.write_text(json.dumps(audit,indent=2),encoding='utf8')
    report.append(f'{stem}: removed {len(removed)} vertices / {old_tri-new_tri} triangles. Remaining geometry/weights, other meshes, bind and all actions exact PASS. Action hash {before_actions}')
(ROOT/'Docs/AdultPocketPreservation.txt').write_text('\n'.join(report)+'\n',encoding='utf8')
print('POCKET_REMOVAL_OK',report,flush=True)

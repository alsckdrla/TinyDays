"""Stage 2-2 original rabbit, generic rig and TWO STATIC reference poses only.
Run with Blender --background --factory-startup --python-exit-code 1 --python.
Only owns ArtSource/RabbitStudy.blend and Assets/Art/Generated/RabbitStudy.*.
"""
import bpy, bmesh, math, json, sys
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.kdtree import KDTree

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Art/Generated'
CAP = ROOT / 'Docs/Captures/Stage22/Blender'
for p in (OUT, CAP, ROOT/'ArtSource'): p.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
scene.render.fps = 30
palette = {'Fur':'DCCDB5','Muzzle':'EDE1CC','Ear':'BD9689','Nose':'8B6D65',
           'Eye':'302D2B','Sclera':'EEE9DD','Shirt':'EEE4CE','Cloth':'657E7A','Pocket':'718C86',
           'Stitch':'B8B695','Button':'B89960','Ground':'D4D5C1'}
materials = {}
def linear(c): return c/12.92 if c <= .04045 else ((c+.055)/1.055)**2.4
for name, h in palette.items():
    mat = bpy.data.materials.new(name); mat.use_nodes = True
    c = tuple(linear(int(h[i:i+2],16)/255) for i in (0,2,4))+(1,)
    mat.diffuse_color = c
    bs = mat.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value = c; bs.inputs['Roughness'].default_value = .88
    materials[name] = mat

bones = {}
def bone(name, head, tail, parent=None): bones[name]=(Vector(head),Vector(tail),parent)
bone('Root',(0,0,0),(0,0,.2))
bone('Spine',(0,0,.59),(0,0,1.105),'Root')
bone('Head',(0,0,1.105),(0,0,1.525),'Spine')
bone('Tail',(0,.19,.65),(0,.36,.69),'Spine')
for side, s in [('L',1),('R',-1)]:
    bone('Ear_'+side,(s*.13,.025,1.60),(s*.195,.070,2.07),'Head')
    bone('UpperArm_'+side,(s*.18,0,1.06),(s*.29,-.015,.87),'Spine')
    bone('Forearm_'+side,(s*.315,-.015,.87),(s*.345,-.075,.645),'UpperArm_'+side)
    bone('Hand_'+side,(s*.345,-.075,.645),(s*.345,-.095,.575),'Forearm_'+side)
    bone('Thigh_'+side,(s*.15,0,.64),(s*.165,.025,.33),'Spine')
    bone('Shin_'+side,(s*.165,.025,.33),(s*.17,0,.105),'Thigh_'+side)
    bone('Foot_'+side,(s*.17,0,.105),(s*.17,-.18,.085),'Shin_'+side)

ad = bpy.data.armatures.new('RabbitGenericSkeleton')
rig = bpy.data.objects.new('RabbitRig',ad); scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig; rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for name,(h,t,p) in bones.items():
    eb=ad.edit_bones.new(name); eb.head=h; eb.tail=t
    if p: eb.parent=ad.edit_bones[p]
bpy.ops.object.mode_set(mode='OBJECT'); rig.show_in_front=True
parts=[]
def attach(obj,name,mat,bone_name):
    obj.name=name; obj.data.materials.append(materials[mat])
    bpy.context.view_layer.objects.active=obj
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    vg=obj.vertex_groups.new(name=bone_name); vg.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    mod=obj.modifiers.new('Rabbit deformation','ARMATURE'); mod.object=rig
    obj.parent=rig; parts.append(obj); return obj
def ell(name,loc,scale,mat,bn,segments=16,rings=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=loc)
    ob=bpy.context.object; ob.scale=scale
    for poly in ob.data.polygons: poly.use_smooth=True
    return attach(ob,name,mat,bn)
def box(name,loc,scale,mat,bn,bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc)
    ob=bpy.context.object; ob.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=ob.modifiers.new('Soft corners','BEVEL');mod.width=bevel;mod.segments=2
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return attach(ob,name,mat,bn)
def segment(name,start,end,radii,mat,bn):
    a,b=Vector(start),Vector(end); ob=ell(name,(a+b)*.5,(radii[0],radii[1],(a-b).length*.62),mat,bn)
    ob.rotation_mode='QUATERNION';ob.rotation_quaternion=Vector((0,0,1)).rotation_difference(b-a)
    return ob
def strap(name,x):
    # One continuous fitted ribbon over each shoulder, rather than floating bars.
    yz=[(-.176,1.045),(-.14,1.08),(-.085,1.12),(0,1.14),
        (.085,1.12),(.14,1.08),(.176,1.045),(.214,.98)]
    verts=[(x+dx,y,z) for y,z in yz for dx in [-.03,.03]]
    faces=[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(yz)-1)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    ob=bpy.data.objects.new(name,mesh);scene.collection.objects.link(ob)
    bpy.context.view_layer.objects.active=ob
    mod=ob.modifiers.new('Strap thickness','SOLIDIFY');mod.thickness=.014
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return attach(ob,name,'Cloth','Spine')

def smoothstep(a,b,x):
    t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)

def soft_limb(name,chain,radius,cloth,cloth_end):
    # Continuous rings across joints; no separate biceps, elbow ball or wrist bead.
    points=[bones[n][0] for n in chain]+[bones[chain[-1]][1]]
    distances=[0]
    for a,b in zip(points,points[1:]): distances.append(distances[-1]+(b-a).length)
    total=distances[-1]; verts=[]; faces=[]; weights=[]; steps=24; sides=16
    for row in range(steps+1):
        d=total*row/steps
        idx=min(len(chain)-1,next((i for i in range(len(chain)) if d<=distances[i+1]),len(chain)-1))
        t=(d-distances[idx])/(distances[idx+1]-distances[idx])
        center=points[idx].lerp(points[idx+1],t)
        tangent=(points[idx+1]-points[idx]).normalized()
        axis=Vector((1,0,0));axis=(axis-tangent*tangent.dot(axis)).normalized();cross=tangent.cross(axis)
        cap=min(1,d/.085,(total-d)/.07)
        width=radius*math.sqrt(max(.0004,1-(1-cap)**2))*(1-.10*d/total)
        ws={chain[idx]:1.0}
        for j in range(1,len(chain)):
            edge=distances[j]
            if abs(d-edge)<.075:
                blend=smoothstep(edge-.075,edge+.075,d);ws={chain[j-1]:1-blend,chain[j]:blend};break
        if chain[0].startswith('UpperArm_'):
            shoulder=smoothstep(0,.18,d)
            ws={bn:w*shoulder for bn,w in ws.items()}
            ws['Spine']=1-shoulder
        for col in range(sides):
            angle=2*math.pi*col/sides
            v=center+width*(axis*math.cos(angle)+cross*math.sin(angle))
            if chain[-1].startswith('Foot_'): v.z=max(0,v.z)
            verts.append(v)
            weights.append(ws)
    for row in range(steps):
        for col in range(sides):
            a=row*sides+col;b=row*sides+(col+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces.extend([tuple(reversed(range(sides))),tuple(steps*sides+i for i in range(sides))])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    ob=bpy.data.objects.new(name,mesh);scene.collection.objects.link(ob)
    attach(ob,name,'Fur',chain[0]);ob.data.materials.append(materials[cloth])
    ob.vertex_groups.clear()
    for bn in chain+(['Spine'] if chain[0].startswith('UpperArm_') else []):
        group=ob.vertex_groups.new(name=bn)
        for i,ws in enumerate(weights):
            if ws.get(bn,0)>0: group.add([i],ws[bn],'REPLACE')
    for p in mesh.polygons:
        p.use_smooth=True
        along=sum(v//sides for v in p.vertices)/len(p.vertices)/steps
        p.material_index=1 if along<cloth_end else 0
    return ob

def fuse_shoulders(objects):
    # Exact unions retain clothing boundary topology while joining shoulders.
    # Recover bone weights from the rest surface after light seam smoothing.
    bpy.ops.object.select_all(action='DESELECT')
    for ob in objects:
        ob.select_set(True)
        for mod in list(ob.modifiers): ob.modifiers.remove(mod)
    operands=[]
    for source in objects:
        copy=source.copy();copy.data=source.data.copy();scene.collection.objects.link(copy)
        copy.select_set(False);operands.append(copy)
    bpy.context.view_layer.objects.active=objects[0]
    for ob in objects: parts.remove(ob)
    bpy.ops.object.join(); ob=bpy.context.object
    samples=[]; kd=KDTree(len(ob.data.vertices))
    for v in ob.data.vertices:
        kd.insert(v.co,v.index)
        samples.append({ob.vertex_groups[g.group].name:g.weight for g in v.groups})
    kd.balance()
    slots=list(ob.data.materials)
    for operand in operands:
        old_slots=list(operand.data.materials)
        mapped=[slots.index(old_slots[p.material_index]) for p in operand.data.polygons]
        operand.data.materials.clear()
        for mat in slots: operand.data.materials.append(mat)
        for p,index in zip(operand.data.polygons,mapped): p.material_index=index
        bm=bmesh.new();bm.from_mesh(operand.data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(operand.data);bm.free()
    ob.data=operands[0].data.copy()
    for operand in operands[1:]:
        union=ob.modifiers.new('Joined shoulder surface','BOOLEAN')
        union.operation='UNION';union.solver='EXACT';union.object=operand
        union.use_self=True;union.use_hole_tolerant=True
        bpy.context.view_layer.update()
        bpy.ops.object.modifier_apply(modifier=union.name)
        assert len(ob.data.vertices)>0, 'Shoulder union produced an empty surface'
    for operand in operands: bpy.data.objects.remove(operand,do_unlink=True)
    smooth=ob.modifiers.new('Soft shoulder flow','SMOOTH');smooth.factor=.3;smooth.iterations=3
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    ob.vertex_groups.clear(); groups={n:ob.vertex_groups.new(name=n) for n in bones}
    for v in ob.data.vertices:
        weights={}
        for _,idx,dist in kd.find_n(v.co,6):
            proximity=1/max(.004,dist)**2
            for bn,w in samples[idx].items(): weights[bn]=weights.get(bn,0)+proximity*w
        strongest=sorted(weights.items(),key=lambda item:item[1],reverse=True)[:4]
        total=sum(w for _,w in strongest)
        point=ob.matrix_world@v.co
        if point.z>1.015 and abs(point.x)<.21:
            # A monotonic neck gradient avoids nearest-surface weight islands
            # when the chest turns horizontal and the head remains raised.
            head_weight=smoothstep(1.015,1.20,point.z)
            strongest=[('Spine',1-head_weight),('Head',head_weight)];total=1
        for bn,w in strongest: groups[bn].add([v.index],w/total,'REPLACE')
    for p in ob.data.polygons:
        p.use_smooth=True
    mod=ob.modifiers.new('Rabbit deformation','ARMATURE');mod.object=rig
    ob.name='Connected torso shoulders and neck';parts.append(ob)

# One pear-shaped surface for shirt and overalls, rather than stacked belly muscles.
torso=ell('Soft pear body',(0,0,.835),(.32,.245,.345),'Shirt','Spine',24,16)
torso.data.materials.append(materials['Cloth'])
for v in torso.data.vertices:
    u=v.co.z/.345; v.co.x*=1-.18*u;v.co.y*=1-.10*u
for p in torso.data.polygons:
    center=sum((torso.data.vertices[i].co for i in p.vertices),Vector())/len(p.vertices)
    z=center.z+.835
    p.material_index=1 if z<.89 or (center.y<0 and abs(center.x)<.19 and z<1.045) else 0
box('Bib pocket',(0,-.245,.875),(.17,.025,.12),'Pocket','Spine',.025)
upper_body=[torso]
for side,s in [('L',1),('R',-1)]:
    strap('Shoulder strap '+side,s*.135)
    ell('Brass button '+side,(s*.135,-.186,1.028),(.020,.012,.020),'Button','Spine',12,6)
    upper_body.append(soft_limb('Soft arm '+side,['UpperArm_'+side,'Forearm_'+side,'Hand_'+side],.104,'Shirt',.43))
    soft_limb('Soft leg and paw '+side,['Thigh_'+side,'Shin_'+side,'Foot_'+side],.118,'Cloth',.50)
ell('Tail',(0,.263,.67),(.12,.12,.115),'Muzzle','Tail',16,8)
upper_body.append(ell('Neck',(0,0,1.135),(.175,.15,.12),'Fur','Head'))
fuse_shoulders(upper_body)
ell('Head',(0,-.016,1.40),(.295,.252,.28),'Fur','Head',24,16)

def rounded_eye(side,s):
    # Concentric surface rings preserve the eye envelope. The 75% pupil is
    # bounded by a true curved ring, not a straight cut or an overlapping disk.
    rx,ry,rz=.048,.027,.062
    center=Vector((s*.095,-.253,1.455));segments=40
    pupil_angle=math.asin(.75)
    angles=[0]+[math.asin(r) for r in [.18,.36,.55,.75,.84,.92,.97]]+[math.pi/2,1.9,2.2,2.55,2.85,math.pi]
    verts=[center+Vector((rx*math.sin(a)*math.cos(2*math.pi*j/segments),
                          -ry*math.cos(a),rz*math.sin(a)*math.sin(2*math.pi*j/segments)))
           for a in angles for j in range(segments)]
    faces=[];indices=[]
    for i in range(len(angles)-1):
        for j in range(segments):
            faces.append((i*segments+j,i*segments+(j+1)%segments,(i+1)*segments+(j+1)%segments,(i+1)*segments+j))
            indices.append(0 if angles[i+1]<=pupil_angle+.00001 else 1)
    mesh=bpy.data.meshes.new('Rounded eye '+side);mesh.from_pydata(verts,[],faces);mesh.update()
    eye=bpy.data.objects.new('Eye '+side,mesh);scene.collection.objects.link(eye)
    attach(eye,'Eye '+side,'Eye','Head');mesh.materials.append(materials['Sclera'])
    for p,index in zip(mesh.polygons,indices):p.material_index=index;p.use_smooth=True
    bm=bmesh.new();bm.from_mesh(mesh)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()

for side,s in [('L',1),('R',-1)]:
    pad=ell('Whisker pad '+side,(s*.035,-.256,1.306),(.047,.037,.054),'Muzzle','Head',20,14)
    for v in pad.data.vertices:
        t=v.co.z/.054
        v.co.x*=1-.20*t
        v.co.x-=s*.013*t
    rounded_eye(side,s)
    ear=segment('Ear shell '+side,bones['Ear_'+side][0],bones['Ear_'+side][1],(.092,.055),'Fur','Ear_'+side)
    a,b,_=bones['Ear_'+side]; center=(a+b)*.5+Vector((0,-.047,0))
    inset=ell('Ear inner '+side,center,(.054,.016,.21),'Ear','Ear_'+side)
    inset.rotation_mode='QUATERNION';inset.rotation_quaternion=Vector((0,0,1)).rotation_difference(b-a)
ell('Nose',(0,-.287,1.361),(.024,.020,.023),'Nose','Head',16,10)

# Join into one skinned mesh with material slots and named vertex groups.
bpy.ops.object.select_all(action='DESELECT')
for p in parts: p.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join()
body=bpy.context.object;body.name='RabbitBody'; body.data.name='RabbitBodyMesh'
for mod in list(body.modifiers): body.modifiers.remove(mod)
mod=body.modifiers.new('Rabbit skin','ARMATURE');mod.object=rig

def ik_joint(a,c,l1,l2,pole):
    a,c=Vector(a),Vector(c); d=c-a; dist=min(d.length,l1+l2-.0001); u=d.normalized()
    x=(l1*l1-l2*l2+dist*dist)/(2*dist)
    v=Vector(pole);v=(v-u*v.dot(u)).normalized()
    return a+u*x+v*math.sqrt(max(0,l1*l1-x*x))
def length(n): return (bones[n][1]-bones[n][0]).length
quad={}
quad['Root']=(Vector((0,0,0)),Vector((0,0,.2)))
quad['Spine']=(Vector((0,.25,.40)),Vector((0,.25,.40))+Vector((0,-1,-.10)).normalized()*length('Spine'))
spine_rotation=(bones['Spine'][1]-bones['Spine'][0]).rotation_difference(quad['Spine'][1]-quad['Spine'][0])
def on_spine(v): return quad['Spine'][0]+spine_rotation@(Vector(v)-bones['Spine'][0])
head_base=on_spine(bones['Head'][0])
quad['Head']=(head_base+Vector((0,-.045,.015)),head_base+Vector((0,-.27,.37)))
# The tail rests behind the rump, rather than rotating onto the top of the back.
quad['Tail']=(Vector((0,.40,.45)),Vector((0,.575,.46)))
for side,s in [('L',1),('R',-1)]:
    a=on_spine(bones['UpperArm_'+side][0]);c=Vector((s*.205,a.y-.08,.08))
    b=ik_joint(a,c,length('UpperArm_'+side),length('Forearm_'+side),(0,1,0))
    quad['UpperArm_'+side]=(a,b);quad['Forearm_'+side]=(b,c)
    quad['Hand_'+side]=(c,c+Vector((0,-.065,-.026)))
    a=on_spine(bones['Thigh_'+side][0]);c=Vector((s*.20,a.y+.09,.105))
    b=ik_joint(a,c,length('Thigh_'+side),length('Shin_'+side),(0,-1,0))
    quad['Thigh_'+side]=(a,b);quad['Shin_'+side]=(b,c)
    quad['Foot_'+side]=(c,c+Vector((0,-.18,-.02)))
    # Ears follow the head's evaluated transform instead of hard-coded endpoints.

def set_pose(targets):
    for pb in rig.pose.bones: pb.matrix_basis=Matrix.Identity(4)
    bpy.context.view_layer.update()
    for n in bones:
        if n not in targets: continue
        h,t=targets[n];rest=ad.bones[n]
        q=(rest.tail_local-rest.head_local).rotation_difference(t-h)
        m=q.to_matrix().to_4x4() @ rest.matrix_local
        m.translation=h;rig.pose.bones[n].matrix=m
        bpy.context.view_layer.update()

actions=[]
rig.animation_data_create()
for name,targets in [('Pose_Biped',{}),('Pose_Quadruped',quad)]:
    rig.animation_data.action=None;set_pose(targets)
    action=bpy.data.actions.new(name);rig.animation_data.action=action;actions.append(action)
    for frame in [1,2]:
        for pb in rig.pose.bones:
            pb.rotation_mode='QUATERNION'
            for prop in ['location','rotation_quaternion','scale']: pb.keyframe_insert(prop,frame=frame,group=pb.name)
    action.use_fake_user=True

rig.animation_data.action=actions[0];scene.frame_set(1)
bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);body.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'RabbitStudy.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},
    apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,
    bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,
    bake_anim_simplify_factor=0,mesh_smooth_type='FACE')

report={'blender':bpy.app.version_string,'source':'RabbitStudy.blend','fbx':'RabbitStudy.fbx',
        'revision':'User sketch face: centered 75-percent pupils, ivory sclera, small nose and descending whisker pads',
        'bones':list(bones),'vertices':len(body.data.vertices),'polygons':len(body.data.polygons),
        'actions':[a.name for a in actions],'poses':{}}
for action in actions:
    rig.animation_data.action=action;scene.frame_set(1);bpy.context.view_layer.update()
    ev=body.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=ev.to_mesh()
    verts=[ev.matrix_world@v.co for v in mesh.vertices]
    lo=[min(v[i] for v in verts) for i in range(3)];hi=[max(v[i] for v in verts) for i in range(3)]
    report['poses'][action.name]={'min':lo,'max':hi,'size':[hi[i]-lo[i] for i in range(3)]}
    contacts={}
    for side in ['L','R']:
        for part,prefixes in [('arm',['UpperArm_','Forearm_','Hand_']),('leg',['Thigh_','Shin_','Foot_'])]:
            groups={body.vertex_groups[p+side].index for p in prefixes}
            indices=[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if g.group in groups)>.99]
            assert indices, f'No contact vertices for {part}_{side}; maximum limb weight {max(sum(g.weight for g in v.groups if g.group in groups) for v in body.data.vertices)}'
            contacts[part+'_'+side]=min(verts[i].z for i in indices)
    report['poses'][action.name]['contact_min_z']=contacts
    for part,z in contacts.items():
        if action.name=='Pose_Quadruped' or part.startswith('leg'):
            assert -.03<=z<=.025, f'{action.name} {part} contact outside tolerance: {z}'
    assert min(v.z for v in verts)>-.03,'Model penetrates ground'
    ev.to_mesh_clear()
(OUT/'RabbitStudy.audit.json').write_text(json.dumps(report,indent=2),encoding='utf8')

# Presentation setup stays in the .blend but is excluded from FBX selection.
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.014));plane=bpy.context.object
plane.name='Review ground';plane.data.materials.append(materials['Ground'])
scene.world.color=(.25,.25,.25)
scene.world.use_nodes=True
scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.55,.58,.62,1)
scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
for name,loc,energy,size in [('Key',(-3,-4,6),450,4),('Fill',(4,-1,3),180,4),('Rim',(1,3,5),300,3)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.shape='DISK';data.size=size
    ob=bpy.data.objects.new(name,data);scene.collection.objects.link(ob);ob.location=loc
    ob.rotation_euler=(Vector((0,0,1))-ob.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',data);scene.collection.objects.link(cam)
scene.camera=cam;data.type='ORTHO';data.ortho_scale=2.8;data.lens=50
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.render.resolution_x=800;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
scene.render.image_settings.file_format='PNG'
views={'Front':(0,-7,1.65),'Side':(7,0,1.65),'Back':(0,7,1.65),'ThreeQuarter':(4,-6,3.4)}
rig.animation_data.action=actions[0];scene.frame_set(1)
cam.location=views['ThreeQuarter'];cam.rotation_euler=(Vector((0,0,1.1))-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/RabbitStudy.blend'))
if '--skip-renders' not in sys.argv:
    for action in actions:
        rig.animation_data.action=action;scene.frame_set(1);bpy.context.view_layer.update()
        for label,loc in views.items():
            cam.location=loc;cam.rotation_euler=(Vector((0,0,1.1))-cam.location).to_track_quat('-Z','Y').to_euler()
            scene.render.filepath=str(CAP/(action.name+'_'+label+'.png'))
            bpy.ops.render.render(write_still=True)
print('RABBIT_STAGE22_OK '+json.dumps(report))

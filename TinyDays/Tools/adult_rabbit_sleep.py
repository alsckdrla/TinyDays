"""Additive supine sleep, optional facial shape, no new bones or topology.

Face weight curves are also exported as JSON for deterministic Unity sampling.
The source shape driver uses the current rig action/time for Blender previews.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.geometry import intersect_ray_tri

ROOT=Path(__file__).resolve().parents[1]
CLIPS=[('Adult_Breathe_Lie',4,True),('Adult_Fall_Asleep',3,False),
       ('Adult_Sleep_Lie',6,True),('Adult_Wake_Lie',1.5,False)]
SHAPE='SleepEyesClosed'

def export_base():
    """Refresh the base FBX from its existing source without rebuilding geometry."""
    bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbit.blend'))
    add_eyes()
    bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbit.blend'))
    bpy.ops.object.select_all(action='DESELECT')
    for ob in bpy.data.objects:
        if ob.type in {'MESH','ARMATURE'}:ob.hide_set(False);ob.select_set(True)
    bpy.context.view_layer.objects.active=bpy.data.objects['AdultRig']
    bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Art/Generated/AdultRabbit/AdultRabbit.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,mesh_smooth_type='FACE')

def ease(x):
    x=max(0,min(1,x));return x*x*x*(10+x*(-15+6*x))

def eye_weight(clip,t):
    if clip==23:return ease((t-.25)/2.25)
    if clip==24:return 1.0
    if clip==25:return 1-ease((t-.12)/.98)
    return 0.0

def add_eyes():
    ob=bpy.data.objects['Head'];mesh=ob.data
    if mesh.shape_keys and SHAPE in mesh.shape_keys.key_blocks:return
    if not mesh.shape_keys:ob.shape_key_add(name='Basis')
    shape=ob.shape_key_add(name=SHAPE)
    # Connected islands distinguish white eye patches from face, neck and ears.
    adjacency={i:set() for i in range(len(mesh.vertices))}
    for p in mesh.polygons:
        for i in p.vertices:adjacency[i].update(p.vertices)
    unseen=set(adjacency);components=[]
    while unseen:
        seed=unseen.pop();group={seed};stack=[seed]
        while stack:
            for n in adjacency[stack.pop()]&unseen:unseen.remove(n);group.add(n);stack.append(n)
        components.append(group)
    face=max(components,key=lambda g:sum(1 for i in g if 1.2<mesh.vertices[i].co.z<1.83))
    mesh.calc_loop_triangles()
    triangles=[t.vertices[:] for t in mesh.loop_triangles if all(i in face for i in t.vertices)]
    dark={i for p in mesh.polygons if mesh.materials[p.material_index].name=='Dark' for i in p.vertices}
    changed=0
    for sign in (1,-1):
        origin=Vector((sign*.190,-2,1.487));direction=Vector((0,1,0));hits=[]
        for ids in triangles:
            a,b,c=(mesh.vertices[i].co for i in ids)
            hit=intersect_ray_tri(a,b,c,direction,origin,True)
            if hit is not None:
                normal=(b-a).cross(c-a).normalized()
                if normal.y>0:normal=-normal
                hits.append((hit,normal))
        center,normal=min(hits,key=lambda h:h[0].y)
        rotation=Vector((0,-1,0)).rotation_difference(normal)
        eyes=[g for g in components if g&dark and sum(mesh.vertices[i].co.x for i in g)*sign>0]
        assert len(eyes)==1
        for i in eyes[0]:
            v=rotation.inverted()@(mesh.vertices[i].co-center)
            v.z=v.z*.045+.004*(1-(v.x/.040)**2)
            v.y=v.y*.16-.0012
            shape.data[i].co=center+rotation@v;changed+=1
        patches=[g for g in components if len(g)==25 and (sum((mesh.vertices[i].co for i in g),Vector())/len(g)-center).length<.065]
        assert len(patches)==1
        for i in patches[0]:shape.data[i].co=mesh.vertices[i].co-normal*.055;changed+=1
    shape.value=0
    print('SLEEP_EYE_VERTICES',changed,flush=True)

def install_preview_driver():
    # A self-contained Text block makes the saved source preview reproducible.
    # Unity uses the baked JSON curve, not an arbitrary FBX action assignment.
    source='''import bpy, json
_tiny_sleep_timing=json.loads(bpy.data.texts['SleepCurves.json'].as_string())
def tiny_sleep_eye(frame):
    rig=bpy.data.objects.get('AdultRig')
    if not rig or not rig.animation_data or not rig.animation_data.action:return 0.0
    n=rig.animation_data.action.name;t=max(0,(frame-1)/30)
    for curve in _tiny_sleep_timing['clips']:
        if curve['name']!=n:continue
        t=t%curve['duration'] if curve['loop'] else min(t,curve['duration'])
        f=t*_tiny_sleep_timing['sampleRate'];a=min(int(f),len(curve['eyes'])-1)
        return curve['eyes'][a]+(curve['eyes'][min(a+1,len(curve['eyes'])-1)]-curve['eyes'][a])*(f-a)
    return 0.0
bpy.app.driver_namespace['tiny_sleep_eye']=tiny_sleep_eye
'''
    text=bpy.data.texts.get('SleepPreview.py') or bpy.data.texts.new('SleepPreview.py')
    text.clear();text.write(source);text.use_module=True
    namespace={};exec(source,namespace)
    key=bpy.data.objects['Head'].data.shape_keys.key_blocks[SHAPE]
    f=key.driver_add('value');f.driver.expression='tiny_sleep_eye(frame)'

def build(rig,base,assign,limb,key):
    add_eyes();scene=bpy.context.scene
    rig.animation_data.action=bpy.data.actions['Adult_Lie_Hold'];scene.frame_set(1);bpy.context.view_layer.update()
    rest={b.name:b.matrix.copy() for b in rig.pose.bones}
    group=[b.name for b in rig.data.bones if b.name=='Spine' or any(p.name=='Spine' for p in b.parent_recursive)]
    inv=rig.data.bones['Spine'].matrix_local.inverted()
    marker=rest['Spine']@inv@Vector((0,-.17,.92));pivot=rest['Head'].translation.copy()
    lever=max(.1,pivot.y-marker.y)
    rows=[]
    for index,(name,duration,loop) in enumerate(CLIPS,22):
        old=bpy.data.actions.get(name)
        if old:bpy.data.actions.remove(old)
        action=bpy.data.actions.new(name);action.use_fake_user=True;rig.animation_data.action=action
        clock=0.0
        for sample in range(round(duration*120)+1):
            t=sample/120;p=t/duration
            sleeping=1.0 if index==24 else 0.0
            if index in (23,25):
                # Standalone source preview starts at phase zero. State-machine
                # playback in Unity retains its existing phase instead.
                decay=(1+2*t/.8)*math.exp(-2*t/.8)
                sleeping=1-decay if index==23 else decay
                if sample:clock+=(1/120)/(4+2*sleeping)
                p=clock
            # Smooth asymmetric phase: shorter inhale, longer exhale; C2 loop.
            phase=p+.04*(1-math.cos(2*math.pi*p))
            breath=.5-.5*math.cos(2*math.pi*phase)
            amplitude=.009+.003*sleeping
            lift=amplitude*breath
            m={n:v.copy() for n,v in rest.items()}
            turn=Matrix.Translation(pivot)@Matrix.Rotation(-lift/lever,4,'X')@Matrix.Translation(-pivot)
            for n in group:m[n]=turn@m[n]
            # The head stays supported. Rotation about its attachment preserves
            # the neck's pivot distance, not a separately translated head.
            for n in ('Head','Ear_L','Ear_R'):m[n]=rest[n].copy()
            for side,sign in [('L',1),('R',-1)]:
                relax=sleeping
                target=rest['Hand_'+side].translation+Vector((0,-.001*relax,lift*.65-.0015*relax))
                limb(m,'UpperArm_'+side,'Forearm_'+side,'Hand_'+side,target,Vector((sign,0,-.25)))
                m['Hand_'+side]=rest['Hand_'+side].copy();m['Hand_'+side].translation=target
            assign(m)
            for n,strength in [('Ear_L',.003),('Ear_R',.002),('NeckSocket',.004)]:
                pb=rig.pose.bones[n];pb.rotation_quaternion=pb.rotation_quaternion@Matrix.Rotation(strength*breath,4,'X').to_quaternion()
            key(1+sample/4)
        for slot in action.slots:
            for layer in action.layers:
                for strip in layer.strips:
                    bag=strip.channelbag(slot)
                    if bag:
                        for curve in bag.fcurves:
                            for point in curve.keyframe_points:point.interpolation='LINEAR'
        rows.append({'clip':index,'name':name,'duration':duration,'loop':loop,'eyes':[eye_weight(index,i/120) for i in range(round(duration*120)+1)]})
    dest=ROOT/'Assets/Art/Generated/AdultRabbit/SleepTiming.json'
    data=json.dumps({'sampleRate':120,'breathing':{'awakeReference':22,'sleepReference':24,'awakePeriod':4,'sleepPeriod':6,'blendSeconds':.8},'clips':rows})
    dest.write_text(data,encoding='utf8')
    text=bpy.data.texts.get('SleepCurves.json') or bpy.data.texts.new('SleepCurves.json');text.clear();text.write(data)
    # Save driver setup only after FBX export (caller invokes it).

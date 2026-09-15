"""Deterministic Tiny Days art library. Run with Blender --background --python."""
import bpy, math, random
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Art/Generated'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE = ROOT / 'ArtSource'
SOURCE.mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for m in list(bpy.data.materials): bpy.data.materials.remove(m)
random.seed(47)
palette = {'cream':'EDD9AF','yellow':'D8A643','roof':'555B60','wood':'956344','darkwood':'684937','leaf':'769C48','leaflight':'A4B858','trunk':'796044','soil':'886345','green':'648C43','orange':'EBA64D','white':'F4EAD4','pink':'DAA798','blue':'638B9E','glass':'8EB8B9','black':'343A3E','red':'BB7158'}
mats = {}
for name, h in palette.items():
    m=bpy.data.materials.new(name); m.diffuse_color=tuple(int(h[i:i+2],16)/255 for i in (0,2,4))+(1,); mats[name]=m
def finish(o,name,mat):
    o.name=name; o.data.materials.append(mats[mat]); return o
def box(name,loc,scale,mat,bevel=0.03):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc); o=bpy.context.object; o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=o.modifiers.new('Soft edges','BEVEL'); mod.width=bevel; mod.segments=1
        bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(o,name,mat)
def ico(name,loc,scale,mat,sub=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=loc); o=bpy.context.object; o.scale=scale; return finish(o,name,mat)
def cyl(name,loc,radius,depth,mat,vertices=8):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=radius,radius2=radius*.83,depth=depth,location=loc); return finish(bpy.context.object,name,mat)
def roof(w,d,z):
    verts=[(-w/2,-d/2,z),(w/2,-d/2,z),(0,-d/2,z+1.35),(-w/2,d/2,z),(w/2,d/2,z),(0,d/2,z+1.35)]
    mesh=bpy.data.meshes.new('Gable'); mesh.from_pydata(verts,[],[tuple(reversed(f)) for f in [(0,2,1),(3,4,5),(0,3,5,2),(2,5,4,1),(0,1,4,3)]]); mesh.update()
    ob=bpy.data.objects.new('Roof',mesh); bpy.context.collection.objects.link(ob); finish(ob,'Roof','roof')
    box('Ridge cap',(0,0,z+1.36),(.16,d+.07,.12),'roof')
def house(warehouse=False):
    w,d=(4.4,3.2) if warehouse else (3.6,3)
    box('Foundation',(0,0,.16),(w+.25,d+.25,.32),'cream')
    box('Walls',(0,0,1.5),(w,d,2.7),'cream' if warehouse else 'yellow')
    roof(w+.65,d+.65,2.82)
    box('Door',(0,-d/2-.04,.94),(1.35 if warehouse else .8,.14,1.75),'darkwood')
    for x in [-w*.32,w*.32]:
        box('Window frame',(x,-d/2-.10,1.6),(.81,.15,.92),'white')
        box('Window glass',(x,-d/2-.19,1.6),(.65,.04,.75),'glass')
        box('Window cross',(x,-d/2-.23,1.6),(.055,.05,.78),'white')
        box('Sill',(x,-d/2-.22,1.1),(.97,.35,.12),'wood')
    box('Doorstep',(0,-d/2-.42,.15),(1.45,.75,.3),'cream')
    for side in [-1,1]:
        box('Side window frame',(side*(w/2+.05),.2,1.6),(.12,.9,.95),'white')
        box('Side glass',(side*(w/2+.12),.2,1.6),(.04,.73,.77),'glass')
        box('Side mullion',(side*(w/2+.15),.2,1.6),(.035,.05,.8),'white')
        box('Side sill',(side*(w/2+.10),.2,1.08),(.32,1.02,.12),'wood')
    for x in [-w/2+.08,w/2-.08]: box('Corner trim',(x,-d/2-.06,1.48),(.12,.12,2.6),'white')
    box('Chimney',(.9,.6,3.7),(.48,.55,1.25),'cream'); box('Chimney cap',(.9,.6,4.3),(.62,.68,.16),'white')
    if warehouse:
        box('Sign',(0,-d/2-.16,2.42),(1.7,.18,.35),'wood')
        for x in [-.5,0,.5]: ico('Grain sign',(x,-d/2-.28,2.42),(.10,.03,.12),'orange')
def rabbit():
    for x in [-.16,.16]:
        side='L' if x<0 else 'R'
        ico('Boot_'+side,(x,-.10,.12),(.13,.22,.13),'darkwood',2)
        cyl('Leg_'+side,(x,0,.32),.10,.32,'blue')
    ico('Body',(0,0,.68),(.32,.23,.4),'blue',2)
    ico('Head',(0,-.025,1.15),(.31,.27,.3),'white',2)
    for x in [-.15,.15]:
        side='L' if x<0 else 'R'
        ob=ico('Ear_'+side,(x,0,1.60),(.095,.08,.33),'white',2); ob.rotation_euler[1]=-x*.8
        ico('InnerEar_'+side,(x,-.075,1.61),(.046,.022,.23),'pink',2)
        ico('Eye_'+side,(x,-.26,1.20),(.035,.025,.04),'black',2)
        ico('Arm_'+side,(x*2.15,-.03,.72),(.105,.12,.26),'white',2)
    ico('Muzzle',(0,-.26,1.07),(.14,.065,.10),'cream',2)
    ico('Nose',(0,-.324,1.11),(.045,.022,.03),'pink',1)
    ico('Tail',(0,.25,.57),(.13,.13,.13),'white',2)
    box('Apron',(0,-.226,.64),(.39,.035,.40),'cream')
def tree():
    cyl('Trunk',(0,0,1.15),.22,2.3,'trunk')
    for x,y,z,s in [(-.5,0,2.35,.9),(.45,.25,2.6,1),(0,-.35,3.12,.95),(0,.2,3.75,.65)]: ico('Crown',(x,y,z),(s,s*.86,s),'leaflight' if z>3 else 'leaf',2)
def fence():
    for x in [-1,1]:
        box('Post',(x,0,.53),(.14,.17,1.06),'wood'); ico('Post cap',(x,0,1.09),(.11,.13,.1),'wood')
    for z in [.35,.79]: box('Rail',(0,0,z),(2.1,.12,.12),'wood')
def crate():
    box('Crate',(0,0,.3),(.65,.55,.6),'wood')
    for x in [-.25,.25]: box('Brace',(x,-.29,.3),(.07,.06,.56),'cream')
    for z in [.17,.36,.54]: box('Slat gap',(0,-.322,z),(.5,.012,.018),'darkwood',0)
    for x in [-.18,0,.18]: ico('Produce',(x,0,.65),(.13,.16,.13),'orange')
def bench():
    for x in [-.65,.65]:
        box('Leg',(x,0,.26),(.12,.5,.52),'darkwood'); box('Back support',(x,.24,.69),(.1,.1,.95),'wood')
    for y in [-.18,0,.18]: box('Seat',(0,y,.53),(1.6,.14,.1),'wood')
    for z in [.85,1.07]: box('Back',(0,.25,z),(1.6,.1,.16),'wood')
def crop():
    ico('Crop root',(0,0,.12),(.12,.12,.17),'orange')
    for a in range(5):
        t=a*math.tau/5; ob=ico('Leaf',(.12*math.cos(t),.12*math.sin(t),.31),(.10,.09,.26),'green'); ob.rotation_euler=(.45*math.sin(t),.45*math.cos(t),0)
def flower():
    cyl('Stem',(0,0,.18),.018,.36,'green',5)
    for a in range(5):
        t=a*math.tau/5; ico('Petal',(.065*math.cos(t),.065*math.sin(t),.36),(.06,.06,.04),'white')
    ico('Pollen',(0,0,.38),(.04,.04,.035),'orange')
def grass():
    for a in range(5):
        t=a*math.tau/5; ob=ico('Blade',(.08*math.cos(t),.08*math.sin(t),.18),(.035,.055,.20),'leaflight' if a%2 else 'green'); ob.rotation_euler=(.3*math.sin(t),.3*math.cos(t),0)
def barrel():
    cyl('Barrel',(0,0,.39),.3,.78,'wood',12)
    for z in [.14,.60]:
        bpy.ops.mesh.primitive_torus_add(major_radius=.285 if z<.5 else .255,minor_radius=.025,major_segments=12,minor_segments=4,location=(0,0,z)); finish(bpy.context.object,'Barrel hoop','roof')
def shrub():
    for x,y,z,s in [(-.3,0,.3,.35),(.25,.1,.33,.4),(0,-.1,.48,.4)]: ico('Bush',(x,y,z),(s,s,s),'leaf',1)
def expansion_shed():
    box('Foundation',(0,0,.12),(3.35,2.55,.24),'cream')
    for x in [-1.45,1.45]:
        for y in [-1.05,1.05]: box('Post',(x,y,1.35),(.16,.16,2.5),'darkwood')
    roof(3.65,2.75,2.5)
    for x in [-1.15,0,1.15]: box('Rear slat',(x,1.12,1.35),(.82,.08,1.85),'wood')
    box('Storage crates',(0,.20,.44),(1.65,1.05,.70),'wood')
assets={'House':lambda:house(False),'Storehouse':lambda:house(True),'ExpansionShed':expansion_shed,'Rabbit':rabbit,'Tree':tree,'Fence':fence,'Crate':crate,'Bench':bench,'Crop':crop,'Flower':flower,'Rock':lambda:ico('Rock',(0,0,.25),(.55,.4,.35),'roof',1),'Grass':grass,'Barrel':barrel,'Shrub':shrub}
for name,fn in assets.items():
    bpy.ops.object.select_all(action='DESELECT')
    before=set(bpy.data.objects); fn(); objects=sorted(set(bpy.data.objects)-before,key=lambda o:o.name)
    for ob in objects: ob.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if name != 'Rabbit' and len(objects)>1: bpy.ops.object.join()
    ob=bpy.context.object
    if name != 'Rabbit': ob.name=name
    bpy.context.scene.cursor.location=(0,0,0)
    if name != 'Rabbit': bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False)
    if name == 'Rabbit':
        for object_to_hide in objects: object_to_hide.hide_set(True)
    else:
        bpy.context.object.hide_set(True)
for i,ob in enumerate(bpy.data.objects): ob.hide_set(False); ob.location.x=(i%5)*7; ob.location.y=(i//5)*7
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'TinyDays_ArtLibrary.blend'))
print('TINYDAYS_ART_OK')

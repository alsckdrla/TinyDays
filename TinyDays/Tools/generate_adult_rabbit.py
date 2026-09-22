"""Adult reference rabbit. Owns only AdultRabbit source/export and review captures.
No subdivision; explicit low-poly rings, imported smooth normals, modular skins.
Blender --background --factory-startup --python-exit-code 1 --python this_file
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
from mathutils.geometry import intersect_ray_tri

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Art/Generated/AdultRabbit'
OUT.mkdir(parents=True, exist_ok=True)
(ROOT/'ArtSource').mkdir(exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system = 'METRIC'
palette = {'Fur':'F3E8DB', 'Cloth':'D9DBD7', 'Pink':'CF8C89',
           'Dark':'403D42', 'Leather':'A87147', 'Brass':'D3B477'}
def lin(v): return v/12.92 if v <= .04045 else ((v+.055)/1.055)**2.4
mats = {}
for n,h in palette.items():
    m=bpy.data.materials.new(n); m.use_nodes=True
    c=tuple(lin(int(h[i:i+2],16)/255) for i in (0,2,4))+(1,)
    m.diffuse_color=c; bs=m.node_tree.nodes.get('Principled BSDF')
    bs.inputs['Base Color'].default_value=c
    bs.inputs['Roughness'].default_value=.73 if n!='Dark' else .48
    mats[n]=m

bones={}
def bone(n,h,t,p=None): bones[n]=(Vector(h),Vector(t),p)
bone('Root',(0,0,0),(0,0,.2))
bone('Pelvis',(0,0,.53),(0,0,.69),'Root')
bone('Spine',(0,0,.69),(0,0,1.10),'Pelvis')
bone('Head',(0,0,1.16),(0,0,1.72),'Spine')
bone('Tail',(0,.21,.58),(0,.39,.60),'Pelvis')
bone('BackSocket',(0,.30,.98),(0,.30,1.10),'Spine')
bone('NeckSocket',(0,0,1.12),(0,0,1.20),'Spine')
for s,k in ((1,'L'),(-1,'R')):
    bone('Ear_'+k,(s*.18,.015,1.76),(s*.27,.025,2.36),'Head')
    bone('UpperArm_'+k,(s*.204,0,1.025),(s*.305,-.025,.83),'Spine')
    bone('Forearm_'+k,(s*.305,-.025,.83),(s*.348,-.065,.665),'UpperArm_'+k)
    bone('Hand_'+k,(s*.348,-.065,.665),(s*.363,-.085,.575),'Forearm_'+k)
    bone('Thigh_'+k,(s*.135,0,.57),(s*.155,0,.34),'Pelvis')
    bone('Shin_'+k,(s*.155,0,.34),(s*.16,-.02,.13),'Thigh_'+k)
    bone('Foot_'+k,(s*.16,-.02,.13),(s*.16,-.18,.09),'Shin_'+k)
arm=bpy.data.armatures.new('AdultStandard_v2')
rig=bpy.data.objects.new('AdultRig',arm); bpy.context.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig;rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for n,(h,t,p) in bones.items():
    b=arm.edit_bones.new(n);b.head=h;b.tail=t
    if p:b.parent=arm.edit_bones[p]
bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
modules={}
def module(n):
    if n not in modules: modules[n]=[[],[],[],[],[]]
    return modules[n]
def add(n,verts,faces,mat,weights,smooth=True):
    vs,fs,ms,ws,ss=module(n); offset=len(vs)
    vs.extend(verts);fs.extend([tuple(i+offset for i in f) for f in faces])
    ms.extend([mat]*len(faces));ss.extend([smooth]*len(faces))
    ws.extend([weights]*len(verts) if isinstance(weights,dict) else weights)
def ell(n,c,r,mat,bn,segments=16,rings=10,tilt=0,head=False,orientation=None):
    vs=[];fs=[];co,si=math.cos(tilt),math.sin(tilt)
    def point(x,y,z):
        if head:
            # Broad lower cheeks and a continuous small projecting muzzle.
            x*=1+.10*math.exp(-((z+.22)/.22)**2)
            if y<0:y-=.105*math.exp(-(x/.25)**2-((z+.14)/.16)**2)*(-y/r[1])
        local=Vector((x*co+z*si,y,-x*si+z*co))
        return Vector(c)+(orientation@local if orientation else local)
    vs.append(point(0,0,r[2]))
    for j in range(1,rings):
        a=math.pi*j/rings
        for i in range(segments):
            t=2*math.pi*i/segments
            vs.append(point(r[0]*math.sin(a)*math.cos(t),r[1]*math.sin(a)*math.sin(t),r[2]*math.cos(a)))
    bottom=len(vs);vs.append(point(0,0,-r[2]))
    for i in range(segments): fs.append((0,1+i,1+(i+1)%segments))
    for j in range(rings-2):
        for i in range(segments):
            a=1+j*segments+i;b=1+j*segments+(i+1)%segments
            fs.append((a,a+segments,b+segments,b))
    for i in range(segments):fs.append((bottom,1+(rings-2)*segments+(i+1)%segments,1+(rings-2)*segments+i))
    add(n,vs,fs,mat,{bn:1})
def tube(n,points,radii,mat,weights,segments=12):
    vs=[];fs=[];ws=[]
    for j,p in enumerate(points):
        tangent=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])
        tangent.normalize();u=tangent.cross(Vector((0,1,0))).normalized();v=tangent.cross(u).normalized()
        for i in range(segments):
            a=2*math.pi*i/segments
            vs.append(Vector(p)+u*math.cos(a)*radii[j][0]+v*math.sin(a)*radii[j][1]);ws.append(weights[j])
    for j in range(len(points)-1):
        for i in range(segments):
            a=j*segments+i;b=j*segments+(i+1)%segments
            fs.append((a,b,b+segments,a+segments))
    fs.append(tuple(reversed(range(segments))));fs.append(tuple(range(len(vs)-segments,len(vs))))
    add(n,vs,fs,mat,ws)
def torso(n,mat,scale=1):
    if n=='Top':
        # Continuous inner lining -> open hem rim -> flared outer coat.
        # Neither cap spans the hem opening. Upper internal closure is hidden inside the torso.
        profile=[(.64,.248,.168),(.395,.300,.205),(.395,.316,.221),
                 (.48,.299,.202),(.64,.265,.179),(.83,.225*scale,.154*scale),
                 (.99,.205*scale,.13*scale),(1.075,.148*scale,.100*scale),(1.11,.095*scale,.080*scale)]
        vs=[];fs=[];ws=[];seg=12
        for z,rx,ry in profile:
            t=max(0,min(1,(z-.50)/.45))
            w={'Pelvis':1-t,'Spine':t} if 0<t<1 else ({'Spine':1} if t else {'Pelvis':1})
            for i in range(seg):
                a=2*math.pi*i/seg;x=rx*math.cos(a);y=ry*math.sin(a)
                # Local front clearance and gentle thigh response; waist remains on the torso.
                lower=max(0,min(1,(.83-z)/(.83-.395)))
                front=max(0,-math.sin(a))
                knee_band=lower*(2-lower)
                y-=(.045*lower+.020*knee_band)*front
                follow=.38*lower*(.35+.65*front)+.07*knee_band*front
                weights={bone:value*(1-follow) for bone,value in w.items()}
                blend=max(0,min(1,.5+x/.20));blend=blend*blend*(3-2*blend)
                weights['Thigh_L']=follow*blend;weights['Thigh_R']=follow*(1-blend)
                vs.append((x,y,z));ws.append({bone:value for bone,value in weights.items() if value>0})
        for j in range(len(profile)-1):
            for i in range(seg):
                a=j*seg+i;b=j*seg+(i+1)%seg;fs.append((a,b,b+seg,a+seg))
        fs.extend([tuple(reversed(range(seg))),tuple(range(len(vs)-seg,len(vs)))])
        add(n,vs,fs,mat,ws)
        return
    zs=[.48,.52,.64,.83,.99,1.075,1.11]
    rs=[(.245,.155),(.263,.17),(.25,.165),(.225,.154),(.205,.13),(.148,.100),(.095,.080)]
    weights=[]
    for z in zs:
        t=max(0,min(1,(z-.50)/.45));weights.append({'Pelvis':1-t,'Spine':t} if 0<t<1 else ({'Spine':1} if t else {'Pelvis':1}))
    tube(n,[(0,0,z) for z in zs],[(x*scale,y*scale) for x,y in rs],mat,weights,16)
def limbs(n,k,arm_limb,mat,scale=1):
    chain=['UpperArm_','Forearm_','Hand_'] if arm_limb else ['Thigh_','Shin_','Foot_']
    a,b,c=[bones[p+k][0] for p in chain]
    pts=[a,a.lerp(b,.35),a.lerp(b,.75),b,b.lerp(c,.25),b.lerp(c,.72),c]
    rr=[.078,.083,.080,.076,.073,.066,.061] if arm_limb else [.112,.108,.095,.085,.083,.078,.075]
    weights=[{chain[0]+k:1},{chain[0]+k:1},{chain[0]+k:.8,chain[1]+k:.2},
             {chain[0]+k:.5,chain[1]+k:.5},{chain[0]+k:.2,chain[1]+k:.8},{chain[1]+k:1},{chain[1]+k:1}]
    if arm_limb:
        # The shoulder surface follows a rounded descending arc; skeleton stays v2.
        pts[0]=Vector((a.x,0,1.015))
        pts[0:0]=[Vector((a.x*.43,0,1.068)),Vector((a.x*.72,0,1.047))]
        rr[0:0]=[.032,.058]
        weights[0:0]=[{'Spine':.95,chain[0]+k:.05},{'Spine':.65,chain[0]+k:.35}]
    tube(n,pts,[(r*scale,r*scale) for r in rr],mat,weights)
def rounded_box(n,c,size,mat,bn,bevel=.03):
    bpy.ops.mesh.primitive_cube_add(size=1,location=c);ob=bpy.context.object
    ob.scale=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        mod=ob.modifiers.new('Single edge chamfer','BEVEL');mod.width=bevel;mod.segments=1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    add(n,[ob.matrix_world@v.co for v in ob.data.vertices],[tuple(p.vertices) for p in ob.data.polygons],mat,{bn:1},False)
    bpy.data.objects.remove(ob,do_unlink=True)
def ribbon(n,points,width,depth,mat,bn):
    # Closed rectangular strap, no subdivision.
    vs=[];fs=[]
    for j,p in enumerate(points):
        p=Vector(p);t=(Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])).normalized()
        seed=Vector((1,0,0)) if abs(t.x)<.9 else Vector((0,1,0))
        u=(seed-t*seed.dot(t)).normalized();v=t.cross(u).normalized()
        vs.extend([p-u*width/2-v*depth/2,p+u*width/2-v*depth/2,p+u*width/2+v*depth/2,p-u*width/2+v*depth/2])
    for j in range(len(points)-1):
        for i in range(4):fs.append((4*j+i,4*j+(i+1)%4,4*(j+1)+(i+1)%4,4*(j+1)+i))
    fs.extend([(3,2,1,0),tuple(range(len(vs)-4,len(vs)))])
    add(n,vs,fs,mat,{bn:1})

def inner_ear(side,key):
    # A fitted patch on the ear surface, not a floating flattened ellipsoid.
    vs=[];fs=[];segments=12;rings=3;tilt=side*.15
    def point(x,z,offset):
        y=.012-.074*math.sqrt(max(.001,1-(x/.112)**2-(z/.245)**2))+offset
        return (side*.223-.09*math.sin(tilt)+x*math.cos(tilt)+z*math.sin(tilt),y,2.055-.09*math.cos(tilt)-x*math.sin(tilt)+z*math.cos(tilt))
    # Keep both layers clear of the faceted shell after the ear is shortened.
    for offset in (-.009,-.004):
        start=len(vs);vs.append(point(0,0,offset))
        for j in range(1,rings+1):
            for i in range(segments):
                a=2*math.pi*i/segments;vs.append(point(.074*j/rings*math.cos(a),.185*j/rings*math.sin(a),offset))
        for i in range(segments):fs.append((start,start+1+i,start+1+(i+1)%segments))
        for j in range(rings-1):
            for i in range(segments):
                a=start+1+j*segments+i;b=start+1+j*segments+(i+1)%segments;fs.append((a,b,b+segments,a+segments))
    layer=1+segments*rings
    for i in range(segments):
        a=1+(rings-1)*segments+i;b=1+(rings-1)*segments+(i+1)%segments;fs.append((a,b,b+layer,a+layer))
    add('Head',vs,fs,'Pink',{'Ear_'+key:1})

def shaped_head():
    # Authored horizontal sections: chin, cheeks, brow and crown, not a scaled sphere.
    sections=[(1.205,.095,.095,-.025),(1.235,.185,.170,-.024),
              (1.295,.290,.231,-.016),(1.39,.360,.273,-.012),
              (1.49,.376,.292,-.005),(1.60,.357,.274,0),
              (1.70,.300,.230,.006),(1.79,.205,.160,.008),(1.825,.095,.078,.01)]
    vs=[];fs=[];seg=24
    for z,rx,ry,cy in sections:
        for i in range(seg):
            a=2*math.pi*i/seg;x=rx*math.cos(a);y=cy+ry*math.sin(a)
            # Broad, integrated muzzle with a soft central cleft; no separate cheek balls.
            if math.sin(a)<0:
                y-=.0575*math.exp(-(x/.19)**2-((z-1.365)/.115)**2)*(-math.sin(a))
            vs.append((x,y,z))
    for j in range(len(sections)-1):
        for i in range(seg):
            a=j*seg+i;b=j*seg+(i+1)%seg;fs.append((a,b,b+seg,a+seg))
    fs.extend([tuple(reversed(range(seg))),tuple(range(len(vs)-seg,len(vs)))])
    add('Head',vs,fs,'Fur',{'Head':1})
shaped_head()
# Keep the bare face surface before adding ears and inset details.
face_vertices=[Vector(v) for v in modules['Head'][0]]
face_triangles=[(f[0],f[i],f[i+1]) for f in modules['Head'][1] for i in range(1,len(f)-1)]
def face_attachment(x,z):
    origin=Vector((x,-2,z));direction=Vector((0,1,0));hits=[]
    for ids in face_triangles:
        a,b,c=(face_vertices[i] for i in ids)
        hit=intersect_ray_tri(a,b,c,direction,origin,True)
        if hit is not None:
            normal=(b-a).cross(c-a).normalized()
            if normal.y>0:normal=-normal
            hits.append((hit,normal))
    assert hits,'No face intersection for detail'
    point,normal=min(hits,key=lambda h:h[0].y)
    if abs(x)<1e-6:normal.x=0;normal.normalize()
    return point,Vector((0,-1,0)).rotation_difference(normal)
ell('Head',(0,0,1.135),(.092,.078,.12),'Fur','Spine',12,6)
for s,k in ((1,'L'),(-1,'R')):
    ell('Head',(s*.223-.09*math.sin(s*.15),.012,2.055-.09*math.cos(s*.15)),(.112,.074,.245),'Fur','Ear_'+k,12,9,tilt=s*.15)
    inner_ear(s,k)
    eye,eye_rotation=face_attachment(s*.190,1.487)
    first_face=len(modules['Head'][1])
    ell('Head',eye,(.040,.026,.051),'Dark','Head',12,7,orientation=eye_rotation)
    eye_faces=list(modules['Head'][1][first_face:]);eye_vs=modules['Head'][0]
    # Project a thin disk onto actual low-poly eye triangles, avoiding a raised white bead.
    patch=[];direction=eye_rotation@Vector((0,1,0))
    for ring in range(3):
        for i in range(1 if ring==0 else 12):
            a=2*math.pi*i/12;x=-s*.009+.009*ring/2*math.cos(a);z=.018+.012*ring/2*math.sin(a)
            origin=eye+eye_rotation@Vector((x+.000001,-1,z+.000001));hits=[]
            for face in eye_faces:
                for j in range(1,len(face)-1):
                    hit=intersect_ray_tri(Vector(eye_vs[face[0]]),Vector(eye_vs[face[j]]),Vector(eye_vs[face[j+1]]),direction,origin,True)
                    if hit is not None:hits.append(hit)
            assert hits,('Highlight missed eye',s,ring,i,x,z,len(eye_faces),tuple(eye),tuple(direction))
            patch.append(min(hits,key=lambda h:(h-origin).length)-direction*.0005)
    faces=[(0,1+i,1+(i+1)%12) for i in range(12)]
    faces += [(1+i,13+i,13+(i+1)%12,1+(i+1)%12) for i in range(12)]
    add('Head',patch,faces,'Fur',{'Head':1})
    ell('BodyHands',bones['Hand_'+k][0].lerp(bones['Hand_'+k][1],.58),(.067,.067,.088),'Fur','Hand_'+k,12,7)
    ell('BodyHands',(s*.301,-.116,.632),(.028,.034,.040),'Fur','Hand_'+k,8,5)
    limbs('BodyArms',k,True,'Fur');limbs('Top',k,True,'Cloth',1.14)
    limbs('BodyLegs',k,False,'Fur');limbs('Bottom',k,False,'Dark',1.11)
    ell('BodyFeet',(s*.16,-.090,.093),(.095,.164,.084),'Fur','Foot_'+k,12,7)
    ell('Shoes',(s*.16,-.095,.101),(.109,.179,.098),'Leather','Foot_'+k,12,7)
nose,nose_rotation=face_attachment(0,1.397)
ell('Head',nose,(.034,.020,.026),'Pink','Head',10,5,orientation=nose_rotation)
torso('BodyTorso','Fur');torso('Top','Cloth',1.06)
ell('BodyTail',(0,.22,.595),(.12,.125,.12),'Fur','Tail',12,8)
rounded_box('Top',(.15,-.165,.642),(.095,.018,.095),'Cloth','Spine',.012)
# Neck scarf ring and two simple folded tails.
# Flared collar: a narrow neck opening and straight sloping sides toward the chest.
tube('Neckwear',[(0,0,1.098),(0,0,1.145),(0,0,1.188)],[(.160,.138),(.129,.110),(.100,.085)],'Pink',[{'NeckSocket':1}]*3,16)
ell('Neckwear',(.040,-.136,1.131),(.047,.040,.041),'Pink','NeckSocket',8,5)
ribbon('Neckwear',[(.035,-.159,1.109),(.052,-.183,1.055),(.078,-.189,.990),(.066,-.203,.925)],.079,.014,'Pink','NeckSocket')
ribbon('Neckwear',[(.017,-.162,1.107),(-.027,-.180,1.048),(-.041,-.198,.982)],.060,.013,'Pink','NeckSocket')
# Backpack body behind the torso. All rigid details share BackSocket weights.
def backpack_shell():
    vs=[];fs=[];seg=16
    for z,rx,ry in [(.515,.155,.065),(.54,.217,.112),(.60,.240,.137),(.86,.247,.145),(1.045,.228,.132),(1.09,.18,.09)]:
        for i in range(seg):
            a=2*math.pi*i/seg
            # Squircle cross-section: broad panels, rounded edges, convex front.
            u=math.cos(a);v=math.sin(a)
            vs.append((rx*math.copysign(abs(u)**.55,u),.322+ry*math.copysign(abs(v)**.55,v),z))
    for j in range(5):
        for i in range(seg):
            a=j*seg+i;b=j*seg+(i+1)%seg;fs.append((a,b,b+seg,a+seg))
    fs.extend([tuple(reversed(range(seg))),tuple(range(5*seg,6*seg))])
    add('Backpack',vs,fs,'Leather',{'BackSocket':1})
    # A continuous thick flap wrapping from the rear over the crown down the face.
    rows=[(.220,1.053,.205),(.245,1.094,.222),(.315,1.118,.231),(.391,1.10,.238),(.451,1.054,.240),(.478,.985,.241),(.486,.89,.232),(.489,.822,.194)]
    vs=[];fs=[];cols=9
    for layer in (0,1):
        for j,(y,z,w) in enumerate(rows):
            for i in range(cols):
                t=2*i/(cols-1)-1
                vs.append((t*w,y-.022*t*t-layer*.016,z+.018*(1-t*t)-layer*.013))
    stride=len(rows)*cols
    for layer in range(2):
        for j in range(len(rows)-1):
            for i in range(cols-1):
                a=layer*stride+j*cols+i;fs.append((a,a+1,a+1+cols,a+cols))
    boundary=list(range(cols))+[j*cols+cols-1 for j in range(1,len(rows))]+list(range(stride-2,stride-cols-1,-1))+[j*cols for j in range(len(rows)-2,0,-1)]
    for i,a in enumerate(boundary):
        b=boundary[(i+1)%len(boundary)];fs.append((a,b,b+stride,a+stride))
    add('Backpack',vs,fs,'Leather',{'BackSocket':1})
backpack_shell()
rounded_box('Backpack',(0,.483,.778),(.065,.025,.17),'Leather','BackSocket',.012)
rounded_box('Backpack',(0,.503,.761),(.089,.021,.073),'Brass','BackSocket',.012)
rounded_box('Backpack',(0,.515,.767),(.055,.006,.040),'Leather','BackSocket',.006)
ribbon('Backpack',[(-.085,.315,1.11),(-.075,.315,1.175),(-.040,.315,1.195),(.040,.315,1.195),(.075,.315,1.175),(.085,.315,1.11)],.025,.023,'Leather','BackSocket')
for s in (-1,1):
    ribbon('Backpack',[(s*.14,.24,.66),(s*.14,.13,.99),(s*.14,.035,1.09),(s*.14,-.060,1.08),(s*.15,-.119,.972),(s*.17,-.150,.79),(s*.22,-.080,.69),(s*.22,.085,.66),(s*.16,.235,.65)],.043,.014,'Leather','Spine')
# Closed badge disks with a four-point compass motif; no image texture needed.
def disk(n,c,r,depth,mat,bn,segments=16):
    vs=[]
    for y in (c[1]-depth/2,c[1]+depth/2):
        for i in range(segments):
            a=2*math.pi*i/segments;vs.append((c[0]+r*math.cos(a),y,c[2]+r*math.sin(a)))
    fs=[tuple(range(segments)),tuple(reversed(range(segments,2*segments)))]
    fs.extend((i,segments+i,segments+(i+1)%segments,(i+1)%segments) for i in range(segments))
    add(n,vs,fs,mat,{bn:1})
disk('Backpack',(0,.496,.952),.078,.014,'Brass','BackSocket')
disk('Backpack',(0,.505,.952),.064,.008,'Fur','BackSocket')
for i in range(4):
    a=i*math.pi/2
    vs=[(0,.511,.952),(.056*math.sin(a),.511,.952+.056*math.cos(a)),(.018*math.sin(a+.7),.512,.952+.018*math.cos(a+.7))]
    add('Backpack',vs,[(0,1,2)],'Dark' if i%2 else 'Leather',{'BackSocket':1},False)

objects=[];report={'reference':'Char_Rabbit_01_high.png','bodyFamily':'AdultStandard_v2','bodyGroup':'Adult','palette':palette,'modules':[],'subdivision':False}
for name,(verts,faces,materials,weights,smooth) in modules.items():
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    ob=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(ob)
    # Recalculate normals consistently on closed explicit geometry.
    import bmesh
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
    for m in mats.values():mesh.materials.append(m)
    for p,mat,sm in zip(mesh.polygons,materials,smooth):p.material_index=list(mats).index(mat);p.use_smooth=sm
    groups={n:ob.vertex_groups.new(name=n) for n in bones}
    for i,w in enumerate(weights):
        assert abs(sum(w.values())-1)<1e-5
        for n,v in w.items():groups[n].add([i],v,'REPLACE')
    # Export explicit nonzero triangles; bevel caps can contain collinear corners.
    mesh.calc_loop_triangles()
    valid=[(tuple(t.vertices),mesh.polygons[t.polygon_index].material_index,mesh.polygons[t.polygon_index].use_smooth) for t in mesh.loop_triangles if t.area>1e-10]
    clean=bpy.data.meshes.new(name+'_triangles');clean.from_pydata([v.co[:] for v in mesh.vertices],[],[t[0] for t in valid]);clean.update()
    for m in mats.values():clean.materials.append(m)
    # Vertex groups are deform-layer data: restore them on the replacement mesh.
    ob.data=clean;mesh=clean
    ob.vertex_groups.clear()
    groups={n:ob.vertex_groups.new(name=n) for n in bones}
    for i,w in enumerate(weights):
        for n,v in w.items():groups[n].add([i],v,'REPLACE')
    for v,w in zip(mesh.vertices,weights):
        actual={ob.vertex_groups[g.group].name:g.weight for g in v.groups if g.weight>0}
        assert actual.keys()==w.keys(),(name,v.index,actual,w)
    for p,(_,mi,sm) in zip(mesh.polygons,valid):p.material_index=mi;p.use_smooth=sm
    modifier=ob.modifiers.new('Shared adult skeleton','ARMATURE');modifier.object=rig
    ob.parent=rig
    mesh.calc_loop_triangles();tris=len(mesh.loop_triangles)
    assert all(t.area>1e-10 for t in mesh.loop_triangles),name+' degenerate faces'
    report['modules'].append({'name':name,'vertices':len(mesh.vertices),'triangles':tris})
    objects.append(ob)
report['trianglesAllModules']=sum(m['triangles'] for m in report['modules'])
assert report['trianglesAllModules']<=6000,report['trianglesAllModules']
report['bones']=list(bones)
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for ob in objects:ob.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(OUT/'AdultRabbit.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},
    apply_unit_scale=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,mesh_smooth_type='FACE')
# Source opens in a clean dressed state with the hidden body modules retained.
for ob in objects:
    if ob.name in ('BodyTorso','BodyArms','BodyLegs','BodyFeet'):ob.hide_render=True;ob.hide_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/AdultRabbit.blend'))
(OUT/'AdultRabbit.audit.json').write_text(json.dumps(report,indent=2),encoding='utf8')
print('ADULT_RABBIT_ART_OK '+str(report['trianglesAllModules']))

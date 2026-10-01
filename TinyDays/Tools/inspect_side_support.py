"""Geometric arm-pillow diagnostic; no model/source mutation."""
import bpy,sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/'Logs/SideDraft.blend'))
r=bpy.data.objects['AdultRig'];r.animation_data.action=bpy.data.actions['Adult_Breathe_Left'];bpy.context.scene.frame_set(1)
def points(ob):
    result=[]
    for v in ob.data.vertices:
        p=Vector()
        for w in v.groups:
            n=ob.vertex_groups[w.group].name;p+=(r.pose.bones[n].matrix@r.data.bones[n].matrix_local.inverted()@v.co)*w.weight
        result.append(p)
    return result
head=bpy.data.objects['Head'];hp=points(head);tree=BVHTree.FromPolygons(hp,[tuple(p.vertices) for p in head.data.polygons])
for name in ('Top','BodyHands'):
    ob=bpy.data.objects[name];ps=points(ob);candidates=[]
    for v,p in zip(ob.data.vertices,ps):
        weight=sum(w.weight for w in v.groups if ob.vertex_groups[w.group].name in ('Forearm_R','Hand_R'))
        if weight<.5:continue
        hit,normal,face,distance=tree.find_nearest(p)
        signed=distance*(1 if (p-hit).dot(normal)>=0 else -1)
        candidates.append((signed,v.index,tuple(p),face,tuple(hit)))
    print(name,'MIN_SIGNED',min(candidates),'CLOSEST',min(candidates,key=lambda x:abs(x[0])),'FLOOR',min(c[2][2] for c in candidates),flush=True)
print('ANATOMICAL_LEFT_SHOULDER',r.pose.bones['UpperArm_R'].head.z,'RIGHT',r.pose.bones['UpperArm_L'].head.z,flush=True)
for name in ('Top','BodyHands'):
    ob=bpy.data.objects[name];ps=points(ob);gaps=[]
    for v,p in zip(ob.data.vertices,ps):
        if sum(w.weight for w in v.groups if ob.vertex_groups[w.group].name in ('Forearm_L','Hand_L'))<.5:continue
        hit,n,face,d=tree.find_nearest(p);gaps.append(d*(1 if (p-hit).dot(n)>=0 else -1))
    print('RIGHT_ARM_HEAD_GAP',name,min(gaps),flush=True)
if '--breath' in sys.argv:
    vals=[]
    ob=bpy.data.objects['Top'];indices=[v.index for v in ob.data.vertices if sum(w.weight for w in v.groups if ob.vertex_groups[w.group].name=='Forearm_R')>.5]
    for frame in range(1,122,3):
        bpy.context.scene.frame_set(frame);bpy.context.view_layer.update();ps=points(ob)
        gaps=[]
        for i in indices:
            hit,n,face,d=tree.find_nearest(ps[i]);gaps.append(d*(1 if (ps[i]-hit).dot(n)>=0 else -1))
        vals.append((min(gaps),min(ps[i].z for i in indices)))
    print('BREATH_GAP_RANGE',min(v[0] for v in vals),max(v[0] for v in vals),'ARM_FLOOR',min(v[1] for v in vals),max(v[1] for v in vals),flush=True)

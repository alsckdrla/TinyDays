"""240 Hz visible sleeve/hand-head support audit, separate from coat-leg checks."""
import bpy,sys,json,numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=Path(__file__).resolve().parents[1]
bpy.ops.wm.open_mainfile(filepath=str(root/('Logs/SideDraft.blend' if '--draft' in sys.argv else 'ArtSource/AdultRabbitMotion.blend')))
rig=bpy.data.objects['AdultRig'];inv={b.name:b.matrix_local.inverted() for b in rig.data.bones};data={}
for name in ('Head','Top','BodyHands'):
    ob=bpy.data.objects[name];pts=np.array([tuple(v.co)+(1,) for v in ob.data.vertices]);weights={}
    for v in ob.data.vertices:
        for w in v.groups:
            n=ob.vertex_groups[w.group].name
            if n not in weights:weights[n]=np.zeros(len(pts))
            weights[n][v.index]=w.weight
    data[name]=(pts,weights)
def world(name):
    pts,weights=data[name];return sum((pts@np.array(rig.pose.bones[n].matrix@inv[n]).T)[:,:3]*w[:,None] for n,w in weights.items())
indices={}
for name in ('Top','BodyHands'):
    for side in ('L','R'):
        indices[name,side]=np.where(sum(w for n,w in data[name][1].items() if n in ('Forearm_'+side,'Hand_'+side))>.5)[0]
polygons=[tuple(p.vertices) for p in bpy.data.objects['Head'].data.polygons]
results=[]
for name,duration in [('Adult_Lie_To_Left',2.4),('Adult_Left_To_Lie',2.4),('Adult_Breathe_Left',4),('Adult_Fall_Asleep_Left',3),('Adult_Sleep_Left',6),('Adult_Wake_Left',1.5)]:
    rig.animation_data.action=bpy.data.actions[name];minimum=(99,None);floor=(99,None);gap_range=[];drift=0;first=None
    for frame in range(round(duration*240)+1):
        bpy.context.scene.frame_set(1+frame//8,subframe=(frame%8)/8);bpy.context.view_layer.update()
        hp=world('Head');tree=BVHTree.FromPolygons([Vector(p) for p in hp],polygons);pillow=99
        for obj in ('Top','BodyHands'):
            ps=world(obj)
            for side in ('L','R'):
                for i in indices[obj,side]:
                    p=Vector(ps[i]);hit,n,face,d=tree.find_nearest(p);signed=d*(1 if (p-hit).dot(n)>=0 else -1)
                    if signed<minimum[0]:minimum=(signed,(frame/240,obj,side,int(i)))
                    if p.z<floor[0]:floor=(p.z,(frame/240,obj,side,int(i)))
                    if obj=='Top' and side=='R':pillow=min(pillow,signed)
            if obj=='Top':
                if first is None:first=ps.copy()
                drift=max(drift,float(np.linalg.norm(ps[indices[obj,'R']]-first[indices[obj,'R']],axis=1).max()))
        gap_range.append(pillow)
    row={'clip':name,'head_arm_min_m':minimum,'arm_floor_min_m':floor,'pillow_gap_range_m':[min(gap_range),max(gap_range)],'all_left_forearm_vertices_motion_m_not_fixed_contact':drift}
    results.append(row);print(json.dumps(row),flush=True)
rig.animation_data.action=bpy.data.actions['Adult_Breathe_Left'];bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
left=rig.pose.bones['UpperArm_R'].head.z;right=rig.pose.bones['UpperArm_L'].head.z
report={'version':'0.111','sampleRate':240,'anatomicalLeftSuffix':'R','leftShoulderHeight':left,'rightShoulderHeight':right,'clips':results}
(root/'Docs/AdultSidePillowVerification.json').write_text(json.dumps(report,indent=2),encoding='utf8')
assert left<right-.1,'Anatomical left must be lower, not the legacy L label'
assert min(r['head_arm_min_m'][0] for r in results)>=-.0005,'Head/arm penetration'
assert min(r['arm_floor_min_m'][0] for r in results)>=-.0005,'Visible arm floor penetration'
assert -.0005<=results[-1]['pillow_gap_range_m'][0] and results[-1]['pillow_gap_range_m'][1]<=.005,'Pillow contact gap'
print('SIDE_PILLOW_OK',flush=True)

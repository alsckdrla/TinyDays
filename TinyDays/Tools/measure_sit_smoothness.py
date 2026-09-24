"""Compare 240Hz Unity samples; no clothing intersection acceptance criteria."""
import csv, json, math
from pathlib import Path

root=Path(__file__).resolve().parents[1]
def measure(path):
    with path.open(encoding='utf-8-sig') as f:rows=list(csv.DictReader(f))
    result={}
    for clip in ('5','6'):
        rr=[r for r in rows if r['clip']==clip]
        t=[float(r['time']) for r in rr]
        out={}
        for channel in ('pelvis','head'):
            h=[float(r[channel]) for r in rr]
            v=[(h[i+1]-h[i-1])/(t[i+1]-t[i-1]) for i in range(1,len(t)-1)]
            a=[(v[i+1]-v[i-1])/(t[i+2]-t[i]) for i in range(1,len(v)-1)]
            low,high=min(h),max(h)
            interior=[abs(v[i-1])<.03 for i in range(1,len(h)-1)
                      if low+(high-low)*.1<h[i]<high-(high-low)*.1]
            stops=sum(x and (i==0 or not interior[i-1]) for i,x in enumerate(interior))
            out[channel]={'range_m':high-low,'peak_speed_m_s':max(map(abs,v)),
                          'peak_acceleration_m_s2':max(map(abs,a)),
                          'interior_slow_episodes':stops,
                          'after_1_1s_peak_speed':max(abs(v[i-1]) for i in range(1,len(h)-1) if t[i]>=1.1)}
        for channel in ('spineAngle','headAngle'):
            angles=[float(r[channel]) for r in rr]
            for i in range(1,len(angles)):
                angles[i]=angles[i-1]+(angles[i]-angles[i-1]+180)%360-180
            velocity=[(angles[i+1]-angles[i-1])/(t[i+1]-t[i-1]) for i in range(1,len(t)-1)]
            acceleration=[(velocity[i+1]-velocity[i-1])/(t[i+2]-t[i]) for i in range(1,len(velocity)-1)]
            start,end=(.8,1.12) if clip=='5' else (.35,.55)
            window=[angles[i] for i in range(len(t)) if start<=t[i]<=end]
            out[channel]={'peak_deg_s':max(map(abs,velocity)), 'peak_deg_s2':max(map(abs,acceleration)),
                          'former_hold_range_deg':max(window)-min(window)}
        for side in ('left','right'):
            points=[[float(r[side+'Hand'+axis]) for axis in 'XYZ'] for r in rr]
            speed=[math.dist(points[i+1],points[i-1])/(t[i+1]-t[i-1]) for i in range(1,len(t)-1)]
            out[side+'Hand']={'peak_speed_m_s':max(speed),'peak_speed_change_m_s2':max(abs(speed[i+1]-speed[i-1])/(t[i+2]-t[i]) for i in range(1,len(speed)-1))}
        result[clip]=out
    return result

before=root/'Docs/AdultRabbitSitMotionBeforeV092.csv'
after=root/'Docs/AdultRabbitSitMotion.csv'
results={'before':measure(before),'after':measure(after)}
checks=[]
for clip in ('5','6'):
    checks.append(results['after'][clip]['pelvis']['interior_slow_episodes']==0)
    checks.append(results['after'][clip]['spineAngle']['former_hold_range_deg']>.25)
    checks.append(results['after'][clip]['head']['peak_acceleration_m_s2']<=results['before'][clip]['head']['peak_acceleration_m_s2'])
    for channel in ('spineAngle','headAngle'):
        checks.append(results['after'][clip][channel]['peak_deg_s2']<=results['before'][clip][channel]['peak_deg_s2'])
results['checks_pass']=all(checks)
target=root/'Docs/AdultRabbitSitSmoothnessV092.json'
target.write_text(json.dumps(results,indent=2),encoding='utf-8')
print(json.dumps(results,indent=2))
if not all(checks):raise SystemExit('Sitting smoothness regression failed')

from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')
old='''    static float RiverDistanceWorld(float sx,float sy)\n    {\n        float cx=RiverCenterX(sy);if(float.IsNaN(cx))return 9999f;\n        float e=.05f,ya=Mathf.Max(RiverSy[0],sy-e),yb=Mathf.Min(RiverSy[RiverSy.Length-1],sy+e);\n        float xa=RiverCenterX(ya),xb=RiverCenterX(yb);\n        float slope=(xb-xa)/Mathf.Max(.001f,yb-ya);\n        return Mathf.Abs(sx-cx)*Cell/Mathf.Sqrt(1f+slope*slope);\n    }'''
new='''    static float RiverDistanceWorld(float sx,float sy)\n    {\n        if(sy<RiverSy[0]-1f||sy>RiverSy[RiverSy.Length-1]+1f)return 9999f;\n        float best=999999f;\n        for(int k=-5;k<=5;k++)\n        {\n            float qy=Mathf.Clamp(sy+k*.16f,RiverSy[0],RiverSy[RiverSy.Length-1]);\n            float qx=RiverCenterX(qy);if(float.IsNaN(qx))continue;\n            float dx=(sx-qx)*Cell,dz=(sy-qy)*Cell;\n            best=Mathf.Min(best,Mathf.Sqrt(dx*dx+dz*dz));\n        }\n        return best;\n    }'''
if s.count(old)!=1: raise SystemExit('RiverDistanceWorld match failed')
s=s.replace(old,new,1)
s=s.replace('float wetClear=halfW+.75f;','float wetClear=halfW+.25f;',1)
s=s.replace('float outer=halfW>0f?halfW+.75f+Mathf.Lerp(5.5f,10.5f,RiverDownstream01(sy)):0f;','float outer=halfW>0f?halfW+.25f+Mathf.Lerp(5.5f,10.5f,RiverDownstream01(sy)):0f;',1)
p.write_text(s,encoding='utf-8')
print('RIVER_DISTANCE_FIX_V11_PATCHED')

from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')
old='''    static float RiverCenterX(float sy)\n    {\n        int i=RiverSegment(sy);if(i<0)return float.NaN;\n        float t=Mathf.InverseLerp(RiverSy[i],RiverSy[i+1],sy);\n        float u=t*t*(3f-2f*t);\n        float x=Mathf.Lerp(RiverX[i],RiverX[i+1],u);\n        return x+(Mathf.PerlinNoise(sy*.19f+7.2f,4.1f)-.5f)*.22f;\n    }'''
new='''    static float RiverCenterX(float sy)\n    {\n        int i=RiverSegment(sy);if(i<0)return float.NaN;\n        float t=Mathf.InverseLerp(RiverSy[i],RiverSy[i+1],sy),t2=t*t,t3=t2*t;\n        float p0=RiverX[Mathf.Max(0,i-1)],p1=RiverX[i],p2=RiverX[i+1],p3=RiverX[Mathf.Min(RiverX.Length-1,i+2)];\n        float x=.5f*((2f*p1)+(-p0+p2)*t+(2f*p0-5f*p1+4f*p2-p3)*t2+(-p0+3f*p1-3f*p2+p3)*t3);\n        float anchorDist=Mathf.Min(Mathf.Min(Mathf.Abs(sy-58f),Mathf.Abs(sy-73.2f)),Mathf.Min(Mathf.Abs(sy-84.5f),Mathf.Abs(sy-103f)));\n        float free=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(anchorDist/1.8f));\n        float meander=((Mathf.PerlinNoise(sy*.145f+7.2f,4.1f)-.5f)*.72f + Mathf.Sin(sy*.53f+1.7f)*.16f)*free;\n        return x+meander;\n    }'''
assert old in s
s=s.replace(old,new,1)
old2='''    static float RiverDistanceWorld(float sx,float sy)\n    {\n        float cx=RiverCenterX(sy);if(float.IsNaN(cx))return 9999f;\n        return Mathf.Abs(sx-cx)*Cell;\n    }'''
new2='''    static float RiverDistanceWorld(float sx,float sy)\n    {\n        float cx=RiverCenterX(sy);if(float.IsNaN(cx))return 9999f;\n        float e=.05f,ya=Mathf.Max(RiverSy[0],sy-e),yb=Mathf.Min(RiverSy[RiverSy.Length-1],sy+e);\n        float xa=RiverCenterX(ya),xb=RiverCenterX(yb);\n        float slope=(xb-xa)/Mathf.Max(.001f,yb-ya);\n        return Mathf.Abs(sx-cx)*Cell/Mathf.Sqrt(1f+slope*slope);\n    }'''
assert old2 in s
s=s.replace(old2,new2,1)
p.write_text(s,encoding='utf-8')
print('RIVER_MEANDER_DISTANCE_PATCHED')

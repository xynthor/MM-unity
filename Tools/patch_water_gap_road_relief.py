from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\BuildNewSorpigalOpenWorld.cs')
s=p.read_text(encoding='utf-8')
insert='''    static float SourceRelief(byte[] heights,int x,int y)\n    {\n        int min=255,max=0;\n        for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)\n        {\n            int xx=Mathf.Clamp(x+ox,0,N-1),yy=Mathf.Clamp(y+oy,0,N-1),v=heights[yy*N+xx];\n            if(v<min)min=v;if(v>max)max=v;\n        }\n        return (max-min)*.25f;\n    }\n\n'''
marker='    static void MarkGridLine(bool[,] mask,Vector2Int a,Vector2Int b)\n'
if 'static float SourceRelief(' not in s:
    if marker not in s: raise SystemExit('MARKER_NOT_FOUND')
    s=s.replace(marker,insert+marker,1)
s=s.replace('bool bridge=(correctedWaterCache[y,x-1]&&correctedWaterCache[y,x+1])||(correctedWaterCache[y-1,x]&&correctedWaterCache[y+1,x]);','bool bridge=(correctedWaterCache[y,x-1]&&correctedWaterCache[y,x+1])||(correctedWaterCache[y-1,x]&&correctedWaterCache[y+1,x])||(correctedWaterCache[y-1,x-1]&&correctedWaterCache[y+1,x+1])||(correctedWaterCache[y-1,x+1]&&correctedWaterCache[y+1,x-1]);')
s=s.replace('Vector2Int best=new Vector2Int(x,y);float current=SourceSlope(maskHeightCache,x,y);','Vector2Int best=new Vector2Int(x,y);float current=SourceSlope(maskHeightCache,x,y),currentRelief=SourceRelief(maskHeightCache,x,y);')
s=s.replace('if(current>.80f)','if(current>.80f || currentRelief>1.50f)')
s=s.replace('float dist=Mathf.Sqrt(ox*ox+oy*oy),sl=SourceSlope(maskHeightCache,xx,yy);','float dist=Mathf.Sqrt(ox*ox+oy*oy),sl=SourceSlope(maskHeightCache,xx,yy),rel=SourceRelief(maskHeightCache,xx,yy);')
s=s.replace('float score=sl*3.4f+dist*.16f+dh*.18f;','float score=sl*3.2f+rel*2.4f+dist*.18f+dh*.18f;')
p.write_text(s,encoding='utf-8')
print('PATCHED_WATER_GAPS_AND_ROAD_RELIEF')
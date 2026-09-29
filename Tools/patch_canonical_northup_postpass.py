from pathlib import Path
p=Path(r'C:\MMUnityPort\Assets\Editor\MMSourceGridExactPostPass.cs')
s=p.read_text(encoding='utf-8-sig')
repls=[
('int sy=Mathf.Clamp(Mathf.FloorToInt(((y+.5f)/r)*N),0,N-1);','int sy=Mathf.Clamp(N-1-Mathf.FloorToInt(((y+.5f)/r)*N),0,N-1);'),
('float x=ox,zp=oz;','float x=ox,zp=-oz;'),
('int sy=Mathf.Clamp(Mathf.RoundToInt(64f+z/4f),0,N-1);','int sy=Mathf.Clamp(Mathf.RoundToInt(64f-z/4f),0,N-1);'),
('float x=(sx-64f)*4f+jx, zp=(sy-64f)*4f+jz;','float x=(sx-64f)*4f+jx, zp=(64f-sy)*4f+jz;'),
('float x=(sx-64f)*4f+((((h>>5)&255)/255f-.5f)*2.8f); float zp=(sy-64f)*4f+((((h>>13)&255)/255f-.5f)*2.8f);','float x=(sx-64f)*4f+((((h>>5)&255)/255f-.5f)*2.8f); float zp=(64f-sy)*4f+((((h>>13)&255)/255f-.5f)*2.8f);'),
('float zp=(sy-64f)*4f+((((h>>13)&255)/255f-.5f)*3.0f);','float zp=(64f-sy)*4f+((((h>>13)&255)/255f-.5f)*3.0f);')]
for a,b in repls:
    c=s.count(a); print('replace',c,a[:70]); s=s.replace(a,b)
p.write_text(s,encoding='utf-8')
# audits
p=Path(r'C:\MMUnityPort\Assets\Editor\AuditOneToOneWorld.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('int sy=Mathf.Clamp(Mathf.FloorToInt(((y+.5f)/td.alphamapHeight)*N),0,N-1);','int sy=Mathf.Clamp(N-1-Mathf.FloorToInt(((y+.5f)/td.alphamapHeight)*N),0,N-1);')
s=s.replace('float x=((sx+.5f)-64f)*4f,zp=((sy+.5f)-64f)*4f;','float x=((sx+.5f)-64f)*4f,zp=(64f-(sy+.5f))*4f;')
p.write_text(s,encoding='utf-8')
p=Path(r'C:\MMUnityPort\Assets\Editor\AuditWorldSourceExact.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('int ax=Mathf.Clamp(sx*4+2,0,td.alphamapWidth-1),ay=Mathf.Clamp(sy*4+2,0,td.alphamapHeight-1);','int ax=Mathf.Clamp(sx*4+2,0,td.alphamapWidth-1),ay=Mathf.Clamp((N-1-sy)*4+2,0,td.alphamapHeight-1);')
s=s.replace('float wx=(sx-64f)*4f,wz=(sy-64f)*4f;','float wx=(sx-64f)*4f,wz=(64f-sy)*4f;')
p.write_text(s,encoding='utf-8')
print('patched postpass + audits')

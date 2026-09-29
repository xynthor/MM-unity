import subprocess,json,threading,queue,sys
timeout_seconds=int(sys.argv[1]) if len(sys.argv)>1 else 15
p=subprocess.Popen([r'C:\Users\batsi\.unity\relay\relay_win.exe','--mcp','--project-path',r'C:\MMUnityPort'],stdin=subprocess.PIPE,stdout=subprocess.PIPE,stderr=subprocess.PIPE,text=True,encoding='utf-8')
q=queue.Queue()
stderr_lines=[]
def read_stderr():
 for line in p.stderr:stderr_lines.append(line)
threading.Thread(target=read_stderr,daemon=True).start()
def read():
 for line in p.stdout:q.put(line)
threading.Thread(target=read,daemon=True).start()
def request(data):
 p.stdin.write(json.dumps(data)+'\n');p.stdin.flush()
 try:
  while True:
   line=q.get(timeout=timeout_seconds)
   try:r=json.loads(line)
   except:continue
   if r.get('id')==data.get('id'):return r
 except queue.Empty:return {'error':'relay response timeout'}
try:
 print(json.dumps(request({'jsonrpc':'2.0','id':1,'method':'initialize','params':{'protocolVersion':'2024-11-05','capabilities':{},'clientInfo':{'name':'CodexUnityConnectionCheck','version':'1.0'}}})))
 p.stdin.write(json.dumps({'jsonrpc':'2.0','method':'notifications/initialized'})+'\n');p.stdin.flush()
 result=request({'jsonrpc':'2.0','id':2,'method':'tools/list','params':{}})
 from pathlib import Path
 Path('Validation/SequentialRepair/unity_mcp_tools.json').write_text(json.dumps(result,indent=2))
 print(json.dumps({'tool_count':len(result.get('result',{}).get('tools',[])),'error':result.get('error')}))
 if len(sys.argv)>2:
  source=Path(sys.argv[2])
  call=({'name':'Unity_RunCommand','arguments':{'Title':source.stem,'Code':source.read_text(encoding='utf-8-sig')}} if source.suffix=='.cs' else json.loads(source.read_text(encoding='utf-8-sig')))
  response=request({'jsonrpc':'2.0','id':3,'method':'tools/call','params':call})
  Path('Validation/SequentialRepair/unity_mcp_last_call.json').write_text(json.dumps(response,indent=2),encoding='utf-8')
  data=response.get('result',{}).get('structuredContent',{})
  if not data:
   for block in response.get('result',{}).get('content',[]):
    if block.get('type')=='text':
     try:data=json.loads(block['text']);break
     except ValueError:pass
  print(json.dumps({'success':data.get('success'),'data':{k:v for k,v in data.get('data',{}).items() if k!='localFixedCode'},'error':data.get('error',response.get('error'))}))
finally:
 p.terminate()
 from pathlib import Path
 Path('Validation/SequentialRepair/unity_mcp_probe_stderr.log').write_text(''.join(stderr_lines),encoding='utf-8')

from pathlib import Path
p=Path(r"C:\MMUnityPort\Assets\Editor\MMRestoreOldGoodSorpigalVegetation.cs")
s=p.read_text(encoding="utf-8-sig")
s=s.replace('''Mathf.Lerp(6.8f,11.2f,D01(a.idx*113+k)),seed, out gr,out gp);''','''Mathf.Lerp(6.8f,11.2f,D01(a.idx*113+k)),"GroveTree_TMP",out gr,out gp);''')
p.write_text(s,encoding="utf-8")
print("GROVE_CALL_FIXED")
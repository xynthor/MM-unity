from pathlib import Path
import uuid
for p in [Path(r"C:\MMUnityPort\Assets\Editor\MMSourceDecorationRestorePass.cs"),Path(r"C:\MMUnityPort\Assets\Editor\ZZLockedSourceDecorationRestore_20260921.cs")]:
    m=Path(str(p)+".meta")
    if not m.exists():
        guid=uuid.uuid4().hex
        txt=f"""fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
        m.write_text(txt,encoding="utf-8")
        print("META",m.name,guid)
    else: print("EXISTS",m)

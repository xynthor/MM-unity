from pathlib import Path
import uuid
p=Path(r"C:\MMUnityPort\Assets\Editor\MMSourceDecorationPlacementAudit.cs")
m=Path(str(p)+".meta")
if not m.exists():
    guid=uuid.uuid4().hex
    m.write_text(f"""fileFormatVersion: 2
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
""",encoding="utf-8")
    print(guid)
else: print("exists")

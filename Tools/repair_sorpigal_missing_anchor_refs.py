from pathlib import Path
import re,json,datetime,shutil
p=Path('Assets/Scenes/NewSorpigal_OpenWorld.unity');s=p.read_text(); blocks=re.split(r'(?=^--- !u!)',s,flags=re.M)
forbidden={'b69a80ea959884646a24bedcf4e52929','f59d8f4ab7e53214592454a3d31c8cc6'}
replacements={}; audit=[]
for b in blocks:
 m=re.match(r'--- !u!1001 &(\d+)\n',b)
 if not m or not any('guid: '+g in b for g in forbidden):continue
 pid=m[1];props=dict(re.findall(r'propertyPath: ([^\n]+)\n      value: ([^\n]*)',b))
 name=props['m_Name']; assert name.startswith('tree06a_'),name
 parent=re.search(r'm_TransformParent: \{fileID: (\d+)\}',b)[1]
 transforms=[a for a in blocks if a.startswith('--- !u!4 ') and 'm_PrefabInstance: {fileID: '+pid+'}' in a]
 assert len(transforms)==1
 tb=transforms[0];tid=re.match(r'--- !u!4 &(\d+)',tb)[1]
 def vec(prefix,axes,defaults):return ', '.join(a+': '+props.get(prefix+'.'+a,d) for a,d in zip(axes,defaults))
 common='  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
 replacements[b]=f'--- !u!1 &{pid}\nGameObject:\n'+common+f'  serializedVersion: 6\n  m_Component:\n  - component: {{fileID: {tid}}}\n  m_Layer: 0\n  m_Name: {name}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: 1\n'
 replacements[tb]=f'--- !u!4 &{tid}\nTransform:\n'+common+f'  m_GameObject: {{fileID: {pid}}}\n  serializedVersion: 2\n  m_LocalRotation: {{{vec("m_LocalRotation","xyzw",["0","0","0","1"])}}}\n  m_LocalPosition: {{{vec("m_LocalPosition","xyz",["0"]*3)}}}\n  m_LocalScale: {{{vec("m_LocalScale","xyz",["1"]*3)}}}\n  m_ConstrainProportionsScale: 0\n  m_Children: []\n  m_Father: {{fileID: {parent}}}\n  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}\n'
 audit.append(dict(name=name,transform_file_id=tid,parent=parent,x=props['m_LocalPosition.x'],z=props['m_LocalPosition.z']))
assert len(audit)==6,len(audit)
backup=Path('Backups')/('SorpigalMissingPrefabAnchors_'+datetime.datetime.now().strftime('%Y%m%d_%H%M%S'));backup.mkdir();shutil.copy2(p,backup/p.name)
new=''.join(replacements.get(b,b) for b in blocks)
assert not any(g in new for g in forbidden)
p.write_text(new,encoding='utf-8',newline='\n')
(backup/'anchor_conversion.json').write_text(json.dumps(audit,indent=2))
print('Converted six missing prefab references to anchor objects, retaining transform file IDs and exact serialized X/Z. Backup:',backup)

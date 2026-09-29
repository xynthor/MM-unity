from pathlib import Path
import zipfile
files=list(Path('Assets/Editor').glob('Build*OpenWorld.cs'))+[Path('Assets/Editor/MMHeroSetup.cs')]
backup=Path('Validation/Hero/HeroCode_before.zip')
if not backup.exists():
 with zipfile.ZipFile(backup,'w',zipfile.ZIP_DEFLATED) as z:
  for p in files:
   if 'BuildPlayer(' in p.read_text() or p.name in ['BuildEnrothLinkedOpenWorld.cs','MMHeroSetup.cs']:z.write(p,p.as_posix())
changed=[]
for p in files:
 s=p.read_text();old=s
 if p.name=='MMHeroSetup.cs':
  s=s.replace('static void ReplacePlayerVisual(MMThirdPersonController ctrl)','public static void ReplacePlayerVisual(MMThirdPersonController ctrl)')
  s=s.replace('visual.name = "Hero Visual";', 'visual.name = "HERAKLIOS Visual";')
  s=s.replace('float targetHeight = cc ? cc.height : 1.08f;', 'float targetHeight = cc ? cc.height * Mathf.Abs(player.transform.lossyScale.y) : 1.08f;')
  s=s.replace('visual.transform.position += Vector3.up * (player.transform.position.y - b.min.y);','float capsuleBottom = cc ? player.transform.TransformPoint(cc.center - Vector3.up * cc.height * 0.5f).y : player.transform.position.y;\n        visual.transform.position += Vector3.up * (capsuleBottom - b.min.y);')
 elif p.name=='BuildEnrothLinkedOpenWorld.cs':
  needle='var ctrl=player.GetComponent<MMThirdPersonController>();'
  if needle in s and 'MMHeroSetup.ReplacePlayerVisual(ctrl);' not in s:s=s.replace(needle,needle+'\n        MMHeroSetup.ReplacePlayerVisual(ctrl);',1)
 else:
  s=s.replace('Assets/Player/Hero/player_character.fbx','Assets/Player/Hero/Heraklios.fbx')
  marker='static RuntimeAnimatorController CreatePlayerAnimatorController()'
  if marker in s:
   a=s.index('{',s.index(marker));depth=1;b=a+1
   while depth:
    if s[b]=='{':depth+=1
    elif s[b]=='}':depth-=1
    b+=1
   s=s[:a]+'''{
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Player/Hero/MMHero.controller");
        if (!controller) throw new Exception("HERAKLIOS Animator Controller is missing; run MMHeroSetup first.");
        return controller;
    }'''+s[b:]
 if s!=old:p.write_text(s);changed.append(str(p))
Path('Validation/Hero/builder_changes.txt').write_text('\n'.join(changed))
print('\n'.join(changed))

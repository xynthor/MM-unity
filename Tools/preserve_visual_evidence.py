from pathlib import Path
p=Path('Tools/VisualRefinementMaterials.cs');s=p.read_text().replace('foreach(var r in rs){','foreach(var r in rs.Where(r=>s.name!="NewSorpigal_OpenWorld")){');p.write_text(s)
import shutil
for ext in ['material_changes.txt','done.txt','protected_before.txt']:
 p=Path('Validation/VisualRefinement/Blackshire_SourceGrid_'+ext)
 if p.exists():shutil.copyfile(p,p.with_name(p.name.replace('_'+ext,'_initial_'+ext)))

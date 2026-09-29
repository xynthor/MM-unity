from PIL import Image,ImageDraw
from pathlib import Path
import csv,collections,json
root=Path('Validation/SequentialRepair')
ids=['searsia_lucida','fir_sapling','island_tree_01','island_tree_02','island_tree_03','jacaranda_tree']
qs=['identity','plus90X','minus90X','plus90Z','minus90Z']
for name in ids:
    sheet=Image.new('RGB',(1500,330),'#222222'); d=ImageDraw.Draw(sheet)
    for i,q in enumerate(qs):
        im=Image.open(root/'calibration'/f'{name}_{q}.png'); im.thumbnail((300,300));sheet.paste(im,(i*300,30));d.text((i*300+5,10),name+' '+q,fill='white')
    sheet.save(root/'calibration'/f'{name}_contact.jpg')
rows=list(csv.DictReader(open(root/'all_transforms_baseline.csv',encoding='utf-8-sig')))
counts=collections.Counter(x['scene'] for x in rows)
(root/'baseline_summary.json').write_text(json.dumps(dict(counts),indent=2))
print(dict(counts))

from PIL import Image,ImageOps,ImageDraw,ImageFont
from pathlib import Path
root=Path(r"C:\MMUnityPort\Validation\AuditScreens")
names=[
"NewSorpigal_OpenWorld.png","CastleIronfist_SourceGrid.png","MireOfTheDamned_SourceGrid.png",
"Dragonsand_SourceGrid.png","HermitsIsle_SourceGrid.png","MistyIslands_SourceGrid.png",
"BootlegBay_SourceGrid.png","FreeHaven_SourceGrid.png","Blackshire_SourceGrid.png",
"ParadiseValley_SourceGrid.png","EelInfestedWaters_SourceGrid.png","SilverCove_SourceGrid.png",
"FrozenHighlands_SourceGrid.png","Kriegspire_SourceGrid.png","SweetWater_SourceGrid.png"]
labels=["New Sorpigal","Castle Ironfist","Mire","Dragonsand","Hermit's Isle","Misty Islands","Bootleg Bay","Free Haven","Blackshire","Paradise Valley","Eel Waters","Silver Cove","Frozen Highlands","Kriegspire","Sweet Water"]
thumb=360; header=34; cols=3; rows=5
sheet=Image.new("RGB",(cols*thumb,rows*(thumb+header)),(30,30,30))
draw=ImageDraw.Draw(sheet)
for i,(fn,lab) in enumerate(zip(names,labels)):
    im=Image.open(root/fn).convert("RGB")
    im=ImageOps.fit(im,(thumb,thumb))
    x=(i%cols)*thumb;y=(i//cols)*(thumb+header)
    sheet.paste(im,(x,y+header))
    draw.rectangle((x,y,x+thumb,y+header),fill=(25,25,25))
    draw.text((x+8,y+8),lab,fill=(255,255,255))
out=Path(r"C:\MMUnityPort\Validation\AuditScreens_CONTACT_RESTORED.png")
sheet.save(out)
print(out)

from pathlib import Path
import struct, shutil, csv
ROOT=Path(r"C:\MMUnityPort")
DT=ROOT/"Extract/TileData/dtile.bin"
b=DT.read_bytes(); n=struct.unpack_from("<I",b,0)[0]
tiles=[]
for i in range(n):
    q=4+i*26
    name=b[q:q+16].split(b"\0",1)[0].decode("latin1","replace")
    _id,bmp,ts,sect,attr=struct.unpack_from("<5h",b,q+16)
    tiles.append((name,ts,sect,attr&0xffff))
cfg={
"SweetWater":"outa1.odm","ParadiseValley":"outa2.odm","HermitsIsle":"outa3.odm",
"Kriegspire":"outb1.odm","Blackshire":"outb2.odm","Dragonsand":"outb3.odm",
"FrozenHighlands":"outc1.odm","FreeHaven":"outc2.odm","MireOfTheDamned":"outc3.odm",
"SilverCove":"outd1.odm","BootlegBay":"outd2.odm","CastleIronfist":"outd3.odm",
"EelInfestedWaters":"oute1.odm","MistyIslands":"oute2.odm","NewSorpigal":"oute3.odm"}
BACK=ROOT/"Backups"/"ExactDTileTruth_20260918"; BACK.mkdir(parents=True,exist_ok=True)
for z,f in cfg.items():
    src=(ROOT/"Extract"/z/f).read_bytes(); td=struct.unpack_from("<8H",src,160)
    data=ROOT/"Assets"/"World"/z/"Data"
    for fn in ("tile_groups_u8.bin","tile_semantics_u8.bin"):
        p=data/fn
        if p.exists(): shutil.copy2(p,BACK/f"{z}_{fn}")
    groups=bytearray(256); sem=bytearray(256)
    rows=[["raw","resolved_dtile_index","name","tileset","section","attr","deep_water","shore","transition","road"]]
    for raw in range(256):
        if 90<=raw<=125: idx=raw-90+td[1]
        elif 126<=raw<=161: idx=raw
        elif 162<=raw<=197: idx=raw-162+td[5]
        elif raw>=198: idx=raw-198+td[7]
        else: idx=raw
        if idx>=len(tiles):
            name,ts,sect,attr=("pending",255,0,0)
        else:
            name,ts,sect,attr=tiles[idx]
        g=ts if 0<=ts<255 else 255
        groups[raw]=g
        deep=bool(attr&0x0002) and not bool(attr&0x0100)
        shore=bool(attr&0x0100)
        trans=bool(attr&0x0200)
        road=(8<=g<255)
        fbits=(1 if deep else 0)|(2 if shore else 0)|(4 if trans else 0)|(8 if road else 0)
        if g==0: fbits|=32
        elif g not in (5,255) and not road: fbits|=16
        sem[raw]=fbits
        rows.append([raw,idx,name,g,sect,hex(attr),int(deep),int(shore),int(trans),int(road)])
    (data/"tile_groups_u8.bin").write_bytes(groups)
    (data/"tile_semantics_u8.bin").write_bytes(sem)
    with (data/"tile_truth_resolved.csv").open("w",newline="",encoding="utf-8") as fh: csv.writer(fh).writerows(rows)
    print("TRUTH",z,"tile_data",td)
print("EXACT_DTILE_TRUTH_DONE zones=15")

from pathlib import Path
import shutil
root=Path(r"C:\MMUnityPort")
for z in ("NewSorpigal","CastleIronfist"):
    src=root/"Backups"/"PreXHandFix_20260917_2305"/z/"Objects"
    dst=root/"Assets"/"World"/z/"Objects"
    n=0
    for p in src.glob("*.obj"):
        shutil.copy2(p,dst/p.name)
        n+=1
    shutil.copy2(root/"Backups"/"PreXHandFix_20260917_2305"/z/"Data"/"model_placement_audit.csv",
                 root/"Assets"/"World"/z/"Data"/"model_placement_audit.csv")
    print(z,n)
print("NS_CI_PREX_GEOMETRY_RESTORED")

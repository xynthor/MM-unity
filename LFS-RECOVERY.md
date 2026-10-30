# Restoring Git LFS assets

GitHub's monthly LFS budget was exhausted when the repository was recreated on October 30, 2026. The **three current branch snapshots** are restored, and the **10 current LFS binary objects** are available as a verified [GitHub Release](https://github.com/xynthor/MM-unity/releases/tag/lfs-assets-20261030). Until LFS uploads are permitted again, a normal clone's automatic LFS download will not work.

## Windows PowerShell

```powershell
$env:GIT_LFS_SKIP_SMUDGE = "1"
git clone https://github.com/xynthor/MM-unity.git
cd MM-unity
powershell -NoProfile -ExecutionPolicy Bypass -File .\Restore-LFS-From-Release.ps1
```

To switch to another branch:

```powershell
git switch codex/canonical-north-dragon
powershell -NoProfile -ExecutionPolicy Bypass -File .\Restore-LFS-From-Release.ps1
```

The script verifies the release ZIP's SHA-256, checks every individual LFS object's SHA-256, installs those objects in the local Git LFS cache, and runs `git lfs checkout`. No GitHub LFS download is needed.

**Release asset:** [MM-unity-current-LFS-assets-20261030.zip](https://github.com/xynthor/MM-unity/releases/download/lfs-assets-20261030/MM-unity-current-LFS-assets-20261030.zip)  
**Archive SHA-256:** `024D13CB19439F9F18736049FF6B7A9C98193CFD47421331BF48BB77EC7A0647`  
**Contents:** 10 distinct objects, 1,233,757,960 uncompressed bytes (1.149 GiB). 

## History and safeguards

The recreated repository intentionally contains clean snapshot commits instead of the historical commits that contributed approximately 12.18 GiB of LFS references. Branch snapshots preserve the exact original file trees at the time of cleanup. The **full original Git history bundle and clean mirror** are backed up locally under `C:\Users\batsi\GitBackups`. These historical backups do **not** preserve obsolete LFS binary revisions, which were not downloaded before deleting the old repository.

Do not delete the recovery release while GitHub's LFS limit blocks uploads. Once quota permits, publish these cached LFS binaries to GitHub LFS and then verify normal clones before considering the recovery release unnecessary.
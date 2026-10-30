# Restore verified Unity LFS binaries while GitHub's monthly LFS quota is exhausted.
# Windows PowerShell 5.1+; run from inside a clone of xynthor/MM-unity.
$ErrorActionPreference = 'Stop'
$release = 'https://github.com/xynthor/MM-unity/releases/download/lfs-assets-20261030/MM-unity-current-LFS-assets-20261030.zip'
$expectedArchiveSha256 = '024d13cb19439f9f18736049ff6b7a9c98193cfd47421331bf48bb77ec7a0647'
$root = (& git rev-parse --show-toplevel)
if ($LASTEXITCODE -ne 0 -or -not $root) { throw 'Run this inside a cloned MM-unity repository.' }
$root = $root.Trim()
$gitDir = (& git rev-parse --absolute-git-dir)
if ($LASTEXITCODE -ne 0 -or -not $gitDir) { throw 'Cannot locate Git directory.' }
$gitDir = $gitDir.Trim()
$work = Join-Path ([IO.Path]::GetTempPath()) ('mm-unity-lfs-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work -Force | Out-Null
try {
  $zip = Join-Path $work 'assets.zip'
  $expanded = Join-Path $work 'assets'
  Write-Output 'Downloading 267 MB verified LFS asset archive from GitHub Releases...'
  Invoke-WebRequest -Uri $release -OutFile $zip -UseBasicParsing
  $zipHash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
  if ($zipHash -ne $expectedArchiveSha256) { throw 'Release archive SHA-256 mismatch. No LFS files installed.' }
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  [IO.Compression.ZipFile]::ExtractToDirectory($zip, $expanded)
  $files = @(Get-ChildItem $expanded -File -Recurse)
  if ($files.Count -ne 10) { throw "Unexpected number of binary objects: $($files.Count), expected 10." }
  $verified = @()
  foreach ($file in $files) {
    $sha = $file.Name.ToLowerInvariant()
    if ($sha -notmatch '^[a-f0-9]{64}$') { throw "Unexpected object filename: $sha" }
    if ((Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sha) {
      throw "LFS binary checksum mismatch: $sha"
    }
    $verified += $file
  }
  $count = 0
  foreach ($file in $verified) {
    $sha = $file.Name.ToLowerInvariant()
    $folder = Join-Path $gitDir ("lfs\objects\" + $sha.Substring(0, 2) + "\" + $sha.Substring(2, 2))
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    $target = Join-Path $folder $sha
    if (-not (Test-Path $target)) { Copy-Item -LiteralPath $file.FullName -Destination $target }
    if ((Get-Item $target).Length -ne $file.Length -or (Get-FileHash $target -Algorithm SHA256).Hash.ToLowerInvariant() -ne $sha) {
      throw "LFS cache verification failed: $sha"
    }
    $count++
  }
  Push-Location $root
  try {
    git lfs checkout
    if ($LASTEXITCODE -ne 0) { throw 'git lfs checkout failed' }
  } finally { Pop-Location }
  Write-Output "SUCCESS: $count verified LFS binaries available locally; current branch assets checked out."
} finally {
  Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
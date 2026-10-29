# Tworzy paczke do publikacji: _release\<Name>-<Wersja>.zip z folderem <Name>-<Wersja>\ w srodku
# (oraz folderami modow z -Include, kazdy z wlasna wersja), zeby po "wypakuj tutaj" od razu bylo widac wersje.
# Gra bierze nazwe moda z ModInfo.xml, nie z folderu.
# Uzycie: .\tools\release.ps1 <ModName> [-Include <Mod2>,<Mod3>]
param([Parameter(Mandatory)][string]$ModName, [string[]]$Include = @())
. "$PSScriptRoot\common.ps1"

$releaseDir = Join-Path $RepoRoot '_release'
$staging = Join-Path $releaseDir "_staging"
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Force $staging | Out-Null

$labels = @()
foreach ($m in @($ModName) + $Include) {
    $source = Get-ModSource $m
    $info = Get-ModInfo $source
    if (-not $info) { throw "Brak ModInfo.xml w $source" }
    if ($info.Name -ne $m) { throw "ModInfo Name '$($info.Name)' != '$m'" }
    if ($info.Version -notmatch '^\d+\.\d+\.\d+\.\d+$') { throw "Wersja '$($info.Version)' w $m nie ma formatu <wersja gry>.<rewizja> (np. 3.4.0.1)" }
    $label = "$($info.Name)-$($info.Version)"
    Copy-ModFiles $source (Join-Path $staging $label)
    $labels += $label
}

$zip = Join-Path $releaseDir "$($labels[0]).zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
# Wpisy z separatorem '/' (Compress-Archive i ZipFile w PS 5.1 zapisuja '\', co psuje rozpakowanie w czesci programow).
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open($zip, [IO.Compression.ZipArchiveMode]::Create)
try {
    Get-ChildItem $staging -Recurse -File | ForEach-Object {
        $entry = $_.FullName.Substring($staging.Length).TrimStart('\').Replace('\', '/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $_.FullName, $entry, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $archive.Dispose() }
Remove-Item $staging -Recurse -Force

# Starsze paczki tego samego moda przestaja byc aktualne.
Get-ChildItem $releaseDir -Filter "$ModName*.zip" | Where-Object { $_.Name -ne "$($labels[0]).zip" -and $_.BaseName -match "^$([regex]::Escape($ModName))(-\d+(\.\d+){3})?$" } | ForEach-Object {
    Remove-Item $_.FullName -Force
    Write-Host "Usunieto stara paczke $($_.Name)"
}
Write-Host "Gotowe: $zip (foldery w srodku: $($labels -join ', '))"

# Wspolne ustawienia dla skryptow. Sciezke gry mozna nadpisac zmienna srodowiskowa SDTD_PATH.
$GamePath = if ($env:SDTD_PATH) { $env:SDTD_PATH } else { 'C:\Program Files\.My\Steam\steamapps\common\7 Days To Die' }
$RepoRoot = Split-Path $PSScriptRoot -Parent
$ModdingRoot = Split-Path $RepoRoot -Parent
$DecompiledRoot = Join-Path $ModdingRoot '_Decompiled'
$GameLogDir = Join-Path $env:APPDATA '7DaysToDie\logs'

function Get-GameBuildId {
    $manifest = Join-Path (Split-Path (Split-Path $GamePath -Parent) -Parent) 'appmanifest_251570.acf'
    if (Test-Path $manifest) {
        $m = Select-String -Path $manifest -Pattern '"buildid"\s+"(\d+)"' | Select-Object -First 1
        if ($m) { return $m.Matches[0].Groups[1].Value }
    }
    return 'unknown'
}

# Zwraca @{ Name; Version } z ModInfo.xml w podanym folderze (albo $null).
function Get-ModInfo([string]$Folder) {
    $file = Join-Path $Folder 'ModInfo.xml'
    if (-not (Test-Path $file)) { return $null }
    [xml]$x = [IO.File]::ReadAllText($file)
    return @{ Name = $x.xml.Name.value; Version = $x.xml.Version.value }
}

# Gotowy mod: _build\<Mod> (DLL) albo <Mod> (XML). Zwraca sciezke.
function Get-ModSource([string]$ModName) {
    $projectDir = Join-Path $RepoRoot $ModName
    $isDll = [bool](Get-ChildItem $projectDir -Filter '*.csproj' -ErrorAction SilentlyContinue)
    if ($isDll) {
        # Zawsze swiezy build (kopiuje tez ModInfo.xml/README.md do _build).
        & dotnet build $projectDir -nologo -v q | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "dotnet build nie powiodl sie dla $ModName" }
        return (Join-Path $RepoRoot "_build\$ModName")
    }
    # Mody generowane z danych gry: zawsze swiezy wynik generatora (README, ModInfo, Config).
    $generator = Join-Path $RepoRoot "_generators\$($ModName -replace '^JC-', '')\generate.py"
    if (Test-Path $generator) {
        & python -I $generator | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Generator $generator zakonczyl sie bledami - patrz report.md" }
    }
    return $projectDir
}

# Kopiuje mod bez plikow zrodlowych i artefaktow builda.
function Copy-ModFiles([string]$Source, [string]$Target) {
    New-Item -ItemType Directory -Force $Target | Out-Null
    $skip = '\\(bin|obj|\.vs|Harmony|Properties|__pycache__)(\\|$)|\.(cs|csproj|user|py|pyc)$'
    Get-ChildItem $Source -Recurse -File | ForEach-Object {
        $rel = $_.FullName.Substring($Source.Length).TrimStart('\')
        if ("\$rel" -notmatch $skip) {
            $dest = Join-Path $Target $rel
            New-Item -ItemType Directory -Force (Split-Path $dest -Parent) | Out-Null
            Copy-Item $_.FullName $dest -Force
        }
    }
}

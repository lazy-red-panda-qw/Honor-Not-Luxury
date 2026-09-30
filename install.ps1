# Installs runtime files and appends one normalized package ID to ModsConfig.
# ASCII-only for Windows PowerShell 5.1. Close RimWorld before installing.
[CmdletBinding()]
param(
    [string]$RimWorldDir = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld',
    [string]$ConfigPath = (Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml'),
    [switch]$WhatIfOnly
)
$ErrorActionPreference = 'Stop'
$packageId = 'local.honornotluxury'
$sourceDir = Join-Path $PSScriptRoot 'HonorNotLuxury'

function FullPath([string]$path) { [IO.Path]::GetFullPath($path).TrimEnd('\') }
function AssertChild([string]$path, [string]$parent) {
    if (-not (FullPath $path).StartsWith((FullPath $parent) + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside the intended directory: $path"
    }
}
function AssertNoReparse([string]$path) {
    $cursor = FullPath $path
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw "Use a physical path rather than a junction or symlink: $cursor"
            }
        }
        $cursor = [IO.Path]::GetDirectoryName($cursor)
    }
}
function ReadXml([string]$path) {
    $settings = New-Object Xml.XmlReaderSettings
    $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
    $reader = [Xml.XmlReader]::Create($path, $settings)
    try {
        $doc = New-Object Xml.XmlDocument
        $doc.PreserveWhitespace = $true
        $doc.XmlResolver = $null
        $doc.Load($reader)
        return ,$doc
    } finally { $reader.Dispose() }
}

$RimWorldDir = FullPath $RimWorldDir
$ConfigPath = FullPath $ConfigPath
$sourceDir = FullPath $sourceDir
$modsDir = Join-Path $RimWorldDir 'Mods'
$target = Join-Path $modsDir 'HonorNotLuxury'
AssertChild $target $modsDir
foreach ($path in @($sourceDir, $target, $ConfigPath)) { AssertNoReparse $path }
if ($sourceDir -eq $target -or $sourceDir.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Run the installer from the extracted project, outside its installation target.'
}
foreach ($path in @((Join-Path $RimWorldDir 'RimWorldWin64.exe'), $modsDir, $ConfigPath,
    (Join-Path $sourceDir 'About\About.xml'), (Join-Path $sourceDir 'Assemblies\HonorNotLuxury.dll'),
    (Join-Path $sourceDir 'Languages'), (Join-Path $sourceDir 'README.md'),
    (Join-Path $sourceDir 'README.en.md'), (Join-Path $sourceDir 'LICENSE'),
    (Join-Path $sourceDir 'Docs\QA_V2_REPORT.md'))) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Required path not found: $path" }
}
$about = ReadXml (Join-Path $sourceDir 'About\About.xml')
if ($about.ModMetaData.packageId -ine $packageId) { throw 'Unexpected source package ID.' }
$config = ReadXml $ConfigPath
$active = $config.SelectSingleNode('/ModsConfigData/activeMods')
if ($null -eq $active) { throw 'ModsConfig.xml has no ModsConfigData/activeMods element.' }
$entries = @($active.SelectNodes('li'))
$existing = @($entries | Where-Object { $_.InnerText.Trim() -ieq $packageId })
$needsConfig = $existing.Count -ne 1 -or $entries.Count -eq 0 -or
    $entries[-1].InnerText -cne $packageId -or $existing[0].InnerText -cne $packageId
if ($needsConfig) {
    foreach ($entry in $existing) { [void]$active.RemoveChild($entry) }
    $entry = $config.CreateElement('li')
    $entry.InnerText = $packageId
    [void]$active.AppendChild($entry)
}
Write-Host "Runtime destination: $target"
Write-Host "Configuration: $ConfigPath"
if ($WhatIfOnly) {
    Write-Host '[Preview] Copy About, Assemblies, Languages, READMEs, LICENSE and QA report only.'
    Write-Host '[Preview] Existing installation will be backed up outside Mods.'
    Write-Host "[Preview] Normalize/append package ID: $needsConfig. No files changed."
    return
}
if (Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue) {
    throw 'Close RimWorld before replacing its loaded mod files.'
}
$stamp = (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8)
$stage = Join-Path $RimWorldDir ('.HonorNotLuxury-stage-' + $stamp)
$stagedMod = Join-Path $stage 'mod'
$backup = Join-Path $RimWorldDir ('HonorNotLuxury-backups\' + $stamp)
$configTemp = $ConfigPath + '.hnl-' + $stamp + '.tmp'
$configBackup = $ConfigPath + '.hnl-' + $stamp + '.bak'
AssertChild $stage $RimWorldDir
AssertChild $backup $RimWorldDir
AssertNoReparse $backup
$oldMoved = $false
$newMoved = $false
try {
    [void](New-Item -ItemType Directory -Path $stagedMod)
    foreach ($part in @('About','Assemblies','Languages','Docs','README.md','README.en.md','LICENSE')) {
        Copy-Item -LiteralPath (Join-Path $sourceDir $part) -Destination $stagedMod -Recurse
    }
    # Keep the existing Workshop item when updating an installed copy. A
    # PublishedFileId.txt bundled with the source takes precedence if supplied.
    $oldWorkshopId = Join-Path $target 'About\PublishedFileId.txt'
    $stagedWorkshopId = Join-Path $stagedMod 'About\PublishedFileId.txt'
    if (-not (Test-Path -LiteralPath $stagedWorkshopId) -and (Test-Path -LiteralPath $oldWorkshopId)) {
        AssertNoReparse $oldWorkshopId
        Copy-Item -LiteralPath $oldWorkshopId -Destination $stagedWorkshopId
    }
    if ((Get-FileHash -LiteralPath (Join-Path $sourceDir 'Assemblies\HonorNotLuxury.dll')).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $stagedMod 'Assemblies\HonorNotLuxury.dll')).Hash) {
        throw 'Staged DLL verification failed.'
    }
    if ($needsConfig) {
        $writerSettings = New-Object Xml.XmlWriterSettings
        $writerSettings.Encoding = New-Object Text.UTF8Encoding($false)
        $writer = [Xml.XmlWriter]::Create($configTemp, $writerSettings)
        try { $config.Save($writer) } finally { $writer.Dispose() }
        [void](ReadXml $configTemp)
    }
    if (Test-Path -LiteralPath $target) {
        [void](New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($backup)) -Force)
        Move-Item -LiteralPath $target -Destination $backup
        $oldMoved = $true
    }
    Move-Item -LiteralPath $stagedMod -Destination $target
    $newMoved = $true
    if ($needsConfig) { [IO.File]::Replace($configTemp, $ConfigPath, $configBackup) }
} catch {
    $installError = $_
    if ($newMoved) {
        AssertChild $target $modsDir
        Move-Item -LiteralPath $target -Destination (Join-Path $stage 'failed-install')
    }
    if ($oldMoved) { Move-Item -LiteralPath $backup -Destination $target }
    throw $installError
} finally {
    if (Test-Path -LiteralPath $configTemp) { Remove-Item -LiteralPath $configTemp -Force }
    if ((Test-Path -LiteralPath $stage) -and @(Get-ChildItem -LiteralPath $stage -Force).Count -eq 0) {
        Remove-Item -LiteralPath $stage -Force
    }
}
Write-Host 'Installed Honor, Not Luxury 2.0. Configure it in Options > Mod settings.'
if ($oldMoved) { Write-Host "Previous installation: $backup" }
if ($needsConfig) { Write-Host "Previous mod configuration: $configBackup" }
Write-Host 'Expected log: [Honor, Not Luxury] 2.0 active: seven scoped policies; bestowing requirements preserved.'

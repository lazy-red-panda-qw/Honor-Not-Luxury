# Builds a redistributable archive from the reviewed mod and source files. No game files are changed.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$projectRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$releaseDir = Join-Path $projectRoot 'Releases'
$outputPath = Join-Path $releaseDir 'HonorNotLuxury-2.0.0.zip'
$temporaryPath = Join-Path $releaseDir ('.HonorNotLuxury-2.0.0-' + [guid]::NewGuid().ToString('N') + '.tmp')
$previousPath = Join-Path $releaseDir 'HonorNotLuxury-2.0.0.previous.zip'

# The Workshop ID belongs to a particular published item. It stays with the
# installed copy and is intentionally excluded from a generic download.
$relativePaths = @(
    'README.md',
    'LICENSE',
    '.gitignore',
    '.github/ISSUE_TEMPLATE/compatibility.md',
    'install.ps1',
    'package.ps1',
    'PUBLISHING.md',
    'WORKSHOP_DESCRIPTION.md',
    'HonorNotLuxury/README.md',
    'HonorNotLuxury/README.en.md',
    'HonorNotLuxury/LICENSE',
    'HonorNotLuxury/.gitignore',
    'HonorNotLuxury/Docs/QA_V2_REPORT.md',
    'HonorNotLuxury/About/About.xml',
    'HonorNotLuxury/Assemblies/HonorNotLuxury.dll',
    'HonorNotLuxury/Languages/ChineseSimplified/Keyed/HonorNotLuxury.xml',
    'HonorNotLuxury/Languages/English/Keyed/HonorNotLuxury.xml',
    'HonorNotLuxury/Source/AssemblyInfo.cs',
    'HonorNotLuxury/Source/HonorNotLuxury.csproj',
    'HonorNotLuxury/Source/HonorNotLuxuryMod.cs',
    'HonorNotLuxury/Source/HonorNotLuxurySettings.cs',
    'HonorNotLuxury/Source/Patches.cs',
    'HonorNotLuxury/Source/RequirementPolicy.cs'
)
$hashes = [ordered]@{}
foreach ($relativePath in $relativePaths) {
    $sourcePath = Join-Path $projectRoot ($relativePath.Replace('/', '\'))
    if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) { throw "Missing release file: $relativePath" }
    $hashes[$relativePath] = (Get-FileHash -LiteralPath $sourcePath -Algorithm SHA256).Hash.ToLowerInvariant()
}
$about = [xml]([IO.File]::ReadAllText((Join-Path $projectRoot 'HonorNotLuxury\About\About.xml')))
if ($about.ModMetaData.packageId -ne 'local.honornotluxury') { throw 'Unexpected package ID.' }

$manifest = [ordered]@{
    name = 'Honor, Not Luxury'
    version = '2.0.0'
    gameAssemblyVersion = '1.6.9676.17735'
    controlledBehaviorChecks = 118
    isolatedInstallerChecks = 20
    inGameValidation = [ordered]@{
        date = '2026-09-27'
        source = 'Maintainer report from a 140+ mod playthrough'
        confirmed = @('Game loads', 'Mod settings visible', 'Independent requirement switches', 'Mood penalty returns when set to Unchanged')
        fullBestowingAndAllModCompatibilityTested = $false
    }
    sha256 = $hashes
}
$manifestJson = ($manifest | ConvertTo-Json -Depth 7) + "`n"

[void](New-Item -ItemType Directory -Path $releaseDir -Force)
try {
    $archiveStream = [IO.File]::Open($temporaryPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite)
    try {
        $archive = [IO.Compression.ZipArchive]::new($archiveStream, [IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            foreach ($relativePath in $relativePaths) {
                $sourcePath = Join-Path $projectRoot ($relativePath.Replace('/', '\'))
                $entry = $archive.CreateEntry($relativePath, [IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = [DateTimeOffset]::new(2026, 9, 27, 0, 0, 0, [TimeSpan]::Zero)
                $inputStream = [IO.File]::OpenRead($sourcePath)
                $entryStream = $entry.Open()
                try { $inputStream.CopyTo($entryStream) }
                finally { $entryStream.Dispose(); $inputStream.Dispose() }
            }
            $entry = $archive.CreateEntry('MANIFEST.json', [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = [DateTimeOffset]::new(2026, 9, 27, 0, 0, 0, [TimeSpan]::Zero)
            $writer = [IO.StreamWriter]::new($entry.Open(), [Text.UTF8Encoding]::new($false))
            try { $writer.Write($manifestJson) } finally { $writer.Dispose() }
        } finally { $archive.Dispose() }
    } finally { $archiveStream.Dispose() }

    $check = [IO.Compression.ZipFile]::OpenRead($temporaryPath)
    try {
        $expectedEntries = @($relativePaths) + @('MANIFEST.json')
        $actualEntries = @($check.Entries | ForEach-Object { $_.FullName })
        if (($actualEntries -join '|') -cne ($expectedEntries -join '|')) { throw 'Archive entry verification failed.' }
        foreach ($relativePath in $relativePaths) {
            $entry = $check.GetEntry($relativePath)
            $entryStream = $entry.Open()
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $actualHash = ([BitConverter]::ToString($sha.ComputeHash($entryStream))).Replace('-', '').ToLowerInvariant() }
            finally { $sha.Dispose(); $entryStream.Dispose() }
            if ($actualHash -cne $hashes[$relativePath]) { throw "Archive hash mismatch: $relativePath" }
        }
    } finally { $check.Dispose() }

    if (Test-Path -LiteralPath $outputPath) {
        if (Test-Path -LiteralPath $previousPath) { Remove-Item -LiteralPath $previousPath -Force }
        [IO.File]::Replace($temporaryPath, $outputPath, $previousPath)
    } else {
        Move-Item -LiteralPath $temporaryPath -Destination $outputPath
    }
    Write-Output "Packaged $outputPath"
    Write-Output "Runtime DLL SHA256: $($hashes['HonorNotLuxury/Assemblies/HonorNotLuxury.dll'])"
} finally {
    if (Test-Path -LiteralPath $temporaryPath) { Remove-Item -LiteralPath $temporaryPath -Force }
}

<#
.SYNOPSIS
  Packs the project into pos-phase<N>.zip for hand-off, without .git, .agents, build output, or secrets.

.EXAMPLE
  .\scripts\make-phase-zip.ps1 -Phase 2
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][int]$Phase,
    [string]$OutDir = (Join-Path $env:USERPROFILE 'Downloads')
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = (Split-Path -Parent $PSScriptRoot).TrimEnd('\')
$zipPath = Join-Path $OutDir "pos-phase$Phase.zip"
$excludedFolders = @('.git', '.agents', 'node_modules', 'bin', 'obj', '.next', '.terraform', 'coverage', 'TestResults', '.vs', '.idea')

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir | Out-Null }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

$zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    $count = 0
    Get-ChildItem -Path $root -Recurse -File -Force | ForEach-Object {
        $relative = $_.FullName.Substring($root.Length + 1)
        $segments = $relative.Split('\')
        if ($segments.Count -gt 1 -and ($segments[0..($segments.Count - 2)] | Where-Object { $excludedFolders -contains $_ })) { return }
        if ($_.Name -eq '.env' -or $_.Name -like '*.tfstate*' -or $_.Name -like '*.tfvars') { return }
        [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, ($relative -replace '\\', '/'), [System.IO.Compression.CompressionLevel]::Optimal)
        $count++
    }
}
finally {
    $zip.Dispose()
}

Write-Host "Created $zipPath ($count files)" -ForegroundColor Green
Write-Host 'Extract into the project folder with:'
Write-Host "  cd D:\pos-qr-code"
Write-Host "  Expand-Archive -Force $zipPath D:\pos-qr-code"

<#
.SYNOPSIS
  One-time (and safe to repeat) local setup for the POS project.

.DESCRIPTION
  1. Checks .NET 10 SDK and Docker Desktop.
  2. Creates .env with random secrets if it does not exist.
  3. Restores tools and builds the solution.
  4. Creates the Identity, Catalog, and Order database migrations if they do not exist yet.
  5. Starts PostgreSQL and RabbitMQ in Docker and waits until they are healthy.

.PARAMETER RunTests
  Also run the unit tests.

.PARAMETER SkipInfra
  Do not start PostgreSQL and RabbitMQ.
#>
[CmdletBinding()]
param(
    [switch]$RunTests,
    [switch]$SkipInfra
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Invoke-Native {
    param([scriptblock]$Command, [string]$ErrorMessage)
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $Command
        $code = $LASTEXITCODE
    }
    finally {
        $ErrorActionPreference = $previous
    }
    if ($code -ne 0) { throw $ErrorMessage }
}

function New-RandomSecret {
    param([int]$Length)
    $chars = 'abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789'.ToCharArray()
    $bytes = New-Object byte[] $Length
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $rng.Dispose()
    -join ($bytes | ForEach-Object { $chars[$_ % $chars.Length] })
}

Write-Host '== 1/5 Checking prerequisites ==' -ForegroundColor Cyan
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET SDK not found. Install the .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0'
}
$dotnetVersion = (& dotnet --version).Trim()
if ([int]($dotnetVersion.Split('.')[0]) -lt 10) {
    throw ".NET SDK $dotnetVersion found, but 10.x is required."
}
Write-Host "   .NET SDK $dotnetVersion"

if (-not $SkipInfra) {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker not found. Install Docker Desktop and start it.'
    }
    Invoke-Native { docker info *> $null } 'Docker is installed but not running. Start Docker Desktop and run this script again.'
    Write-Host '   Docker is running'
}

Write-Host '== 2/5 Environment file ==' -ForegroundColor Cyan
$envFile = Join-Path $root '.env'
if (Test-Path $envFile) {
    Write-Host '   .env already exists, keeping it'
}
else {
    $lines = foreach ($line in (Get-Content (Join-Path $root '.env.example'))) {
        if ($line -match '^JWT_SIGNING_KEY=__GENERATED__') {
            'JWT_SIGNING_KEY=' + (New-RandomSecret 64)
        }
        elseif ($line -match '__GENERATED__') {
            $line -replace '__GENERATED__', (New-RandomSecret 24)
        }
        else {
            $line
        }
    }
    [System.IO.File]::WriteAllLines($envFile, [string[]]$lines)
    Write-Host '   Created .env with random secrets (it is gitignored)'
}

Write-Host '== 3/5 Restore and build ==' -ForegroundColor Cyan
Invoke-Native { dotnet tool restore } 'dotnet tool restore failed.'
Invoke-Native { dotnet build POS.slnx -c Debug } 'Build failed. Copy the first error message and send it to the AI.'

Write-Host '== 4/5 Database migrations ==' -ForegroundColor Cyan
foreach ($service in @('IdentityService', 'CatalogService', 'OrderService')) {
    $migrations = Join-Path $root "services\$service\Migrations"
    if (Test-Path $migrations) {
        Write-Host "   ${service}: migrations folder exists, keeping it"
    }
    else {
        Invoke-Native {
            dotnet ef migrations add InitialCreate --project "services/$service" --output-dir Migrations
        } "Creating the $service migration failed."
        Write-Host "   ${service}: created InitialCreate migration (commit this folder to git)"
    }
}

if ($RunTests) {
    Write-Host '== Running unit tests ==' -ForegroundColor Cyan
    Invoke-Native { dotnet test POS.slnx -c Debug --no-build } 'Unit tests failed.'
}

if (-not $SkipInfra) {
    Write-Host '== 5/5 Starting PostgreSQL and RabbitMQ ==' -ForegroundColor Cyan
    Invoke-Native {
        docker compose -f infra/docker-compose.yml --env-file .env up -d --wait postgres rabbitmq
    } 'docker compose failed. If port 5432 is busy, change POSTGRES_HOST_PORT in .env.'
}

Write-Host ''
Write-Host 'Setup finished. Next: .\scripts\run-all.ps1' -ForegroundColor Green

<#
.SYNOPSIS
  One-time (and safe to repeat) local setup for the POS project.

.DESCRIPTION
  1. Checks .NET 10 SDK and Docker Desktop.
  2. Creates .env with random secrets if it does not exist.
  3. Restores tools and builds the solution.
  4. Creates the Identity, Catalog, and Order database migrations if they do not exist yet.
  5. Installs the web menu packages (apps/frontend) and creates its .env.local.
  6. Starts PostgreSQL and RabbitMQ in Docker and waits until they are healthy.

.PARAMETER RunTests
  Also run the unit tests (.NET and the web menu: type-check, lint, tests).

.PARAMETER SkipInfra
  Do not start PostgreSQL and RabbitMQ.
#>
[CmdletBinding()]
param(
    [switch]$RunTests,
    [switch]$SkipInfra,
    [switch]$SkipFrontend
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

if (-not $SkipFrontend) {
    if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
        throw 'Node.js not found. Install Node.js 22 LTS from https://nodejs.org (or run with -SkipFrontend).'
    }
    $nodeVersion = (& node --version).Trim().TrimStart('v')
    if ([int]($nodeVersion.Split('.')[0]) -lt 22) {
        throw "Node.js $nodeVersion found, but 22 or newer is required (or run with -SkipFrontend)."
    }
    Write-Host "   Node.js $nodeVersion"
}

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
    if (Select-String -Path $envFile -Pattern '__GENERATED__' -Quiet) {
        throw @'
.env still contains the placeholder __GENERATED__ (it was probably copied from .env.example by hand).
The services would refuse to start because the secrets are not real. Fix, then run this script again:
  docker compose -f infra/docker-compose.yml --env-file .env down -v
  Remove-Item .env
This deletes the local databases only. This script creates a new .env with random passwords.
'@
    }
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

if (-not $SkipFrontend) {
    Write-Host '== Web menu packages ==' -ForegroundColor Cyan
    $frontend = Join-Path $root 'apps\frontend'
    $envLocal = Join-Path $frontend '.env.local'
    if (-not (Test-Path $envLocal)) {
        Copy-Item (Join-Path $frontend '.env.example') $envLocal
        Write-Host '   Created apps\frontend\.env.local'
    }
    # On Windows "npm" is a PowerShell script that the execution policy can block. npm.cmd always works.
    $npm = if (Get-Command npm.cmd -ErrorAction SilentlyContinue) { 'npm.cmd' } else { 'npm' }
    Push-Location $frontend
    try {
        Invoke-Native { & $npm ci } 'npm ci failed in apps\frontend.'
        if ($RunTests) {
            Invoke-Native { & $npm run typecheck } 'Web menu type-check failed.'
            Invoke-Native { & $npm run lint } 'Web menu lint failed.'
            Invoke-Native { & $npm test } 'Web menu tests failed.'
        }
    }
    finally {
        Pop-Location
    }
}

if (-not $SkipInfra) {
    # A PostgreSQL installed on this PC can sit on the same port as the Docker one. Then "localhost" reaches the wrong
    # database and the services fail with "password authentication failed".
    $portLine = Get-Content (Join-Path $root '.env') | Where-Object { $_ -match '^POSTGRES_HOST_PORT=\d+' } | Select-Object -First 1
    $dbPort = if ($portLine -match '=(\d+)') { [int]$Matches[1] } else { 5432 }
    foreach ($listener in @(Get-NetTCPConnection -LocalPort $dbPort -State Listen -ErrorAction SilentlyContinue)) {
        $owner = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
        if ($owner -and $owner.ProcessName -like 'postgres*') {
            Write-Host "Another PostgreSQL ($($owner.ProcessName), process $($owner.Id)) is already using port $dbPort on this PC." -ForegroundColor Red
            Write-Host 'The services would connect to it instead of the Docker database. Change the POSTGRES_HOST_PORT line in .env to this and run this script again:' -ForegroundColor Red
            Write-Host '  POSTGRES_HOST_PORT=5433' -ForegroundColor Yellow
            throw "Port $dbPort is taken by another PostgreSQL."
        }
    }
}

if (-not $SkipInfra) {
    Write-Host '== 5/5 Starting PostgreSQL and RabbitMQ ==' -ForegroundColor Cyan
    Invoke-Native {
        docker compose -f infra/docker-compose.yml --env-file .env up -d --wait postgres rabbitmq
    } 'docker compose failed. If port 5432 is busy, change POSTGRES_HOST_PORT in .env.'
}

Write-Host ''
Write-Host 'Setup finished. Next: .\scripts\run-all.ps1' -ForegroundColor Green

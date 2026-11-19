<#
.SYNOPSIS
  Starts the API Gateway (5000), Identity (5003), Catalog (5001), Order (5002), Notification (5005),
  and the web menu (3000), each in its own window.
  PostgreSQL and RabbitMQ must already be running (scripts\dev-setup.ps1 starts them).

.PARAMETER LanIp
  Your PC's address on the local network, for example 192.168.1.50. Lets a phone on the same Wi-Fi open the menu
  at http://<LanIp>:3000. Allow ports 3000 and 5000 in Windows Firewall for private networks first.

.PARAMETER SkipFrontend
  Start only the backend services.
#>
[CmdletBinding()]
param(
    [string]$LanIp = '',
    [switch]$SkipFrontend
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$envFile = Join-Path $root '.env'
if (-not (Test-Path $envFile)) {
    throw '.env not found. Run .\scripts\dev-setup.ps1 first.'
}

$settings = @{}
foreach ($line in (Get-Content $envFile)) {
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$') {
        $settings[$Matches[1]] = $Matches[2]
    }
}

$problems = @()
foreach ($name in @('POSTGRES_USER', 'POSTGRES_PASSWORD', 'RABBITMQ_USER', 'RABBITMQ_PASSWORD', 'JWT_SIGNING_KEY', 'SEED_ADMIN_PASSWORD')) {
    $value = $settings[$name]
    if (-not $value) { $problems += "$name is missing" }
    elseif ($value -like '*__GENERATED__*') { $problems += "$name is still the placeholder __GENERATED__" }
}
if ($settings['JWT_SIGNING_KEY'] -and $settings['JWT_SIGNING_KEY'].Length -lt 32) {
    $problems += "JWT_SIGNING_KEY is only $($settings['JWT_SIGNING_KEY'].Length) characters (needs 32 or more)"
}
if ($problems.Count -gt 0) {
    Write-Host 'The .env file is not ready:' -ForegroundColor Red
    $problems | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Write-Host 'Fix (this deletes the local databases only, then creates a new .env with random passwords):' -ForegroundColor Yellow
    Write-Host '  docker compose -f infra/docker-compose.yml --env-file .env down -v'
    Write-Host '  Remove-Item .env'
    Write-Host '  .\scripts\dev-setup.ps1'
    throw '.env is not ready.'
}

$port = if ($settings['POSTGRES_HOST_PORT']) { $settings['POSTGRES_HOST_PORT'] } else { '5432' }

# A PostgreSQL installed on this PC can sit on the same port as the Docker one. Then "localhost" reaches the wrong
# database and the services fail with "password authentication failed".
$hostPortNumber = [int]$port
foreach ($listener in @(Get-NetTCPConnection -LocalPort $hostPortNumber -State Listen -ErrorAction SilentlyContinue)) {
    $owner = Get-Process -Id $listener.OwningProcess -ErrorAction SilentlyContinue
    if ($owner -and $owner.ProcessName -like 'postgres*') {
        throw @"
Another PostgreSQL ($($owner.ProcessName), process $($owner.Id)) is already using port $hostPortNumber on this PC.
The services would connect to it instead of the Docker database and fail to log in. Fix:
  1. In .env change the line to:   POSTGRES_HOST_PORT=5433
  2. docker compose -f infra/docker-compose.yml --env-file .env up -d --wait postgres rabbitmq
  3. Run this script again.
"@
    }
}

# Child windows inherit these variables. Secrets never go into source files.
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__IdentityDb = "Host=localhost;Port=$port;Database=identity_db;Username=$($settings['POSTGRES_USER']);Password=$($settings['POSTGRES_PASSWORD'])"
$env:ConnectionStrings__CatalogDb = "Host=localhost;Port=$port;Database=catalog_db;Username=$($settings['POSTGRES_USER']);Password=$($settings['POSTGRES_PASSWORD'])"
$env:ConnectionStrings__OrderDb = "Host=localhost;Port=$port;Database=order_db;Username=$($settings['POSTGRES_USER']);Password=$($settings['POSTGRES_PASSWORD'])"
$env:RabbitMq__Host = 'localhost'
$env:RabbitMq__Username = $settings['RABBITMQ_USER']
$env:RabbitMq__Password = $settings['RABBITMQ_PASSWORD']
$env:Currency__UsdToKhrRate = if ($settings['USD_TO_KHR_RATE']) { $settings['USD_TO_KHR_RATE'] } else { '4000' }
$env:Jwt__SigningKey = $settings['JWT_SIGNING_KEY']
$env:Seed__AdminUsername = $settings['SEED_ADMIN_USERNAME']
$env:Seed__AdminPassword = $settings['SEED_ADMIN_PASSWORD']

if ($LanIp) {
    # The phone's browser calls the gateway on this PC, so the gateway must allow that origin.
    $env:Cors__AllowedOrigins__1 = "http://${LanIp}:3000"
    $env:NEXT_PUBLIC_API_URL = "http://${LanIp}:5000"
    $env:ALLOWED_DEV_ORIGINS = $LanIp
}

function Start-Window {
    param([string]$Title, [string]$Directory, [string]$Run)
    $command = "`$Host.UI.RawUI.WindowTitle = '$Title'; Set-Location '$Directory'; $Run"
    Start-Process powershell -ArgumentList @('-NoExit', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', "`"$command`"")
}

function Start-ServiceWindow {
    param([string]$Title, [string]$Project)
    Start-Window -Title $Title -Directory $root -Run "dotnet run --project $Project"
}

Start-ServiceWindow -Title 'POS Identity :5003' -Project 'services/IdentityService'
Start-ServiceWindow -Title 'POS Catalog :5001' -Project 'services/CatalogService'
Start-ServiceWindow -Title 'POS Order :5002' -Project 'services/OrderService'
Start-ServiceWindow -Title 'POS Notification :5005' -Project 'services/NotificationService'
Start-ServiceWindow -Title 'POS Gateway :5000' -Project 'services/ApiGateway'

$frontend = Join-Path $root 'apps\frontend'
$frontendStarted = $false
if (-not $SkipFrontend) {
    if (Test-Path (Join-Path $frontend 'node_modules')) {
        Start-Window -Title 'POS Web :3000' -Directory $frontend -Run 'npm.cmd run dev'
        $frontendStarted = $true
    }
    else {
        Write-Host 'Web menu not started: run .\scripts\dev-setup.ps1 first (it installs the web packages).' -ForegroundColor Yellow
    }
}

Write-Host ''
if ($frontendStarted) {
    Write-Host 'Started six windows: Identity 5003, Catalog 5001, Order 5002, Notification 5005, Gateway 5000, Web 3000.' -ForegroundColor Green
}
else {
    Write-Host 'Started five windows: Identity 5003, Catalog 5001, Order 5002, Notification 5005, Gateway 5000.' -ForegroundColor Green
}
Write-Host 'Wait until every window says "Now listening" or "Ready" (about 30 seconds), then run:'
Write-Host '  .\scripts\smoke-test.ps1'
Write-Host '  .\scripts\smoke-test-phase2.ps1'
Write-Host '  .\scripts\smoke-test-phase3.ps1'
if ($frontendStarted) {
    Write-Host 'Open the menu: http://localhost:3000/?table=1'
    if ($LanIp) { Write-Host "On your phone (same Wi-Fi): http://${LanIp}:3000/?table=1" }
}
Write-Host "Admin login: $($settings['SEED_ADMIN_USERNAME']) / (SEED_ADMIN_PASSWORD in .env)"
Write-Host 'To stop: close all the windows, then  docker compose -f infra/docker-compose.yml --env-file .env down'

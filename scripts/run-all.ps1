<#
.SYNOPSIS
  Starts the API Gateway (5000), Identity (5003), Catalog (5001), Order (5002), and Notification (5005), each in its own window.
  PostgreSQL and RabbitMQ must already be running (scripts\dev-setup.ps1 starts them).
#>
[CmdletBinding()]
param()

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

$port = if ($settings['POSTGRES_HOST_PORT']) { $settings['POSTGRES_HOST_PORT'] } else { '5432' }

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

function Start-ServiceWindow {
    param([string]$Title, [string]$Project)
    $command = "`$Host.UI.RawUI.WindowTitle = '$Title'; Set-Location '$root'; dotnet run --project $Project"
    Start-Process powershell -ArgumentList @('-NoExit', '-NoProfile', '-Command', "`"$command`"")
}

Start-ServiceWindow -Title 'POS Identity :5003' -Project 'services/IdentityService'
Start-ServiceWindow -Title 'POS Catalog :5001' -Project 'services/CatalogService'
Start-ServiceWindow -Title 'POS Order :5002' -Project 'services/OrderService'
Start-ServiceWindow -Title 'POS Notification :5005' -Project 'services/NotificationService'
Start-ServiceWindow -Title 'POS Gateway :5000' -Project 'services/ApiGateway'

Write-Host ''
Write-Host 'Started five windows: Identity 5003, Catalog 5001, Order 5002, Notification 5005, Gateway 5000.' -ForegroundColor Green
Write-Host 'Wait until every window says "Now listening" (about 20 seconds), then run:'
Write-Host '  .\scripts\smoke-test.ps1'
Write-Host '  .\scripts\smoke-test-phase2.ps1'
Write-Host "Admin login: $($settings['SEED_ADMIN_USERNAME']) / (SEED_ADMIN_PASSWORD in .env)"
Write-Host 'To stop: close the five windows, then  docker compose -f infra/docker-compose.yml --env-file .env down'

<#
.SYNOPSIS
  Starts the API Gateway (port 5000) and the Identity Service (port 5003), each in its own window.
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
$env:Jwt__SigningKey = $settings['JWT_SIGNING_KEY']
$env:Seed__AdminUsername = $settings['SEED_ADMIN_USERNAME']
$env:Seed__AdminPassword = $settings['SEED_ADMIN_PASSWORD']

function Start-ServiceWindow {
    param([string]$Title, [string]$Project)
    $command = "`$Host.UI.RawUI.WindowTitle = '$Title'; Set-Location '$root'; dotnet run --project $Project"
    Start-Process powershell -ArgumentList @('-NoExit', '-NoProfile', '-Command', "`"$command`"")
}

Start-ServiceWindow -Title 'POS Identity :5003' -Project 'services/IdentityService'
Start-ServiceWindow -Title 'POS Gateway :5000' -Project 'services/ApiGateway'

Write-Host ''
Write-Host 'Started two windows: Identity (5003) and Gateway (5000).' -ForegroundColor Green
Write-Host 'Wait until both windows say "Now listening", then run:  .\scripts\smoke-test.ps1'
Write-Host "Admin login: $($settings['SEED_ADMIN_USERNAME']) / (SEED_ADMIN_PASSWORD in .env)"
Write-Host 'To stop: close the two windows, then  docker compose -f infra/docker-compose.yml --env-file .env down'

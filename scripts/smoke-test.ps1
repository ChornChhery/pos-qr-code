<#
.SYNOPSIS
  Calls the running system through the gateway and checks login, refresh, RBAC, and user management.
#>
[CmdletBinding()]
param([string]$BaseUrl = 'http://localhost:5000')

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$settings = @{}
foreach ($line in (Get-Content (Join-Path $root '.env'))) {
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$') {
        $settings[$Matches[1]] = $Matches[2]
    }
}
$adminUser = if ($settings['SEED_ADMIN_USERNAME']) { $settings['SEED_ADMIN_USERNAME'] } else { 'admin' }
$adminPassword = $settings['SEED_ADMIN_PASSWORD']

$script:failures = 0

function Invoke-Api {
    param([string]$Method, [string]$Path, $Body = $null, [string]$Token = $null, [string]$Cookie = $null)
    $headers = @{}
    if ($Token) { $headers['Authorization'] = "Bearer $Token" }
    $request = @{
        Uri = "$BaseUrl$Path"; Method = $Method; Headers = $headers
        UseBasicParsing = $true; ContentType = 'application/json'
    }
    if ($Cookie -match '^([^=]+)=(.*)$') {
        # A cookie sent through a web session works in Windows PowerShell and PowerShell 7 alike.
        $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
        $session.Cookies.Add((New-Object System.Net.Cookie($Matches[1], $Matches[2], '/', ([uri]$BaseUrl).Host)))
        $request['WebSession'] = $session
    }
    if ($null -ne $Body) { $request['Body'] = ($Body | ConvertTo-Json -Compress) }
    try {
        $response = Invoke-WebRequest @request
        return @{ Status = [int]$response.StatusCode; Json = ($response.Content | ConvertFrom-Json); Raw = $response }
    }
    catch {
        $status = 0
        $json = $null
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $text = $reader.ReadToEnd()
                if ($text) { $json = $text | ConvertFrom-Json }
            }
            catch { }
        }
        return @{ Status = $status; Json = $json; Raw = $null }
    }
}

function Check {
    param([string]$Name, [bool]$Condition)
    if ($Condition) { Write-Host "  PASS  $Name" -ForegroundColor Green }
    else { Write-Host "  FAIL  $Name" -ForegroundColor Red; $script:failures++ }
}

Write-Host "Smoke test against $BaseUrl" -ForegroundColor Cyan

$r = Invoke-Api GET '/health'
Check 'gateway /health returns 200' ($r.Status -eq 200)

$r = Invoke-Api POST '/api/auth/login' @{ username = $adminUser; password = 'definitely-wrong-password' }
Check 'login with wrong password returns 401' ($r.Status -eq 401)

$r = Invoke-Api POST '/api/auth/login' @{ username = $adminUser; password = $adminPassword }
Check 'admin login returns 200' ($r.Status -eq 200)
if ($r.Status -ne 200) { Write-Host 'Cannot continue without an admin login.' -ForegroundColor Red; exit 1 }
$adminToken = $r.Json.data.accessToken
Check 'login response has an access token' (-not [string]::IsNullOrWhiteSpace($adminToken))
Check 'refresh token is not in the JSON body' ($r.Raw.Content -notmatch 'refreshToken')
$setCookie = ($r.Raw.Headers['Set-Cookie'] -join ';')
Check 'refresh token is set as an HttpOnly cookie' (($setCookie -match 'refresh_token=') -and ($setCookie -match '(?i)httponly'))
$cookieValue = if ($setCookie -match 'refresh_token=([^;]+)') { $Matches[1] } else { '' }

$r = Invoke-Api GET '/api/auth/users'
Check 'users without a token returns 401' ($r.Status -eq 401)

$r = Invoke-Api GET '/api/auth/users' -Token $adminToken
Check 'users with admin token returns 200' ($r.Status -eq 200)

$newUser = 'smoke' + (Get-Random -Minimum 10000 -Maximum 99999)
$r = Invoke-Api POST '/api/auth/register' @{ username = $newUser; password = 'Smoke-Test-Pass-1'; role = 'CASHIER' } -Token $adminToken
Check 'admin can register a CASHIER (201)' ($r.Status -eq 201)
$newUserId = $r.Json.data.id

$r = Invoke-Api POST '/api/auth/register' @{ username = $newUser; password = 'Smoke-Test-Pass-1'; role = 'CASHIER' } -Token $adminToken
Check 'registering the same username returns 409' ($r.Status -eq 409)

$r = Invoke-Api POST '/api/auth/register' @{ username = 'x'; password = 'short'; role = 'GUEST' } -Token $adminToken
Check 'invalid register body returns 400' ($r.Status -eq 400)

$r = Invoke-Api POST '/api/auth/login' @{ username = $newUser; password = 'Smoke-Test-Pass-1' }
Check 'cashier login returns 200' ($r.Status -eq 200)
$cashierToken = $r.Json.data.accessToken

$r = Invoke-Api GET '/api/auth/users' -Token $cashierToken
Check 'cashier cannot list users (403)' ($r.Status -eq 403)

$r = Invoke-Api PUT "/api/auth/users/$newUserId/role" @{ role = 'MANAGER' } -Token $adminToken
Check 'admin can change a role (200)' ($r.Status -eq 200)

$adminId = $null
$list = Invoke-Api GET '/api/auth/users?pageSize=100' -Token $adminToken
foreach ($u in $list.Json.data.items) { if ($u.username -eq $adminUser) { $adminId = $u.id } }
$r = Invoke-Api PUT "/api/auth/users/$adminId/role" @{ role = 'CASHIER' } -Token $adminToken
Check 'admin cannot change their own role (403)' ($r.Status -eq 403)

$r = Invoke-Api POST '/api/auth/refresh' -Cookie "refresh_token=$cookieValue"
Check 'refresh with the cookie returns 200' ($r.Status -eq 200)

$r = Invoke-Api POST '/api/auth/refresh'
Check 'refresh without a cookie returns 401' ($r.Status -eq 401)

$r = Invoke-Api GET '/api/catalog/products'
Check 'catalog route reaches the gateway (200 once Catalog runs, 502 if it is not running)' (@(200, 502, 503) -contains $r.Status)

Write-Host ''
if ($script:failures -eq 0) { Write-Host 'All checks passed.' -ForegroundColor Green; exit 0 }
Write-Host "$script:failures check(s) failed." -ForegroundColor Red
exit 1

<#
.SYNOPSIS
  Checks the web menu (port 3000) against the running backend: server-rendered menu, riel prices, language,
  and that the gateway allows the browser calls the menu makes (CORS). Run it after run-all.ps1.
  To click through the menu yourself, open http://localhost:3000/?table=1
#>
[CmdletBinding()]
param(
    [string]$WebUrl = 'http://localhost:3000',
    [string]$ApiUrl = 'http://localhost:5000'
)

$ErrorActionPreference = 'Stop'
$script:failures = 0

function Check {
    param([string]$Name, [bool]$Condition)
    if ($Condition) { Write-Host "  PASS  $Name" -ForegroundColor Green }
    else { Write-Host "  FAIL  $Name" -ForegroundColor Red; $script:failures++ }
}

function Get-Page {
    param([string]$Url, [hashtable]$Headers = @{}, [string]$LangCookie = '')
    try {
        $request = @{ Uri = $Url; Headers = $Headers; UseBasicParsing = $true }
        if ($LangCookie) {
            # A cookie sent through a web session works in Windows PowerShell and PowerShell 7 alike.
            $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
            $session.Cookies.Add((New-Object System.Net.Cookie('lang', $LangCookie, '/', ([uri]$Url).Host)))
            $request['WebSession'] = $session
        }
        $response = Invoke-WebRequest @request
        return @{ Status = [int]$response.StatusCode; Body = [string]$response.Content; Headers = $response.Headers }
    }
    catch {
        $status = 0
        if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
        return @{ Status = $status; Body = ''; Headers = @{} }
    }
}

function Test-Preflight {
    param([string]$Path)
    $origin = 'http://localhost:3000'
    try {
        $response = Invoke-WebRequest -Uri "$ApiUrl$Path" -Method Options -UseBasicParsing -Headers @{
            'Origin'                         = $origin
            'Access-Control-Request-Method'  = 'POST'
            'Access-Control-Request-Headers' = 'content-type'
        }
        return ([int]$response.StatusCode -lt 300) -and ($response.Headers['Access-Control-Allow-Origin'] -eq $origin)
    }
    catch { return $false }
}

Write-Host "Phase 3 smoke test: web $WebUrl, API $ApiUrl" -ForegroundColor Cyan

Write-Host 'Menu page' -ForegroundColor Cyan
$page = Get-Page "$WebUrl/?table=7"
Check 'the web menu answers on port 3000' ($page.Status -eq 200)
if ($page.Status -ne 200) { Write-Host 'Start it with .\scripts\run-all.ps1 and wait for "Ready".' -ForegroundColor Red; exit 1 }
Check 'the page is English by default' ($page.Body -match '<html lang="en"')
Check 'the menu is rendered on the server (Iced Latte is in the HTML)' ($page.Body -match 'Iced Latte')
Check 'prices appear in riel as well as dollars' (($page.Body -match '៛') -and ($page.Body -match '\$2\.50'))
Check 'security headers are set' (($page.Headers['X-Content-Type-Options'] -eq 'nosniff') -and ($page.Headers['X-Frame-Options'] -eq 'DENY'))

$khmer = Get-Page "$WebUrl/" -LangCookie 'km'
Check 'the Khmer cookie switches the page to Khmer' ($khmer.Body -match '<html lang="km"')
$phone = Get-Page "$WebUrl/" @{ 'Accept-Language' = 'km-KH,km;q=0.9,en;q=0.8' }
Check 'a phone set to Khmer gets Khmer on the first visit' ($phone.Body -match '<html lang="km"')

Write-Host 'Order page' -ForegroundColor Cyan
$bad = Get-Page "$WebUrl/order/not-a-real-id"
Check 'a made-up order id shows the not-found page' ($bad.Body -match '404')

Write-Host 'Browser calls reach the gateway (CORS)' -ForegroundColor Cyan
Check 'the checkout call from the web menu is allowed' (Test-Preflight '/api/orders/checkout')
Check 'the cart call from the web menu is allowed' (Test-Preflight '/api/orders/cart')

try {
    $products = (Invoke-RestMethod -Uri "$ApiUrl/api/catalog/products").data
    $tea = $products | Where-Object { $_.name -eq 'Lemon Tea' } | Select-Object -First 1
    $body = @{ tableNumber = 1; items = @(@{ productId = $tea.id; quantity = 1; modifierOptionIds = @() }) } | ConvertTo-Json -Depth 5 -Compress
    $created = (Invoke-RestMethod -Uri "$ApiUrl/api/orders/checkout" -Method Post -ContentType 'application/json' -Body $body).data
    $track = Invoke-WebRequest -Uri "$ApiUrl/api/orders/$($created.id)" -UseBasicParsing -Headers @{ Origin = 'http://localhost:3000' }
    Check 'the order page can read an order across origins' ($track.Headers['Access-Control-Allow-Origin'] -eq 'http://localhost:3000')
    $orderPage = Get-Page "$WebUrl/order/$($created.id)"
    Check 'the order page opens for a real order' ($orderPage.Status -eq 200)
}
catch {
    Check "a test order can be placed through the gateway ($($_.Exception.Message))" $false
}

Write-Host ''
Write-Host 'Not covered here: tapping through the menu in a browser. Open http://localhost:3000/?table=1 and order a drink.' -ForegroundColor Yellow
if ($script:failures -eq 0) { Write-Host 'All Phase 3 checks passed.' -ForegroundColor Green; exit 0 }
Write-Host "$script:failures check(s) failed." -ForegroundColor Red
exit 1

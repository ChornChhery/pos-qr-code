<#
.SYNOPSIS
  End-to-end check of Phase 2 through the gateway: menu, riel prices, cart math, checkout,
  order status rules, role checks, stock deduction, and the SignalR hub endpoints.
  Needs the sample menu and 10 tables, which Development mode creates on first start.
#>
[CmdletBinding()]
param([string]$BaseUrl = 'http://localhost:5000')

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$settings = @{}
foreach ($line in (Get-Content (Join-Path $root '.env'))) {
    if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$') { $settings[$Matches[1]] = $Matches[2] }
}
$adminUser = if ($settings['SEED_ADMIN_USERNAME']) { $settings['SEED_ADMIN_USERNAME'] } else { 'admin' }
$adminPassword = $settings['SEED_ADMIN_PASSWORD']
$rate = if ($settings['USD_TO_KHR_RATE']) { [decimal]$settings['USD_TO_KHR_RATE'] } else { [decimal]4000 }

$script:failures = 0

function Invoke-Api {
    param([string]$Method, [string]$Path, $Body = $null, [string]$Token = $null)
    $headers = @{}
    if ($Token) { $headers['Authorization'] = "Bearer $Token" }
    $request = @{ Uri = "$BaseUrl$Path"; Method = $Method; Headers = $headers; UseBasicParsing = $true; ContentType = 'application/json' }
    if ($null -ne $Body) { $request['Body'] = ($Body | ConvertTo-Json -Depth 8 -Compress) }
    try {
        $response = Invoke-WebRequest @request
        $json = $null
        if ($response.Content) { try { $json = $response.Content | ConvertFrom-Json } catch { } }
        return @{ Status = [int]$response.StatusCode; Json = $json }
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
        return @{ Status = $status; Json = $json }
    }
}

function Check {
    param([string]$Name, [bool]$Condition)
    if ($Condition) { Write-Host "  PASS  $Name" -ForegroundColor Green }
    else { Write-Host "  FAIL  $Name" -ForegroundColor Red; $script:failures++ }
}

function Wait-Until {
    param([scriptblock]$Condition, [int]$TimeoutSeconds = 45)
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (& $Condition) { return $true }
        Start-Sleep -Seconds 2
    }
    return $false
}

function Expected-Khr([decimal]$usd) {
    [long]([math]::Round($usd * $rate / 100, 0, [MidpointRounding]::AwayFromZero) * 100)
}

Write-Host "Phase 2 smoke test against $BaseUrl" -ForegroundColor Cyan

# ---- Sign-ins ----
$r = Invoke-Api POST '/api/auth/login' @{ username = $adminUser; password = $adminPassword }
if ($r.Status -ne 200) { Write-Host 'Admin login failed. Run scripts\smoke-test.ps1 first to see why.' -ForegroundColor Red; exit 1 }
$adminToken = $r.Json.data.accessToken

$suffix = Get-Random -Minimum 10000 -Maximum 99999
$kitchenUser = "kitchen$suffix"
$cashierUser = "cashier$suffix"
$pw = 'Smoke-Test-Pass-1'
$null = Invoke-Api POST '/api/auth/register' @{ username = $kitchenUser; password = $pw; role = 'KITCHEN' } -Token $adminToken
$null = Invoke-Api POST '/api/auth/register' @{ username = $cashierUser; password = $pw; role = 'CASHIER' } -Token $adminToken
$kitchenToken = (Invoke-Api POST '/api/auth/login' @{ username = $kitchenUser; password = $pw }).Json.data.accessToken
$cashierToken = (Invoke-Api POST '/api/auth/login' @{ username = $cashierUser; password = $pw }).Json.data.accessToken
Check 'kitchen and cashier test users can sign in' ($kitchenToken -and $cashierToken)

# ---- Catalog ----
Write-Host 'Catalog' -ForegroundColor Cyan
$r = Invoke-Api GET '/api/catalog/categories'
Check 'categories are public and the sample menu exists' (($r.Status -eq 200) -and ($r.Json.data.Count -ge 3))
$drinks = $r.Json.data | Where-Object { $_.name -eq 'Drinks' } | Select-Object -First 1

$r = Invoke-Api GET '/api/catalog/products'
Check 'products are public' ($r.Status -eq 200)
$latte = $r.Json.data | Where-Object { $_.name -eq 'Iced Latte' } | Select-Object -First 1
$tea = $r.Json.data | Where-Object { $_.name -eq 'Lemon Tea' } | Select-Object -First 1
$mango = $r.Json.data | Where-Object { $_.name -eq 'Mango Sticky Rice' } | Select-Object -First 1
if (-not ($latte -and $tea -and $mango)) { Write-Host 'Sample menu products not found. Start the services in Development mode on an empty database.' -ForegroundColor Red; exit 1 }
Check 'price is also returned in riel' ($latte.priceKhr -eq (Expected-Khr $latte.price))
Check 'public callers do not see stock counts' ($null -eq $tea.stockCount)

$r = Invoke-Api GET "/api/catalog/products/$($tea.id)" -Token $adminToken
Check 'admin sees the stock count' ($r.Status -eq 200 -and $null -ne $r.Json.data.stockCount)
$teaStockBefore = [int]$r.Json.data.stockCount

$newProduct = @{ categoryId = $drinks.id; name = "Smoke item $suffix"; description = 'test'; price = 1.25; isAvailable = $true; stockCount = $null; lowStockThreshold = 5; modifiers = @() }
$r = Invoke-Api POST '/api/catalog/products' $newProduct
Check 'creating a product without a token returns 401' ($r.Status -eq 401)
$r = Invoke-Api POST '/api/catalog/products' $newProduct -Token $cashierToken
Check 'a cashier cannot create a product (403)' ($r.Status -eq 403)
$r = Invoke-Api POST '/api/catalog/products' $newProduct -Token $adminToken
Check 'admin can create a product (201)' ($r.Status -eq 201)
$smokeId = $r.Json.data.id
$r = Invoke-Api POST '/api/catalog/products' (@{ categoryId = $drinks.id; name = ''; price = -5 }) -Token $adminToken
Check 'an invalid product returns 400' ($r.Status -eq 400)
$r = Invoke-Api PATCH "/api/catalog/products/$smokeId/sold-out" @{ isSoldOut = $true } -Token $adminToken
Check 'admin can mark a product sold out' ($r.Status -eq 200 -and $r.Json.data.isAvailable -eq $false)
$r = Invoke-Api GET '/api/catalog/products'
Check 'a sold-out product disappears from the public menu' (-not ($r.Json.data | Where-Object { $_.id -eq $smokeId }))

# ---- Cart (waits for the Order service to receive the menu over RabbitMQ) ----
Write-Host 'Cart and checkout' -ForegroundColor Cyan
$size = $latte.modifiers | Where-Object { $_.groupName -eq 'Size' }
$large = $size.options | Where-Object { $_.name -eq 'Large' }
$extras = $latte.modifiers | Where-Object { $_.groupName -eq 'Extras' }
$shot = $extras.options | Where-Object { $_.name -eq 'Extra shot' }

$cartBody = @{ items = @(@{ productId = $latte.id; quantity = 2; modifierOptionIds = @($large.id, $shot.id) }) }
$synced = Wait-Until { (Invoke-Api POST '/api/orders/cart' $cartBody).Status -eq 200 }
Check 'the Order service received the menu from Catalog' $synced
$r = Invoke-Api POST '/api/orders/cart' $cartBody
$expectedSubtotal = 2 * ($latte.price + $large.priceAdd + $shot.priceAdd)
Check "cart total is server-calculated ($expectedSubtotal USD)" ($r.Json.data.subtotal -eq $expectedSubtotal -and $r.Json.data.total -eq $expectedSubtotal)
Check 'cart has no tax and no discount' ($r.Json.data.tax -eq 0 -and $r.Json.data.discount -eq 0)
Check 'cart total is also shown in riel' ($r.Json.data.totalKhr -eq (Expected-Khr $expectedSubtotal))

$r = Invoke-Api POST '/api/orders/cart' @{ items = @(@{ productId = $latte.id; quantity = 1 }) }
Check 'a missing required option returns 400 with a message' ($r.Status -eq 400 -and $r.Json.errors.Count -ge 1)
$r = Invoke-Api POST '/api/orders/cart' @{ items = @(@{ productId = $mango.id; quantity = 99 }) }
Check 'asking for more than the stock returns 400' ($r.Status -eq 400)
$r = Invoke-Api POST '/api/orders/cart' @{ items = @(@{ productId = $smokeId; quantity = 1 }) }
Check 'a sold-out product cannot be priced' ($r.Status -eq 400)

$r = Invoke-Api GET '/api/orders/tables'
Check 'tables are public' ($r.Status -eq 200 -and $r.Json.data.Count -ge 1)
$r = Invoke-Api POST '/api/orders/checkout' @{ tableNumber = 9999; items = @(@{ productId = $tea.id; quantity = 1 }) }
Check 'checkout for an unknown table returns 404' ($r.Status -eq 404)

$r = Invoke-Api POST '/api/orders/checkout' @{ tableNumber = 1; notes = 'smoke test'; items = @(@{ productId = $tea.id; quantity = 2; notes = 'no ice' }) }
Check 'checkout creates an order (201)' ($r.Status -eq 201)
$orderId = $r.Json.data.id
Check 'the new order starts as PENDING with the right total' ($r.Json.data.status -eq 'PENDING' -and $r.Json.data.total -eq (2 * $tea.price))
Check 'the order id is a random UUID' ($orderId -match '^[0-9a-f]{8}-[0-9a-f]{4}-4')

$r = Invoke-Api GET "/api/orders/$orderId"
Check 'a guest can track the order by its id' ($r.Status -eq 200 -and $r.Json.data.items.Count -eq 1)
$r = Invoke-Api GET '/api/orders/tables'
Check 'the table is now OCCUPIED' (($r.Json.data | Where-Object { $_.tableNumber -eq 1 }).status -eq 'OCCUPIED')

# ---- Staff access and status rules ----
Write-Host 'Staff access and status rules' -ForegroundColor Cyan
$r = Invoke-Api GET '/api/orders'
Check 'listing orders without a token returns 401' ($r.Status -eq 401)
$r = Invoke-Api GET '/api/orders?status=PENDING' -Token $cashierToken
Check 'a cashier can list orders' ($r.Status -eq 200 -and ($r.Json.data.items | Where-Object { $_.id -eq $orderId }))

$r = Invoke-Api PATCH "/api/orders/$orderId/status" @{ status = 'COOKING' } -Token $cashierToken
Check 'a cashier cannot change order status (403)' ($r.Status -eq 403)
$r = Invoke-Api PATCH "/api/orders/$orderId/status" @{ status = 'COMPLETED' } -Token $kitchenToken
Check 'skipping from PENDING to COMPLETED is refused (409)' ($r.Status -eq 409)
$r = Invoke-Api PATCH "/api/orders/$orderId/status" @{ status = 'SHIPPED' } -Token $kitchenToken
Check 'an unknown status returns 400' ($r.Status -eq 400)
$r = Invoke-Api PATCH "/api/orders/$orderId/status" @{ status = 'COOKING' } -Token $kitchenToken
Check 'kitchen moves the order to COOKING' ($r.Status -eq 200 -and $r.Json.data.status -eq 'COOKING')
$r = Invoke-Api PATCH "/api/orders/$orderId/status" @{ status = 'READY' } -Token $kitchenToken
Check 'kitchen moves the order to READY' ($r.Status -eq 200 -and $r.Json.data.status -eq 'READY')
$r = Invoke-Api PATCH "/api/orders/$orderId/status" @{ status = 'COMPLETED' } -Token $kitchenToken
Check 'the order can be COMPLETED' ($r.Status -eq 200 -and $null -ne $r.Json.data.completedAt)
$r = Invoke-Api PATCH "/api/orders/$orderId/status" @{ status = 'COOKING' } -Token $kitchenToken
Check 'a completed order cannot go back (409)' ($r.Status -eq 409)
$r = Invoke-Api GET '/api/orders/tables'
Check 'the table is AVAILABLE again after its last order completes' (($r.Json.data | Where-Object { $_.tableNumber -eq 1 }).status -eq 'AVAILABLE')

# ---- Stock deduction through RabbitMQ ----
Write-Host 'Stock deduction (asynchronous)' -ForegroundColor Cyan
$deducted = Wait-Until { (Invoke-Api GET "/api/catalog/products/$($tea.id)" -Token $adminToken).Json.data.stockCount -eq ($teaStockBefore - 2) }
Check "Catalog deducted 2 from Lemon Tea ($teaStockBefore -> $($teaStockBefore - 2))" $deducted

# ---- SignalR hubs (negotiate only; open them from a browser or a client to see live events) ----
Write-Host 'SignalR hubs' -ForegroundColor Cyan
$r = Invoke-Api POST '/hubs/kitchen/negotiate?negotiateVersion=1'
Check 'the kitchen hub refuses anonymous callers (401)' ($r.Status -eq 401)
$r = Invoke-Api POST '/hubs/kitchen/negotiate?negotiateVersion=1' -Token $cashierToken
Check 'the kitchen hub refuses a cashier (403)' ($r.Status -eq 403)
$r = Invoke-Api POST '/hubs/kitchen/negotiate?negotiateVersion=1' -Token $kitchenToken
Check 'the kitchen hub accepts a kitchen user' ($r.Status -eq 200)
$r = Invoke-Api POST '/hubs/cashier/negotiate?negotiateVersion=1' -Token $cashierToken
Check 'the cashier hub accepts a cashier' ($r.Status -eq 200)

Write-Host ''
if ($script:failures -eq 0) { Write-Host 'All Phase 2 checks passed.' -ForegroundColor Green; exit 0 }
Write-Host "$script:failures check(s) failed." -ForegroundColor Red
exit 1

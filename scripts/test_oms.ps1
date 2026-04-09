# =============================================================
# test_oms.ps1 - OMS Integration End-to-End Test Script
# Usage: .\scripts\test_oms.ps1
# =============================================================

$BASE_URL    = "http://localhost:5080"
$CUSTOMER_ID = "00000000-0000-0000-0000-000000000001"
$PROVIDER    = "DEFAULT_OMS"

function Write-Header($text) {
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host " $text" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
}

function Write-Step($step, $text) {
    Write-Host ""
    Write-Host "[STEP $step] $text" -ForegroundColor Yellow
}

function Write-OK($text)   { Write-Host "  [OK]   $text" -ForegroundColor Green }
function Write-Fail($text) { Write-Host "  [FAIL] $text" -ForegroundColor Red }
function Write-Info($text) { Write-Host "  ...    $text" -ForegroundColor Gray }

# ── STEP 0: Health Check ─────────────────────────────────────
Write-Header "OMS Integration Test"

Write-Step "0" "Health Check: TMS API at $BASE_URL"
try {
    $health = Invoke-RestMethod -Uri "$BASE_URL/health" -Method GET -ErrorAction Stop
    $statusVal = if ($health.status) { $health.status } else { "OK" }
    Write-OK "TMS API is up: $statusVal"
} catch {
    Write-Fail "TMS API not responding. Run docker first!"
    Write-Info "docker compose -f docker-compose.oms-test.yml up --build -d"
    exit 1
}

# ── STEP 1: Get Syncs before ────────────────────────────────
Write-Step "1" "GET /api/integrations/oms/syncs (before webhook)"
try {
    $syncs = Invoke-RestMethod -Uri "$BASE_URL/api/integrations/oms/syncs" -Method GET
    Write-Info "Pending syncs count: $($syncs.items.Count)"
} catch {
    Write-Fail "Cannot reach syncs endpoint: $($_.Exception.Message)"
}

# ── STEP 2: Count orders before ─────────────────────────────
Write-Step "2" "GET /api/orders (before webhook)"
try {
    $ordersBefore = Invoke-RestMethod -Uri "$BASE_URL/api/orders/" -Method GET
    $countBefore = $ordersBefore.items.Count
    Write-Info "Orders before: $countBefore"
} catch {
    $countBefore = 0
    Write-Info "Could not fetch orders (may be empty): $($_.Exception.Message)"
}

# ── STEP 3: Send Webhook ─────────────────────────────────────
Write-Step "3" "POST /api/integrations/oms/webhook/$PROVIDER"

$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$externalRef = "OMS-TEST-$timestamp"

$webhookBody = @{
    order_id       = $externalRef
    orderId        = $externalRef
    customerId     = $CUSTOMER_ID
    externalRef    = $externalRef
    pickupAddress  = @{
        street      = "123 Thanon Phahon Yothin"
        subDistrict = "Chatuchak"
        district    = "Chatuchak"
        province    = "Bangkok"
        postalCode  = "10900"
    }
    dropoffAddress = @{
        street      = "456 Thanon Sukhumvit"
        subDistrict = "Khlong Toei"
        district    = "Khlong Toei"
        province    = "Bangkok"
        postalCode  = "10110"
    }
    items = @(
        @{ description = "Test Product A"; qty = 5; gross_weight_kg = 10.5 }
        @{ description = "Test Product B"; qty = 2; gross_weight_kg = 3.0 }
    )
}

$webhookJson = $webhookBody | ConvertTo-Json -Depth 5
Write-Info "Sending ExternalRef: $externalRef"

try {
    $webhookResult = Invoke-RestMethod `
        -Uri "$BASE_URL/api/integrations/oms/webhook/$PROVIDER" `
        -Method POST `
        -ContentType "application/json" `
        -Body $webhookJson

    $syncId = $webhookResult.syncId
    Write-OK "Webhook accepted! SyncId = $syncId"
    Write-Info "Status: $($webhookResult.status)"
    Write-Info "Message: $($webhookResult.message)"
} catch {
    Write-Fail "Webhook failed: $($_.Exception.Message)"
    exit 1
}

# ── STEP 4: Idempotency Test ─────────────────────────────────
Write-Step "4" "POST webhook again (idempotency test - same externalRef)"
try {
    $dupResult = Invoke-RestMethod `
        -Uri "$BASE_URL/api/integrations/oms/webhook/$PROVIDER" `
        -Method POST `
        -ContentType "application/json" `
        -Body $webhookJson

    $dupSyncId = $dupResult.syncId
    if ([string]::IsNullOrEmpty($dupSyncId) -or $dupSyncId -eq "00000000-0000-0000-0000-000000000000") {
        Write-OK "Idempotency OK - duplicate was ignored (syncId = empty/zero)"
    } else {
        Write-Info "Duplicate returned syncId: $dupSyncId (may be new sync)"
    }
} catch {
    Write-Info "Duplicate request error (expected if truly idempotent): $($_.Exception.Message)"
}

# ── STEP 5: Wait for Worker ──────────────────────────────────
Write-Step "5" "Waiting 12 seconds for ProcessOmsSyncWorker to run..."
Start-Sleep -Seconds 12

# ── STEP 6: Check Sync Status ────────────────────────────────
Write-Step "6" "GET /api/integrations/oms/syncs (check status after worker)"
try {
    $syncsAfter = Invoke-RestMethod -Uri "$BASE_URL/api/integrations/oms/syncs" -Method GET
    if ($syncsAfter.items.Count -gt 0) {
        foreach ($s in $syncsAfter.items) {
            $line = "  Sync [$($s.id)] Ref=$($s.externalOrderRef) Status=$($s.status) Retry=$($s.retryCount)"
            if ($s.status -eq "Succeeded") {
                Write-OK $line
            } else {
                Write-Info $line
            }
            if ($s.errorMessage) {
                Write-Fail "  ErrorMessage: $($s.errorMessage)"
            }
        }
    } else {
        Write-OK "No pending syncs (all may have succeeded already)"
    }
} catch {
    Write-Fail "Cannot fetch syncs: $($_.Exception.Message)"
}

# ── STEP 7: Check TMS Orders ─────────────────────────────────
Write-Step "7" "GET /api/orders (check new OMS order)"
try {
    $ordersAfter = Invoke-RestMethod -Uri "$BASE_URL/api/orders/" -Method GET
    $countAfter = $ordersAfter.items.Count
    Write-Info "Orders after: $countAfter (before: $countBefore)"

    $omsOrders = $ordersAfter.items | Where-Object { $_.notes -like "*OMS Import*" }
    if ($omsOrders) {
        $latest = $omsOrders | Select-Object -Last 1
        Write-OK "OMS Order found in TMS!"
        Write-Info "  OrderId     : $($latest.id)"
        Write-Info "  OrderNumber : $($latest.orderNumber)"
        Write-Info "  Status      : $($latest.status)"
        Write-Info "  Notes       : $($latest.notes)"
    } elseif ($countAfter -gt $countBefore) {
        Write-OK "New orders were created (count increased by $($countAfter - $countBefore))"
        $latest = $ordersAfter.items | Select-Object -Last 1
        Write-Info "  Latest order: $($latest.id) - $($latest.orderNumber)"
    } else {
        Write-Fail "No OMS Order found yet - Worker may still be processing"
        Write-Info "Check logs: docker logs tms_api --tail 100"
    }
} catch {
    Write-Fail "Cannot fetch orders: $($_.Exception.Message)"
}

# ── STEP 8: Test Retry Endpoint ──────────────────────────────
Write-Step "8" "POST /api/integrations/oms/syncs/{syncId}/retry"
if ($syncId -and $syncId -ne "00000000-0000-0000-0000-000000000000") {
    try {
        $retryResult = Invoke-RestMethod `
            -Uri "$BASE_URL/api/integrations/oms/syncs/$syncId/retry" `
            -Method POST
        Write-OK "Retry endpoint: $($retryResult.message)"
    } catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 404) {
            Write-Info "404 - Sync not found (expected if already succeeded)"
        } else {
            Write-Info "Retry response: $($_.Exception.Message)"
        }
    }
} else {
    Write-Info "Skipping retry test (no valid syncId)"
}

# ── Summary ──────────────────────────────────────────────────
Write-Header "Test Complete - Access Points"
Write-Host "  TMS API     : $BASE_URL" -ForegroundColor White
Write-Host "  Swagger UI  : $BASE_URL/swagger" -ForegroundColor White
Write-Host "  OMS Mock    : http://localhost:9999" -ForegroundColor White
Write-Host "  RabbitMQ UI : http://localhost:15672  (tms/tms_dev)" -ForegroundColor White
Write-Host "  PostgreSQL  : localhost:5434 (tms_dev / tms_admin / tms_dev_password)" -ForegroundColor White
Write-Host ""
Write-Host "[Useful commands]" -ForegroundColor Magenta
Write-Host "  docker logs tms_api -f" -ForegroundColor DarkGray
Write-Host "  docker logs oms_mock -f   (see OMS callback)" -ForegroundColor DarkGray
Write-Host ""

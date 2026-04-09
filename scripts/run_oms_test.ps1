# =============================================================
# run_oms_test.ps1 — OMS Integration Full Test (One-Click)
# Usage: .\scripts\run_oms_test.ps1
#
# สิ่งที่ script นี้ทำ:
#   1. Build & Start Docker containers (TMS API + infra + OMS mock)
#   2. รัน EF Core Migrations ทุก module
#   3. Seed OmsFieldMappings ลงใน DB
#   4. ทดสอบ OMS Integration end-to-end
# =============================================================

param(
    [switch]$SkipBuild,    # ข้าม docker build (ใช้ image ที่มีอยู่)
    [switch]$SkipMigrate,  # ข้าม migration (ถ้า DB พร้อมแล้ว)
    [switch]$SkipTest,     # แค่ start services ไม่รัน test
    [switch]$Down          # หยุด services ทั้งหมด
)

$Root          = (Resolve-Path "$PSScriptRoot\..").Path
$ComposeFile   = "$Root\docker-compose.oms-test.yml"
$BASE_URL      = "http://localhost:5080"
$PG_CONTAINER  = "tms_postgres"
$PG_USER       = "tms_admin"
$PG_DB         = "tms_dev"

function Write-Header($text) {
    Write-Host "`n============================================" -ForegroundColor Cyan
    Write-Host " $text" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
}

function Write-Step($step, $text) {
    Write-Host "`n[STEP $step] $text" -ForegroundColor Yellow
}

function Write-OK($text)   { Write-Host "  [OK]   $text" -ForegroundColor Green }
function Write-Fail($text) { Write-Host "  [FAIL] $text" -ForegroundColor Red }
function Write-Info($text) { Write-Host "  ...    $text" -ForegroundColor Gray }

# ── Down mode ────────────────────────────────────────────────────
if ($Down) {
    Write-Header "Stopping OMS Test Environment"
    docker compose -f $ComposeFile down -v
    Write-OK "All containers stopped."
    exit 0
}

Write-Header "OMS Integration — Full Test Runner"
Write-Info "Root: $Root"
Write-Info "Compose: $ComposeFile"

# ── STEP 1: Start Docker ─────────────────────────────────────────
Write-Step "1" "Starting Docker services"

if ($SkipBuild) {
    docker compose -f $ComposeFile up -d
} else {
    docker compose -f $ComposeFile up --build -d
}

if ($LASTEXITCODE -ne 0) {
    Write-Fail "docker compose failed. ตรวจสอบ Docker Desktop ว่าเปิดอยู่หรือเปล่า"
    exit 1
}
Write-OK "Docker services started"

# ── รอ PostgreSQL พร้อม ──────────────────────────────────────────
Write-Step "1b" "Waiting for PostgreSQL to be ready..."
$maxWait = 60; $waited = 0
while ($waited -lt $maxWait) {
    $pgReady = docker exec $PG_CONTAINER pg_isready -U $PG_USER -d $PG_DB 2>&1
    if ($pgReady -match "accepting connections") { break }
    Start-Sleep -Seconds 3
    $waited += 3
    Write-Info "รอ PostgreSQL... ($waited/$maxWait วิ)"
}
if ($waited -ge $maxWait) {
    Write-Fail "PostgreSQL ไม่พร้อมภายใน $maxWait วิ"
    exit 1
}
Write-OK "PostgreSQL is ready"

# ── รอ TMS API พร้อม ────────────────────────────────────────────
Write-Step "1c" "Waiting for TMS API to be ready..."
$maxWait = 120; $waited = 0
while ($waited -lt $maxWait) {
    try {
        $r = Invoke-WebRequest -Uri "$BASE_URL/health" -Method GET -TimeoutSec 5 -ErrorAction Stop
        if ($r.StatusCode -lt 400) { break }
    } catch { }
    Start-Sleep -Seconds 5
    $waited += 5
    Write-Info "รอ TMS API... ($waited/$maxWait วิ)"
}
if ($waited -ge $maxWait) {
    Write-Fail "TMS API ไม่พร้อมภายใน $maxWait วิ"
    Write-Info "ดู logs: docker logs tms_api --tail 50"
    exit 1
}
Write-OK "TMS API is ready at $BASE_URL"

# ── STEP 2: Run Migrations ──────────────────────────────────────
if (-not $SkipMigrate) {
    Write-Step "2" "Running EF Core Migrations (all modules)"
    Push-Location $Root
    & "$Root\scripts\migrate-all.ps1" update
    Pop-Location

    if ($LASTEXITCODE -ne 0) {
        Write-Fail "Migration failed! ดู error ด้านบน"
        exit 1
    }
    Write-OK "All migrations applied"
} else {
    Write-Info "[SkipMigrate] ข้าม migration"
}

# ── STEP 3: Seed OmsFieldMappings ──────────────────────────────
Write-Step "3" "Seeding OmsFieldMappings for DEFAULT_OMS"

# รัน seed SQL ผ่าน psql ใน container
$seedSql = @"
DELETE FROM itg."OmsFieldMappings" WHERE "OmsProviderCode" = 'DEFAULT_OMS';

INSERT INTO itg."OmsFieldMappings"
    ("Id", "OmsProviderCode", "OmsField", "TmsField", "IsRequired", "TransformExpression", "UpdatedAt")
VALUES
    (gen_random_uuid(), 'DEFAULT_OMS', 'customerId',                 'customerId',                TRUE,  NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'externalRef',                'externalRef',               TRUE,  NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.street',       'pickupAddress.street',      FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.subDistrict',  'pickupAddress.subDistrict', FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.district',     'pickupAddress.district',    FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.province',     'pickupAddress.province',    FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.postalCode',   'pickupAddress.postalCode',  FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.street',      'dropoffAddress.street',     FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.subDistrict', 'dropoffAddress.subDistrict',FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.district',    'dropoffAddress.district',   FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.province',    'dropoffAddress.province',   FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.postalCode',  'dropoffAddress.postalCode', FALSE, NULL, NOW());

SELECT COUNT(*) AS "MappingCount", "OmsProviderCode" FROM itg."OmsFieldMappings" GROUP BY "OmsProviderCode";
"@

$seedSql | docker exec -i $PG_CONTAINER psql -U $PG_USER -d $PG_DB

if ($LASTEXITCODE -ne 0) {
    Write-Fail "Seed failed! Schema อาจยังไม่ถูก migrate"
    exit 1
}
Write-OK "OmsFieldMappings seeded for DEFAULT_OMS (12 rows)"

# ── STEP 4: Run Test ────────────────────────────────────────────
if (-not $SkipTest) {
    Write-Step "4" "Running OMS End-to-End Tests"
    & "$Root\scripts\test_oms.ps1"
}

# ── Final Summary ───────────────────────────────────────────────
Write-Header "Environment Ready"
Write-Host ""
Write-Host "  TMS API     : $BASE_URL" -ForegroundColor White
Write-Host "  Swagger UI  : $BASE_URL/swagger" -ForegroundColor White
Write-Host "  OMS Mock    : http://localhost:9999" -ForegroundColor White
Write-Host "  RabbitMQ UI : http://localhost:15672  (tms/tms_dev)" -ForegroundColor White
Write-Host "  PostgreSQL  : localhost:5434  ($PG_DB / $PG_USER / tms_dev_password)" -ForegroundColor White
Write-Host ""
Write-Host "  คำสั่งที่มีประโยชน์:" -ForegroundColor Magenta
Write-Host "  docker logs tms_api -f" -ForegroundColor DarkGray
Write-Host "  docker logs oms_mock -f" -ForegroundColor DarkGray
Write-Host "  .\scripts\run_oms_test.ps1 -SkipBuild -SkipMigrate  (re-run test เร็ว)" -ForegroundColor DarkGray
Write-Host "  .\scripts\run_oms_test.ps1 -Down  (หยุด services ทั้งหมด)" -ForegroundColor DarkGray
Write-Host ""

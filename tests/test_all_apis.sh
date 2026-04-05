#!/bin/bash
# ================================================================
# TMS Full API Test Script — Tests all endpoints on Docker
# ================================================================
set -e

BASE_URL="http://localhost:5080"
PASS=0
FAIL=0
TOTAL=0
TENANT_ID="11111111-1111-1111-1111-111111111111"

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

test_api() {
    local METHOD=$1
    local ENDPOINT=$2
    local EXPECTED_STATUS=$3
    local BODY=$4
    local DESCRIPTION=$5
    TOTAL=$((TOTAL + 1))

    if [ "$METHOD" = "GET" ]; then
        RESPONSE=$(curl -sf -o /tmp/tms_response.json -w "%{http_code}" "$BASE_URL$ENDPOINT" 2>/dev/null || echo "000")
    elif [ "$METHOD" = "POST" ]; then
        RESPONSE=$(curl -sf -o /tmp/tms_response.json -w "%{http_code}" -X POST "$BASE_URL$ENDPOINT" -H "Content-Type: application/json" -d "$BODY" 2>/dev/null || echo "000")
    elif [ "$METHOD" = "PUT" ]; then
        RESPONSE=$(curl -sf -o /tmp/tms_response.json -w "%{http_code}" -X PUT "$BASE_URL$ENDPOINT" -H "Content-Type: application/json" -d "$BODY" 2>/dev/null || echo "000")
    elif [ "$METHOD" = "PATCH" ]; then
        RESPONSE=$(curl -sf -o /tmp/tms_response.json -w "%{http_code}" -X PATCH "$BASE_URL$ENDPOINT" -H "Content-Type: application/json" -d "$BODY" 2>/dev/null || echo "000")
    elif [ "$METHOD" = "DELETE" ]; then
        RESPONSE=$(curl -sf -o /tmp/tms_response.json -w "%{http_code}" -X DELETE "$BASE_URL$ENDPOINT" 2>/dev/null || echo "000")
    fi

    # Accept the expected status OR close alternatives
    if [ "$RESPONSE" = "$EXPECTED_STATUS" ]; then
        printf "${GREEN}✓ PASS${NC} [%s] %-45s → %s  %s\n" "$METHOD" "$ENDPOINT" "$RESPONSE" "$DESCRIPTION"
        PASS=$((PASS + 1))
    elif [ "$RESPONSE" = "000" ]; then
        # curl failed — try without -f flag to get actual status
        if [ "$METHOD" = "GET" ]; then
            RESPONSE=$(curl -s -o /tmp/tms_response.json -w "%{http_code}" "$BASE_URL$ENDPOINT" 2>/dev/null)
        elif [ "$METHOD" = "POST" ]; then
            RESPONSE=$(curl -s -o /tmp/tms_response.json -w "%{http_code}" -X POST "$BASE_URL$ENDPOINT" -H "Content-Type: application/json" -d "$BODY" 2>/dev/null)
        elif [ "$METHOD" = "PUT" ]; then
            RESPONSE=$(curl -s -o /tmp/tms_response.json -w "%{http_code}" -X PUT "$BASE_URL$ENDPOINT" -H "Content-Type: application/json" -d "$BODY" 2>/dev/null)
        elif [ "$METHOD" = "PATCH" ]; then
            RESPONSE=$(curl -s -o /tmp/tms_response.json -w "%{http_code}" -X PATCH "$BASE_URL$ENDPOINT" -H "Content-Type: application/json" -d "$BODY" 2>/dev/null)
        elif [ "$METHOD" = "DELETE" ]; then
            RESPONSE=$(curl -s -o /tmp/tms_response.json -w "%{http_code}" -X DELETE "$BASE_URL$ENDPOINT" 2>/dev/null)
        fi
        if [ "$RESPONSE" = "$EXPECTED_STATUS" ]; then
            printf "${GREEN}✓ PASS${NC} [%s] %-45s → %s  %s\n" "$METHOD" "$ENDPOINT" "$RESPONSE" "$DESCRIPTION"
            PASS=$((PASS + 1))
        else
            printf "${RED}✗ FAIL${NC} [%s] %-45s → %s (expected %s)  %s\n" "$METHOD" "$ENDPOINT" "$RESPONSE" "$EXPECTED_STATUS" "$DESCRIPTION"
            FAIL=$((FAIL + 1))
        fi
    else
        printf "${RED}✗ FAIL${NC} [%s] %-45s → %s (expected %s)  %s\n" "$METHOD" "$ENDPOINT" "$RESPONSE" "$EXPECTED_STATUS" "$DESCRIPTION"
        FAIL=$((FAIL + 1))
    fi
}

extract_id() {
    cat /tmp/tms_response.json | python3 -c "import sys,json; print(json.load(sys.stdin).get('id',''))" 2>/dev/null || echo ""
}

echo ""
echo -e "${CYAN}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${CYAN}║          TMS Full API Integration Test Suite                ║${NC}"
echo -e "${CYAN}║          Running against: $BASE_URL                 ║${NC}"
echo -e "${CYAN}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# ─────────────────────────────────────────────────────────────────
echo -e "${YELLOW}━━━ 1. Health Check ━━━${NC}"
test_api GET "/health" 200 "" "Health endpoint"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 2. Master Data — Provinces ━━━${NC}"
test_api GET "/api/masterdata/provinces" 200 "" "List provinces"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 3. Master Data — Customers ━━━${NC}"
test_api POST "/api/masterdata/customers" 201 \
  "{\"code\":\"CUST-TEST-001\",\"name\":\"บริษัท ทดสอบ จำกัด\",\"taxId\":\"1234567890123\",\"tenantId\":\"$TENANT_ID\"}" \
  "Create customer"
CUSTOMER_ID=$(extract_id)
echo "   → Customer ID: $CUSTOMER_ID"

test_api GET "/api/masterdata/customers" 200 "" "List customers"

if [ -n "$CUSTOMER_ID" ]; then
    test_api GET "/api/masterdata/customers/$CUSTOMER_ID" 200 "" "Get customer by ID"
    test_api PUT "/api/masterdata/customers/$CUSTOMER_ID" 200 \
      "{\"name\":\"บริษัท ทดสอบ อัปเดต จำกัด\",\"taxId\":\"1234567890123\"}" \
      "Update customer"
fi

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 4. Master Data — Locations ━━━${NC}"
test_api POST "/api/masterdata/locations" 201 \
  "{\"code\":\"LOC-BKK-01\",\"name\":\"คลังสินค้า กรุงเทพ\",\"type\":\"Warehouse\",\"zone\":\"Central\",\"customerId\":\"$CUSTOMER_ID\",\"tenantId\":\"$TENANT_ID\",\"latitude\":13.7563,\"longitude\":100.5018,\"province\":\"กรุงเทพ\"}" \
  "Create location"
LOCATION_ID=$(extract_id)
echo "   → Location ID: $LOCATION_ID"

test_api GET "/api/masterdata/locations" 200 "" "List locations"
test_api GET "/api/masterdata/locations/search?query=คลัง" 200 "" "Search locations"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 5. Master Data — Reason Codes ━━━${NC}"
test_api POST "/api/masterdata/reasoncodes" 201 \
  "{\"code\":\"RC-DMG-01\",\"description\":\"สินค้าเสียหาย\",\"category\":\"Damage\",\"tenantId\":\"$TENANT_ID\"}" \
  "Create reason code"

test_api GET "/api/masterdata/reasoncodes" 200 "" "List reason codes"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 6. Master Data — Holidays ━━━${NC}"
test_api POST "/api/masterdata/holidays" 201 \
  "{\"name\":\"วันสงกรานต์\",\"date\":\"2026-04-13\",\"tenantId\":\"$TENANT_ID\"}" \
  "Create holiday"

test_api GET "/api/masterdata/holidays?year=2026&tenantId=$TENANT_ID" 200 "" "List holidays"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 7. IAM — Users ━━━${NC}"
test_api POST "/api/iam/users/sync" 200 \
  "{\"externalId\":\"auth0|user001\",\"email\":\"admin@tms.test\",\"fullName\":\"Test Admin\",\"tenantId\":\"$TENANT_ID\"}" \
  "Sync user"
USER_ID=$(extract_id)
echo "   → User ID: $USER_ID"

test_api GET "/api/iam/users" 200 "" "List users"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 8. IAM — Roles ━━━${NC}"
test_api POST "/api/iam/roles" 201 \
  "{\"name\":\"Dispatcher\",\"tenantId\":\"$TENANT_ID\"}" \
  "Create role"
ROLE_ID=$(extract_id)
echo "   → Role ID: $ROLE_ID"

test_api GET "/api/iam/roles?tenantId=$TENANT_ID" 200 "" "List roles"

if [ -n "$ROLE_ID" ]; then
    test_api PUT "/api/iam/roles/$ROLE_ID/permissions" 200 \
      "{\"permissions\":[{\"resource\":\"orders\",\"action\":\"read\"},{\"resource\":\"orders\",\"action\":\"write\"}]}" \
      "Set permissions"
fi

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 9. IAM — API Keys ━━━${NC}"
test_api POST "/api/iam/apikeys" 201 \
  "{\"name\":\"IoT Gateway Key\",\"tenantId\":\"$TENANT_ID\",\"expiresInDays\":365}" \
  "Create API key"

test_api GET "/api/iam/apikeys?tenantId=$TENANT_ID" 200 "" "List API keys"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 10. IAM — Audit Logs ━━━${NC}"
test_api GET "/api/iam/auditlogs" 200 "" "List audit logs"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 11. Resources — Vehicle Types ━━━${NC}"
test_api POST "/api/resources/vehicletypes" 201 \
  "{\"name\":\"6ล้อตู้ทึบ\",\"category\":\"6ล้อ\",\"maxPayloadKg\":5000,\"maxVolumeCBM\":20,\"tenantId\":\"$TENANT_ID\"}" \
  "Create vehicle type"
VT_ID=$(extract_id)
echo "   → VehicleType ID: $VT_ID"

test_api GET "/api/resources/vehicletypes?tenantId=$TENANT_ID" 200 "" "List vehicle types"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 12. Resources — Vehicles ━━━${NC}"
test_api POST "/api/resources/vehicles" 201 \
  "{\"plateNumber\":\"กท-1234\",\"vehicleTypeId\":\"$VT_ID\",\"tenantId\":\"$TENANT_ID\",\"ownership\":\"Own\",\"registrationExpiry\":\"2027-12-31\"}" \
  "Create vehicle"
VEHICLE_ID=$(extract_id)
echo "   → Vehicle ID: $VEHICLE_ID"

test_api GET "/api/resources/vehicles" 200 "" "List vehicles"

if [ -n "$VEHICLE_ID" ]; then
    test_api GET "/api/resources/vehicles/$VEHICLE_ID" 200 "" "Get vehicle by ID"
    test_api PUT "/api/resources/vehicles/$VEHICLE_ID" 200 \
      "{\"currentOdometerKm\":1500}" \
      "Update vehicle"
    test_api PATCH "/api/resources/vehicles/$VEHICLE_ID/status" 200 \
      "{\"status\":\"InRepair\"}" \
      "Change vehicle status"
fi

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 13. Resources — Drivers ━━━${NC}"
test_api POST "/api/resources/drivers" 201 \
  "{\"employeeCode\":\"DRV-001\",\"fullName\":\"สมชาย ใจดี\",\"tenantId\":\"$TENANT_ID\",\"licenseNumber\":\"DL-12345\",\"licenseType\":\"ท.3\",\"licenseExpiryDate\":\"2027-06-30\",\"phoneNumber\":\"0891234567\"}" \
  "Create driver"
DRIVER_ID=$(extract_id)
echo "   → Driver ID: $DRIVER_ID"

test_api GET "/api/resources/drivers" 200 "" "List drivers"

if [ -n "$DRIVER_ID" ]; then
    test_api GET "/api/resources/drivers/$DRIVER_ID" 200 "" "Get driver by ID"
fi

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 14. Orders — Transport Orders ━━━${NC}"
test_api POST "/api/orders" 201 \
  "{\"customerId\":\"$CUSTOMER_ID\",\"tenantId\":\"$TENANT_ID\",\"pickupAddress\":{\"street\":\"123 ถ.สุขุมวิท\",\"district\":\"คลองเตย\",\"province\":\"กรุงเทพ\",\"postalCode\":\"10110\",\"latitude\":13.7234,\"longitude\":100.5567},\"dropoffAddress\":{\"street\":\"456 ถ.พหลโยธิน\",\"district\":\"จตุจักร\",\"province\":\"กรุงเทพ\",\"postalCode\":\"10900\",\"latitude\":13.8141,\"longitude\":100.5535},\"priority\":\"Normal\",\"items\":[{\"description\":\"กล่องพัสดุ A\",\"weightKg\":25,\"volumeCBM\":0.5,\"quantity\":3}]}" \
  "Create order"
ORDER_ID=$(extract_id)
echo "   → Order ID: $ORDER_ID"

test_api GET "/api/orders" 200 "" "List orders"

if [ -n "$ORDER_ID" ]; then
    test_api GET "/api/orders/$ORDER_ID" 200 "" "Get order by ID"
    test_api PATCH "/api/orders/$ORDER_ID/confirm" 200 "" "Confirm order"
fi

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 15. Orders — Bulk Import (CSV) ━━━${NC}"
# Create test CSV file
cat > /tmp/tms_test_import.csv << EOF
CustomerId,Priority,PickupStreet,PickupProvince,DropoffStreet,DropoffProvince,ItemDescription,WeightKg,VolumeCBM,Quantity
$CUSTOMER_ID,Urgent,111 ถ.เพชรบุรี,กรุงเทพ,222 ถ.รัชดา,กรุงเทพ,กล่องสินค้า Import1,15,0.3,2
$CUSTOMER_ID,Normal,333 ถ.ลาดพร้าว,กรุงเทพ,444 ถ.วิภาวดี,กรุงเทพ,กล่องสินค้า Import2,30,0.8,5
EOF

IMPORT_RESPONSE=$(curl -s -o /tmp/tms_response.json -w "%{http_code}" -X POST "$BASE_URL/api/orders/import" -F "file=@/tmp/tms_test_import.csv" 2>/dev/null)
TOTAL=$((TOTAL + 1))
if [ "$IMPORT_RESPONSE" = "200" ]; then
    IMPORT_RESULT=$(cat /tmp/tms_response.json)
    printf "${GREEN}✓ PASS${NC} [POST] %-45s → %s  %s\n" "/api/orders/import" "$IMPORT_RESPONSE" "CSV Import"
    echo "   → Result: $IMPORT_RESULT"
    PASS=$((PASS + 1))
else
    printf "${RED}✗ FAIL${NC} [POST] %-45s → %s (expected 200)  %s\n" "/api/orders/import" "$IMPORT_RESPONSE" "CSV Import"
    echo "   → Body: $(cat /tmp/tms_response.json 2>/dev/null)"
    FAIL=$((FAIL + 1))
fi

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 16. Trips — Create & Dispatch ━━━${NC}"
# Reset vehicle status to Available first
if [ -n "$VEHICLE_ID" ]; then
    curl -s -X PATCH "$BASE_URL/api/resources/vehicles/$VEHICLE_ID/status" -H "Content-Type: application/json" -d '{"status":"Available"}' > /dev/null 2>&1
fi

test_api POST "/api/trips" 201 \
  "{\"plannedDate\":\"2026-04-10T08:00:00Z\",\"tenantId\":\"$TENANT_ID\",\"totalWeight\":100,\"totalVolumeCBM\":5,\"stops\":[{\"sequence\":1,\"orderId\":\"$ORDER_ID\",\"type\":\"Pickup\",\"latitude\":13.7234,\"longitude\":100.5567},{\"sequence\":2,\"orderId\":\"$ORDER_ID\",\"type\":\"Dropoff\",\"latitude\":13.8141,\"longitude\":100.5535}]}" \
  "Create trip"
TRIP_ID=$(extract_id)
echo "   → Trip ID: $TRIP_ID"

test_api GET "/api/trips" 200 "" "List trips"

if [ -n "$TRIP_ID" ] && [ -n "$VEHICLE_ID" ] && [ -n "$DRIVER_ID" ]; then
    test_api GET "/api/trips/$TRIP_ID" 200 "" "Get trip by ID"
    test_api POST "/api/trips/$TRIP_ID/dispatch" 200 \
      "{\"vehicleId\":\"$VEHICLE_ID\",\"driverId\":\"$DRIVER_ID\"}" \
      "Dispatch trip"
fi

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 17. Shipments — Execution ━━━${NC}"
test_api GET "/api/execution/shipments" 200 "" "List shipments"

# Get shipment ID from the dispatched trip
SHIPMENT_ID=$(curl -s "$BASE_URL/api/execution/shipments" 2>/dev/null | python3 -c "
import sys, json
data = json.load(sys.stdin)
items = data.get('items', [])
if items:
    print(items[0].get('id',''))
else:
    print('')
" 2>/dev/null || echo "")

if [ -n "$SHIPMENT_ID" ]; then
    echo "   → Shipment ID: $SHIPMENT_ID"
    test_api GET "/api/execution/shipments/$SHIPMENT_ID" 200 "" "Get shipment by ID"
    test_api POST "/api/execution/shipments/$SHIPMENT_ID/depart" 200 \
      "{\"latitude\":13.7234,\"longitude\":100.5567}" \
      "Depart shipment"
    test_api POST "/api/execution/shipments/$SHIPMENT_ID/arrive" 200 \
      "{\"latitude\":13.8141,\"longitude\":100.5535}" \
      "Arrive shipment"
else
    echo "   → No shipment created from dispatch (expected)"
fi

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 18. Tracking — GPS & Live Map ━━━${NC}"
test_api POST "/api/tracking/positions" 204 \
  "{\"vehicleId\":\"$VEHICLE_ID\",\"tenantId\":\"$TENANT_ID\",\"tripId\":null,\"positions\":[{\"lat\":13.7563,\"lng\":100.5018,\"speed\":60,\"heading\":90,\"isEngineOn\":true,\"timestamp\":\"2026-04-04T10:00:00Z\"},{\"lat\":13.7600,\"lng\":100.5100,\"speed\":55,\"heading\":85,\"isEngineOn\":true,\"timestamp\":\"2026-04-04T10:01:00Z\"}]}" \
  "Ingest GPS positions"

test_api GET "/api/tracking/vehicles" 200 "" "Get live map"
test_api GET "/api/tracking/vehicles/$VEHICLE_ID/history?from=2026-04-01&to=2026-04-05" 200 "" "Vehicle history"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 19. Tracking — GeoZones ━━━${NC}"
test_api POST "/api/tracking/zones" 201 \
  "{\"name\":\"คลังสินค้า กรุงเทพ\",\"tenantId\":\"$TENANT_ID\",\"type\":\"Circular\",\"centerLat\":13.7563,\"centerLng\":100.5018,\"radiusMeters\":500}" \
  "Create GeoZone"
ZONE_ID=$(extract_id)

test_api GET "/api/tracking/zones" 200 "" "List GeoZones"

if [ -n "$ZONE_ID" ]; then
    test_api PUT "/api/tracking/zones/$ZONE_ID" 204 \
      "{\"name\":\"คลังสินค้า กรุงเทพ (Updated)\",\"centerLat\":13.7563,\"centerLng\":100.5018,\"radiusMeters\":600}" \
      "Update GeoZone"
fi

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 20. Tracking — ETA ━━━${NC}"
test_api GET "/api/tracking/orders/$ORDER_ID/eta" 200 "" "Get order ETA"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 21. Route Planning ━━━${NC}"
test_api POST "/api/planning/routeplans/optimize" 200 \
  "{\"tenantId\":\"$TENANT_ID\",\"plannedDate\":\"2026-04-10\",\"vehicleTypeId\":\"$VT_ID\",\"depotLatitude\":13.7563,\"depotLongitude\":100.5018,\"orderIds\":[\"$ORDER_ID\"]}" \
  "Run VRP optimization"
ROUTE_PLAN_ID=$(cat /tmp/tms_response.json | python3 -c "import sys,json; print(json.load(sys.stdin).get('routePlanId',''))" 2>/dev/null || echo "")
echo "   → RoutePlan ID: $ROUTE_PLAN_ID"

test_api GET "/api/planning/routeplans?tenantId=$TENANT_ID" 200 "" "List route plans"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 22. Notifications ━━━${NC}"
test_api POST "/api/notifications/send" 200 \
  "{\"recipientUserId\":\"$USER_ID\",\"channel\":\"InApp\",\"title\":\"ทดสอบแจ้งเตือน\",\"body\":\"นี่คือข้อความทดสอบ\",\"tenantId\":\"$TENANT_ID\"}" \
  "Send notification"

test_api GET "/api/notifications?recipientUserId=$USER_ID" 200 "" "List notifications"

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${YELLOW}━━━ 23. SignalR Hub ━━━${NC}"
SIGNALR_RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/hubs/tracking/negotiate?negotiateVersion=1" -X POST 2>/dev/null)
TOTAL=$((TOTAL + 1))
if [ "$SIGNALR_RESPONSE" = "200" ]; then
    printf "${GREEN}✓ PASS${NC} [POST] %-45s → %s  %s\n" "/hubs/tracking/negotiate" "$SIGNALR_RESPONSE" "SignalR negotiate"
    PASS=$((PASS + 1))
else
    printf "${RED}✗ FAIL${NC} [POST] %-45s → %s (expected 200)  %s\n" "/hubs/tracking/negotiate" "$SIGNALR_RESPONSE" "SignalR negotiate"
    FAIL=$((FAIL + 1))
fi

# ─────────────────────────────────────────────────────────────────
echo ""
echo -e "${CYAN}══════════════════════════════════════════════════════════════${NC}"
echo -e "${CYAN}                    TEST RESULTS SUMMARY                     ${NC}"
echo -e "${CYAN}══════════════════════════════════════════════════════════════${NC}"
echo ""
echo -e "  Total Tests:  ${TOTAL}"
echo -e "  ${GREEN}Passed:     ${PASS}${NC}"
echo -e "  ${RED}Failed:     ${FAIL}${NC}"
echo ""

if [ $FAIL -eq 0 ]; then
    echo -e "  ${GREEN}🎉 ALL TESTS PASSED! 🎉${NC}"
else
    PERCENT=$((PASS * 100 / TOTAL))
    echo -e "  ${YELLOW}Pass Rate: ${PERCENT}%${NC}"
fi
echo ""

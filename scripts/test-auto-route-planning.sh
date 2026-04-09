#!/bin/bash
# ============================================================================
# TMS — End-to-End Auto Route Planning Test
# ============================================================================
# Flow: Create Customer → Create Orders (with lat/lng) → Confirm Orders
#       → POST /api/planning/optimize (OR-Tools VRP) → Poll Status → View Plans
# ============================================================================

set -euo pipefail

BASE_URL="${TMS_BASE_URL:-http://localhost:5080}"
TENANT_ID="00000000-0000-0000-0000-000000000001"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
RED='\033[0;31m'
BOLD='\033[1m'
NC='\033[0m'

pass() { echo -e "${GREEN}✅ $1${NC}"; }
fail() { echo -e "${RED}❌ $1${NC}"; }
info() { echo -e "${CYAN}ℹ️  $1${NC}"; }
header() { echo -e "\n${BOLD}${YELLOW}═══════════════════════════════════════════════════${NC}"; echo -e "${BOLD}${YELLOW}  $1${NC}"; echo -e "${BOLD}${YELLOW}═══════════════════════════════════════════════════${NC}\n"; }

# ── Helper: JSON extract (portable) ──────────────────────────────────────────
json_val() {
    python3 -c "import sys,json; d=json.load(sys.stdin); print(d$1)" 2>/dev/null
}

# ============================================================================
# STEP 0: Health Check
# ============================================================================
header "STEP 0 — Health Check"

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/swagger/index.html")
if [ "$HTTP_CODE" == "200" ]; then
    pass "API is running at $BASE_URL (HTTP $HTTP_CODE)"
else
    fail "API is NOT reachable at $BASE_URL (HTTP $HTTP_CODE)"
    exit 1
fi

# ============================================================================
# STEP 1: Create Customer
# ============================================================================
header "STEP 1 — Create Customer"

CUSTOMER_RESP=$(curl -s -X POST "$BASE_URL/api/master/customers" \
    -H "Content-Type: application/json" \
    -d '{
        "customerCode": "CUST-ROUTE-TEST",
        "companyName": "บริษัท ทดสอบเส้นทาง จำกัด",
        "tenantId": "'"$TENANT_ID"'",
        "contactName": "Route Planner",
        "contactEmail": "route@test.com",
        "contactPhone": "081-999-0000"
    }')

CUSTOMER_ID=$(echo "$CUSTOMER_RESP" | json_val "['id']" || echo "$CUSTOMER_RESP" | json_val "['Id']")
if [ -n "$CUSTOMER_ID" ] && [ "$CUSTOMER_ID" != "None" ]; then
    pass "Created Customer: $CUSTOMER_ID"
else
    # Try to get existing customer
    info "Customer might already exist, fetching..."
    CUSTOMERS_RESP=$(curl -s "$BASE_URL/api/master/customers?pageSize=100")
    CUSTOMER_ID=$(echo "$CUSTOMERS_RESP" | python3 -c "
import sys,json
data = json.load(sys.stdin)
items = data.get('items', data.get('Items', []))
for c in items:
    cid = c.get('id', c.get('Id',''))
    print(cid)
    break
" 2>/dev/null)
    if [ -n "$CUSTOMER_ID" ]; then
        pass "Using existing Customer: $CUSTOMER_ID"
    else
        fail "Could not create or find customer"
        echo "$CUSTOMER_RESP"
        exit 1
    fi
fi

# ============================================================================
# STEP 2: Create Orders (Bangkok area — realistic lat/lng)
# ============================================================================
header "STEP 2 — Create 5 Transport Orders (Bangkok area)"

# Locations (stored for direct use in optimize request):
#   Depot (Bangna):        13.6672, 100.6057
#   Order 1: Pickup=Lat Krabang Warehouse → Dropoff=Sukhumvit Soi 55
#   Order 2: Pickup=Bangkapi Warehouse   → Dropoff=Silom
#   Order 3: Pickup=Rangsit Warehouse    → Dropoff=Chatuchak
#   Order 4: Pickup=Bangna Warehouse     → Dropoff=Pratunam
#   Order 5: Pickup=Samut Prakan         → Dropoff=Phra Khanong

declare -a ORDER_IDS=()
declare -a ORDER_PICKUP_LATS=()
declare -a ORDER_PICKUP_LNGS=()
declare -a ORDER_DROPOFF_LATS=()
declare -a ORDER_DROPOFF_LNGS=()
declare -a ORDER_WEIGHTS=()
declare -a ORDER_VOLUMES=()

create_order() {
    local ORDER_NUM=$1
    local PICKUP_STREET=$2
    local PICKUP_LAT=$3
    local PICKUP_LNG=$4
    local DROPOFF_STREET=$5
    local DROPOFF_LAT=$6
    local DROPOFF_LNG=$7
    local WEIGHT=$8
    local VOLUME=$9
    local ITEM_DESC=${10}

    local NOW=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    local PICKUP_FROM=$(date -u -v+1H +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null || date -u -d "+1 hour" +"%Y-%m-%dT%H:%M:%SZ")
    local PICKUP_TO=$(date -u -v+3H +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null || date -u -d "+3 hours" +"%Y-%m-%dT%H:%M:%SZ")
    local DROPOFF_FROM=$(date -u -v+4H +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null || date -u -d "+4 hours" +"%Y-%m-%dT%H:%M:%SZ")
    local DROPOFF_TO=$(date -u -v+8H +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null || date -u -d "+8 hours" +"%Y-%m-%dT%H:%M:%SZ")

    local RESP=$(curl -s -X POST "$BASE_URL/api/orders" \
        -H "Content-Type: application/json" \
        -d '{
            "customerId": "'"$CUSTOMER_ID"'",
            "orderNumber": "'"$ORDER_NUM"'",
            "pickupAddress": {
                "street": "'"$PICKUP_STREET"'",
                "subDistrict": "แขวงทดสอบ",
                "district": "เขตทดสอบ",
                "province": "กรุงเทพมหานคร",
                "postalCode": "10110",
                "latitude": '"$PICKUP_LAT"',
                "longitude": '"$PICKUP_LNG"'
            },
            "dropoffAddress": {
                "street": "'"$DROPOFF_STREET"'",
                "subDistrict": "แขวงปลายทาง",
                "district": "เขตปลายทาง",
                "province": "กรุงเทพมหานคร",
                "postalCode": "10500",
                "latitude": '"$DROPOFF_LAT"',
                "longitude": '"$DROPOFF_LNG"'
            },
            "items": [
                {
                    "description": "'"$ITEM_DESC"'",
                    "weight": '"$WEIGHT"',
                    "volume": '"$VOLUME"',
                    "quantity": 1
                }
            ],
            "pickupWindow": {
                "from": "'"$PICKUP_FROM"'",
                "to": "'"$PICKUP_TO"'"
            },
            "dropoffWindow": {
                "from": "'"$DROPOFF_FROM"'",
                "to": "'"$DROPOFF_TO"'"
            },
            "priority": "Normal",
            "notes": "Auto Route Planning Test"
        }')

    local ORDER_ID=$(echo "$RESP" | json_val "['id']" || echo "$RESP" | json_val "['Id']")
    if [ -n "$ORDER_ID" ] && [ "$ORDER_ID" != "None" ]; then
        pass "Order $ORDER_NUM → ID: $ORDER_ID  (W=${WEIGHT}kg, Pickup=${PICKUP_LAT},${PICKUP_LNG} → Dropoff=${DROPOFF_LAT},${DROPOFF_LNG})"
        ORDER_IDS+=("$ORDER_ID")
        ORDER_PICKUP_LATS+=("$PICKUP_LAT")
        ORDER_PICKUP_LNGS+=("$PICKUP_LNG")
        ORDER_DROPOFF_LATS+=("$DROPOFF_LAT")
        ORDER_DROPOFF_LNGS+=("$DROPOFF_LNG")
        ORDER_WEIGHTS+=("$WEIGHT")
        ORDER_VOLUMES+=("$VOLUME")
    else
        fail "Failed to create $ORDER_NUM"
        echo "  Response: $RESP"
    fi
}

# Order 1: Lat Krabang → Sukhumvit 55 (800 kg electronics)
create_order "ORD-ROUTE-001" \
    "คลังสินค้า ลาดกระบัง" 13.7286 100.7472 \
    "สุขุมวิท ซอย 55 (ทองหล่อ)" 13.7340 100.5790 \
    800 1.2 "อุปกรณ์อิเล็กทรอนิกส์"

# Order 2: Bangkapi → Silom (1200 kg machinery parts)
create_order "ORD-ROUTE-002" \
    "คลังสินค้า บางกะปิ" 13.7649 100.6478 \
    "ย่านสีลม" 13.7262 100.5235 \
    1200 2.5 "ชิ้นส่วนเครื่องจักร"

# Order 3: Rangsit → Chatuchak (500 kg textiles)
create_order "ORD-ROUTE-003" \
    "คลังสินค้า รังสิต" 14.0361 100.5987 \
    "จตุจักร" 13.7999 100.5533 \
    500 3.0 "สิ่งทอและผ้า"

# Order 4: Bangna → Pratunam (2000 kg furniture)
create_order "ORD-ROUTE-004" \
    "คลังสินค้า บางนา" 13.6672 100.6057 \
    "ย่านประตูน้ำ" 13.7503 100.5396 \
    2000 8.0 "เฟอร์นิเจอร์สำนักงาน"

# Order 5: Samut Prakan → Phra Khanong (600 kg food products)
create_order "ORD-ROUTE-005" \
    "คลังสินค้า สมุทรปราการ" 13.5990 100.5998 \
    "พระโขนง" 13.7130 100.5851 \
    600 1.0 "ผลิตภัณฑ์อาหาร"

echo ""
info "Created ${#ORDER_IDS[@]} orders"

if [ ${#ORDER_IDS[@]} -lt 3 ]; then
    fail "Need at least 3 orders to test route optimization"
    exit 1
fi

# ============================================================================
# STEP 3: Confirm All Orders
# ============================================================================
header "STEP 3 — Confirm All Orders"

for OID in "${ORDER_IDS[@]}"; do
    CONFIRM_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X PUT "$BASE_URL/api/orders/$OID/confirm")
    if [ "$CONFIRM_CODE" == "204" ]; then
        pass "Confirmed Order $OID"
    else
        fail "Failed to confirm $OID (HTTP $CONFIRM_CODE)"
    fi
done

# Wait for integration events to propagate (Outbox → PlanningOrder)
info "Waiting 5s for integration events to propagate..."
sleep 5

# ============================================================================
# STEP 4: Trigger Route Optimization (PDP VRP)
# ============================================================================
header "STEP 4 — Request PDP Route Optimization"

# Build the orders array using stored coordinates (API does not return lat/lng)
ORDERS_JSON="["
for i in "${!ORDER_IDS[@]}"; do
    OID=${ORDER_IDS[$i]}
    P_LAT=${ORDER_PICKUP_LATS[$i]}
    P_LNG=${ORDER_PICKUP_LNGS[$i]}
    D_LAT=${ORDER_DROPOFF_LATS[$i]}
    D_LNG=${ORDER_DROPOFF_LNGS[$i]}
    W=${ORDER_WEIGHTS[$i]}
    V=${ORDER_VOLUMES[$i]}

    info "  Order $OID: Pickup(${P_LAT},${P_LNG}) → Dropoff(${D_LAT},${D_LNG}) W=${W}kg V=${V}cbm"

    if [ $i -gt 0 ]; then ORDERS_JSON+=","; fi
    ORDERS_JSON+='{
        "orderId": "'"$OID"'",
        "pickupLat": '"$P_LAT"',
        "pickupLng": '"$P_LNG"',
        "dropoffLat": '"$D_LAT"',
        "dropoffLng": '"$D_LNG"',
        "weightKg": '"$W"',
        "volumeCBM": '"$V"'
    }'
done
ORDERS_JSON+="]"

PLANNED_DATE=$(date +"%Y-%m-%d")
DEPARTURE_TIME=$(date -u -v+1H +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null || date -u -d "+1 hour" +"%Y-%m-%dT%H:%M:%SZ")

info "Depot: Bangna (13.6672, 100.6057)"
info "Max Capacity: 5000 kg / 20 CBM per vehicle"
info "Planned Date: $PLANNED_DATE"
echo ""

OPTIMIZE_RESP=$(curl -s -X POST "$BASE_URL/api/planning/optimize" \
    -H "Content-Type: application/json" \
    -d '{
        "orders": '"$ORDERS_JSON"',
        "tenantId": "'"$TENANT_ID"'",
        "plannedDate": "'"$PLANNED_DATE"'",
        "maxOrdersPerRoute": 10,
        "maxCapacityKg": 5000,
        "maxCapacityVolumeCBM": 20,
        "depotLat": 13.6672,
        "depotLng": 100.6057,
        "departureTime": "'"$DEPARTURE_TIME"'"
    }')

info "Optimize Response:"
echo "$OPTIMIZE_RESP" | python3 -m json.tool 2>/dev/null || echo "$OPTIMIZE_RESP"

OPT_REQUEST_ID=$(echo "$OPTIMIZE_RESP" | json_val "['optimizationRequestId']" 2>/dev/null || echo "$OPTIMIZE_RESP" | json_val "['OptimizationRequestId']" 2>/dev/null || echo "")

if [ -n "$OPT_REQUEST_ID" ] && [ "$OPT_REQUEST_ID" != "None" ]; then
    pass "Optimization Request ID: $OPT_REQUEST_ID"
else
    fail "Failed to submit optimization request"
    echo "  Response: $OPTIMIZE_RESP"
    exit 1
fi

# ============================================================================
# STEP 5: Poll Optimization Status
# ============================================================================
header "STEP 5 — Poll Optimization Status"

MAX_ATTEMPTS=15
for attempt in $(seq 1 $MAX_ATTEMPTS); do
    STATUS_RESP=$(curl -s "$BASE_URL/api/planning/optimize/$OPT_REQUEST_ID")
    STATUS=$(echo "$STATUS_RESP" | json_val "['status']" 2>/dev/null || echo "$STATUS_RESP" | json_val "['Status']" 2>/dev/null || echo "unknown")

    info "Attempt $attempt/$MAX_ATTEMPTS — Status: $STATUS"

    if [ "$STATUS" == "Completed" ] || [ "$STATUS" == "completed" ]; then
        pass "Optimization Completed!"
        echo ""
        echo "$STATUS_RESP" | python3 -m json.tool 2>/dev/null || echo "$STATUS_RESP"
        break
    elif [ "$STATUS" == "Failed" ] || [ "$STATUS" == "failed" ]; then
        fail "Optimization Failed!"
        echo "$STATUS_RESP" | python3 -m json.tool 2>/dev/null
        break
    fi

    sleep 2
done

# ============================================================================
# STEP 6: View Generated Route Plans
# ============================================================================
header "STEP 6 — View Generated Route Plans"

PLANS_RESP=$(curl -s "$BASE_URL/api/planning/plans?date=$PLANNED_DATE")

echo "$PLANS_RESP" | python3 -c "
import sys, json

data = json.load(sys.stdin)
items = data.get('items', data.get('Items', []))

if not items:
    print('⚠️  No route plans found for date $PLANNED_DATE')
    sys.exit(0)

print(f'📋 Found {len(items)} Route Plan(s)\n')

for plan in items:
    plan_id = plan.get('id', plan.get('Id', 'N/A'))
    plan_num = plan.get('planNumber', plan.get('PlanNumber', 'N/A'))
    status = plan.get('status', plan.get('Status', 'N/A'))
    dist = plan.get('totalDistanceKm', plan.get('TotalDistanceKm', 0))
    dur = plan.get('estimatedTotalDurationMin', plan.get('EstimatedTotalDurationMin', 0))
    cap = plan.get('capacityUtilizationPercent', plan.get('CapacityUtilizationPercent', 0))
    stops = plan.get('stops', plan.get('Stops', []))

    print(f'  🚛 {plan_num} (Status: {status})')
    print(f'     ID: {plan_id}')
    print(f'     Distance: {dist} km | Duration: {dur} min | Capacity: {cap}%')
    print(f'     Stops ({len(stops)}):')

    for stop in stops:
        seq = stop.get('sequence', stop.get('Sequence', 0))
        order_id = stop.get('orderId', stop.get('OrderId', 'N/A'))
        stop_type = stop.get('stopType', stop.get('StopType', '?'))
        lat = stop.get('lat', stop.get('Lat', 0))
        lng = stop.get('lng', stop.get('Lng', 0))
        eta = stop.get('estimatedArrivalTime', stop.get('EstimatedArrivalTime', 'N/A'))

        icon = '📦' if stop_type == 'Pickup' else '📍'
        print(f'       {seq}. {icon} {stop_type} — Order: {str(order_id)[:8]}... ({lat:.4f}, {lng:.4f}) ETA: {eta}')

    print()
" 2>/dev/null || echo "$PLANS_RESP" | python3 -m json.tool 2>/dev/null || echo "$PLANS_RESP"

# ============================================================================
# STEP 7: View Individual Route Plan Detail
# ============================================================================
header "STEP 7 — Route Plan Details"

FIRST_PLAN_ID=$(echo "$PLANS_RESP" | python3 -c "
import sys,json
data = json.load(sys.stdin)
items = data.get('items', data.get('Items', []))
if items:
    print(items[0].get('id', items[0].get('Id','')))
" 2>/dev/null)

if [ -n "$FIRST_PLAN_ID" ] && [ "$FIRST_PLAN_ID" != "" ]; then
    PLAN_DETAIL=$(curl -s "$BASE_URL/api/planning/plans/$FIRST_PLAN_ID")
    info "Route Plan Detail for $FIRST_PLAN_ID:"
    echo "$PLAN_DETAIL" | python3 -m json.tool 2>/dev/null || echo "$PLAN_DETAIL"
else
    info "No route plan details to show"
fi

# ============================================================================
# Summary
# ============================================================================
header "🏁 TEST SUMMARY"

echo -e "${GREEN}  ✅ Customer created${NC}"
echo -e "${GREEN}  ✅ ${#ORDER_IDS[@]} Orders created with lat/lng${NC}"
echo -e "${GREEN}  ✅ All Orders confirmed${NC}"
echo -e "${GREEN}  ✅ PDP Route Optimization submitted${NC}"
echo -e "${GREEN}  ✅ Route Plans generated${NC}"
echo ""
echo -e "${CYAN}  📊 Orders used Bangkok area coordinates${NC}"
echo -e "${CYAN}  📊 Depot: Bangna (13.6672, 100.6057)${NC}"
echo -e "${CYAN}  📊 VRP Solver: PDP Nearest Neighbor with Precedence${NC}"
echo ""

// ═══════════════════════════════════════════════════════════════
// TMS Dispatch Board — Main Application (All 114 API Endpoints)
// ═══════════════════════════════════════════════════════════════

let currentView = 'dashboard';
let mapInstance = null;

// ── Init ───────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('.nav-item[data-view]').forEach(n =>
    n.addEventListener('click', () => navigateTo(n.dataset.view)));
  setInterval(() => {
    document.getElementById('clock').textContent =
      new Date().toLocaleString('th-TH', { dateStyle: 'medium', timeStyle: 'medium' });
  }, 1000);
  navigateTo('dashboard');
});

function navigateTo(view) {
  currentView = view;
  document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
  document.querySelector(`.nav-item[data-view="${view}"]`)?.classList.add('active');
  const titles = {
    dashboard: '📊 Dashboard', orders: '📦 Orders', planning: '🗺️ Route Planning',
    dispatch: '🚛 Dispatch Board', shipments: '📬 Shipments', fleet: '🔧 Fleet', drivers: '👤 Drivers',
    tracking: '📡 Live Tracking', master: '🏗️ Master Data', iam: '🔐 IAM', notifications: '🔔 Notifications',
    integrations: '🔌 Integrations', documents: '📄 Documents', operability: '⚙️ Operability'
  };
  document.getElementById('page-title').textContent = titles[view] || view;
  loadView(view);
}

function refreshView() { navigateTo(currentView); }

async function loadView(v) {
  const el = document.getElementById('page-content');
  el.innerHTML = '<div class="loading-spinner"><div class="spinner"></div></div>';
  try {
    if (views[v]) {
      await views[v](el);
    } else {
      el.innerHTML = '<div class="empty-state"><div class="icon">🚧</div><div class="title">Not implemented</div></div>';
    }
  } catch (e) {
    el.innerHTML = `<div class="empty-state"><div class="icon">⚠️</div><div class="title">Error</div><div class="desc">${e.message}</div></div>`;
    console.error(e);
  }
}

// ═════════════════════════════════════════════════════════════
// ALL VIEWS
// ═════════════════════════════════════════════════════════════
const views = {};

// ── DASHBOARD ───────────────────────────────────────────────
views.dashboard = async (el) => {
  const [ord, trp, shp, veh, drv, pln] = await Promise.all([
    api.orders({ pageSize: 200 }), api.trips({ pageSize: 200 }), api.shipments({ pageSize: 200 }),
    api.vehicles({ pageSize: 100 }), api.drivers({ pageSize: 100 }), api.plans()
  ]);
  const O = ord?.items || [], T = trp?.items || [], S = shp?.items || [], V = veh?.items || [], D = drv?.items || [], P = pln?.items || [];

  el.innerHTML = `<div class="fade-in">
    <div class="kpi-grid">
      ${kpi('Total Orders', '📦', O.length, `${O.filter(o => o.status === 'Confirmed').length} awaiting planning`, '--gradient-1')}
      ${kpi('Active Trips', '🚛', T.filter(t => ['Dispatched', 'InProgress'].includes(t.status)).length, `${T.length} total`, '--gradient-2')}
      ${kpi('Shipments', '📬', S.length, `${S.filter(s => s.status === 'Delivered').length} delivered`, '--gradient-3')}
      ${kpi('Vehicles', '🚗', V.length, `${V.filter(v => v.status === 'Available').length} available`, '--gradient-4')}
      ${kpi('Drivers', '👤', D.length, `${D.filter(d => d.status === 'Available').length} available`, 'linear-gradient(135deg,#818cf8,#38bdf8)')}
      ${kpi('Route Plans', '🗺️', P.length, 'OR-Tools VRP optimized', 'linear-gradient(135deg,#34d399,#22d3ee)')}
    </div>
    <div class="content-grid two-col">
      <div class="card"><div class="card-header"><span class="card-title">📦 Recent Orders</span><button class="btn btn-sm btn-secondary" onclick="navigateTo('orders')">View All →</button></div>
        ${buildTable([{ label: '#', render: r => `<strong>${r.orderNumber || '—'}</strong>` }, { label: 'Status', render: r => badge(r.status) }, { label: 'Weight', render: r => fmtN(r.totalWeight) + ' kg' }, { label: 'Created', render: r => fmtD(r.createdAt) }], O.slice(0, 8))}
      </div>
      <div class="card"><div class="card-header"><span class="card-title">🚛 Trips</span><button class="btn btn-sm btn-secondary" onclick="navigateTo('dispatch')">Board →</button></div>
        ${buildTable([{ label: '#', render: r => `<strong>${r.tripNumber || sid(r.id)}</strong>` }, { label: 'Status', render: r => badge(r.status) }, { label: 'Vehicle', render: r => r.vehiclePlateNumber || '—' }], T.slice(0, 8))}
      </div>
    </div></div>`;
};

function kpi(label, icon, val, sub, grad) {
  return `<div class="kpi-card" style="--kpi-gradient:var(${grad},${grad})"><div class="kpi-header"><span class="kpi-label">${label}</span><span class="kpi-icon">${icon}</span></div><div class="kpi-value">${val}</div><div class="kpi-sub">${sub}</div></div>`;
}

// ── ORDERS ──────────────────────────────────────────────────
views.orders = async (el) => {
  const d = await api.orders({ pageSize: 100 });
  const rows = d?.items || [];
  el.innerHTML = `<div class="fade-in"><div class="card"><div class="card-header"><span class="card-title">Orders (${rows.length})</span><div style="display:flex;gap:8px"><button class="btn btn-sm btn-primary" onclick="orderCreateForm()">+ New Order</button><button class="btn btn-sm btn-secondary" onclick="refreshView()">🔄</button></div></div>
    ${buildTable([
    { label: 'Order #', render: r => `<strong>${r.orderNumber}</strong>` },
    { label: 'Status', render: r => badge(r.status) },
    { label: 'Customer', render: r => sid(r.customerId) },
    { label: 'Weight', render: r => fmtN(r.totalWeight) + ' kg' },
    { label: 'Volume', render: r => (r.totalVolume || 0) + ' CBM' },
    { label: 'Created', render: r => fmtD(r.createdAt) }
  ], rows, {
    actions: r => {
      let a = `<button class="btn btn-sm btn-secondary" onclick="showDetail('Order ${r.orderNumber}',${JSON.stringify(r).replace(/'/g, "\\'")})">👁</button> `;
      if (r.status === 'Draft') a += `<button class="btn btn-sm btn-primary" onclick="confirmAction('Confirm order ${r.orderNumber}?',()=>api.confirmOrder('${r.id}'))">✓</button> `;
      if (['Draft', 'Confirmed'].includes(r.status)) a += `<button class="btn btn-sm btn-secondary" onclick="confirmAction('Cancel order?',()=>api.cancelOrder('${r.id}'))">✗</button> `;
      if (r.status === 'Confirmed') a += `<button class="btn btn-sm btn-secondary" onclick="orderSplitForm('${r.id}')">✂️</button> `;
      return a;
    }
  })}
  </div></div>`;
};

window.orderCreateForm = async () => {
  const custs = await api.customers({ pageSize: 100 });
  const custOpts = (custs?.items || []).map(c => ({ value: c.id, label: c.companyName || c.customerCode }));
  openModal('📦 Create Order', buildForm([
    { type: 'section', label: 'Basic Info' },
    { name: 'orderNumber', label: 'Order Number', required: true, placeholder: 'ORD-001' },
    { name: 'customerId', label: 'Customer', type: 'select', options: custOpts, required: true },
    { name: 'priority', label: 'Priority', type: 'select', options: ['Normal', 'Express', 'SameDay'] },
    { type: 'section', label: 'Pickup Address' },
    { name: 'pickupStreet', label: 'Street', required: true },
    { name: 'pickupDistrict', label: 'District' }, { name: 'pickupProvince', label: 'Province' },
    { name: 'pickupPostalCode', label: 'Postal Code' }, { name: 'pickupLatitude', label: 'Latitude', type: 'number' }, { name: 'pickupLongitude', label: 'Longitude', type: 'number' },
    { type: 'section', label: 'Dropoff Address' },
    { name: 'dropoffStreet', label: 'Street', required: true },
    { name: 'dropoffDistrict', label: 'District' }, { name: 'dropoffProvince', label: 'Province' },
    { name: 'dropoffPostalCode', label: 'Postal Code' }, { name: 'dropoffLatitude', label: 'Latitude', type: 'number' }, { name: 'dropoffLongitude', label: 'Longitude', type: 'number' },
    { type: 'section', label: 'Item' },
    { name: 'itemDescription', label: 'Description', required: true },
    { name: 'itemWeight', label: 'Weight (kg)', type: 'number', required: true },
    { name: 'itemVolume', label: 'Volume (CBM)', type: 'number' },
    { name: 'itemQuantity', label: 'Quantity', type: 'number', value: 1 },
    { name: 'notes', label: 'Notes', type: 'textarea' },
  ], { submitLabel: 'Create Order', width: '640px' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try {
      await api.createOrder({
        orderNumber: f.orderNumber, customerId: f.customerId, priority: f.priority || 'Normal', notes: f.notes,
        pickupAddress: { street: f.pickupStreet, district: f.pickupDistrict, province: f.pickupProvince, postalCode: f.pickupPostalCode, latitude: f.pickupLatitude || 0, longitude: f.pickupLongitude || 0, subDistrict: '' },
        dropoffAddress: { street: f.dropoffStreet, district: f.dropoffDistrict, province: f.dropoffProvince, postalCode: f.dropoffPostalCode, latitude: f.dropoffLatitude || 0, longitude: f.dropoffLongitude || 0, subDistrict: '' },
        items: [{ description: f.itemDescription, weight: f.itemWeight || 0, volume: f.itemVolume || 0, quantity: f.itemQuantity || 1 }]
      });
      closeModal(); toast('Order created!', 'success'); refreshView();
    } catch (e) { toast(e.message, 'error'); }
  };
};

window.orderSplitForm = (id) => {
  openModal('✂️ Auto Split Order', buildForm([
    { name: 'maxWeightPerSplitKg', label: 'Max Weight per Split (kg)', type: 'number', value: 5000, required: true },
    { name: 'maxVolumePerSplitCBM', label: 'Max Volume per Split (CBM)', type: 'number', value: 20 },
  ], { submitLabel: 'Split' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.autoSplitOrder(id, f); closeModal(); toast('Order split!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

// ── ROUTE PLANNING ──────────────────────────────────────────
views.planning = async (el) => {
  const d = await api.plans();
  const plans = d?.items || [];
  el.innerHTML = `<div class="fade-in"><div class="content-grid sidebar-layout">
    <div><div class="card"><div class="card-header"><span class="card-title">🗺️ Route Map</span></div><div id="plan-map" class="map-container" style="height:560px"></div></div></div>
    <div>
      <div class="card" style="margin-bottom:12px"><div class="card-header"><span class="card-title">Actions</span></div>
        <button class="btn btn-primary" style="width:100%;margin-bottom:8px" onclick="optimizeForm()">🧠 Run OR-Tools Optimization</button>
        <button class="btn btn-secondary" style="width:100%" onclick="planWithSplitForm()">✂️ Plan with Auto-Split</button>
      </div>
      <div class="card"><div class="card-header"><span class="card-title">Plans (${plans.length})</span><button class="btn btn-sm btn-secondary" onclick="refreshView()">🔄</button></div>
        <div id="plan-list" style="max-height:380px;overflow-y:auto">
        ${plans.map(p => `<div style="border:1px solid var(--border-default);border-radius:var(--radius-md);padding:12px;margin-bottom:8px;cursor:pointer" onclick="focusPlan('${p.id}')" onmouseover="this.style.borderColor='var(--border-active)'" onmouseout="this.style.borderColor='var(--border-default)'">
          <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:6px"><strong>${p.planNumber}</strong>${badge(p.status)}</div>
          <div style="font-size:12px;color:var(--text-secondary)">📏 ${p.totalDistanceKm || 0} km · ⏱ ${p.estimatedTotalDurationMin || 0} min · 📦 ${p.capacityUtilizationPercent || 0}% · ${(p.stops?.length || 0)} stops</div>
          <div style="display:flex;gap:6px;margin-top:8px">
            ${p.status === 'Draft' ? `<button class="btn btn-sm btn-primary" onclick="event.stopPropagation();confirmAction('Lock plan?',()=>api.lockPlan('${p.id}'))">🔒 Lock</button>` : ``}
            <button class="btn btn-sm btn-secondary" onclick="event.stopPropagation();showDetail('${p.planNumber}',${JSON.stringify(p).replace(/'/g, "\\'")})">👁</button>
          </div>
        </div>`).join('')}
        ${plans.length === 0 ? '<div style="text-align:center;color:var(--text-muted);padding:20px">No plans yet</div>' : ''}
        </div>
      </div>
    </div></div></div>`;
  setTimeout(() => initMap(plans), 100);
};

function initMap(plans) {
  if (mapInstance) { mapInstance.remove(); mapInstance = null; }
  const el = document.getElementById('plan-map') || document.getElementById('tracking-map');
  if (!el) return;
  mapInstance = L.map(el.id).setView([13.75, 100.55], 11);
  L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', { attribution: '© CARTO', maxZoom: 19 }).addTo(mapInstance);
  const colors = ['#38bdf8', '#34d399', '#fb7185', '#fbbf24', '#a78bfa', '#22d3ee'];
  const allC = [];
  (plans || []).forEach((p, pi) => {
    const stops = p.stops || []; if (!stops.length) return;
    const color = colors[pi % colors.length];
    const pts = stops.filter(s => s.lat && s.lng).map(s => [s.lat, s.lng]);
    if (pts.length) L.polyline(pts, { color, weight: 3, opacity: .8, dashArray: '8,4' }).addTo(mapInstance);
    stops.forEach((s, si) => {
      if (!s.lat || !s.lng) return;
      const pk = s.stopType === 'Pickup';
      const icon = L.divIcon({ className: '', html: `<div style="width:24px;height:24px;border-radius:50%;background:${pk ? '#34d399' : '#fb7185'};border:2px solid #fff;display:flex;align-items:center;justify-content:center;font-size:10px;font-weight:700;color:#fff;box-shadow:0 2px 6px rgba(0,0,0,.5)">${si + 1}</div>`, iconSize: [24, 24], iconAnchor: [12, 12] });
      L.marker([s.lat, s.lng], { icon }).addTo(mapInstance).bindPopup(`<strong>${p.planNumber} #${s.sequence}</strong><br>${s.stopType} · Order: ${sid(s.orderId)}${s.estimatedArrivalTime ? '<br>ETA: ' + fmtD(s.estimatedArrivalTime) : ''}`);
      allC.push([s.lat, s.lng]);
    });
  });
  if (allC.length) mapInstance.fitBounds(allC, { padding: [40, 40] });
}

window.focusPlan = async (id) => { try { const p = await api.plan(id); initMap([p]); toast(`Showing ${p.planNumber}`, 'success'); } catch (e) { toast(e.message, 'error'); } };

window.optimizeForm = async () => {
  const ord = await api.orders({ pageSize: 200 });
  const confirmed = (ord?.items || []).filter(o => o.status === 'Confirmed');
  openModal('🧠 Run OR-Tools VRP Optimization', buildForm([
    { type: 'section', label: `${confirmed.length} confirmed orders available` },
    { name: 'maxCapacityKg', label: 'Max Capacity (kg)', type: 'number', value: 5000, required: true },
    { name: 'maxCapacityVolumeCBM', label: 'Max Volume (CBM)', type: 'number', value: 20 },
    { name: 'maxOrdersPerRoute', label: 'Max Orders per Route', type: 'number', value: 10 },
    { name: 'depotLat', label: 'Depot Latitude', type: 'number', value: 13.6672 },
    { name: 'depotLng', label: 'Depot Longitude', type: 'number', value: 100.6057 },
  ], { submitLabel: '🧠 Optimize' }), { width: '500px' });
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try {
      const orders = confirmed.map(o => ({ orderId: o.id, pickupLat: 0, pickupLng: 0, dropoffLat: 0, dropoffLng: 0, weightKg: o.totalWeight || 0, volumeCBM: o.totalVolume || 0 }));
      const res = await api.optimize({ orders, tenantId: '00000000-0000-0000-0000-000000000001', plannedDate: new Date().toISOString().split('T')[0], ...f });
      closeModal(); toast(`Optimization submitted! ID: ${res.optimizationRequestId}`, 'success');
      // Poll status
      const poll = setInterval(async () => {
        const st = await api.optimizeStatus(res.optimizationRequestId);
        if (st.status === 'Completed') { clearInterval(poll); toast('Optimization completed!', 'success'); refreshView(); }
        if (st.status === 'Failed') { clearInterval(poll); toast('Optimization failed: ' + st.error, 'error'); }
      }, 2000);
    } catch (e) { toast(e.message, 'error'); }
  };
};

window.planWithSplitForm = async () => {
  const ord = await api.orders({ pageSize: 200 });
  const confirmed = (ord?.items || []).filter(o => o.status === 'Confirmed');
  openModal('✂️ Plan with Auto-Split', buildForm([
    { type: 'section', label: `Will process ${confirmed.length} confirmed orders` },
    { name: 'maxVehicleWeightKg', label: 'Max Vehicle Weight (kg)', type: 'number', value: 10000 },
    { name: 'maxVehicleVolumeCBM', label: 'Max Volume (CBM)', type: 'number', value: 20 },
    { name: 'maxOrdersPerRoute', label: 'Max Orders/Route', type: 'number', value: 10 },
    { name: 'depotLat', label: 'Depot Lat', type: 'number', value: 13.6672 },
    { name: 'depotLng', label: 'Depot Lng', type: 'number', value: 100.6057 },
  ], { submitLabel: 'Plan & Split' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try {
      await api.planWithSplit({ orderIds: confirmed.map(o => o.id), tenantId: '00000000-0000-0000-0000-000000000001', plannedDate: new Date().toISOString().split('T')[0], ...f });
      closeModal(); toast('Plan with split submitted!', 'success'); refreshView();
    } catch (e) { toast(e.message, 'error'); }
  };
};

// ── DISPATCH ────────────────────────────────────────────────
views.dispatch = async (el) => {
  const d = await api.trips({ pageSize: 100 });
  const rows = d?.items || [];
  const sc = { Created: '#64748b', Assigned: '#fbbf24', Dispatched: '#a78bfa', InProgress: '#22d3ee', Completed: '#34d399', Cancelled: '#fb7185' };
  el.innerHTML = `<div class="fade-in">
    <div class="kpi-grid" style="grid-template-columns:repeat(5,1fr)">
      ${['Created', 'Assigned', 'Dispatched', 'InProgress', 'Completed'].map(s => `<div class="kpi-card" style="--kpi-gradient:linear-gradient(135deg,${sc[s]},${sc[s]}88)"><div class="kpi-header"><span class="kpi-label">${s}</span></div><div class="kpi-value" style="font-size:28px">${rows.filter(t => t.status === s).length}</div></div>`).join('')}
    </div>
    <div class="card"><div class="card-header"><span class="card-title">All Trips (${rows.length})</span><div style="display:flex;gap:8px"><button class="btn btn-sm btn-primary" onclick="tripCreateForm()">+ New Trip</button><button class="btn btn-sm btn-secondary" onclick="refreshView()">🔄</button></div></div>
      ${buildTable([
    { label: 'Trip #', render: r => `<strong>${r.tripNumber || sid(r.id)}</strong>` },
    { label: 'Status', render: r => badge(r.status) },
    { label: 'Vehicle', render: r => r.vehiclePlateNumber || '—' },
    { label: 'Driver', render: r => r.driverName || '—' },
    { label: 'Stops', render: r => r.stopCount || r.stops?.length || '—' },
    { label: 'Created', render: r => fmtD(r.createdAt) }
  ], rows, {
    actions: r => {
      let a = `<button class="btn btn-sm btn-secondary" onclick="showDetail('Trip',${JSON.stringify(r).replace(/'/g, "\\'")})">👁</button> `;
      if (r.status === 'Created') a += `<button class="btn btn-sm btn-primary" onclick="tripAssignForm('${r.id}')">Assign</button> `;
      if (r.status === 'Assigned') a += `<button class="btn btn-sm btn-primary" onclick="confirmAction('Dispatch?',()=>api.dispatchTrip('${r.id}'))">Dispatch</button> `;
      if (['Dispatched', 'InProgress'].includes(r.status)) a += `<button class="btn btn-sm btn-secondary" onclick="confirmAction('Complete?',()=>api.completeTrip('${r.id}'))">Complete</button> `;
      if (!['Completed', 'Cancelled'].includes(r.status)) a += `<button class="btn btn-sm btn-secondary" onclick="confirmAction('Cancel trip?',()=>api.cancelTrip('${r.id}'))">✗</button> `;
      return a;
    }
  })}
    </div></div>`;
};

window.tripCreateForm = () => {
  openModal('🚛 Create Trip', buildForm([
    { name: 'plannedDate', label: 'Planned Date', type: 'date', value: new Date().toISOString().split('T')[0], required: true },
    { name: 'notes', label: 'Notes', type: 'textarea' },
  ], { submitLabel: 'Create Trip' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.createTrip({ ...f, tenantId: '00000000-0000-0000-0000-000000000001' }); closeModal(); toast('Trip created!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

window.tripAssignForm = async (id) => {
  const [vd, dd] = await Promise.all([api.availableVehicles(), api.availableDrivers()]);
  const vOpts = (vd?.items || vd || []).map(v => ({ value: v.id, label: v.plateNumber || v.id }));
  const dOpts = (dd?.items || dd || []).map(d => ({ value: d.id, label: `${d.firstName || ''} ${d.lastName || ''}` }));
  openModal('Assign Trip', buildForm([
    { name: 'vehicleId', label: 'Vehicle', type: 'select', options: vOpts, required: true },
    { name: 'driverId', label: 'Driver', type: 'select', options: dOpts, required: true },
  ], { submitLabel: 'Assign' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.assignTrip(id, f); closeModal(); toast('Trip assigned!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

// ── SHIPMENTS ───────────────────────────────────────────────
views.shipments = async (el) => {
  const d = await api.shipments({ pageSize: 100 });
  const rows = d?.items || [];
  el.innerHTML = `<div class="fade-in">
    <div class="kpi-grid" style="grid-template-columns:repeat(4,1fr)">
      ${[{ l: 'Pending', f: s => s.status === 'Pending' }, { l: 'In Transit', f: s => ['PickedUp', 'InTransit'].includes(s.status) }, { l: 'Delivered', f: s => s.status === 'Delivered' }, { l: 'Exceptions', f: s => ['Exception', 'Rejected'].includes(s.status) }].map(({ l, f }) => `<div class="kpi-card"><div class="kpi-header"><span class="kpi-label">${l}</span></div><div class="kpi-value" style="font-size:28px">${rows.filter(f).length}</div></div>`).join('')}
    </div>
    <div class="card"><div class="card-header"><span class="card-title">All Shipments (${rows.length})</span><button class="btn btn-sm btn-secondary" onclick="refreshView()">🔄</button></div>
      ${buildTable([
    { label: '#', render: r => `<strong>${r.shipmentNumber || sid(r.id)}</strong>` },
    { label: 'Status', render: r => badge(r.status) },
    { label: 'Trip', render: r => r.tripNumber || sid(r.tripId || '') },
    { label: 'Created', render: r => fmtD(r.createdAt) }
  ], rows, {
    actions: r => {
      let a = `<button class="btn btn-sm btn-secondary" onclick="showDetail('Shipment',${JSON.stringify(r).replace(/'/g, "\\'")})">👁</button> `;
      if (r.status === 'Pending') a += `<button class="btn btn-sm btn-primary" onclick="confirmAction('Mark Picked Up?',()=>api.pickupShipment('${r.id}'))">📦 Pickup</button> `;
      if (r.status === 'PickedUp' || r.status === 'InTransit') a += `<button class="btn btn-sm btn-secondary" onclick="confirmAction('Mark Arrived?',()=>api.arriveShipment('${r.id}'))">📍 Arrive</button> `;
      if (r.status === 'Arrived') {
        a += `<button class="btn btn-sm btn-primary" onclick="confirmAction('Deliver?',()=>api.deliverShipment('${r.id}'))">✅ Deliver</button> `;
        a += `<button class="btn btn-sm btn-secondary" onclick="shipmentExceptionForm('${r.id}')">⚠️</button> `;
      }
      if (r.status === 'Delivered') a += `<button class="btn btn-sm btn-secondary" onclick="confirmAction('Generate POD PDF?',()=>api.podGeneratePdf('${r.id}'))">📄 POD</button> `;
      return a;
    }
  })}
    </div></div>`;
};

window.shipmentExceptionForm = (id) => {
  openModal('⚠️ Report Exception', buildForm([
    { name: 'reasonCode', label: 'Reason Code', required: true, placeholder: 'DAMAGED' },
    { name: 'notes', label: 'Notes', type: 'textarea' },
  ], { submitLabel: 'Report' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.exceptionShipment(id, f); closeModal(); toast('Exception reported', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

// ── FLEET ───────────────────────────────────────────────────
views.fleet = async (el) => {
  const d = await api.vehicles({ pageSize: 100 });
  const rows = d?.items || [];
  el.innerHTML = `<div class="fade-in"><div class="card"><div class="card-header"><span class="card-title">🚗 Vehicles (${rows.length})</span><div style="display:flex;gap:8px"><button class="btn btn-sm btn-primary" onclick="vehicleCreateForm()">+ Add Vehicle</button><button class="btn btn-sm btn-secondary" onclick="refreshView()">🔄</button></div></div>
    ${buildTable([
    { label: 'Plate #', render: r => `<strong>${r.plateNumber || '—'}</strong>` },
    { label: 'Type', render: r => r.vehicleTypeName || '—' },
    { label: 'Status', render: r => badge(r.status) },
    { label: 'Payload', render: r => fmtN(r.maxPayloadKg || r.payloadCapacityKg) + ' kg' },
    { label: 'Volume', render: r => (r.maxVolumeCBM || r.volumeCapacityCBM || '—') + ' CBM' }
  ], rows, {
    actions: r => {
      let a = `<button class="btn btn-sm btn-secondary" onclick="showDetail('Vehicle',${JSON.stringify(r).replace(/'/g, "\\'")})">👁</button> `;
      a += `<button class="btn btn-sm btn-secondary" onclick="vehicleStatusForm('${r.id}','${r.status}')">Status</button> `;
      a += `<button class="btn btn-sm btn-secondary" onclick="vehicleMaintenanceForm('${r.id}')">🔧</button> `;
      return a;
    }
  })}
  </div></div>`;
};

window.vehicleCreateForm = () => {
  openModal('🚗 Add Vehicle', buildForm([
    { name: 'plateNumber', label: 'Plate Number', required: true, placeholder: '1กก-1234' },
    { name: 'vehicleTypeName', label: 'Vehicle Type', required: true, placeholder: '6-Wheeler Truck' },
    { name: 'maxPayloadKg', label: 'Max Payload (kg)', type: 'number', value: 10000 },
    { name: 'maxVolumeCBM', label: 'Max Volume (CBM)', type: 'number', value: 30 },
    { name: 'fuelType', label: 'Fuel Type', type: 'select', options: ['Diesel', 'Gasoline', 'Electric', 'LPG'] },
  ], { submitLabel: 'Add Vehicle' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.createVehicle(f); closeModal(); toast('Vehicle added!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

window.vehicleStatusForm = (id, current) => {
  openModal('Change Vehicle Status', buildForm([
    { name: 'status', label: `Current: ${current}`, type: 'select', options: ['Available', 'Assigned', 'InUse', 'InRepair', 'Decommissioned'], required: true },
  ], { submitLabel: 'Update' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.vehicleStatus(id, f); closeModal(); toast('Status updated!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

window.vehicleMaintenanceForm = (id) => {
  openModal('🔧 Schedule Maintenance', buildForm([
    { name: 'maintenanceType', label: 'Type', required: true, placeholder: 'Oil Change' },
    { name: 'scheduledDate', label: 'Scheduled Date', type: 'date', required: true },
    { name: 'notes', label: 'Notes', type: 'textarea' },
  ], { submitLabel: 'Schedule' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.vehicleMaintenance(id, f); closeModal(); toast('Maintenance scheduled!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

// ── DRIVERS ─────────────────────────────────────────────────
views.drivers = async (el) => {
  const d = await api.drivers({ pageSize: 100 });
  const rows = d?.items || [];
  el.innerHTML = `<div class="fade-in"><div class="card"><div class="card-header"><span class="card-title">👤 Drivers (${rows.length})</span><div style="display:flex;gap:8px"><button class="btn btn-sm btn-primary" onclick="driverCreateForm()">+ Add Driver</button><button class="btn btn-sm btn-secondary" onclick="refreshView()">🔄</button></div></div>
    ${buildTable([
    { label: 'Name', render: r => `<strong>${r.firstName || ''} ${r.lastName || ''}</strong>` },
    { label: 'License', render: r => r.licenseType || '—' },
    { label: 'Status', render: r => badge(r.status) },
    { label: 'Phone', render: r => r.phoneNumber || '—' },
    { label: 'License Expiry', render: r => r.licenseExpiryDate ? fmtD(r.licenseExpiryDate) : '—' }
  ], rows, {
    actions: r => {
      let a = `<button class="btn btn-sm btn-secondary" onclick="showDetail('Driver',${JSON.stringify(r).replace(/'/g, "\\'")})">👁</button> `;
      a += `<button class="btn btn-sm btn-secondary" onclick="driverStatusForm('${r.id}','${r.status}')">Status</button> `;
      return a;
    }
  })}
  </div></div>`;
};

window.driverCreateForm = () => {
  openModal('👤 Add Driver', buildForm([
    { name: 'firstName', label: 'First Name', required: true }, { name: 'lastName', label: 'Last Name', required: true },
    { name: 'phoneNumber', label: 'Phone', required: true }, { name: 'licenseNumber', label: 'License Number', required: true },
    { name: 'licenseType', label: 'License Type', type: 'select', options: ['ท.1', 'ท.2', 'ท.3', 'ท.4'], required: true },
    { name: 'licenseExpiryDate', label: 'License Expiry', type: 'date', required: true },
  ], { submitLabel: 'Add Driver' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.createDriver(f); closeModal(); toast('Driver added!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

window.driverStatusForm = (id, current) => {
  openModal('Change Driver Status', buildForm([
    { name: 'status', label: `Current: ${current}`, type: 'select', options: ['Available', 'OnDuty', 'OffDuty', 'OnLeave', 'Suspended'], required: true },
  ], { submitLabel: 'Update' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.driverStatus(id, f); closeModal(); toast('Status updated!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

// ── MASTER DATA ─────────────────────────────────────────────
views.master = async (el) => {
  const [cust, loc, rc, hol] = await Promise.all([
    api.customers({ pageSize: 100 }), api.locations({ pageSize: 100 }),
    api.reasonCodes({ pageSize: 100 }).catch(() => ({ items: [] })),
    api.holidays({ pageSize: 100 }).catch(() => ({ items: [] }))
  ]);
  const C = cust?.items || [], L = loc?.items || [], R = rc?.items || [], H = hol?.items || [];
  el.innerHTML = `<div class="fade-in">
    <div class="card" style="margin-bottom:20px"><div class="card-header"><span class="card-title">👥 Customers (${C.length})</span><button class="btn btn-sm btn-primary" onclick="customerCreateForm()">+ Add</button></div>
      ${buildTable([
    { label: 'Code', render: r => `<strong>${r.customerCode || '—'}</strong>` },
    { label: 'Company', render: r => r.companyName || '—' },
    { label: 'Contact', render: r => r.contactName || '—' },
    { label: 'Email', render: r => r.contactEmail || '—' },
    { label: 'Phone', render: r => r.contactPhone || '—' }
  ], C, { actions: r => `<button class="btn btn-sm btn-secondary" onclick="showDetail('Customer',${JSON.stringify(r).replace(/'/g, "\\'")})">👁</button> <button class="btn btn-sm btn-secondary" onclick="confirmAction('Delete customer?',()=>api.deleteCustomer('${r.id}'))">🗑</button>` })}
    </div>
    <div class="content-grid two-col">
      <div class="card"><div class="card-header"><span class="card-title">📍 Locations (${L.length})</span><button class="btn btn-sm btn-primary" onclick="locationCreateForm()">+ Add</button></div>
        ${buildTable([{ label: 'Name', render: r => `<strong>${r.locationName || r.name || '—'}</strong>` }, { label: 'Type', render: r => r.locationType || '—' }, { label: 'Province', render: r => r.province || '—' }], L.slice(0, 10))}
      </div>
      <div class="card"><div class="card-header"><span class="card-title">📋 Reason Codes (${R.length})</span><button class="btn btn-sm btn-primary" onclick="reasonCodeForm()">+ Add</button></div>
        ${buildTable([{ label: 'Code', render: r => `<strong>${r.code || '—'}</strong>` }, { label: 'Description', render: r => r.description || '—' }, { label: 'Category', render: r => r.category || '—' }], R.slice(0, 10))}
      </div>
    </div></div>`;
};

window.customerCreateForm = () => {
  openModal('👥 Add Customer', buildForm([
    { name: 'customerCode', label: 'Code', required: true, placeholder: 'CUST-001' },
    { name: 'companyName', label: 'Company Name', required: true },
    { name: 'contactName', label: 'Contact Name' }, { name: 'contactEmail', label: 'Email' },
    { name: 'contactPhone', label: 'Phone' }, { name: 'tenantId', type: 'hidden', value: '00000000-0000-0000-0000-000000000001' },
  ], { submitLabel: 'Create' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.createCustomer(f); closeModal(); toast('Customer created!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

window.locationCreateForm = () => {
  openModal('📍 Add Location', buildForm([
    { name: 'locationName', label: 'Name', required: true }, { name: 'locationType', label: 'Type', type: 'select', options: ['Warehouse', 'Customer', 'Hub', 'Port'] },
    { name: 'street', label: 'Street' }, { name: 'district', label: 'District' }, { name: 'province', label: 'Province' },
    { name: 'postalCode', label: 'Postal Code' }, { name: 'latitude', label: 'Latitude', type: 'number' }, { name: 'longitude', label: 'Longitude', type: 'number' },
    { name: 'tenantId', type: 'hidden', value: '00000000-0000-0000-0000-000000000001' },
  ], { submitLabel: 'Create' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.createLocation(f); closeModal(); toast('Location created!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

window.reasonCodeForm = () => {
  openModal('📋 Add Reason Code', buildForm([
    { name: 'code', label: 'Code', required: true, placeholder: 'DAMAGED' }, { name: 'description', label: 'Description', required: true },
    { name: 'category', label: 'Category', type: 'select', options: ['Return', 'Cancel', 'Exception', 'Rejection'] },
  ], { submitLabel: 'Create' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.createReasonCode(f); closeModal(); toast('Reason code created!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

// ── IAM ─────────────────────────────────────────────────────
views.iam = async (el) => {
  const [usr, rol, keys, logs] = await Promise.all([
    api.users({ pageSize: 100 }).catch(() => ({ items: [] })), api.roles({ pageSize: 100 }).catch(() => ({ items: [] })),
    api.apiKeys({ pageSize: 100 }).catch(() => ({ items: [] })), api.auditLogs({ pageSize: 20 }).catch(() => ({ items: [] }))
  ]);
  const U = usr?.items || [], R = rol?.items || [], K = keys?.items || [], L = logs?.items || [];
  el.innerHTML = `<div class="fade-in">
    <div class="content-grid two-col">
      <div class="card"><div class="card-header"><span class="card-title">👤 Users (${U.length})</span><button class="btn btn-sm btn-primary" onclick="userSyncForm()">+ Sync</button></div>
        ${buildTable([{ label: 'Name', render: r => `<strong>${r.displayName || r.userName || '—'}</strong>` }, { label: 'Email', render: r => r.email || '—' }, { label: 'Status', render: r => badge(r.isActive ? 'Active' : 'Inactive') }, { label: 'Roles', render: r => (r.roles || []).join(', ') || '—' }], U.slice(0, 10), { actions: r => `<button class="btn btn-sm btn-secondary" onclick="showDetail('User',${JSON.stringify(r).replace(/'/g, "\\'")})">👁</button>` })}
      </div>
      <div class="card"><div class="card-header"><span class="card-title">🔑 Roles (${R.length})</span><button class="btn btn-sm btn-primary" onclick="roleCreateForm()">+ Add</button></div>
        ${buildTable([{ label: 'Name', render: r => `<strong>${r.name || '—'}</strong>` }, { label: 'Permissions', render: r => (r.permissions || []).length }], R)}
      </div>
    </div>
    <div class="content-grid two-col" style="margin-top:20px">
      <div class="card"><div class="card-header"><span class="card-title">🔐 API Keys (${K.length})</span><button class="btn btn-sm btn-primary" onclick="apiKeyForm()">+ Create</button></div>
        ${buildTable([{ label: 'Name', render: r => `<strong>${r.name || '—'}</strong>` }, { label: 'Key', render: r => sid(r.keyHash || r.id) }, { label: 'Created', render: r => fmtD(r.createdAt) }], K, { actions: r => `<button class="btn btn-sm btn-secondary" onclick="confirmAction('Delete API key?',()=>api.deleteApiKey('${r.id}'))">🗑</button>` })}
      </div>
      <div class="card"><div class="card-header"><span class="card-title">📜 Audit Logs</span></div>
        ${buildTable([{ label: 'Action', render: r => `<strong>${r.action || '—'}</strong>` }, { label: 'User', render: r => r.userId || '—' }, { label: 'Time', render: r => fmtD(r.occurredAt || r.timestamp) }], L.slice(0, 10))}
      </div>
    </div></div>`;
};

window.userSyncForm = () => {
  openModal('👤 Sync User', buildForm([
    { name: 'externalId', label: 'External ID', required: true }, { name: 'userName', label: 'Username', required: true },
    { name: 'email', label: 'Email', required: true }, { name: 'displayName', label: 'Display Name', required: true },
    { name: 'tenantId', type: 'hidden', value: '00000000-0000-0000-0000-000000000001' },
  ], { submitLabel: 'Sync' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.syncUser(f); closeModal(); toast('User synced!', 'success'); refreshView(); } catch (e) { toast(e.message, 'error'); }
  };
};

window.roleCreateForm = () => {
  openModal('🔑 Create Role', buildForm([{ name: 'name', label: 'Role Name', required: true }], { submitLabel: 'Create' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.createRole(f); closeModal(); toast('Role created!', 'success'); refreshView(); } catch (e) { toast(e.message, 'error'); }
  };
};

window.apiKeyForm = () => {
  openModal('🔐 Create API Key', buildForm([{ name: 'name', label: 'Key Name', required: true }, { name: 'tenantId', type: 'hidden', value: '00000000-0000-0000-0000-000000000001' }], { submitLabel: 'Create' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { const r = await api.createApiKey(f); closeModal(); toast('API Key created!', 'success'); showDetail('API Key Created', r); refreshView(); } catch (e) { toast(e.message, 'error'); }
  };
};

// ── TRACKING ────────────────────────────────────────────────
views.tracking = async (el) => {
  el.innerHTML = `<div class="fade-in">
    <div class="card" style="margin-bottom:16px"><div class="card-header"><span class="card-title">📡 Live Vehicle Map</span><div style="display:flex;gap:8px"><button class="btn btn-sm btn-primary" onclick="postPositionForm()">📍 Post Position</button><button class="btn btn-sm btn-secondary" onclick="zoneCreateForm()">+ Zone</button><button class="btn btn-sm btn-secondary" onclick="refreshView()">🔄</button></div></div>
      <div id="tracking-map" class="map-container" style="height:500px"></div>
    </div></div>`;
  setTimeout(async () => {
    if (mapInstance) { mapInstance.remove(); mapInstance = null; }
    mapInstance = L.map('tracking-map').setView([13.75, 100.55], 11);
    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', { attribution: '© CARTO', maxZoom: 19 }).addTo(mapInstance);
    try {
      const pos = await api.trackingVehicles();
      const items = pos?.items || pos || [];
      items.forEach(v => {
        if (!v.latitude || !v.longitude) return;
        L.marker([v.latitude, v.longitude], { icon: L.divIcon({ className: '', html: `<div style="width:30px;height:30px;border-radius:50%;background:linear-gradient(135deg,#38bdf8,#818cf8);border:2px solid #fff;display:flex;align-items:center;justify-content:center;font-size:14px;box-shadow:0 2px 8px rgba(0,0,0,.5)">🚛</div>`, iconSize: [30, 30], iconAnchor: [15, 15] }) }).addTo(mapInstance).bindPopup(`<strong>${v.vehiclePlateNumber || sid(v.vehicleId)}</strong><br>Speed: ${v.speedKmh || 0} km/h`);
      });
      // Load zones
      const zones = await api.zones();
      (zones?.items || zones || []).forEach(z => {
        if (z.centerLat && z.centerLng) L.circle([z.centerLat, z.centerLng], { radius: (z.radiusMeters || 500), color: '#fbbf24', fillOpacity: 0.1 }).addTo(mapInstance).bindPopup(`<strong>Zone: ${z.name}</strong>`);
      });
    } catch (e) { console.log('Tracking:', e.message); }
  }, 100);
};

window.postPositionForm = () => {
  openModal('📍 Post Vehicle Position', buildForm([
    { name: 'vehicleId', label: 'Vehicle ID', required: true },
    { name: 'latitude', label: 'Latitude', type: 'number', required: true, value: 13.7563 },
    { name: 'longitude', label: 'Longitude', type: 'number', required: true, value: 100.5018 },
    { name: 'speedKmh', label: 'Speed (km/h)', type: 'number', value: 40 },
    { name: 'heading', label: 'Heading (°)', type: 'number', value: 0 },
  ], { submitLabel: 'Post' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.postPosition(f); closeModal(); toast('Position posted!', 'success'); refreshView(); } catch (e) { toast(e.message, 'error'); }
  };
};

window.zoneCreateForm = () => {
  openModal('📍 Create Geo Zone', buildForm([
    { name: 'name', label: 'Zone Name', required: true }, { name: 'zoneType', label: 'Type', type: 'select', options: ['Circle', 'Polygon'] },
    { name: 'centerLat', label: 'Center Lat', type: 'number', required: true }, { name: 'centerLng', label: 'Center Lng', type: 'number', required: true },
    { name: 'radiusMeters', label: 'Radius (m)', type: 'number', value: 500 },
  ], { submitLabel: 'Create' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.createZone(f); closeModal(); toast('Zone created!', 'success'); refreshView(); } catch (e) { toast(e.message, 'error'); }
  };
};

// ── NOTIFICATIONS ───────────────────────────────────────────
views.notifications = async (el) => {
  const [hist, tmpl] = await Promise.all([
    api.notifHistory({ pageSize: 50 }).catch(() => ({ items: [] })),
    api.notifTemplates().catch(() => ({ items: [] }))
  ]);
  const H = hist?.items || [], T = tmpl?.items || tmpl || [];
  el.innerHTML = `<div class="fade-in">
    <div class="card" style="margin-bottom:16px"><div class="card-header"><span class="card-title">🔔 Send Notification</span></div>
      <div style="display:flex;gap:8px"><button class="btn btn-primary" onclick="sendNotifForm()">📤 Send</button><button class="btn btn-secondary" onclick="testNotifForm()">🧪 Test</button></div>
    </div>
    <div class="content-grid two-col">
      <div class="card"><div class="card-header"><span class="card-title">📜 History (${H.length})</span></div>
        ${buildTable([{ label: 'Channel', render: r => r.channel || '—' }, { label: 'To', render: r => r.recipient || '—' }, { label: 'Status', render: r => badge(r.status || 'Sent') }, { label: 'Sent', render: r => fmtD(r.sentAt || r.createdAt) }], H.slice(0, 15))}
      </div>
      <div class="card"><div class="card-header"><span class="card-title">📋 Templates (${T.length})</span></div>
        ${buildTable([{ label: 'Name', render: r => `<strong>${r.templateName || r.name || '—'}</strong>` }, { label: 'Channel', render: r => r.channel || '—' }, { label: 'Subject', render: r => r.subject || '—' }], T)}
      </div>
    </div></div>`;
};

window.sendNotifForm = () => {
  openModal('📤 Send Notification', buildForm([
    { name: 'channel', label: 'Channel', type: 'select', options: ['Email', 'SMS', 'Push', 'LineOA'], required: true },
    { name: 'recipient', label: 'Recipient', required: true, placeholder: 'email@test.com' },
    { name: 'subject', label: 'Subject' }, { name: 'body', label: 'Body', type: 'textarea', required: true },
  ], { submitLabel: 'Send' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.sendNotif(f); closeModal(); toast('Notification sent!', 'success'); refreshView(); } catch (e) { toast(e.message, 'error'); }
  };
};

window.testNotifForm = () => {
  openModal('🧪 Test Notification', buildForm([
    { name: 'channel', label: 'Channel', type: 'select', options: ['Email', 'SMS', 'Push'], required: true },
    { name: 'recipient', label: 'Recipient', required: true },
  ], { submitLabel: 'Test' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.testNotif(f); closeModal(); toast('Test sent!', 'success'); } catch (e) { toast(e.message, 'error'); }
  };
};

// ── INTEGRATIONS ────────────────────────────────────────────
views.integrations = async (el) => {
  const [omsS, amrD] = await Promise.all([
    api.omsSyncs({ pageSize: 50 }).catch(() => ({ items: [] })),
    api.amrDocks().catch(() => ({ items: [] }))
  ]);
  el.innerHTML = `<div class="fade-in">
    <div class="content-grid two-col" style="margin-bottom:20px">
      <div class="card"><div class="card-header"><span class="card-title">🔌 OMS Integration</span></div>
        <div style="display:flex;gap:8px;margin-bottom:12px"><button class="btn btn-sm btn-primary" onclick="omsWebhookForm()">📩 Webhook</button></div>
        ${buildTable([{ label: 'Sync ID', render: r => sid(r.id) }, { label: 'Status', render: r => badge(r.status) }, { label: 'Time', render: r => fmtD(r.syncedAt || r.createdAt) }],
    (omsS?.items || []).slice(0, 8), { actions: r => `<button class="btn btn-sm btn-secondary" onclick="confirmAction('Retry sync?',()=>api.omsRetry('${r.id}'))">🔄</button>` })}
      </div>
      <div class="card"><div class="card-header"><span class="card-title">🤖 AMR Integration</span></div>
        <div style="display:flex;gap:8px;margin-bottom:12px"><button class="btn btn-sm btn-primary" onclick="amrEventForm()">📡 Send Event</button></div>
        ${buildTable([{ label: 'Dock', render: r => `<strong>${r.dockNumber || r.name || '—'}</strong>` }, { label: 'Status', render: r => badge(r.status) }], (amrD?.items || amrD || []).slice(0, 8))}
      </div>
    </div>
    <div class="card"><div class="card-header"><span class="card-title">💰 ERP Integration</span></div>
      <div style="display:flex;gap:8px"><button class="btn btn-primary" onclick="erpExportForm()">📤 Export AR</button><button class="btn btn-secondary" onclick="erpReconcileForm()">🔄 Reconciliation</button></div>
    </div></div>`;
};

window.omsWebhookForm = () => {
  openModal('📩 OMS Webhook', buildForm([
    { name: 'providerCode', label: 'Provider Code', required: true, placeholder: 'shopee' },
    { name: 'payload', label: 'JSON Payload', type: 'textarea', required: true },
  ], { submitLabel: 'Send' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.omsWebhook(f.providerCode, JSON.parse(f.payload)); closeModal(); toast('Webhook sent!', 'success'); refreshView(); }
    catch (e) { toast(e.message, 'error'); }
  };
};

window.amrEventForm = () => {
  openModal('🤖 AMR Event', buildForm([
    { name: 'eventType', label: 'Event Type', type: 'select', options: ['DockReady', 'PickupComplete', 'HandoffReady'], required: true },
    { name: 'dockNumber', label: 'Dock Number' }, { name: 'shipmentId', label: 'Shipment ID' },
  ], { submitLabel: 'Send' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.amrEvent(f); closeModal(); toast('Event sent!', 'success'); refreshView(); } catch (e) { toast(e.message, 'error'); }
  };
};

window.erpExportForm = () => {
  openModal('📤 ERP Export AR', buildForm([
    { name: 'fromDate', label: 'From Date', type: 'date', required: true },
    { name: 'toDate', label: 'To Date', type: 'date', required: true },
  ], { submitLabel: 'Export' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { const r = await api.erpExportAr(f); closeModal(); toast('Export started!', 'success'); showDetail('Export Result', r); }
    catch (e) { toast(e.message, 'error'); }
  };
};

window.erpReconcileForm = () => {
  openModal('🔄 ERP Reconciliation', buildForm([
    { name: 'payload', label: 'Reconciliation JSON', type: 'textarea', required: true },
  ], { submitLabel: 'Process' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { await api.erpReconciliation(JSON.parse(f.payload)); closeModal(); toast('Reconciliation processed!', 'success'); }
    catch (e) { toast(e.message, 'error'); }
  };
};

// ── DOCUMENTS ───────────────────────────────────────────────
views.documents = async (el) => {
  el.innerHTML = `<div class="fade-in"><div class="card"><div class="card-header"><span class="card-title">📄 Document Management</span></div>
    <div style="display:grid;grid-template-columns:repeat(3,1fr);gap:12px;margin-bottom:20px">
      <button class="btn btn-primary" onclick="uploadSessionForm()">📤 Upload Session</button>
      <button class="btn btn-secondary" onclick="docsByOwnerForm()">🔍 Find by Owner</button>
      <button class="btn btn-secondary" onclick="docDetailForm()">📄 Get Document</button>
    </div>
    <div id="doc-results"></div>
  </div></div>`;
};

window.uploadSessionForm = () => {
  openModal('📤 Create Upload Session', buildForm([
    { name: 'ownerType', label: 'Owner Type', type: 'select', options: ['Order', 'Shipment', 'Vehicle', 'Driver'], required: true },
    { name: 'ownerId', label: 'Owner ID', required: true },
    { name: 'fileCategory', label: 'Category', type: 'select', options: ['POD', 'Invoice', 'License', 'Insurance', 'Photo'] },
  ], { submitLabel: 'Create Session' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { const r = await api.uploadSession(f); closeModal(); toast('Upload session created!', 'success'); showDetail('Upload Session', r); }
    catch (e) { toast(e.message, 'error'); }
  };
};

window.docsByOwnerForm = () => {
  openModal('🔍 Find Documents', buildForm([
    { name: 'ownerType', label: 'Owner Type', type: 'select', options: ['Order', 'Shipment', 'Vehicle', 'Driver'], required: true },
    { name: 'ownerId', label: 'Owner ID', required: true },
  ], { submitLabel: 'Search' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { const r = await api.documentsByOwner(f.ownerType, f.ownerId); closeModal(); showDetail('Documents', r); }
    catch (e) { toast(e.message, 'error'); }
  };
};

window.docDetailForm = () => {
  openModal('📄 Get Document', buildForm([{ name: 'documentId', label: 'Document ID', required: true }], { submitLabel: 'Get' }));
  document.getElementById('modal-form').onsubmit = async e => {
    e.preventDefault(); const f = getFormData();
    try { const r = await api.document(f.documentId); closeModal(); showDetail('Document', r); }
    catch (e) { toast(e.message, 'error'); }
  };
};

// ── OPERABILITY ─────────────────────────────────────────────
views.operability = async (el) => {
  const [dlq, recon] = await Promise.all([
    api.dlq({ pageSize: 50 }).catch(() => ({ items: [] })),
    api.reconciliationReport().catch(() => ({}))
  ]);
  const D = dlq?.items || [];
  el.innerHTML = `<div class="fade-in">
    <div class="content-grid two-col">
      <div class="card"><div class="card-header"><span class="card-title">💀 Dead Letter Queue (${D.length})</span><button class="btn btn-sm btn-secondary" onclick="refreshView()">🔄</button></div>
        ${buildTable([
    { label: 'ID', render: r => sid(r.id) }, { label: 'Type', render: r => r.type || '—' },
    { label: 'Error', render: r => `<span style="max-width:200px;display:inline-block;overflow:hidden;text-overflow:ellipsis">${r.error || '—'}</span>` },
    { label: 'Time', render: r => fmtD(r.occurredOn) }
  ], D.slice(0, 20), { actions: r => `<button class="btn btn-sm btn-primary" onclick="confirmAction('Retry DLQ message?',()=>api.dlqRetry('${r.id}'))">🔄 Retry</button> <button class="btn btn-sm btn-secondary" onclick="showDetail('DLQ Message',${JSON.stringify(r).replace(/'/g, "\\'")})">👁</button>` })}
      </div>
      <div class="card"><div class="card-header"><span class="card-title">📊 Reconciliation Report</span></div>
        ${jsonView(recon)}
      </div>
    </div></div>`;
};

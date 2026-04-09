// ═══════════════════════════════════════════════════════════════
// TMS Dashboard — Complete API Client (All 114 Endpoints)
// ═══════════════════════════════════════════════════════════════

const API = '';

class TmsApi {
  async _req(method, path, body) {
    const opts = { method, headers: { 'Content-Type': 'application/json' } };
    if (body) opts.body = JSON.stringify(body);
    const res = await fetch(`${API}${path}`, opts);
    if (res.status === 204) return null;
    if (!res.ok) { const t = await res.text(); throw new Error(`HTTP ${res.status}: ${t.substring(0, 300)}`); }
    const ct = res.headers.get('content-type') || '';
    return ct.includes('json') ? res.json() : res.text();
  }
  _qs(p) { const q = new URLSearchParams(); if(p) Object.entries(p).forEach(([k,v]) => { if(v!==undefined&&v!==null&&v!=='') q.set(k,v); }); const s=q.toString(); return s?`?${s}`:''; }
  get(p, q)        { return this._req('GET', p + this._qs(q)); }
  post(p, b)       { return this._req('POST', p, b); }
  put(p, b)        { return this._req('PUT', p, b); }
  del(p)           { return this._req('DELETE', p); }

  // ── Health ──────────────────────────────────────────────────
  health()                              { return this.get('/health'); }

  // ── Orders (8) ──────────────────────────────────────────────
  orders(q)                             { return this.get('/api/orders', q); }
  order(id)                             { return this.get(`/api/orders/${id}`); }
  createOrder(d)                        { return this.post('/api/orders', d); }
  confirmOrder(id)                      { return this.put(`/api/orders/${id}/confirm`); }
  cancelOrder(id, d)                    { return this.put(`/api/orders/${id}/cancel`, d); }
  amendOrder(id, d)                     { return this.put(`/api/orders/${id}/amend`, d); }
  splitOrder(id, d)                     { return this.post(`/api/orders/${id}/split`, d); }
  autoSplitOrder(id, d)                 { return this.post(`/api/orders/${id}/split/auto`, d); }
  orderSplits(id)                       { return this.get(`/api/orders/${id}/splits`); }
  importOrders(d)                       { return this.post('/api/orders/import', d); }

  // ── Route Planning (8) ─────────────────────────────────────
  plans(q)                              { return this.get('/api/planning/plans', q); }
  plan(id)                              { return this.get(`/api/planning/plans/${id}`); }
  lockPlan(id)                          { return this.put(`/api/planning/plans/${id}/lock`); }
  updateStops(id, d)                    { return this.put(`/api/planning/plans/${id}/stops`, d); }
  optimize(d)                           { return this.post('/api/planning/optimize', d); }
  optimizeStatus(id)                    { return this.get(`/api/planning/optimize/${id}`); }
  planWithSplit(d)                      { return this.post('/api/planning/plan-with-split', d); }
  reoptimizeTrip(id, d)                 { return this.post(`/api/planning/trips/${id}/reoptimize`, d); }

  // ── Trips / Dispatch (9) ────────────────────────────────────
  trips(q)                              { return this.get('/api/trips', q); }
  trip(id)                              { return this.get(`/api/trips/${id}`); }
  createTrip(d)                         { return this.post('/api/trips', d); }
  tripBoard()                           { return this.get('/api/trips/board'); }
  assignTrip(id, d)                     { return this.put(`/api/trips/${id}/assign`, d); }
  dispatchTrip(id)                      { return this.put(`/api/trips/${id}/dispatch`); }
  completeTrip(id)                      { return this.put(`/api/trips/${id}/complete`); }
  cancelTrip(id, d)                     { return this.put(`/api/trips/${id}/cancel`, d); }
  reassignTrip(id, d)                   { return this.put(`/api/trips/${id}/reassign`, d); }
  addTripStops(id, d)                   { return this.post(`/api/trips/${id}/stops`, d); }

  // ── Shipments (10) ──────────────────────────────────────────
  shipments(q)                          { return this.get('/api/shipments', q); }
  shipment(id)                          { return this.get(`/api/shipments/${id}`); }
  driverShipments()                     { return this.get('/api/shipments/driver/today'); }
  pickupShipment(id, d)                 { return this.put(`/api/shipments/${id}/pickup`, d); }
  arriveShipment(id, d)                 { return this.put(`/api/shipments/${id}/arrive`, d); }
  deliverShipment(id, d)               { return this.put(`/api/shipments/${id}/deliver`, d); }
  partialDeliverShipment(id, d)         { return this.put(`/api/shipments/${id}/partial-deliver`, d); }
  exceptionShipment(id, d)             { return this.put(`/api/shipments/${id}/exception`, d); }
  rejectShipment(id, d)                { return this.put(`/api/shipments/${id}/reject`, d); }
  approvePod(id, d)                    { return this.put(`/api/shipments/${id}/pod/approve`, d); }

  // ── POD (4) ─────────────────────────────────────────────────
  podEvaluateGet(sid)                   { return this.get(`/api/execution/pod/${sid}/evaluate`); }
  podEvaluatePost(sid, d)               { return this.post(`/api/execution/pod/${sid}/evaluate`, d); }
  podSubmit(sid, d)                     { return this.put(`/api/execution/pod/${sid}/submit`, d); }
  podAttachments(sid, d)                { return this.post(`/api/execution/pod/${sid}/attachments`, d); }
  podGeneratePdf(sid)                   { return this.post(`/api/execution/pod/${sid}/generate-pdf`); }

  // ── Vehicles (7) ────────────────────────────────────────────
  vehicles(q)                           { return this.get('/api/vehicles', q); }
  vehicle(id)                           { return this.get(`/api/vehicles/${id}`); }
  createVehicle(d)                      { return this.post('/api/vehicles', d); }
  updateVehicle(id, d)                  { return this.put(`/api/vehicles/${id}`, d); }
  vehicleStatus(id, d)                  { return this.put(`/api/vehicles/${id}/status`, d); }
  vehicleMaintenance(id, d)             { return this.post(`/api/vehicles/${id}/maintenance`, d); }
  availableVehicles()                   { return this.get('/api/vehicles/available'); }
  vehicleExpiryAlerts()                 { return this.get('/api/vehicles/expiry-alerts'); }

  // ── Drivers (6) ─────────────────────────────────────────────
  drivers(q)                            { return this.get('/api/drivers', q); }
  driver(id)                            { return this.get(`/api/drivers/${id}`); }
  createDriver(d)                       { return this.post('/api/drivers', d); }
  updateDriver(id, d)                   { return this.put(`/api/drivers/${id}`, d); }
  driverStatus(id, d)                   { return this.put(`/api/drivers/${id}/status`, d); }
  driverHos(id)                         { return this.get(`/api/drivers/${id}/hos`); }
  availableDrivers()                    { return this.get('/api/drivers/available'); }
  driverExpiryAlerts()                  { return this.get('/api/drivers/expiry-alerts'); }

  // ── Master Data (9) ─────────────────────────────────────────
  customers(q)                          { return this.get('/api/master/customers', q); }
  customer(id)                          { return this.get(`/api/master/customers/${id}`); }
  createCustomer(d)                     { return this.post('/api/master/customers', d); }
  updateCustomer(id, d)                 { return this.put(`/api/master/customers/${id}`, d); }
  deleteCustomer(id)                    { return this.del(`/api/master/customers/${id}`); }
  locations(q)                          { return this.get('/api/master/locations', q); }
  createLocation(d)                     { return this.post('/api/master/locations', d); }
  updateLocation(id, d)                 { return this.put(`/api/master/locations/${id}`, d); }
  searchLocations(q)                    { return this.get('/api/master/locations/search', q); }
  provinces()                           { return this.get('/api/master/provinces'); }
  reasonCodes(q)                        { return this.get('/api/master/reason-codes', q); }
  createReasonCode(d)                   { return this.post('/api/master/reason-codes', d); }
  holidays(q)                           { return this.get('/api/master/holidays', q); }
  createHoliday(d)                      { return this.post('/api/master/holidays', d); }

  // ── IAM (9) ─────────────────────────────────────────────────
  users(q)                              { return this.get('/api/iam/users', q); }
  user(id)                              { return this.get(`/api/iam/users/${id}`); }
  syncUser(d)                           { return this.post('/api/iam/users/sync', d); }
  deactivateUser(id)                    { return this.put(`/api/iam/users/${id}/deactivate`); }
  updateUserRoles(id, d)                { return this.put(`/api/iam/users/${id}/roles`, d); }
  roles(q)                              { return this.get('/api/iam/roles', q); }
  createRole(d)                         { return this.post('/api/iam/roles', d); }
  updateRolePermissions(id, d)          { return this.put(`/api/iam/roles/${id}/permissions`, d); }
  apiKeys(q)                            { return this.get('/api/iam/api-keys', q); }
  createApiKey(d)                       { return this.post('/api/iam/api-keys', d); }
  deleteApiKey(id)                      { return this.del(`/api/iam/api-keys/${id}`); }
  auditLogs(q)                          { return this.get('/api/iam/audit-logs', q); }

  // ── Tracking (6) ────────────────────────────────────────────
  trackingVehicles()                    { return this.get('/api/tracking/vehicles'); }
  vehicleHistory(id, q)                 { return this.get(`/api/tracking/vehicles/${id}/history`, q); }
  postPosition(d)                       { return this.post('/api/tracking/positions', d); }
  orderEta(orderId)                     { return this.get(`/api/tracking/orders/${orderId}/eta`); }
  zones(q)                              { return this.get('/api/tracking/zones', q); }
  createZone(d)                         { return this.post('/api/tracking/zones', d); }
  updateZone(id, d)                     { return this.put(`/api/tracking/zones/${id}`, d); }

  // ── Notifications (4) ───────────────────────────────────────
  notifHistory(q)                       { return this.get('/api/platform/notifications/history', q); }
  sendNotif(d)                          { return this.post('/api/platform/notifications/send', d); }
  notifTemplates()                      { return this.get('/api/platform/notifications/templates'); }
  testNotif(d)                          { return this.post('/api/platform/notifications/test', d); }

  // ── Documents (5) ───────────────────────────────────────────
  document(id)                          { return this.get(`/api/documents/${id}`); }
  documentsByOwner(type, ownerId)       { return this.get(`/api/documents/owners/${type}/${ownerId}`); }
  downloadUrl(id)                       { return this.get(`/api/documents/${id}/download-url`); }
  uploadSession(d)                      { return this.post('/api/documents/upload-session', d); }
  completeUpload(sid)                   { return this.post(`/api/documents/upload-session/${sid}/complete`); }
  localUpload(key, d)                   { return this.post(`/api/documents/local-upload/${key}`, d); }

  // ── Integration: OMS (3) ────────────────────────────────────
  omsSyncs(q)                           { return this.get('/api/integrations/oms/syncs', q); }
  omsRetry(id)                          { return this.post(`/api/integrations/oms/syncs/${id}/retry`); }
  omsWebhook(code, d)                   { return this.post(`/api/integrations/oms/webhook/${code}`, d); }

  // ── Integration: AMR (3) ────────────────────────────────────
  amrDocks()                            { return this.get('/api/integrations/amr/docks'); }
  amrEvent(d)                           { return this.post('/api/integrations/amr/events', d); }
  amrConfirmHandoff(id)                 { return this.put(`/api/integrations/amr/handoffs/${id}/confirm`); }

  // ── Integration: ERP (2) ────────────────────────────────────
  erpExportAr(d)                        { return this.post('/api/integrations/erp/export/ar', d); }
  erpReconciliation(d)                  { return this.post('/api/integrations/erp/reconciliation', d); }

  // ── Operability (3) ─────────────────────────────────────────
  dlq(q)                                { return this.get('/api/admin/operability/dlq', q); }
  dlqRetry(id)                          { return this.post(`/api/admin/operability/dlq/${id}/retry`); }
  reconciliationReport()                { return this.get('/api/admin/operability/reconciliation/report'); }
}

window.api = new TmsApi();

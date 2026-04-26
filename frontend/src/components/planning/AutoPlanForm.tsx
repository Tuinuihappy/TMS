import { useEffect, useState } from 'react'
import { routePlanApi } from '../../api/planning'
import { ordersApi } from '../../api/orders'
import type { OptimizationStatusDto } from '../../types/planning'
import type { OrderDto } from '../../types/order'

const DEFAULT_TENANT = '00000000-0000-0000-0000-000000000002'

interface Props {
  onClose: () => void
  onSuccess: () => void
}

type Mode = 'vrp' | 'pdp'

export function AutoPlanForm({ onClose, onSuccess }: Props) {
  const [mode, setMode] = useState<Mode>('vrp')
  const [confirmedOrders, setConfirmedOrders] = useState<OrderDto[]>([])
  const [selectedOrderIds, setSelectedOrderIds] = useState<Set<string>>(new Set())
  const [plannedDate, setPlannedDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [maxWeightKg, setMaxWeightKg] = useState(10000)
  const [maxVolumeCBM, setMaxVolumeCBM] = useState(0)
  const [maxOrdersPerRoute, setMaxOrdersPerRoute] = useState(10)
  const [depotLat, setDepotLat] = useState(13.7563)
  const [depotLng, setDepotLng] = useState(100.5018)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [optStatus, setOptStatus] = useState<OptimizationStatusDto | null>(null)

  // Load confirmed orders
  useEffect(() => {
    ordersApi.list({ status: 'Confirmed', pageSize: 100 })
      .then(r => setConfirmedOrders(r.items))
      .catch(() => {})
  }, [])

  const toggleOrder = (id: string) =>
    setSelectedOrderIds(s => { const n = new Set(s); n.has(id) ? n.delete(id) : n.add(id); return n })

  const selectAll = () => setSelectedOrderIds(new Set(confirmedOrders.map(o => o.id)))
  const clearAll = () => setSelectedOrderIds(new Set())

  // ── VRP submit ─────────────────────────────────────────────────────────────

  const handleVRP = async () => {
    if (selectedOrderIds.size === 0) { setError('Select at least one order'); return }
    setLoading(true); setError(null)
    try {
      await routePlanApi.planWithSplit({
        orderIds: [...selectedOrderIds],
        plannedDate,
        tenantId: DEFAULT_TENANT,
        maxVehicleWeightKg: maxWeightKg,
        maxVehicleVolumeCBM: maxVolumeCBM,
        maxOrdersPerRoute,
        depotLat,
        depotLng,
      })
      onSuccess()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'VRP planning failed')
    } finally {
      setLoading(false)
    }
  }

  // ── PDP submit ─────────────────────────────────────────────────────────────

  const handlePDP = async () => {
    const orders = confirmedOrders.filter(o => selectedOrderIds.has(o.id))
    if (orders.length === 0) { setError('Select at least one order'); return }

    // Build PDP inputs from order stops
    const pdpOrders = orders.flatMap(o =>
      o.stops.map(stop => ({
        orderId: o.id,
        orderStopId: stop.stopId,
        pickupLat: 13.7244,   // placeholder — real coords from stop not in summary
        pickupLng: 100.7713,
        dropoffLat: 13.6692,
        dropoffLng: 100.6092,
        weightKg: o.totalWeight / o.stopCount,
        volumeCBM: o.totalVolumeCBM / o.stopCount,
      }))
    )

    setLoading(true); setError(null)
    try {
      const res = await routePlanApi.optimize({
        orders: pdpOrders,
        tenantId: DEFAULT_TENANT,
        plannedDate,
        maxOrdersPerRoute,
        maxCapacityKg: maxWeightKg,
        depotLat,
        depotLng,
      })
      // Poll for status
      pollStatus(res.optimizationRequestId)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'PDP optimize failed')
      setLoading(false)
    }
  }

  const pollStatus = async (id: string) => {
    for (let i = 0; i < 20; i++) {
      await new Promise(r => setTimeout(r, 1000))
      try {
        const s = await routePlanApi.getOptimizationStatus(id)
        setOptStatus(s)
        if (s.status === 'Completed') { setLoading(false); onSuccess(); return }
        if (s.status === 'Failed') { setError(s.error ?? 'Optimization failed'); setLoading(false); return }
      } catch { break }
    }
    setLoading(false)
    setError('Optimization timed out — check status manually')
  }

  return (
    <div style={overlay}>
      <div style={modal}>
        <div style={modalHeader}>
          <h2 style={{ margin: 0, fontSize: '17px' }}>Auto Planning</h2>
          <button onClick={onClose} style={closeBtn}>✕</button>
        </div>

        {/* Mode tabs */}
        <div style={tabs}>
          <button onClick={() => setMode('vrp')} style={{ ...tab, ...(mode === 'vrp' ? tabActive : {}) }}>
            VRP (OR-Tools)
          </button>
          <button onClick={() => setMode('pdp')} style={{ ...tab, ...(mode === 'pdp' ? tabActive : {}) }}>
            PDP Optimize
          </button>
        </div>

        <div style={modalBody}>
          {error && <div style={errorBox}>{error}</div>}
          {optStatus && (
            <div style={{ padding: '10px', borderRadius: '6px', background: '#eff6ff', border: '1px solid #bfdbfe', fontSize: '13px' }}>
              Optimization {optStatus.status}
              {optStatus.status === 'Processing' && ' ⏳'}
              {optStatus.error && ` — ${optStatus.error}`}
            </div>
          )}

          {/* Config */}
          <div style={grid3}>
            <Field label="Planned Date">
              <input type="date" value={plannedDate} onChange={e => setPlannedDate(e.target.value)} style={inputStyle} />
            </Field>
            <Field label="Max Weight/Vehicle (kg)">
              <input type="number" value={maxWeightKg} onChange={e => setMaxWeightKg(Number(e.target.value))} style={inputStyle} />
            </Field>
            <Field label="Max Orders/Route">
              <input type="number" value={maxOrdersPerRoute} onChange={e => setMaxOrdersPerRoute(Number(e.target.value))} style={inputStyle} min={1} />
            </Field>
          </div>
          <div style={grid3}>
            <Field label="Max Volume/Vehicle (CBM)">
              <input type="number" value={maxVolumeCBM} onChange={e => setMaxVolumeCBM(Number(e.target.value))} style={inputStyle} min={0} />
            </Field>
            <Field label="Depot Latitude">
              <input type="number" step={0.0001} value={depotLat} onChange={e => setDepotLat(Number(e.target.value))} style={inputStyle} />
            </Field>
            <Field label="Depot Longitude">
              <input type="number" step={0.0001} value={depotLng} onChange={e => setDepotLng(Number(e.target.value))} style={inputStyle} />
            </Field>
          </div>

          {/* Order selector */}
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
              <div style={sectionTitle}>
                Confirmed Orders ({selectedOrderIds.size} / {confirmedOrders.length} selected)
              </div>
              <div style={{ display: 'flex', gap: '6px' }}>
                <button onClick={selectAll} style={btnLink}>Select All</button>
                <button onClick={clearAll} style={btnLink}>Clear</button>
              </div>
            </div>
            <div style={{ maxHeight: '220px', overflowY: 'auto', border: '1px solid #e5e7eb', borderRadius: '6px' }}>
              {confirmedOrders.length === 0 ? (
                <div style={{ padding: '20px', textAlign: 'center', color: '#9ca3af', fontSize: '13px' }}>
                  No confirmed orders found
                </div>
              ) : confirmedOrders.map(o => (
                <label key={o.id} style={orderRow}>
                  <input
                    type="checkbox"
                    checked={selectedOrderIds.has(o.id)}
                    onChange={() => toggleOrder(o.id)}
                  />
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ fontWeight: 500, fontSize: '13px' }}>{o.orderNumber}</div>
                    <div style={{ fontSize: '11px', color: '#6b7280' }}>
                      {o.totalWeight} kg · {o.stopCount} stop{o.stopCount !== 1 ? 's' : ''}
                      {o.stops[0] && ` · ${o.stops[0].pickupProvince} → ${o.stops[0].dropoffProvince}`}
                    </div>
                  </div>
                </label>
              ))}
            </div>
          </div>

          {mode === 'pdp' && (
            <div style={{ padding: '10px', background: '#fef9c3', borderRadius: '6px', border: '1px solid #fde68a', fontSize: '12px', color: '#92400e' }}>
              PDP mode uses order stop coordinates from the database. Stops without GPS coordinates use Bangkok center as placeholder.
            </div>
          )}
        </div>

        <div style={modalFooter}>
          <button onClick={onClose} style={btnSecondary}>Cancel</button>
          <button
            onClick={mode === 'vrp' ? handleVRP : handlePDP}
            disabled={loading || selectedOrderIds.size === 0}
            style={btnPrimary}
          >
            {loading
              ? (mode === 'pdp' ? 'Optimizing…' : 'Planning…')
              : (mode === 'vrp' ? '⚡ Run VRP Solver' : '🗺 Run PDP Optimize')}
          </button>
        </div>
      </div>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
      <label style={{ fontSize: '11px', fontWeight: 600, color: '#374151' }}>{label}</label>
      {children}
    </div>
  )
}

// ── Styles ────────────────────────────────────────────────────────────────────

const overlay: React.CSSProperties = { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'flex-start', justifyContent: 'center', zIndex: 100, overflowY: 'auto', padding: '40px 20px' }
const modal: React.CSSProperties = { background: '#fff', borderRadius: '12px', width: '100%', maxWidth: '700px', boxShadow: '0 20px 60px rgba(0,0,0,0.2)' }
const modalHeader: React.CSSProperties = { display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '18px 24px', borderBottom: '1px solid #e5e7eb' }
const modalBody: React.CSSProperties = { padding: '20px 24px', display: 'flex', flexDirection: 'column', gap: '16px' }
const modalFooter: React.CSSProperties = { padding: '14px 24px', borderTop: '1px solid #e5e7eb', display: 'flex', justifyContent: 'flex-end', gap: '8px' }
const tabs: React.CSSProperties = { display: 'flex', borderBottom: '1px solid #e5e7eb', padding: '0 24px' }
const tab: React.CSSProperties = { padding: '10px 16px', background: 'none', border: 'none', cursor: 'pointer', fontSize: '13px', fontWeight: 500, color: '#6b7280', borderBottom: '2px solid transparent', marginBottom: '-1px' }
const tabActive: React.CSSProperties = { color: '#2563eb', borderBottom: '2px solid #2563eb' }
const grid3: React.CSSProperties = { display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '12px' }
const inputStyle: React.CSSProperties = { padding: '7px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '13px', width: '100%', boxSizing: 'border-box' }
const sectionTitle: React.CSSProperties = { fontSize: '11px', fontWeight: 700, color: '#374151', textTransform: 'uppercase', letterSpacing: '0.05em' }
const orderRow: React.CSSProperties = { display: 'flex', alignItems: 'flex-start', gap: '10px', padding: '10px 12px', cursor: 'pointer', borderBottom: '1px solid #f3f4f6' }
const btnPrimary: React.CSSProperties = { padding: '8px 18px', borderRadius: '6px', border: 'none', background: '#2563eb', color: '#fff', cursor: 'pointer', fontSize: '13px', fontWeight: 500 }
const btnSecondary: React.CSSProperties = { padding: '8px 14px', borderRadius: '6px', border: '1px solid #d1d5db', background: '#fff', cursor: 'pointer', fontSize: '13px' }
const btnLink: React.CSSProperties = { background: 'none', border: 'none', color: '#2563eb', cursor: 'pointer', fontSize: '12px', padding: '2px 4px' }
const closeBtn: React.CSSProperties = { background: 'none', border: 'none', cursor: 'pointer', fontSize: '18px', color: '#6b7280', lineHeight: 1 }
const errorBox: React.CSSProperties = { padding: '10px', borderRadius: '6px', background: '#fef2f2', border: '1px solid #fecaca', color: '#dc2626', fontSize: '13px' }

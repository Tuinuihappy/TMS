import { useState } from 'react'
import { routePlanApi } from '../../api/planning'
import type { RoutePlanDto, RouteStopDto } from '../../types/planning'

interface Props {
  plan: RoutePlanDto
  onClose: () => void
  onRefresh: () => void
}

const STOP_TYPE_COLOR: Record<string, string> = { Pickup: '#2563eb', Dropoff: '#16a34a' }

export function RoutePlanDetail({ plan, onClose, onRefresh }: Props) {
  const [stops, setStops] = useState<RouteStopDto[]>([...plan.stops].sort((a, b) => a.sequence - b.sequence))
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [dirty, setDirty] = useState(false)

  const canLock = plan.status === 'Draft'
  const canReorder = plan.status === 'Draft'

  const moveStop = (index: number, dir: -1 | 1) => {
    const next = [...stops]
    const target = index + dir
    if (target < 0 || target >= next.length) return
    ;[next[index], next[target]] = [next[target], next[index]]
    next.forEach((s, i) => { s.sequence = i + 1 })
    setStops(next)
    setDirty(true)
  }

  const handleSaveOrder = async () => {
    setLoading(true); setError(null)
    try {
      await routePlanApi.reorderStops(plan.id, stops.map(s => ({ stopId: s.id, newSequence: s.sequence })))
      setSuccess('Stop order saved')
      setDirty(false)
      setTimeout(() => setSuccess(null), 2000)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to reorder')
    } finally {
      setLoading(false)
    }
  }

  const handleLock = async () => {
    if (!confirm(`Lock plan ${plan.planNumber}? This will auto-create a Trip.`)) return
    setLoading(true); setError(null)
    try {
      await routePlanApi.lock(plan.id)
      setSuccess('Plan locked — Trip created automatically')
      setTimeout(() => { onRefresh(); onClose() }, 1000)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Lock failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={overlay}>
      <div style={modal}>
        {/* Header */}
        <div style={modalHeader}>
          <div>
            <h2 style={{ margin: 0, fontSize: '17px' }}>{plan.planNumber}</h2>
            <div style={{ fontSize: '12px', color: '#6b7280', marginTop: '3px' }}>
              {plan.plannedDate} · {plan.stops.length} stops · {plan.totalDistanceKm.toFixed(1)} km
              · ~{plan.estimatedTotalDurationMin} min · {plan.capacityUtilizationPercent}% capacity
            </div>
          </div>
          <button onClick={onClose} style={closeBtn}>✕</button>
        </div>

        <div style={modalBody}>
          {error   && <div style={errorBox}>{error}</div>}
          {success && <div style={successBox}>{success}</div>}

          {/* Stats row */}
          <div style={{ display: 'flex', gap: '16px', flexWrap: 'wrap', marginBottom: '4px' }}>
            {[
              ['Status', plan.status],
              ['Planned Date', plan.plannedDate],
              ['Distance', `${plan.totalDistanceKm.toFixed(1)} km`],
              ['Duration', `~${plan.estimatedTotalDurationMin} min`],
              ['Capacity', `${plan.capacityUtilizationPercent}%`],
            ].map(([label, value]) => (
              <div key={label}>
                <div style={{ fontSize: '10px', fontWeight: 700, color: '#9ca3af', textTransform: 'uppercase', letterSpacing: '0.05em' }}>{label}</div>
                <div style={{ fontSize: '13px', fontWeight: 500 }}>{value}</div>
              </div>
            ))}
          </div>

          {/* Stops list */}
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
              <div style={sectionTitle}>Stops ({stops.length})</div>
              {dirty && canReorder && (
                <button onClick={handleSaveOrder} disabled={loading} style={btnSave}>
                  {loading ? 'Saving…' : 'Save Order'}
                </button>
              )}
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              {stops.map((stop, i) => (
                <div key={stop.id} style={stopRow}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '10px', flex: 1, minWidth: 0 }}>
                    <span style={{ ...seqBadge, background: STOP_TYPE_COLOR[stop.stopType] ?? '#6b7280' }}>
                      {i + 1}
                    </span>
                    <div style={{ minWidth: 0 }}>
                      <div style={{ fontSize: '12px', fontWeight: 600, color: STOP_TYPE_COLOR[stop.stopType] ?? '#374151' }}>
                        {stop.stopType}
                      </div>
                      <div style={{ fontSize: '11px', color: '#6b7280', fontFamily: 'monospace' }}>
                        {stop.lat.toFixed(4)}, {stop.lng.toFixed(4)}
                      </div>
                      {stop.estimatedArrivalTime && (
                        <div style={{ fontSize: '11px', color: '#9ca3af' }}>
                          ETA: {new Date(stop.estimatedArrivalTime).toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit' })}
                        </div>
                      )}
                    </div>
                  </div>
                  {canReorder && (
                    <div style={{ display: 'flex', gap: '4px', flexShrink: 0 }}>
                      <button onClick={() => moveStop(i, -1)} disabled={i === 0} style={arrowBtn}>↑</button>
                      <button onClick={() => moveStop(i, 1)} disabled={i === stops.length - 1} style={arrowBtn}>↓</button>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div style={modalFooter}>
          <button onClick={onClose} style={btnSecondary}>Close</button>
          {canLock && (
            <button onClick={handleLock} disabled={loading} style={btnLock}>
              {loading ? 'Locking…' : '🔒 Lock Plan → Create Trip'}
            </button>
          )}
        </div>
      </div>
    </div>
  )
}

// ── Styles ────────────────────────────────────────────────────────────────────

const overlay: React.CSSProperties = { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'flex-start', justifyContent: 'center', zIndex: 100, overflowY: 'auto', padding: '40px 20px' }
const modal: React.CSSProperties = { background: '#fff', borderRadius: '12px', width: '100%', maxWidth: '650px', boxShadow: '0 20px 60px rgba(0,0,0,0.2)' }
const modalHeader: React.CSSProperties = { display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', padding: '20px 24px', borderBottom: '1px solid #e5e7eb' }
const modalBody: React.CSSProperties = { padding: '20px 24px', display: 'flex', flexDirection: 'column', gap: '16px' }
const modalFooter: React.CSSProperties = { padding: '14px 24px', borderTop: '1px solid #e5e7eb', display: 'flex', justifyContent: 'flex-end', gap: '8px' }
const sectionTitle: React.CSSProperties = { fontSize: '11px', fontWeight: 700, color: '#374151', textTransform: 'uppercase', letterSpacing: '0.05em' }
const stopRow: React.CSSProperties = { display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 10px', borderRadius: '6px', background: '#f9fafb', border: '1px solid #f3f4f6' }
const seqBadge: React.CSSProperties = { display: 'inline-flex', alignItems: 'center', justifyContent: 'center', width: '22px', height: '22px', borderRadius: '50%', color: '#fff', fontSize: '11px', fontWeight: 700, flexShrink: 0 }
const arrowBtn: React.CSSProperties = { padding: '3px 7px', borderRadius: '4px', border: '1px solid #d1d5db', background: '#fff', cursor: 'pointer', fontSize: '12px' }
const btnPrimary: React.CSSProperties = { padding: '8px 16px', borderRadius: '6px', border: 'none', background: '#2563eb', color: '#fff', cursor: 'pointer', fontSize: '13px', fontWeight: 500 }
const btnSecondary: React.CSSProperties = { padding: '8px 14px', borderRadius: '6px', border: '1px solid #d1d5db', background: '#fff', cursor: 'pointer', fontSize: '13px' }
const btnLock: React.CSSProperties = { ...btnPrimary, background: '#7c3aed' }
const btnSave: React.CSSProperties = { padding: '5px 12px', borderRadius: '6px', border: 'none', background: '#16a34a', color: '#fff', cursor: 'pointer', fontSize: '12px' }
const closeBtn: React.CSSProperties = { background: 'none', border: 'none', cursor: 'pointer', fontSize: '18px', color: '#6b7280', lineHeight: 1 }
const errorBox: React.CSSProperties = { padding: '10px', borderRadius: '6px', background: '#fef2f2', border: '1px solid #fecaca', color: '#dc2626', fontSize: '13px' }
const successBox: React.CSSProperties = { padding: '10px', borderRadius: '6px', background: '#f0fdf4', border: '1px solid #bbf7d0', color: '#16a34a', fontSize: '13px' }

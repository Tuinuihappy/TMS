import { useEffect, useState } from 'react'
import { tripApi, resourceApi, type VehicleSummary, type DriverSummary } from '../../api/planning'
import type { TripDto } from '../../types/planning'

interface Props {
  trip: TripDto
  onClose: () => void
  onRefresh: () => void
}

type Panel = 'detail' | 'assign' | 'cancel'

const STATUS_COLOR: Record<string, string> = {
  Created: '#6b7280', Assigned: '#2563eb', Dispatched: '#d97706',
  InTransit: '#f59e0b', Completed: '#16a34a', Cancelled: '#dc2626',
}
const STOP_TYPE_COLOR: Record<string, string> = { Pickup: '#2563eb', Dropoff: '#16a34a' }

export function TripDetail({ trip, onClose, onRefresh }: Props) {
  const [panel, setPanel] = useState<Panel>('detail')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const canAssign    = trip.status === 'Created' || trip.status === 'Assigned'
  const canDispatch  = trip.status === 'Assigned'
  const canComplete  = trip.status === 'Dispatched' || trip.status === 'InTransit'
  const canCancel    = !['Completed', 'Cancelled'].includes(trip.status)
  const canReassign  = trip.status === 'Assigned' || trip.status === 'Dispatched'

  const run = async (label: string, fn: () => Promise<void>) => {
    setLoading(true); setError(null)
    try {
      await fn()
      setSuccess(`${label} successful`)
      setTimeout(() => { onRefresh(); onClose() }, 800)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Operation failed')
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
            <h2 style={{ margin: 0, fontSize: '17px' }}>{trip.tripNumber}</h2>
            <div style={{ display: 'flex', gap: '8px', alignItems: 'center', marginTop: '4px' }}>
              <span style={{ ...badge, background: STATUS_COLOR[trip.status] ?? '#6b7280' }}>{trip.status}</span>
              <span style={{ fontSize: '12px', color: '#6b7280' }}>
                {new Date(trip.plannedDate).toLocaleDateString('th-TH')}
              </span>
            </div>
          </div>
          <button onClick={onClose} style={closeBtn}>✕</button>
        </div>

        {/* Tabs */}
        <div style={tabs}>
          {(['detail', 'assign', 'cancel'] as Panel[]).map(p => (
            <button
              key={p}
              disabled={p === 'assign' ? !canAssign && !canReassign : p === 'cancel' ? !canCancel : false}
              onClick={() => { setPanel(p); setError(null); setSuccess(null) }}
              style={{ ...tab, ...(panel === p ? tabActive : {}) }}
            >
              {p === 'detail' ? 'Details' : p === 'assign' ? 'Assign Resources' : 'Cancel'}
            </button>
          ))}
        </div>

        <div style={modalBody}>
          {error   && <div style={errorBox}>{error}</div>}
          {success && <div style={successBox}>{success}</div>}

          {panel === 'detail' && (
            <DetailPanel
              trip={trip}
              canDispatch={canDispatch}
              canComplete={canComplete}
              loading={loading}
              onDispatch={() => run('Dispatch', () => tripApi.dispatch(trip.id))}
              onComplete={() => run('Complete', () => tripApi.complete(trip.id))}
            />
          )}
          {panel === 'assign' && (
            <AssignPanel
              trip={trip}
              loading={loading}
              canReassign={canReassign && !canAssign}
              onSubmit={(vehicleId, driverId) =>
                run('Assign', () =>
                  canReassign && !canAssign
                    ? tripApi.reassign(trip.id, vehicleId, driverId)
                    : tripApi.assign(trip.id, vehicleId, driverId)
                )
              }
            />
          )}
          {panel === 'cancel' && (
            <CancelPanel
              loading={loading}
              onSubmit={reason => run('Cancel', () => tripApi.cancel(trip.id, reason))}
            />
          )}
        </div>
      </div>
    </div>
  )
}

// ── Detail Panel ─────────────────────────────────────────────────────────────

function DetailPanel({ trip, canDispatch, canComplete, loading, onDispatch, onComplete }: {
  trip: TripDto; canDispatch: boolean; canComplete: boolean; loading: boolean
  onDispatch: () => void; onComplete: () => void
}) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      {/* Stats */}
      <div style={infoGrid}>
        {[
          ['Trip ID', trip.id, true],
          ['Weight', `${trip.totalWeight.toLocaleString()} kg`],
          ['Volume', `${trip.totalVolumeCBM} CBM`],
          ['Distance', trip.totalDistanceKm ? `${trip.totalDistanceKm.toFixed(1)} km` : '—'],
          ['Est. Duration', trip.estimatedDurationMin ? `~${trip.estimatedDurationMin} min` : '—'],
          ['Created', new Date(trip.createdAt).toLocaleString('th-TH')],
        ].map(([label, value, mono]) => (
          <div key={String(label)}>
            <div style={infoLabel}>{label}</div>
            <div style={{ fontSize: '13px', fontFamily: mono ? 'monospace' : undefined, wordBreak: 'break-all' }}>{String(value)}</div>
          </div>
        ))}
      </div>

      {/* Stops */}
      {trip.stops.length > 0 && (
        <div>
          <div style={sectionTitle}>Stops ({trip.stops.length})</div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', marginTop: '8px' }}>
            {[...trip.stops].sort((a, b) => a.sequence - b.sequence).map((stop, i) => (
              <div key={stop.id} style={stopRow}>
                <span style={{ ...seqBadge, background: STOP_TYPE_COLOR[stop.type] ?? '#6b7280' }}>{i + 1}</span>
                <div style={{ flex: 1 }}>
                  <div style={{ fontSize: '12px', fontWeight: 600, color: STOP_TYPE_COLOR[stop.type] ?? '#374151' }}>
                    {stop.type}
                  </div>
                  <div style={{ fontSize: '12px', color: '#374151' }}>
                    {stop.addressName && <span>{stop.addressName} · </span>}
                    {stop.addressProvince}
                  </div>
                  {stop.windowFrom && (
                    <div style={{ fontSize: '11px', color: '#9ca3af' }}>
                      Window: {fmtTime(stop.windowFrom)} – {fmtTime(stop.windowTo)}
                    </div>
                  )}
                  {stop.arrivalAt && (
                    <div style={{ fontSize: '11px', color: '#16a34a' }}>✓ Arrived {fmtTime(stop.arrivalAt)}</div>
                  )}
                </div>
                <span style={{ ...badge, background: stopStatusColor(stop.status), fontSize: '10px' }}>
                  {stop.status}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Actions */}
      <div style={{ display: 'flex', gap: '8px', paddingTop: '8px', borderTop: '1px solid #f3f4f6' }}>
        {canDispatch && (
          <button onClick={onDispatch} disabled={loading} style={{ ...btnAction, background: '#d97706' }}>
            {loading ? 'Dispatching…' : '🚚 Dispatch'}
          </button>
        )}
        {canComplete && (
          <button onClick={onComplete} disabled={loading} style={{ ...btnAction, background: '#16a34a' }}>
            {loading ? 'Completing…' : '✓ Complete'}
          </button>
        )}
        {trip.cancelReason && (
          <div style={{ fontSize: '12px', color: '#dc2626', padding: '6px 0' }}>
            Cancelled: {trip.cancelReason}
          </div>
        )}
      </div>
    </div>
  )
}

// ── Assign Panel ─────────────────────────────────────────────────────────────

function AssignPanel({ trip, loading, canReassign, onSubmit }: {
  trip: TripDto; loading: boolean; canReassign: boolean
  onSubmit: (vehicleId: string, driverId: string) => void
}) {
  const [vehicles, setVehicles] = useState<VehicleSummary[]>([])
  const [drivers, setDrivers] = useState<DriverSummary[]>([])
  const [vehicleId, setVehicleId] = useState(trip.vehicleId ?? '')
  const [driverId, setDriverId] = useState(trip.driverId ?? '')

  useEffect(() => {
    resourceApi.vehicles().then(r => setVehicles(r.items ?? [])).catch(() => {})
    resourceApi.drivers().then(r => setDrivers(r.items ?? [])).catch(() => {})
  }, [])

  return (
    <form onSubmit={e => { e.preventDefault(); onSubmit(vehicleId, driverId) }}
      style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      {canReassign && (
        <div style={{ padding: '10px', background: '#fef3c7', borderRadius: '6px', border: '1px solid #fde68a', fontSize: '12px', color: '#92400e' }}>
          This trip is already dispatched — reassigning will override the current vehicle/driver.
        </div>
      )}

      <Field label="Vehicle *">
        <select required value={vehicleId} onChange={e => setVehicleId(e.target.value)} style={inputStyle}>
          <option value="">Select vehicle…</option>
          {vehicles.map(v => (
            <option key={v.id} value={v.id}>{v.plateNumber} ({v.category})</option>
          ))}
        </select>
      </Field>

      <Field label="Driver *">
        <select required value={driverId} onChange={e => setDriverId(e.target.value)} style={inputStyle}>
          <option value="">Select driver…</option>
          {drivers.map(d => (
            <option key={d.id} value={d.id}>{d.fullName} ({d.employeeCode})</option>
          ))}
        </select>
      </Field>

      <div style={{ display: 'flex', justifyContent: 'flex-end', paddingTop: '8px', borderTop: '1px solid #f3f4f6' }}>
        <button type="submit" disabled={loading || !vehicleId || !driverId} style={btnPrimary}>
          {loading ? 'Assigning…' : canReassign ? 'Reassign' : 'Assign'}
        </button>
      </div>
    </form>
  )
}

// ── Cancel Panel ─────────────────────────────────────────────────────────────

function CancelPanel({ loading, onSubmit }: { loading: boolean; onSubmit: (reason: string) => void }) {
  const [reason, setReason] = useState('')
  return (
    <form onSubmit={e => { e.preventDefault(); onSubmit(reason) }}
      style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      <div style={{ padding: '10px', background: '#fef2f2', borderRadius: '6px', border: '1px solid #fecaca', fontSize: '13px', color: '#991b1b' }}>
        Cancelling a trip cannot be undone.
      </div>
      <Field label="Reason *">
        <textarea required value={reason} onChange={e => setReason(e.target.value)} rows={3}
          style={{ ...inputStyle, resize: 'vertical' }} placeholder="e.g. Vehicle breakdown" />
      </Field>
      <div style={{ display: 'flex', justifyContent: 'flex-end', paddingTop: '8px', borderTop: '1px solid #f3f4f6' }}>
        <button type="submit" disabled={loading || !reason.trim()} style={{ ...btnPrimary, background: '#dc2626' }}>
          {loading ? 'Cancelling…' : 'Cancel Trip'}
        </button>
      </div>
    </form>
  )
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
      <label style={{ fontSize: '11px', fontWeight: 600, color: '#374151' }}>{label}</label>
      {children}
    </div>
  )
}

function fmtTime(d?: string) {
  if (!d) return ''
  return new Date(d).toLocaleString('th-TH', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function stopStatusColor(s: string) {
  const m: Record<string, string> = { Pending: '#6b7280', Arrived: '#d97706', Completed: '#16a34a', Skipped: '#dc2626' }
  return m[s] ?? '#6b7280'
}

// ── Styles ────────────────────────────────────────────────────────────────────

const overlay: React.CSSProperties = { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'flex-start', justifyContent: 'center', zIndex: 100, overflowY: 'auto', padding: '40px 20px' }
const modal: React.CSSProperties = { background: '#fff', borderRadius: '12px', width: '100%', maxWidth: '660px', boxShadow: '0 20px 60px rgba(0,0,0,0.2)' }
const modalHeader: React.CSSProperties = { display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', padding: '18px 24px', borderBottom: '1px solid #e5e7eb' }
const modalBody: React.CSSProperties = { padding: '20px 24px' }
const tabs: React.CSSProperties = { display: 'flex', borderBottom: '1px solid #e5e7eb', padding: '0 24px' }
const tab: React.CSSProperties = { padding: '10px 16px', background: 'none', border: 'none', cursor: 'pointer', fontSize: '13px', fontWeight: 500, color: '#6b7280', borderBottom: '2px solid transparent', marginBottom: '-1px' }
const tabActive: React.CSSProperties = { color: '#2563eb', borderBottom: '2px solid #2563eb' }
const badge: React.CSSProperties = { display: 'inline-block', padding: '2px 8px', borderRadius: '12px', fontSize: '11px', fontWeight: 600, color: '#fff' }
const infoGrid: React.CSSProperties = { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }
const infoLabel: React.CSSProperties = { fontSize: '10px', fontWeight: 700, color: '#9ca3af', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '2px' }
const sectionTitle: React.CSSProperties = { fontSize: '11px', fontWeight: 700, color: '#374151', textTransform: 'uppercase', letterSpacing: '0.05em' }
const stopRow: React.CSSProperties = { display: 'flex', alignItems: 'flex-start', gap: '10px', padding: '8px 10px', borderRadius: '6px', background: '#f9fafb', border: '1px solid #f3f4f6' }
const seqBadge: React.CSSProperties = { display: 'inline-flex', alignItems: 'center', justifyContent: 'center', width: '20px', height: '20px', borderRadius: '50%', color: '#fff', fontSize: '11px', fontWeight: 700, flexShrink: 0, marginTop: '1px' }
const inputStyle: React.CSSProperties = { padding: '7px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '13px', width: '100%', boxSizing: 'border-box' }
const btnPrimary: React.CSSProperties = { padding: '8px 18px', borderRadius: '6px', border: 'none', background: '#2563eb', color: '#fff', cursor: 'pointer', fontSize: '13px', fontWeight: 500 }
const btnAction: React.CSSProperties = { padding: '7px 14px', borderRadius: '6px', border: 'none', color: '#fff', cursor: 'pointer', fontSize: '13px', fontWeight: 500 }
const closeBtn: React.CSSProperties = { background: 'none', border: 'none', cursor: 'pointer', fontSize: '18px', color: '#6b7280', lineHeight: 1 }
const errorBox: React.CSSProperties = { padding: '10px', borderRadius: '6px', background: '#fef2f2', border: '1px solid #fecaca', color: '#dc2626', fontSize: '13px', marginBottom: '8px' }
const successBox: React.CSSProperties = { padding: '10px', borderRadius: '6px', background: '#f0fdf4', border: '1px solid #bbf7d0', color: '#16a34a', fontSize: '13px', marginBottom: '8px' }

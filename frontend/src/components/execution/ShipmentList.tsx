import { useCallback, useEffect, useState } from 'react'
import { shipmentApi } from '../../api/execution'
import type { ShipmentDto } from '../../types/execution'
import type { PagedResult } from '../../types/order'

const STATUS_OPTIONS = ['Pending','PickedUp','InTransit','Arrived','Delivered','PartialDelivered','Rejected','Exception']

const STATUS_COLOR: Record<string, string> = {
  Pending: '#6b7280', PickedUp: '#2563eb', InTransit: '#d97706',
  Arrived: '#7c3aed', Delivered: '#16a34a', PartialDelivered: '#059669',
  Rejected: '#dc2626', Exception: '#b45309',
}

// ── List view ─────────────────────────────────────────────────────────────────

interface ListProps {
  onSelect: (s: ShipmentDto) => void
  refreshKey: number
}

export function ShipmentList({ onSelect, refreshKey }: ListProps) {
  const [result, setResult] = useState<PagedResult<ShipmentDto> | null>(null)
  const [status, setStatus] = useState('')
  const [tripId, setTripId] = useState('')
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true); setError(null)
    try {
      const r = await shipmentApi.list({
        page, pageSize: 20,
        status: status || undefined,
        tripId: tripId || undefined,
      })
      setResult(r)
    } catch (e) { setError(e instanceof Error ? e.message : 'Failed') }
    finally { setLoading(false) }
  }, [page, status, tripId, refreshKey]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => { load() }, [load])

  const totalPages = result ? Math.ceil(result.totalCount / result.pageSize) : 1

  return (
    <div>
      {/* Filters */}
      <div style={{ display: 'flex', gap: '10px', marginBottom: '14px', flexWrap: 'wrap', alignItems: 'center' }}>
        <select value={status} onChange={e => { setStatus(e.target.value); setPage(1) }} style={sel}>
          <option value="">All Statuses</option>
          {STATUS_OPTIONS.map(s => <option key={s}>{s}</option>)}
        </select>
        <input
          placeholder="Trip ID (UUID)"
          value={tripId}
          onChange={e => { setTripId(e.target.value); setPage(1) }}
          style={{ ...sel, width: '260px' }}
        />
        <button onClick={load} style={btnSec}>Refresh</button>
        {result && <span style={{ color: '#6b7280', fontSize: '12px' }}>{result.totalCount} shipments</span>}
      </div>

      {error && <div style={errorBox}>{error}</div>}

      <div style={{ overflowX: 'auto', borderRadius: '8px', border: '1px solid #e5e7eb' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
          <thead>
            <tr style={{ background: '#f9fafb', borderBottom: '1px solid #e5e7eb' }}>
              {['Shipment #', 'Status', 'Address', 'Items', 'Picked Up', 'Delivered', 'POD'].map(h => (
                <th key={h} style={th}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={7} style={center}>Loading…</td></tr>
            ) : !result?.items.length ? (
              <tr><td colSpan={7} style={center}>No shipments found</td></tr>
            ) : result.items.map(s => (
              <tr key={s.id} onClick={() => onSelect(s)} style={tr}
                onMouseEnter={e => (e.currentTarget.style.background = '#f9fafb')}
                onMouseLeave={e => (e.currentTarget.style.background = '')}>
                <td style={td}>
                  <div style={{ fontWeight: 500 }}>{s.shipmentNumber}</div>
                  <div style={{ fontSize: '11px', color: '#9ca3af' }}>{s.id.slice(0, 8)}…</div>
                </td>
                <td style={td}>
                  <span style={{ ...badge, background: STATUS_COLOR[s.status] ?? '#6b7280' }}>{s.status}</span>
                </td>
                <td style={td}>
                  <div>{s.addressName ?? '—'}</div>
                  <div style={{ fontSize: '11px', color: '#6b7280' }}>{s.addressProvince}</div>
                </td>
                <td style={td}>{s.items.length}</td>
                <td style={td}>{s.pickedUpAt ? fmtTime(s.pickedUpAt) : '—'}</td>
                <td style={td}>{s.deliveredAt ? fmtTime(s.deliveredAt) : '—'}</td>
                <td style={td}>
                  {s.pod
                    ? <span style={{ ...badge, background: podColor(s.pod.approvalStatus) }}>{s.pod.approvalStatus}</span>
                    : <span style={{ color: '#d1d5db' }}>—</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div style={{ display: 'flex', gap: '8px', marginTop: '12px', justifyContent: 'center', alignItems: 'center' }}>
          <button disabled={page <= 1} onClick={() => setPage(p => p - 1)} style={btnSec}>← Prev</button>
          <span style={{ fontSize: '13px', color: '#6b7280' }}>Page {page} / {totalPages}</span>
          <button disabled={page >= totalPages} onClick={() => setPage(p => p + 1)} style={btnSec}>Next →</button>
        </div>
      )}
    </div>
  )
}

// ── Driver Today view ─────────────────────────────────────────────────────────

interface DriverProps {
  onSelect: (s: ShipmentDto) => void
  refreshKey: number
}

export function DriverTodayView({ onSelect, refreshKey }: DriverProps) {
  const [driverId, setDriverId] = useState('')
  const [items, setItems]       = useState<ShipmentDto[]>([])
  const [loading, setLoading]   = useState(false)
  const [error, setError]       = useState<string | null>(null)
  const [searched, setSearched] = useState(false)

  const load = async () => {
    if (!driverId.trim()) return
    setLoading(true); setError(null); setSearched(true)
    try {
      const r = await shipmentApi.driverToday(driverId.trim())
      setItems(r.items ?? [])
    } catch (e) { setError(e instanceof Error ? e.message : 'Failed') }
    finally { setLoading(false) }
  }

  // Re-fetch when parent triggers refresh (after action)
  useEffect(() => {
    if (searched && driverId) load()
  }, [refreshKey]) // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div>
      <div style={{ display: 'flex', gap: '10px', marginBottom: '16px' }}>
        <input
          placeholder="Driver ID (UUID)"
          value={driverId}
          onChange={e => setDriverId(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && load()}
          style={{ ...sel, width: '300px' }}
        />
        <button onClick={load} disabled={!driverId.trim() || loading} style={btnPri}>
          {loading ? 'Loading…' : "Load Today's Work"}
        </button>
      </div>

      {error && <div style={errorBox}>{error}</div>}

      {!searched ? (
        <div style={emptyState}>Enter a Driver ID to see today's shipments</div>
      ) : items.length === 0 ? (
        <div style={emptyState}>No shipments today for this driver</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {items.map((s, i) => (
            <div key={s.id} onClick={() => onSelect(s)} style={driverCard}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div>
                  <div style={{ fontWeight: 600, fontSize: '14px' }}>
                    Stop {i + 1} — {s.addressName ?? s.addressProvince}
                  </div>
                  <div style={{ fontSize: '12px', color: '#6b7280', marginTop: '2px' }}>
                    {s.shipmentNumber} · {s.items.length} item{s.items.length !== 1 ? 's' : ''}
                    {s.addressStreet && ` · ${s.addressStreet}`}
                  </div>
                </div>
                <span style={{ ...badge, background: STATUS_COLOR[s.status] ?? '#6b7280' }}>{s.status}</span>
              </div>
              {s.exceptionReason && (
                <div style={{ marginTop: '6px', fontSize: '12px', color: '#b45309', background: '#fef3c7', padding: '4px 8px', borderRadius: '4px' }}>
                  ⚠ {s.exceptionReason}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ── helpers ───────────────────────────────────────────────────────────────────

function fmtTime(d: string) {
  return new Date(d).toLocaleString('th-TH', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function podColor(s: string) {
  return s === 'Approved' || s === 'AutoApproved' ? '#16a34a' : s === 'Rejected' ? '#dc2626' : '#6b7280'
}

// ── styles ────────────────────────────────────────────────────────────────────

const sel: React.CSSProperties         = { padding: '6px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '13px' }
const btnPri: React.CSSProperties      = { padding: '7px 14px', borderRadius: '6px', border: 'none', background: '#2563eb', color: '#fff', cursor: 'pointer', fontSize: '13px', fontWeight: 500 }
const btnSec: React.CSSProperties      = { padding: '7px 12px', borderRadius: '6px', border: '1px solid #d1d5db', background: '#fff', cursor: 'pointer', fontSize: '13px' }
const badge: React.CSSProperties       = { display: 'inline-block', padding: '2px 8px', borderRadius: '12px', fontSize: '11px', fontWeight: 600, color: '#fff' }
const th: React.CSSProperties          = { padding: '9px 12px', textAlign: 'left', fontSize: '11px', fontWeight: 600, color: '#6b7280', textTransform: 'uppercase', letterSpacing: '0.05em' }
const td: React.CSSProperties          = { padding: '11px 12px', verticalAlign: 'middle' }
const tr: React.CSSProperties          = { borderBottom: '1px solid #f3f4f6', cursor: 'pointer' }
const center: React.CSSProperties      = { textAlign: 'center', padding: '32px', color: '#9ca3af' }
const emptyState: React.CSSProperties  = { textAlign: 'center', padding: '40px', color: '#9ca3af', fontSize: '13px' }
const driverCard: React.CSSProperties  = { padding: '14px 16px', background: '#fff', borderRadius: '8px', border: '1px solid #e5e7eb', cursor: 'pointer' }
const errorBox: React.CSSProperties    = { padding: '10px', borderRadius: '6px', background: '#fef2f2', border: '1px solid #fecaca', color: '#dc2626', fontSize: '13px', marginBottom: '12px' }

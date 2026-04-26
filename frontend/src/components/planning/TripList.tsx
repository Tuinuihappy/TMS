import { useCallback, useEffect, useState } from 'react'
import { tripApi } from '../../api/planning'
import type { TripDto } from '../../types/planning'
import type { PagedResult } from '../../types/order'

const TRIP_STATUS_COLOR: Record<string, string> = {
  Created: '#6b7280', Assigned: '#2563eb', Dispatched: '#d97706',
  InTransit: '#f59e0b', Completed: '#16a34a', Cancelled: '#dc2626',
}

const TRIP_STATUS_OPTIONS = ['Created', 'Assigned', 'Dispatched', 'InTransit', 'Completed', 'Cancelled']

interface Props {
  onSelect: (trip: TripDto) => void
  refreshKey: number
}

export function TripList({ onSelect, refreshKey }: Props) {
  const [result, setResult] = useState<PagedResult<TripDto> | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [status, setStatus] = useState('')
  const [date, setDate] = useState('')
  const [page, setPage] = useState(1)

  const load = useCallback(async () => {
    setLoading(true); setError(null)
    try {
      const r = await tripApi.list({ page, pageSize: 20, status: status || undefined, date: date || undefined })
      setResult(r)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load trips')
    } finally {
      setLoading(false)
    }
  }, [page, status, date, refreshKey]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => { load() }, [load])

  const totalPages = result ? Math.ceil(result.totalCount / result.pageSize) : 1

  return (
    <div>
      {/* Filters */}
      <div style={{ display: 'flex', gap: '10px', marginBottom: '14px', flexWrap: 'wrap', alignItems: 'center' }}>
        <select value={status} onChange={e => { setStatus(e.target.value); setPage(1) }} style={selectStyle}>
          <option value="">All Statuses</option>
          {TRIP_STATUS_OPTIONS.map(s => <option key={s}>{s}</option>)}
        </select>
        <input type="date" value={date} onChange={e => { setDate(e.target.value); setPage(1) }} style={selectStyle} />
        <button onClick={load} style={btnSecondary}>Refresh</button>
        {result && <span style={{ color: '#6b7280', fontSize: '12px' }}>{result.totalCount} trips</span>}
      </div>

      {error && <div style={errorBox}>{error}</div>}

      {/* Table */}
      <div style={{ overflowX: 'auto', borderRadius: '8px', border: '1px solid #e5e7eb' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
          <thead>
            <tr style={{ background: '#f9fafb', borderBottom: '1px solid #e5e7eb' }}>
              {['Trip Number', 'Status', 'Date', 'Weight', 'Stops', 'Vehicle', 'Driver', 'Dispatched'].map(h => (
                <th key={h} style={thStyle}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={8} style={centerCell}>Loading…</td></tr>
            ) : !result || result.items.length === 0 ? (
              <tr><td colSpan={8} style={centerCell}>No trips found</td></tr>
            ) : result.items.map(trip => (
              <tr key={trip.id} onClick={() => onSelect(trip)} style={trStyle}
                onMouseEnter={e => (e.currentTarget.style.background = '#f9fafb')}
                onMouseLeave={e => (e.currentTarget.style.background = '')}>
                <td style={tdStyle}>
                  <span style={{ fontWeight: 500 }}>{trip.tripNumber}</span>
                </td>
                <td style={tdStyle}>
                  <span style={{ ...badge, background: TRIP_STATUS_COLOR[trip.status] ?? '#6b7280' }}>
                    {trip.status}
                  </span>
                </td>
                <td style={tdStyle}>{new Date(trip.plannedDate).toLocaleDateString('th-TH')}</td>
                <td style={tdStyle}>{trip.totalWeight.toLocaleString()} kg</td>
                <td style={tdStyle}>{trip.stops.length}</td>
                <td style={tdStyle}>{trip.vehicleId ? trip.vehicleId.slice(0, 8) + '…' : <em style={{ color: '#9ca3af' }}>Unassigned</em>}</td>
                <td style={tdStyle}>{trip.driverId ? trip.driverId.slice(0, 8) + '…' : <em style={{ color: '#9ca3af' }}>Unassigned</em>}</td>
                <td style={tdStyle}>{trip.dispatchedAt ? new Date(trip.dispatchedAt).toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit' }) : '—'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div style={{ display: 'flex', gap: '8px', marginTop: '12px', justifyContent: 'center', alignItems: 'center' }}>
          <button disabled={page <= 1} onClick={() => setPage(p => p - 1)} style={btnSecondary}>← Prev</button>
          <span style={{ fontSize: '13px', color: '#6b7280' }}>Page {page} / {totalPages}</span>
          <button disabled={page >= totalPages} onClick={() => setPage(p => p + 1)} style={btnSecondary}>Next →</button>
        </div>
      )}
    </div>
  )
}

// ── Dispatch Board ────────────────────────────────────────────────────────────

interface BoardProps {
  onSelect: (trip: TripDto) => void
  refreshKey: number
}

export function DispatchBoard({ onSelect, refreshKey }: BoardProps) {
  const [board, setBoard] = useState<{ trips: TripDto[]; summary: { total: number; dispatched: number; pending: number } } | null>(null)
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true); setError(null)
    try {
      const r = await tripApi.board(date)
      setBoard(r)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load board')
    } finally {
      setLoading(false)
    }
  }, [date, refreshKey]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => { load() }, [load])

  const summary = board?.summary

  return (
    <div>
      {/* Board controls */}
      <div style={{ display: 'flex', gap: '10px', marginBottom: '16px', alignItems: 'center', flexWrap: 'wrap' }}>
        <input type="date" value={date} onChange={e => setDate(e.target.value)} style={selectStyle} />
        <button onClick={load} style={btnSecondary}>Refresh</button>
        {summary && (
          <div style={{ display: 'flex', gap: '16px', marginLeft: '8px' }}>
            <Stat label="Total" value={summary.total} />
            <Stat label="Dispatched" value={summary.dispatched} color="#d97706" />
            <Stat label="Pending" value={summary.pending} color="#2563eb" />
          </div>
        )}
      </div>

      {error && <div style={errorBox}>{error}</div>}

      {loading ? (
        <div style={emptyState}>Loading…</div>
      ) : !board || board.trips.length === 0 ? (
        <div style={emptyState}>No trips on {date}</div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(260px, 1fr))', gap: '10px' }}>
          {board.trips.map(trip => (
            <div key={trip.id} onClick={() => onSelect(trip)} style={tripCard}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div style={{ fontWeight: 600, fontSize: '14px' }}>{trip.tripNumber}</div>
                <span style={{ ...badge, background: TRIP_STATUS_COLOR[trip.status] ?? '#6b7280' }}>
                  {trip.status}
                </span>
              </div>
              <div style={{ fontSize: '12px', color: '#6b7280', marginTop: '6px' }}>
                {trip.totalWeight.toLocaleString()} kg · {trip.stops.length} stops
              </div>
              {trip.vehicleId
                ? <div style={{ fontSize: '11px', color: '#374151', marginTop: '4px' }}>🚚 {trip.vehicleId.slice(0, 8)}…</div>
                : <div style={{ fontSize: '11px', color: '#ef4444', marginTop: '4px' }}>⚠ No vehicle assigned</div>
              }
              {trip.dispatchedAt && (
                <div style={{ fontSize: '11px', color: '#6b7280', marginTop: '2px' }}>
                  Dispatched {new Date(trip.dispatchedAt).toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit' })}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

function Stat({ label, value, color }: { label: string; value: number; color?: string }) {
  return (
    <div style={{ textAlign: 'center' }}>
      <div style={{ fontSize: '18px', fontWeight: 700, color: color ?? '#111827', lineHeight: 1 }}>{value}</div>
      <div style={{ fontSize: '10px', color: '#6b7280', textTransform: 'uppercase' }}>{label}</div>
    </div>
  )
}

// ── Styles ────────────────────────────────────────────────────────────────────

const selectStyle: React.CSSProperties = { padding: '6px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '13px' }
const btnSecondary: React.CSSProperties = { padding: '7px 12px', borderRadius: '6px', border: '1px solid #d1d5db', background: '#fff', cursor: 'pointer', fontSize: '13px' }
const badge: React.CSSProperties = { display: 'inline-block', padding: '2px 8px', borderRadius: '12px', fontSize: '11px', fontWeight: 600, color: '#fff' }
const thStyle: React.CSSProperties = { padding: '9px 12px', textAlign: 'left', fontSize: '11px', fontWeight: 600, color: '#6b7280', textTransform: 'uppercase', letterSpacing: '0.05em' }
const tdStyle: React.CSSProperties = { padding: '11px 12px', verticalAlign: 'middle' }
const trStyle: React.CSSProperties = { borderBottom: '1px solid #f3f4f6', cursor: 'pointer' }
const centerCell: React.CSSProperties = { textAlign: 'center', padding: '32px', color: '#9ca3af' }
const tripCard: React.CSSProperties = { padding: '14px', background: '#fff', borderRadius: '8px', border: '1px solid #e5e7eb', cursor: 'pointer' }
const emptyState: React.CSSProperties = { textAlign: 'center', padding: '40px', color: '#9ca3af', fontSize: '13px' }
const errorBox: React.CSSProperties = { padding: '10px', borderRadius: '6px', background: '#fef2f2', border: '1px solid #fecaca', color: '#dc2626', fontSize: '13px', marginBottom: '12px' }

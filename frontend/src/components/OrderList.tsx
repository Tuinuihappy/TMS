import { useEffect, useState, useCallback } from 'react'
import { ordersApi, type GetOrdersParams } from '../api/orders'
import type { OrderDto, OrderStatus } from '../types/order'

const STATUS_OPTIONS: OrderStatus[] = ['Draft', 'Confirmed', 'InTransit', 'Delivered', 'Cancelled']

const STATUS_COLORS: Record<string, string> = {
  Draft: '#6b7280',
  Confirmed: '#2563eb',
  InTransit: '#d97706',
  Delivered: '#16a34a',
  Cancelled: '#dc2626',
}

interface Props {
  onSelect: (order: OrderDto) => void
  onCreateNew: () => void
  onImport: () => void
  refreshKey: number
}

export function OrderList({ onSelect, onCreateNew, onImport, refreshKey }: Props) {
  const [orders, setOrders] = useState<OrderDto[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [filters, setFilters] = useState<GetOrdersParams>({
    page: 1,
    pageSize: 20,
    status: '',
    customerId: '',
  })

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await ordersApi.list({
        ...filters,
        status: filters.status || undefined,
        customerId: filters.customerId || undefined,
      })
      setOrders(result.items)
      setTotalCount(result.totalCount)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load orders')
    } finally {
      setLoading(false)
    }
  }, [filters, refreshKey]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => { load() }, [load])

  const totalPages = Math.ceil(totalCount / (filters.pageSize ?? 20))

  return (
    <div style={{ padding: '24px' }}>
      {/* Header */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
        <div>
          <h1 style={{ margin: 0, fontSize: '22px', fontWeight: 600 }}>Orders</h1>
          <span style={{ color: '#6b7280', fontSize: '13px' }}>{totalCount} total</span>
        </div>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button onClick={onImport} style={btnSecondary}>Import CSV</button>
          <button onClick={onCreateNew} style={btnPrimary}>+ New Order</button>
        </div>
      </div>

      {/* Filters */}
      <div style={{ display: 'flex', gap: '12px', marginBottom: '16px', flexWrap: 'wrap' }}>
        <select
          value={filters.status ?? ''}
          onChange={e => setFilters(f => ({ ...f, status: e.target.value, page: 1 }))}
          style={selectStyle}
        >
          <option value="">All Statuses</option>
          {STATUS_OPTIONS.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
        <input
          placeholder="Customer ID (UUID)"
          value={filters.customerId ?? ''}
          onChange={e => setFilters(f => ({ ...f, customerId: e.target.value, page: 1 }))}
          style={{ ...inputStyle, width: '280px' }}
        />
        <select
          value={filters.pageSize ?? 20}
          onChange={e => setFilters(f => ({ ...f, pageSize: Number(e.target.value), page: 1 }))}
          style={selectStyle}
        >
          {[10, 20, 50].map(n => <option key={n} value={n}>{n} / page</option>)}
        </select>
        <button onClick={load} style={btnSecondary}>Refresh</button>
      </div>

      {/* Error */}
      {error && <div style={errorBox}>{error}</div>}

      {/* Table */}
      <div style={{ overflowX: 'auto', borderRadius: '8px', border: '1px solid #e5e7eb' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
          <thead>
            <tr style={{ background: '#f9fafb', borderBottom: '1px solid #e5e7eb' }}>
              {['Order Number', 'Status', 'Priority', 'Weight (kg)', 'Stops', 'Items', 'Created'].map(h => (
                <th key={h} style={thStyle}>{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px', color: '#9ca3af' }}>Loading…</td></tr>
            ) : orders.length === 0 ? (
              <tr><td colSpan={7} style={{ textAlign: 'center', padding: '40px', color: '#9ca3af' }}>No orders found</td></tr>
            ) : orders.map(o => (
              <tr
                key={o.id}
                onClick={() => onSelect(o)}
                style={{ borderBottom: '1px solid #f3f4f6', cursor: 'pointer', transition: 'background 0.1s' }}
                onMouseEnter={e => (e.currentTarget.style.background = '#f9fafb')}
                onMouseLeave={e => (e.currentTarget.style.background = '')}
              >
                <td style={tdStyle}>
                  <span style={{ fontWeight: 500 }}>{o.orderNumber}</span>
                  <div style={{ color: '#9ca3af', fontSize: '11px', marginTop: '2px' }}>{o.id.slice(0, 8)}…</div>
                </td>
                <td style={tdStyle}>
                  <span style={{
                    display: 'inline-block',
                    padding: '2px 8px',
                    borderRadius: '12px',
                    fontSize: '11px',
                    fontWeight: 600,
                    color: '#fff',
                    background: STATUS_COLORS[o.status] ?? '#6b7280',
                  }}>{o.status}</span>
                </td>
                <td style={tdStyle}>{o.priority}</td>
                <td style={tdStyle}>{o.totalWeight.toLocaleString()}</td>
                <td style={tdStyle}>{o.stopCount}</td>
                <td style={tdStyle}>{o.itemCount}</td>
                <td style={tdStyle}>{new Date(o.createdAt).toLocaleDateString('th-TH')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div style={{ display: 'flex', gap: '8px', marginTop: '16px', alignItems: 'center', justifyContent: 'center' }}>
          <button
            disabled={(filters.page ?? 1) <= 1}
            onClick={() => setFilters(f => ({ ...f, page: (f.page ?? 1) - 1 }))}
            style={btnSecondary}
          >← Prev</button>
          <span style={{ color: '#6b7280', fontSize: '13px' }}>
            Page {filters.page} of {totalPages}
          </span>
          <button
            disabled={(filters.page ?? 1) >= totalPages}
            onClick={() => setFilters(f => ({ ...f, page: (f.page ?? 1) + 1 }))}
            style={btnSecondary}
          >Next →</button>
        </div>
      )}
    </div>
  )
}

// ── Styles ─────────────────────────────────────────────────────────────────

const btnPrimary: React.CSSProperties = {
  padding: '8px 16px', borderRadius: '6px', border: 'none',
  background: '#2563eb', color: '#fff', cursor: 'pointer', fontSize: '13px', fontWeight: 500,
}
const btnSecondary: React.CSSProperties = {
  padding: '8px 14px', borderRadius: '6px', border: '1px solid #d1d5db',
  background: '#fff', cursor: 'pointer', fontSize: '13px',
}
const selectStyle: React.CSSProperties = {
  padding: '7px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '13px',
}
const inputStyle: React.CSSProperties = {
  padding: '7px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '13px',
}
const thStyle: React.CSSProperties = {
  padding: '10px 14px', textAlign: 'left', fontSize: '11px',
  fontWeight: 600, color: '#6b7280', textTransform: 'uppercase', letterSpacing: '0.05em',
}
const tdStyle: React.CSSProperties = {
  padding: '12px 14px', verticalAlign: 'top',
}
const errorBox: React.CSSProperties = {
  padding: '12px', borderRadius: '6px', background: '#fef2f2',
  border: '1px solid #fecaca', color: '#dc2626', marginBottom: '16px', fontSize: '13px',
}

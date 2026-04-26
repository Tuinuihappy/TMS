import { useState } from 'react'
import { ordersApi, ApiError } from '../api/orders'
import type { OrderDto, OrderPriority, AmendOrderRequest, AddressDto } from '../types/order'

interface Props {
  order: OrderDto
  onClose: () => void
  onRefresh: () => void
}

type Panel = 'detail' | 'amend' | 'cancel'

const STATUS_COLORS: Record<string, string> = {
  Draft: '#6b7280',
  Confirmed: '#2563eb',
  InTransit: '#d97706',
  Delivered: '#16a34a',
  Cancelled: '#dc2626',
}

export function OrderDetail({ order, onClose, onRefresh }: Props) {
  const [panel, setPanel] = useState<Panel>('detail')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  const canConfirm = order.status === 'Draft'
  const canAmend = order.status === 'Draft' || order.status === 'Confirmed'
  const canCancel = order.status === 'Draft' || order.status === 'Confirmed'

  const run = async (label: string, fn: () => Promise<void>) => {
    setLoading(true)
    setError(null)
    try {
      await fn()
      setSuccessMsg(`${label} successful`)
      setTimeout(() => { onRefresh(); onClose() }, 800)
    } catch (e) {
      const msg = e instanceof ApiError ? e.message : 'Operation failed'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  const handleConfirm = () => run('Confirm', () => ordersApi.confirm(order.id))

  return (
    <div style={overlay}>
      <div style={modal}>
        {/* Header */}
        <div style={modalHeader}>
          <div>
            <h2 style={{ margin: 0, fontSize: '18px' }}>{order.orderNumber}</h2>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginTop: '4px' }}>
              <span style={{ ...statusBadge, background: STATUS_COLORS[order.status] ?? '#6b7280' }}>
                {order.status}
              </span>
              <span style={{ color: '#6b7280', fontSize: '12px' }}>{order.priority}</span>
            </div>
          </div>
          <button onClick={onClose} style={closeBtn}>✕</button>
        </div>

        {/* Tabs */}
        <div style={tabs}>
          {(['detail', 'amend', 'cancel'] as Panel[]).map(p => (
            <button
              key={p}
              disabled={p === 'amend' ? !canAmend : p === 'cancel' ? !canCancel : false}
              onClick={() => { setPanel(p); setError(null); setSuccessMsg(null) }}
              style={{ ...tab, ...(panel === p ? tabActive : {}) }}
            >
              {p === 'detail' ? 'Details' : p === 'amend' ? 'Amend' : 'Cancel'}
            </button>
          ))}
        </div>

        <div style={modalBody}>
          {error && <div style={errorBox}>{error}</div>}
          {successMsg && <div style={successBox}>{successMsg}</div>}

          {panel === 'detail' && (
            <DetailPanel order={order} onConfirm={canConfirm ? handleConfirm : undefined} loading={loading} />
          )}
          {panel === 'amend' && (
            <AmendPanel
              order={order}
              onSubmit={(req) => run('Amend', () => ordersApi.amend(order.id, req))}
              loading={loading}
            />
          )}
          {panel === 'cancel' && (
            <CancelPanel
              onSubmit={(reason) => run('Cancel', () => ordersApi.cancel(order.id, reason))}
              loading={loading}
            />
          )}
        </div>
      </div>
    </div>
  )
}

// ── Detail Panel ────────────────────────────────────────────────────────────

function DetailPanel({ order, onConfirm, loading }: {
  order: OrderDto
  onConfirm?: () => void
  loading: boolean
}) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      <div style={infoGrid}>
        <InfoRow label="Order ID" value={order.id} mono />
        <InfoRow label="Customer ID" value={order.customerId} mono />
        <InfoRow label="Total Weight" value={`${order.totalWeight.toLocaleString()} kg`} />
        <InfoRow label="Total Volume" value={`${order.totalVolumeCBM} CBM`} />
        <InfoRow label="Stops" value={String(order.stopCount)} />
        <InfoRow label="Items" value={String(order.itemCount)} />
        <InfoRow label="Created" value={new Date(order.createdAt).toLocaleString('th-TH')} />
        {order.updatedAt && <InfoRow label="Updated" value={new Date(order.updatedAt).toLocaleString('th-TH')} />}
        {order.notes && <InfoRow label="Notes" value={order.notes} />}
      </div>

      {order.stops.length > 0 && (
        <div>
          <div style={sectionTitle}>Stops</div>
          {order.stops.map((stop, i) => (
            <div key={stop.stopId} style={stopCard}>
              <div style={{ fontWeight: 600, fontSize: '12px', color: '#6b7280', marginBottom: '8px' }}>
                STOP {i + 1}
              </div>
              <div style={grid2}>
                <div>
                  <div style={{ fontSize: '11px', color: '#2563eb', fontWeight: 600, marginBottom: '4px' }}>PICKUP</div>
                  <div style={{ fontSize: '13px' }}>{stop.pickupSummary}</div>
                  {stop.pickupWindowFrom && (
                    <div style={{ fontSize: '11px', color: '#9ca3af', marginTop: '4px' }}>
                      {fmt(stop.pickupWindowFrom)} – {fmt(stop.pickupWindowTo)}
                    </div>
                  )}
                </div>
                <div>
                  <div style={{ fontSize: '11px', color: '#16a34a', fontWeight: 600, marginBottom: '4px' }}>DROPOFF</div>
                  <div style={{ fontSize: '13px' }}>{stop.dropoffSummary}</div>
                  {stop.dropoffWindowFrom && (
                    <div style={{ fontSize: '11px', color: '#9ca3af', marginTop: '4px' }}>
                      {fmt(stop.dropoffWindowFrom)} – {fmt(stop.dropoffWindowTo)}
                    </div>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {onConfirm && (
        <div style={{ display: 'flex', justifyContent: 'flex-end', paddingTop: '8px', borderTop: '1px solid #f3f4f6' }}>
          <button onClick={onConfirm} disabled={loading} style={btnConfirm}>
            {loading ? 'Confirming…' : '✓ Confirm Order'}
          </button>
        </div>
      )}
    </div>
  )
}

// ── Amend Panel ─────────────────────────────────────────────────────────────

function AmendPanel({ order, onSubmit, loading }: {
  order: OrderDto
  onSubmit: (req: AmendOrderRequest) => void
  loading: boolean
}) {
  const [priority, setPriority] = useState<OrderPriority | ''>('')
  const [notes, setNotes] = useState('')
  const [stopId, setStopId] = useState('')
  const [dropoffStreet, setDropoffStreet] = useState('')
  const [dropoffProvince, setDropoffProvince] = useState('')
  const [dropoffSubDistrict, setDropoffSubDistrict] = useState('')
  const [dropoffDistrict, setDropoffDistrict] = useState('')
  const [dropoffPostalCode, setDropoffPostalCode] = useState('')

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const req: AmendOrderRequest = {}
    if (priority) req.priority = priority
    if (notes) req.notes = notes
    if (stopId && dropoffStreet) {
      req.stopId = stopId
      req.dropoffAddress = {
        street: dropoffStreet,
        subDistrict: dropoffSubDistrict,
        district: dropoffDistrict,
        province: dropoffProvince,
        postalCode: dropoffPostalCode,
      } as AddressDto
    }
    onSubmit(req)
  }

  return (
    <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      <div style={sectionTitle}>Order-level Changes</div>
      <div style={grid2}>
        <Field label="Priority">
          <select value={priority} onChange={e => setPriority(e.target.value as OrderPriority | '')} style={inputStyle}>
            <option value="">No change</option>
            <option>Normal</option>
            <option>Express</option>
            <option>SameDay</option>
          </select>
        </Field>
        <Field label="Notes">
          <input value={notes} onChange={e => setNotes(e.target.value)} style={inputStyle} placeholder="Leave blank to keep current" />
        </Field>
      </div>

      <div style={sectionTitle}>Stop Address Change (optional)</div>
      <Field label="Stop">
        <select value={stopId} onChange={e => setStopId(e.target.value)} style={inputStyle}>
          <option value="">Select stop to amend…</option>
          {order.stops.map(s => (
            <option key={s.stopId} value={s.stopId}>
              Stop {s.sequence}: {s.dropoffSummary.slice(0, 40)}
            </option>
          ))}
        </select>
      </Field>

      {stopId && (
        <>
          <div style={{ fontSize: '12px', color: '#6b7280', fontWeight: 600 }}>New Dropoff Address</div>
          <input placeholder="Street *" required value={dropoffStreet} onChange={e => setDropoffStreet(e.target.value)} style={inputStyle} />
          <div style={grid2}>
            <input placeholder="Sub-district" value={dropoffSubDistrict} onChange={e => setDropoffSubDistrict(e.target.value)} style={inputStyle} />
            <input placeholder="District" value={dropoffDistrict} onChange={e => setDropoffDistrict(e.target.value)} style={inputStyle} />
          </div>
          <div style={grid2}>
            <input placeholder="Province *" required value={dropoffProvince} onChange={e => setDropoffProvince(e.target.value)} style={inputStyle} />
            <input placeholder="Postal Code" value={dropoffPostalCode} onChange={e => setDropoffPostalCode(e.target.value)} style={inputStyle} />
          </div>
        </>
      )}

      <div style={{ display: 'flex', justifyContent: 'flex-end', paddingTop: '8px', borderTop: '1px solid #f3f4f6' }}>
        <button type="submit" disabled={loading || (!priority && !notes && !stopId)} style={btnPrimary}>
          {loading ? 'Saving…' : 'Save Changes'}
        </button>
      </div>
    </form>
  )
}

// ── Cancel Panel ────────────────────────────────────────────────────────────

function CancelPanel({ onSubmit, loading }: { onSubmit: (reason: string) => void; loading: boolean }) {
  const [reason, setReason] = useState('')

  return (
    <form onSubmit={e => { e.preventDefault(); onSubmit(reason) }}
      style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      <div style={{ padding: '12px', background: '#fef3c7', borderRadius: '8px', border: '1px solid #fde68a', fontSize: '13px', color: '#92400e' }}>
        This action cannot be undone. The order will be permanently cancelled.
      </div>
      <Field label="Cancellation Reason *">
        <textarea
          required
          value={reason}
          onChange={e => setReason(e.target.value)}
          rows={3}
          style={{ ...inputStyle, resize: 'vertical' }}
          placeholder="e.g. Customer requested cancellation"
        />
      </Field>
      <div style={{ display: 'flex', justifyContent: 'flex-end', paddingTop: '8px', borderTop: '1px solid #f3f4f6' }}>
        <button type="submit" disabled={loading || !reason.trim()} style={btnCancel}>
          {loading ? 'Cancelling…' : 'Cancel Order'}
        </button>
      </div>
    </form>
  )
}

// ── Helpers ─────────────────────────────────────────────────────────────────

function InfoRow({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div>
      <div style={{ fontSize: '11px', fontWeight: 600, color: '#9ca3af', textTransform: 'uppercase', letterSpacing: '0.05em' }}>{label}</div>
      <div style={{ fontSize: '13px', marginTop: '2px', fontFamily: mono ? 'monospace' : undefined, wordBreak: 'break-all' }}>{value}</div>
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

function fmt(d?: string) {
  if (!d) return ''
  return new Date(d).toLocaleString('th-TH', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}

// ── Styles ───────────────────────────────────────────────────────────────────

const overlay: React.CSSProperties = {
  position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)',
  display: 'flex', alignItems: 'flex-start', justifyContent: 'center',
  zIndex: 100, overflowY: 'auto', padding: '40px 20px',
}
const modal: React.CSSProperties = {
  background: '#fff', borderRadius: '12px', width: '100%', maxWidth: '700px',
  boxShadow: '0 20px 60px rgba(0,0,0,0.2)',
}
const modalHeader: React.CSSProperties = {
  display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start',
  padding: '20px 24px', borderBottom: '1px solid #e5e7eb',
}
const modalBody: React.CSSProperties = { padding: '24px' }
const tabs: React.CSSProperties = {
  display: 'flex', borderBottom: '1px solid #e5e7eb', padding: '0 24px',
}
const tab: React.CSSProperties = {
  padding: '10px 16px', background: 'none', border: 'none', cursor: 'pointer',
  fontSize: '13px', fontWeight: 500, color: '#6b7280', borderBottom: '2px solid transparent',
  marginBottom: '-1px',
}
const tabActive: React.CSSProperties = {
  color: '#2563eb', borderBottom: '2px solid #2563eb',
}
const statusBadge: React.CSSProperties = {
  display: 'inline-block', padding: '2px 8px', borderRadius: '12px',
  fontSize: '11px', fontWeight: 600, color: '#fff',
}
const infoGrid: React.CSSProperties = { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }
const grid2: React.CSSProperties = { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }
const stopCard: React.CSSProperties = {
  border: '1px solid #e5e7eb', borderRadius: '8px', padding: '12px', marginBottom: '8px',
}
const sectionTitle: React.CSSProperties = {
  fontSize: '11px', fontWeight: 700, color: '#374151', textTransform: 'uppercase',
  letterSpacing: '0.05em', marginBottom: '4px',
}
const inputStyle: React.CSSProperties = {
  padding: '7px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '13px', width: '100%', boxSizing: 'border-box',
}
const btnPrimary: React.CSSProperties = {
  padding: '8px 20px', borderRadius: '6px', border: 'none', background: '#2563eb', color: '#fff', cursor: 'pointer', fontSize: '13px', fontWeight: 500,
}
const btnConfirm: React.CSSProperties = { ...btnPrimary, background: '#16a34a' }
const btnCancel: React.CSSProperties = { ...btnPrimary, background: '#dc2626' }
const closeBtn: React.CSSProperties = {
  background: 'none', border: 'none', cursor: 'pointer', fontSize: '18px', color: '#6b7280', lineHeight: 1,
}
const errorBox: React.CSSProperties = {
  padding: '12px', borderRadius: '6px', background: '#fef2f2', border: '1px solid #fecaca', color: '#dc2626', fontSize: '13px', marginBottom: '8px',
}
const successBox: React.CSSProperties = {
  padding: '12px', borderRadius: '6px', background: '#f0fdf4', border: '1px solid #bbf7d0', color: '#16a34a', fontSize: '13px', marginBottom: '8px',
}

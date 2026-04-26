import { useState } from 'react'
import { ordersApi, ApiError } from '../api/orders'
import type { OrderStopDto, OrderItemDto, AddressDto, OrderPriority } from '../types/order'

interface Props {
  onSuccess: () => void
  onCancel: () => void
}

const emptyAddress = (): AddressDto => ({
  street: '', subDistrict: '', district: '', province: '', postalCode: '',
})

const emptyItem = (): OrderItemDto => ({
  description: '', weightKg: 0, volumeCBM: 0, quantity: 1,
})

const emptyStop = (): OrderStopDto => ({
  sequence: 1,
  pickupAddress: emptyAddress(),
  dropoffAddress: emptyAddress(),
  items: [emptyItem()],
})

export function CreateOrderForm({ onSuccess, onCancel }: Props) {
  const [customerId, setCustomerId] = useState('')
  const [orderNumber, setOrderNumber] = useState('')
  const [priority, setPriority] = useState<OrderPriority>('Normal')
  const [notes, setNotes] = useState('')
  const [stops, setStops] = useState<OrderStopDto[]>([emptyStop()])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // ── Stop helpers ────────────────────────────────────────────

  const updateStop = (si: number, patch: Partial<OrderStopDto>) =>
    setStops(s => s.map((stop, i) => i === si ? { ...stop, ...patch } : stop))

  const updateAddress = (si: number, field: 'pickupAddress' | 'dropoffAddress', patch: Partial<AddressDto>) =>
    updateStop(si, { [field]: { ...stops[si][field], ...patch } })

  const updateItem = (si: number, ii: number, patch: Partial<OrderItemDto>) =>
    updateStop(si, {
      items: stops[si].items.map((item, i) => i === ii ? { ...item, ...patch } : item),
    })

  const addItem = (si: number) => updateStop(si, { items: [...stops[si].items, emptyItem()] })
  const removeItem = (si: number, ii: number) =>
    updateStop(si, { items: stops[si].items.filter((_, i) => i !== ii) })

  const addStop = () => setStops(s => [...s, { ...emptyStop(), sequence: s.length + 1 }])
  const removeStop = (si: number) => setStops(s => s.filter((_, i) => i !== si))

  // ── Submit ──────────────────────────────────────────────────

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    setLoading(true)
    try {
      await ordersApi.create({
        customerId,
        orderNumber: orderNumber || undefined,
        stops,
        priority,
        notes: notes || undefined,
      })
      onSuccess()
    } catch (err) {
      if (err instanceof ApiError && err.body) {
        const body = err.body as Record<string, unknown>
        const errors = body.errors as Record<string, string[]> | undefined
        if (errors) {
          setError(Object.values(errors).flat().join(' · '))
        } else {
          setError(err.message)
        }
      } else {
        setError(err instanceof Error ? err.message : 'Failed to create order')
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={overlay}>
      <div style={modal}>
        <div style={modalHeader}>
          <h2 style={{ margin: 0, fontSize: '18px' }}>New Order</h2>
          <button onClick={onCancel} style={closeBtn}>✕</button>
        </div>

        <form onSubmit={handleSubmit}>
          <div style={modalBody}>
            {error && <div style={errorBox}>{error}</div>}

            {/* Order meta */}
            <section style={section}>
              <h3 style={sectionTitle}>Order Details</h3>
              <div style={grid2}>
                <Field label="Customer ID *">
                  <input
                    required
                    value={customerId}
                    onChange={e => setCustomerId(e.target.value)}
                    placeholder="UUID"
                    style={inputStyle}
                  />
                </Field>
                <Field label="Order Number">
                  <input
                    value={orderNumber}
                    onChange={e => setOrderNumber(e.target.value)}
                    placeholder="Auto-generated if blank"
                    style={inputStyle}
                  />
                </Field>
              </div>
              <div style={grid2}>
                <Field label="Priority">
                  <select value={priority} onChange={e => setPriority(e.target.value as OrderPriority)} style={inputStyle}>
                    <option>Normal</option>
                    <option>Express</option>
                    <option>SameDay</option>
                  </select>
                </Field>
                <Field label="Notes">
                  <input value={notes} onChange={e => setNotes(e.target.value)} style={inputStyle} />
                </Field>
              </div>
            </section>

            {/* Stops */}
            {stops.map((stop, si) => (
              <section key={si} style={{ ...section, border: '1px solid #e5e7eb', borderRadius: '8px', padding: '16px' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
                  <h3 style={{ ...sectionTitle, margin: 0 }}>Stop {si + 1}</h3>
                  {stops.length > 1 && (
                    <button type="button" onClick={() => removeStop(si)} style={btnDanger}>Remove Stop</button>
                  )}
                </div>

                <div style={grid2}>
                  {/* Pickup */}
                  <div>
                    <div style={{ fontWeight: 600, fontSize: '12px', color: '#2563eb', marginBottom: '8px', textTransform: 'uppercase' }}>
                      Pickup Address
                    </div>
                    <AddressForm
                      value={stop.pickupAddress}
                      onChange={patch => updateAddress(si, 'pickupAddress', patch)}
                    />
                  </div>
                  {/* Dropoff */}
                  <div>
                    <div style={{ fontWeight: 600, fontSize: '12px', color: '#16a34a', marginBottom: '8px', textTransform: 'uppercase' }}>
                      Dropoff Address
                    </div>
                    <AddressForm
                      value={stop.dropoffAddress}
                      onChange={patch => updateAddress(si, 'dropoffAddress', patch)}
                    />
                  </div>
                </div>

                {/* Items */}
                <div style={{ marginTop: '16px' }}>
                  <div style={{ fontWeight: 600, fontSize: '12px', color: '#6b7280', marginBottom: '8px', textTransform: 'uppercase' }}>
                    Items
                  </div>
                  {stop.items.map((item, ii) => (
                    <div key={ii} style={{ display: 'flex', gap: '8px', marginBottom: '8px', alignItems: 'flex-end', flexWrap: 'wrap' }}>
                      <Field label="Description *">
                        <input
                          required
                          value={item.description}
                          onChange={e => updateItem(si, ii, { description: e.target.value })}
                          style={{ ...inputStyle, width: '160px' }}
                        />
                      </Field>
                      <Field label="Weight kg *">
                        <input
                          required type="number" min={0.01} step={0.01}
                          value={item.weightKg || ''}
                          onChange={e => updateItem(si, ii, { weightKg: Number(e.target.value) })}
                          style={{ ...inputStyle, width: '90px' }}
                        />
                      </Field>
                      <Field label="Volume CBM">
                        <input
                          type="number" min={0} step={0.01}
                          value={item.volumeCBM || ''}
                          onChange={e => updateItem(si, ii, { volumeCBM: Number(e.target.value) })}
                          style={{ ...inputStyle, width: '90px' }}
                        />
                      </Field>
                      <Field label="Qty *">
                        <input
                          required type="number" min={1}
                          value={item.quantity}
                          onChange={e => updateItem(si, ii, { quantity: Number(e.target.value) })}
                          style={{ ...inputStyle, width: '70px' }}
                        />
                      </Field>
                      <Field label="SKU">
                        <input
                          value={item.sku ?? ''}
                          onChange={e => updateItem(si, ii, { sku: e.target.value })}
                          style={{ ...inputStyle, width: '100px' }}
                        />
                      </Field>
                      {stop.items.length > 1 && (
                        <button type="button" onClick={() => removeItem(si, ii)} style={{ ...btnDanger, marginBottom: '1px' }}>✕</button>
                      )}
                    </div>
                  ))}
                  <button type="button" onClick={() => addItem(si)} style={btnLink}>+ Add Item</button>
                </div>
              </section>
            ))}

            <button type="button" onClick={addStop} style={{ ...btnSecondary, alignSelf: 'flex-start' }}>
              + Add Stop
            </button>
          </div>

          {/* Footer */}
          <div style={modalFooter}>
            <button type="button" onClick={onCancel} style={btnSecondary}>Cancel</button>
            <button type="submit" disabled={loading} style={btnPrimary}>
              {loading ? 'Creating…' : 'Create Order'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Sub-components ──────────────────────────────────────────────────────────

function AddressForm({ value, onChange }: { value: AddressDto; onChange: (p: Partial<AddressDto>) => void }) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
      <input placeholder="Name" value={value.name ?? ''} onChange={e => onChange({ name: e.target.value })} style={inputStyle} />
      <input placeholder="Street *" required value={value.street} onChange={e => onChange({ street: e.target.value })} style={inputStyle} />
      <div style={{ display: 'flex', gap: '6px' }}>
        <input placeholder="Sub-district" value={value.subDistrict} onChange={e => onChange({ subDistrict: e.target.value })} style={{ ...inputStyle, flex: 1 }} />
        <input placeholder="District" value={value.district} onChange={e => onChange({ district: e.target.value })} style={{ ...inputStyle, flex: 1 }} />
      </div>
      <div style={{ display: 'flex', gap: '6px' }}>
        <input placeholder="Province *" required value={value.province} onChange={e => onChange({ province: e.target.value })} style={{ ...inputStyle, flex: 1 }} />
        <input placeholder="Postal" value={value.postalCode} onChange={e => onChange({ postalCode: e.target.value })} style={{ ...inputStyle, width: '80px' }} />
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

// ── Styles ──────────────────────────────────────────────────────────────────

const overlay: React.CSSProperties = {
  position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)',
  display: 'flex', alignItems: 'flex-start', justifyContent: 'center',
  zIndex: 100, overflowY: 'auto', padding: '40px 20px',
}
const modal: React.CSSProperties = {
  background: '#fff', borderRadius: '12px', width: '100%', maxWidth: '900px',
  boxShadow: '0 20px 60px rgba(0,0,0,0.2)',
}
const modalHeader: React.CSSProperties = {
  display: 'flex', justifyContent: 'space-between', alignItems: 'center',
  padding: '20px 24px', borderBottom: '1px solid #e5e7eb',
}
const modalBody: React.CSSProperties = {
  padding: '24px', display: 'flex', flexDirection: 'column', gap: '20px',
}
const modalFooter: React.CSSProperties = {
  padding: '16px 24px', borderTop: '1px solid #e5e7eb',
  display: 'flex', justifyContent: 'flex-end', gap: '8px',
}
const section: React.CSSProperties = { display: 'flex', flexDirection: 'column', gap: '12px' }
const sectionTitle: React.CSSProperties = { fontSize: '13px', fontWeight: 600, color: '#374151', margin: '0 0 4px', textTransform: 'uppercase', letterSpacing: '0.05em' }
const grid2: React.CSSProperties = { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }
const inputStyle: React.CSSProperties = { padding: '7px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '13px', width: '100%', boxSizing: 'border-box' }
const btnPrimary: React.CSSProperties = { padding: '8px 20px', borderRadius: '6px', border: 'none', background: '#2563eb', color: '#fff', cursor: 'pointer', fontSize: '13px', fontWeight: 500 }
const btnSecondary: React.CSSProperties = { padding: '8px 14px', borderRadius: '6px', border: '1px solid #d1d5db', background: '#fff', cursor: 'pointer', fontSize: '13px' }
const btnDanger: React.CSSProperties = { padding: '4px 10px', borderRadius: '6px', border: '1px solid #fecaca', background: '#fef2f2', color: '#dc2626', cursor: 'pointer', fontSize: '12px' }
const btnLink: React.CSSProperties = { background: 'none', border: 'none', color: '#2563eb', cursor: 'pointer', fontSize: '12px', padding: '4px 0' }
const closeBtn: React.CSSProperties = { background: 'none', border: 'none', cursor: 'pointer', fontSize: '18px', color: '#6b7280', lineHeight: 1 }
const errorBox: React.CSSProperties = { padding: '12px', borderRadius: '6px', background: '#fef2f2', border: '1px solid #fecaca', color: '#dc2626', fontSize: '13px' }

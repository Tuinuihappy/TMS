import { useRef, useState } from 'react'
import { shipmentApi, podApi } from '../../api/execution'
import type { ShipmentDto, PodDocumentDto, VerificationType } from '../../types/execution'

interface Props {
  shipment: ShipmentDto
  onClose: () => void
  onRefresh: () => void
}

type Panel = 'detail' | 'deliver' | 'pod'

const STATUS_COLOR: Record<string, string> = {
  Pending: '#6b7280', PickedUp: '#2563eb', InTransit: '#d97706',
  Arrived: '#7c3aed', Delivered: '#16a34a', PartialDelivered: '#059669',
  Rejected: '#dc2626', Exception: '#b45309',
}

const REASON_CODES = ['REFUSED', 'WRONG_ADDRESS', 'CLOSED', 'DAMAGED', 'NO_CONTACT', 'OTHER']

export function ShipmentDetail({ shipment: initial, onClose, onRefresh }: Props) {
  const [shipment, setShipment] = useState(initial)
  const [panel, setPanel] = useState<Panel>('detail')
  const [loading, setLoading] = useState(false)
  const [error, setError]   = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const canPickup  = shipment.status === 'Pending'
  const canArrive  = shipment.status === 'PickedUp' || shipment.status === 'InTransit'
  const canDeliver = shipment.status === 'Arrived'
  const canReject  = ['Pending','PickedUp','InTransit','Arrived'].includes(shipment.status)
  const canException = !['Delivered','Rejected'].includes(shipment.status)
  const canApprovePod = shipment.pod?.approvalStatus === 'Pending'

  const run = async (label: string, fn: () => Promise<void>, nextPanel?: Panel) => {
    setLoading(true); setError(null)
    try {
      await fn()
      setSuccess(`${label} recorded`)
      // Refresh shipment data inline
      const updated = await shipmentApi.get(shipment.id)
      setShipment(updated)
      if (nextPanel) setPanel(nextPanel)
      setTimeout(() => setSuccess(null), 2500)
      onRefresh()
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
            <h2 style={{ margin: 0, fontSize: '17px' }}>{shipment.shipmentNumber}</h2>
            <div style={{ display: 'flex', gap: '8px', alignItems: 'center', marginTop: '4px' }}>
              <span style={{ ...badge, background: STATUS_COLOR[shipment.status] ?? '#6b7280' }}>
                {shipment.status}
              </span>
              {shipment.addressName && (
                <span style={{ fontSize: '12px', color: '#6b7280' }}>{shipment.addressName}</span>
              )}
            </div>
          </div>
          <button onClick={onClose} style={closeBtn}>✕</button>
        </div>

        {/* Tabs */}
        <div style={tabs}>
          {(['detail', 'deliver', 'pod'] as Panel[]).map(p => (
            <button
              key={p}
              disabled={p === 'deliver' ? !canDeliver : false}
              onClick={() => { setPanel(p); setError(null); setSuccess(null) }}
              style={{ ...tab, ...(panel === p ? tabActive : {}) }}
            >
              {p === 'detail' ? 'Details' : p === 'deliver' ? 'Deliver / Partial' : 'POD Document'}
            </button>
          ))}
        </div>

        <div style={modalBody}>
          {error   && <div style={errBox}>{error}</div>}
          {success && <div style={okBox}>{success}</div>}

          {panel === 'detail' && (
            <DetailPanel
              shipment={shipment}
              loading={loading}
              canPickup={canPickup}
              canArrive={canArrive}
              canReject={canReject}
              canException={canException}
              canApprovePod={canApprovePod}
              onPickup={() => run('Pickup', () => shipmentApi.pickup(shipment.id))}
              onArrive={() => run('Arrive', () => shipmentApi.arrive(shipment.id))}
              onApprovePod={() => run('Approve POD', () => shipmentApi.approvePod(shipment.id, '00000000-0000-0000-0000-000000000001'))}
              onReject={(reason, code) => run('Reject', () => shipmentApi.reject(shipment.id, reason, code))}
              onException={(reason, code) => run('Exception', () => shipmentApi.exception(shipment.id, reason, code))}
            />
          )}

          {panel === 'deliver' && (
            <DeliverPanel
              shipment={shipment}
              loading={loading}
              onDeliver={(items, pod, partial) =>
                run(partial ? 'Partial Deliver' : 'Deliver',
                  () => partial
                    ? shipmentApi.partialDeliver(shipment.id, { items, pod })
                    : shipmentApi.deliver(shipment.id, { items, pod }),
                  'pod')
              }
            />
          )}

          {panel === 'pod' && (
            <PodPanel
              shipment={shipment}
              loading={loading}
              onUpload={(file, type) => run('Upload', () =>
                podApi.upload(shipment.id, file, type).then(() => {})
              )}
              onSubmit={() => run('Submit POD', () => podApi.submit(shipment.id))}
              onEvaluate={(approved, reason) =>
                run(approved ? 'Approve POD' : 'Reject POD',
                  () => podApi.evaluate(shipment.id, '00000000-0000-0000-0000-000000000001', approved, reason))
              }
              onGeneratePdf={() => run('Generate PDF', async () => {
                const r = await podApi.generatePdf(shipment.id)
                if (r.pdfUrl) window.open(r.pdfUrl, '_blank')
              })}
            />
          )}
        </div>
      </div>
    </div>
  )
}

// ── Detail Panel ──────────────────────────────────────────────────────────────

function DetailPanel({ shipment, loading, canPickup, canArrive, canReject, canException, canApprovePod,
  onPickup, onArrive, onApprovePod, onReject, onException }: {
  shipment: ShipmentDto; loading: boolean
  canPickup: boolean; canArrive: boolean; canReject: boolean
  canException: boolean; canApprovePod: boolean
  onPickup: () => void; onArrive: () => void; onApprovePod: () => void
  onReject: (reason: string, code: string) => void
  onException: (reason: string, code: string) => void
}) {
  const [showReject, setShowReject] = useState(false)
  const [showException, setShowException] = useState(false)
  const [reason, setReason]     = useState('')
  const [reasonCode, setReasonCode] = useState('REFUSED')

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      {/* Info grid */}
      <div style={infoGrid}>
        {[
          ['Shipment ID',  shipment.id,          true],
          ['Trip ID',      shipment.tripId,       true],
          ['Order ID',     shipment.orderId,      true],
          ['Address',      [shipment.addressStreet, shipment.addressProvince].filter(Boolean).join(', ') || '—'],
          ['Picked Up',    shipment.pickedUpAt   ? fmtDt(shipment.pickedUpAt)  : '—'],
          ['Arrived',      shipment.arrivedAt    ? fmtDt(shipment.arrivedAt)   : '—'],
          ['Delivered',    shipment.deliveredAt  ? fmtDt(shipment.deliveredAt) : '—'],
        ].map(([label, value, mono]) => (
          <div key={String(label)}>
            <div style={infoLabel}>{label}</div>
            <div style={{ fontSize: '13px', fontFamily: mono ? 'monospace' : undefined, wordBreak: 'break-all' }}>
              {String(value)}
            </div>
          </div>
        ))}
      </div>

      {/* Items */}
      <div>
        <div style={secTitle}>Items ({shipment.items.length})</div>
        <div style={{ border: '1px solid #e5e7eb', borderRadius: '6px', overflow: 'hidden', marginTop: '8px' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '12px' }}>
            <thead>
              <tr style={{ background: '#f9fafb' }}>
                {['Description','SKU','Expected','Delivered','Status'].map(h => (
                  <th key={h} style={{ padding: '7px 10px', textAlign: 'left', fontWeight: 600, color: '#6b7280', fontSize: '11px', textTransform: 'uppercase' }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {shipment.items.map(item => (
                <tr key={item.id} style={{ borderTop: '1px solid #f3f4f6' }}>
                  <td style={{ padding: '8px 10px' }}>{item.description}</td>
                  <td style={{ padding: '8px 10px', color: '#6b7280' }}>{item.sku ?? '—'}</td>
                  <td style={{ padding: '8px 10px' }}>{item.expectedQty}</td>
                  <td style={{ padding: '8px 10px' }}>{item.deliveredQty}</td>
                  <td style={{ padding: '8px 10px' }}>
                    <span style={{ ...badge, background: item.status === 'Delivered' ? '#16a34a' : item.status === 'Pending' ? '#6b7280' : '#d97706', fontSize: '10px' }}>
                      {item.status}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* POD quick info */}
      {shipment.pod && (
        <div style={{ padding: '10px', background: '#f0fdf4', borderRadius: '6px', border: '1px solid #bbf7d0', fontSize: '13px' }}>
          <div style={{ fontWeight: 600, marginBottom: '4px' }}>POD — {shipment.pod.approvalStatus}</div>
          {shipment.pod.receiverName && <div>Receiver: {shipment.pod.receiverName}</div>}
          <div style={{ color: '#6b7280', fontSize: '12px' }}>{shipment.pod.photoUrls.length} photo(s)</div>
        </div>
      )}

      {/* Exception info */}
      {shipment.exceptionReason && (
        <div style={{ padding: '10px', background: '#fef3c7', borderRadius: '6px', border: '1px solid #fde68a', fontSize: '13px' }}>
          <strong>Exception [{shipment.exceptionReasonCode}]:</strong> {shipment.exceptionReason}
        </div>
      )}

      {/* Reject / Exception forms */}
      {(showReject || showException) && (
        <div style={{ padding: '14px', background: '#fef2f2', borderRadius: '8px', border: '1px solid #fecaca', display: 'flex', flexDirection: 'column', gap: '10px' }}>
          <div style={{ fontWeight: 600, fontSize: '13px', color: '#991b1b' }}>
            {showReject ? 'Reject Shipment' : 'Record Exception'}
          </div>
          <Field label="Reason Code">
            <select value={reasonCode} onChange={e => setReasonCode(e.target.value)} style={inputStyle}>
              {REASON_CODES.map(c => <option key={c}>{c}</option>)}
            </select>
          </Field>
          <Field label="Reason Details">
            <textarea value={reason} onChange={e => setReason(e.target.value)} rows={2}
              style={{ ...inputStyle, resize: 'vertical' }} placeholder="Describe the issue…" />
          </Field>
          <div style={{ display: 'flex', gap: '8px' }}>
            <button
              onClick={() => {
                if (showReject) onReject(reason, reasonCode)
                else onException(reason, reasonCode)
                setShowReject(false); setShowException(false)
              }}
              disabled={loading || !reason.trim()}
              style={{ ...actionBtn, background: '#dc2626' }}
            >
              {loading ? 'Saving…' : 'Confirm'}
            </button>
            <button onClick={() => { setShowReject(false); setShowException(false) }} style={btnSec}>
              Cancel
            </button>
          </div>
        </div>
      )}

      {/* Primary action buttons */}
      <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', paddingTop: '8px', borderTop: '1px solid #f3f4f6' }}>
        {canPickup && (
          <button onClick={onPickup} disabled={loading} style={{ ...actionBtn, background: '#2563eb' }}>
            📦 Pickup
          </button>
        )}
        {canArrive && (
          <button onClick={onArrive} disabled={loading} style={{ ...actionBtn, background: '#7c3aed' }}>
            📍 Arrived
          </button>
        )}
        {canApprovePod && (
          <button onClick={onApprovePod} disabled={loading} style={{ ...actionBtn, background: '#16a34a' }}>
            ✓ Approve POD
          </button>
        )}
        {canReject && !showReject && !showException && (
          <button onClick={() => setShowReject(true)} style={{ ...actionBtn, background: '#dc2626' }}>
            ✗ Reject
          </button>
        )}
        {canException && !showReject && !showException && (
          <button onClick={() => setShowException(true)} style={{ ...actionBtn, background: '#b45309' }}>
            ⚠ Exception
          </button>
        )}
      </div>
    </div>
  )
}

// ── Deliver Panel ─────────────────────────────────────────────────────────────

function DeliverPanel({ shipment, loading, onDeliver }: {
  shipment: ShipmentDto; loading: boolean
  onDeliver: (
    items: Array<{ shipmentItemId: string; deliveredQty: number }>,
    pod: { receiverName?: string; signatureUrl?: string; latitude?: number; longitude?: number },
    partial: boolean
  ) => void
}) {
  const [qtys, setQtys] = useState<Record<string, number>>(
    Object.fromEntries(shipment.items.map(i => [i.id, i.expectedQty]))
  )
  const [receiverName, setReceiverName] = useState('')
  const [signatureUrl, setSignatureUrl] = useState('')
  const [lat, setLat] = useState('')
  const [lng, setLng] = useState('')

  const isPartial = shipment.items.some(i => (qtys[i.id] ?? i.expectedQty) < i.expectedQty)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    const items = shipment.items.map(i => ({ shipmentItemId: i.id, deliveredQty: qtys[i.id] ?? i.expectedQty }))
    const pod = {
      receiverName: receiverName || undefined,
      signatureUrl: signatureUrl || undefined,
      latitude:  lat  ? Number(lat)  : undefined,
      longitude: lng  ? Number(lng)  : undefined,
    }
    onDeliver(items, pod, isPartial)
  }

  return (
    <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      {/* Items quantity */}
      <div>
        <div style={secTitle}>Delivered Quantities</div>
        <div style={{ border: '1px solid #e5e7eb', borderRadius: '6px', overflow: 'hidden', marginTop: '8px' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '13px' }}>
            <thead>
              <tr style={{ background: '#f9fafb' }}>
                {['Item','Expected','Delivered'].map(h => (
                  <th key={h} style={{ padding: '8px 12px', textAlign: 'left', fontWeight: 600, fontSize: '11px', color: '#6b7280', textTransform: 'uppercase' }}>{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {shipment.items.map(item => (
                <tr key={item.id} style={{ borderTop: '1px solid #f3f4f6' }}>
                  <td style={{ padding: '8px 12px' }}>
                    <div style={{ fontWeight: 500 }}>{item.description}</div>
                    {item.sku && <div style={{ fontSize: '11px', color: '#9ca3af' }}>{item.sku}</div>}
                  </td>
                  <td style={{ padding: '8px 12px', color: '#6b7280' }}>{item.expectedQty}</td>
                  <td style={{ padding: '8px 12px' }}>
                    <input
                      type="number" min={0} max={item.expectedQty}
                      value={qtys[item.id] ?? item.expectedQty}
                      onChange={e => setQtys(q => ({ ...q, [item.id]: Number(e.target.value) }))}
                      style={{ ...inputStyle, width: '70px' }}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {isPartial && (
          <div style={{ marginTop: '8px', padding: '8px 10px', background: '#fef3c7', borderRadius: '6px', border: '1px solid #fde68a', fontSize: '12px', color: '#92400e' }}>
            ⚠ Partial delivery detected — some items are under expected quantity.
          </div>
        )}
      </div>

      {/* POD fields */}
      <div>
        <div style={secTitle}>Proof of Delivery</div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', marginTop: '8px' }}>
          <Field label="Receiver Name">
            <input value={receiverName} onChange={e => setReceiverName(e.target.value)} style={inputStyle} placeholder="ชื่อผู้รับ" />
          </Field>
          <Field label="Signature URL">
            <input value={signatureUrl} onChange={e => setSignatureUrl(e.target.value)} style={inputStyle} placeholder="https://…" />
          </Field>
          <Field label="GPS Latitude">
            <input type="number" step="0.00001" value={lat} onChange={e => setLat(e.target.value)} style={inputStyle} placeholder="13.7563" />
          </Field>
          <Field label="GPS Longitude">
            <input type="number" step="0.00001" value={lng} onChange={e => setLng(e.target.value)} style={inputStyle} placeholder="100.5018" />
          </Field>
        </div>
      </div>

      <div style={{ display: 'flex', justifyContent: 'flex-end', paddingTop: '8px', borderTop: '1px solid #f3f4f6' }}>
        <button type="submit" disabled={loading} style={{ ...actionBtn, background: isPartial ? '#d97706' : '#16a34a' }}>
          {loading ? 'Saving…' : isPartial ? '📦 Partial Deliver' : '✓ Deliver'}
        </button>
      </div>
    </form>
  )
}

// ── POD Panel ─────────────────────────────────────────────────────────────────

function PodPanel({ shipment, loading, onUpload, onSubmit, onEvaluate, onGeneratePdf }: {
  shipment: ShipmentDto; loading: boolean
  onUpload: (file: File, type: string) => void
  onSubmit: () => void
  onEvaluate: (approved: boolean, reason?: string) => void
  onGeneratePdf: () => void
}) {
  const [verType, setVerType] = useState<VerificationType>('BoxPhoto')
  const [rejectReason, setRejectReason] = useState('')
  const [showRejectForm, setShowRejectForm] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  const pod = shipment.pod
  const podDoc = null as PodDocumentDto | null // evaluated via separate fetch if needed

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Current POD status */}
      {pod ? (
        <div style={{ padding: '14px', background: '#f0fdf4', borderRadius: '8px', border: '1px solid #bbf7d0' }}>
          <div style={{ fontWeight: 600, marginBottom: '8px' }}>
            POD Status: <span style={{ color: podApprovalColor(pod.approvalStatus) }}>{pod.approvalStatus}</span>
          </div>
          {pod.receiverName && <div style={{ fontSize: '13px' }}>Receiver: {pod.receiverName}</div>}
          <div style={{ fontSize: '12px', color: '#6b7280', marginTop: '4px' }}>
            Captured: {fmtDt(pod.capturedAt)} · {pod.photoUrls.length} photo(s)
          </div>
          {pod.photoUrls.length > 0 && (
            <div style={{ display: 'flex', gap: '8px', marginTop: '8px', flexWrap: 'wrap' }}>
              {pod.photoUrls.map((url, i) => (
                <a key={i} href={url} target="_blank" rel="noreferrer"
                  style={{ display: 'inline-block', padding: '4px 8px', background: '#fff', border: '1px solid #bbf7d0', borderRadius: '4px', fontSize: '12px', color: '#2563eb' }}>
                  📷 Photo {i + 1}
                </a>
              ))}
            </div>
          )}
        </div>
      ) : (
        <div style={{ padding: '12px', background: '#f9fafb', borderRadius: '6px', border: '1px solid #e5e7eb', fontSize: '13px', color: '#6b7280' }}>
          No POD recorded yet.
        </div>
      )}

      {/* Upload attachment */}
      <div>
        <div style={secTitle}>Upload Attachment</div>
        <div style={{ display: 'flex', gap: '10px', marginTop: '8px', alignItems: 'center', flexWrap: 'wrap' }}>
          <select value={verType} onChange={e => setVerType(e.target.value as VerificationType)} style={{ ...inputStyle, width: 'auto' }}>
            <option value="BoxPhoto">Box Photo</option>
            <option value="Signature">Signature</option>
            <option value="DocumentScan">Document Scan</option>
          </select>
          <input ref={fileRef} type="file" accept="image/*,.pdf" style={{ display: 'none' }}
            onChange={e => { const f = e.target.files?.[0]; if (f) { onUpload(f, verType); e.target.value = '' } }}
          />
          <button onClick={() => fileRef.current?.click()} disabled={loading} style={btnSec}>
            📎 Choose File
          </button>
        </div>
      </div>

      {/* Submit POD */}
      <div>
        <div style={secTitle}>Submit POD</div>
        <p style={{ fontSize: '12px', color: '#6b7280', margin: '4px 0 8px' }}>
          Submit the POD for review. The system will auto-approve if geo-distance is within threshold.
        </p>
        <button onClick={onSubmit} disabled={loading} style={{ ...actionBtn, background: '#2563eb' }}>
          {loading ? 'Submitting…' : '📤 Submit POD'}
        </button>
      </div>

      {/* Evaluate (Approve / Reject) */}
      {pod?.approvalStatus === 'Pending' && (
        <div>
          <div style={secTitle}>Evaluate POD (Dispatcher)</div>
          {!showRejectForm ? (
            <div style={{ display: 'flex', gap: '8px', marginTop: '8px' }}>
              <button onClick={() => onEvaluate(true)} disabled={loading} style={{ ...actionBtn, background: '#16a34a' }}>
                ✓ Approve
              </button>
              <button onClick={() => setShowRejectForm(true)} style={{ ...actionBtn, background: '#dc2626' }}>
                ✗ Reject
              </button>
            </div>
          ) : (
            <div style={{ marginTop: '8px', display: 'flex', flexDirection: 'column', gap: '8px' }}>
              <Field label="Reject Reason">
                <textarea value={rejectReason} onChange={e => setRejectReason(e.target.value)} rows={2}
                  style={{ ...inputStyle, resize: 'vertical' }} placeholder="Reason for rejection…" />
              </Field>
              <div style={{ display: 'flex', gap: '8px' }}>
                <button onClick={() => { onEvaluate(false, rejectReason); setShowRejectForm(false) }}
                  disabled={loading || !rejectReason.trim()} style={{ ...actionBtn, background: '#dc2626' }}>
                  {loading ? 'Saving…' : 'Confirm Reject'}
                </button>
                <button onClick={() => setShowRejectForm(false)} style={btnSec}>Cancel</button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Generate PDF */}
      {(pod?.approvalStatus === 'Approved' || pod?.approvalStatus === 'AutoApproved') && (
        <div>
          <div style={secTitle}>E-Receipt PDF</div>
          <button onClick={onGeneratePdf} disabled={loading} style={{ ...actionBtn, background: '#6b7280', marginTop: '8px' }}>
            {loading ? 'Generating…' : '📄 Generate PDF'}
          </button>
          {podDoc?.status && (
            <div style={{ marginTop: '6px', fontSize: '12px', color: '#6b7280' }}>
              Doc Ref: {podDoc.documentReference}
            </div>
          )}
        </div>
      )}
    </div>
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

function fmtDt(d: string) {
  return new Date(d).toLocaleString('th-TH', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function podApprovalColor(s: string) {
  return s === 'Approved' || s === 'AutoApproved' ? '#16a34a' : s === 'Rejected' ? '#dc2626' : '#d97706'
}

// ── Styles ────────────────────────────────────────────────────────────────────

const overlay: React.CSSProperties     = { position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'flex-start', justifyContent: 'center', zIndex: 100, overflowY: 'auto', padding: '40px 20px' }
const modal: React.CSSProperties       = { background: '#fff', borderRadius: '12px', width: '100%', maxWidth: '720px', boxShadow: '0 20px 60px rgba(0,0,0,0.2)' }
const modalHeader: React.CSSProperties = { display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', padding: '18px 24px', borderBottom: '1px solid #e5e7eb' }
const modalBody: React.CSSProperties   = { padding: '20px 24px' }
const tabs: React.CSSProperties        = { display: 'flex', borderBottom: '1px solid #e5e7eb', padding: '0 24px' }
const tab: React.CSSProperties         = { padding: '10px 16px', background: 'none', border: 'none', cursor: 'pointer', fontSize: '13px', fontWeight: 500, color: '#6b7280', borderBottom: '2px solid transparent', marginBottom: '-1px' }
const tabActive: React.CSSProperties   = { color: '#2563eb', borderBottom: '2px solid #2563eb' }
const badge: React.CSSProperties       = { display: 'inline-block', padding: '2px 8px', borderRadius: '12px', fontSize: '11px', fontWeight: 600, color: '#fff' }
const infoGrid: React.CSSProperties    = { display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }
const infoLabel: React.CSSProperties   = { fontSize: '10px', fontWeight: 700, color: '#9ca3af', textTransform: 'uppercase', letterSpacing: '0.05em', marginBottom: '2px' }
const secTitle: React.CSSProperties    = { fontSize: '11px', fontWeight: 700, color: '#374151', textTransform: 'uppercase', letterSpacing: '0.05em' }
const inputStyle: React.CSSProperties  = { padding: '7px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '13px', width: '100%', boxSizing: 'border-box' }
const actionBtn: React.CSSProperties   = { padding: '7px 14px', borderRadius: '6px', border: 'none', color: '#fff', cursor: 'pointer', fontSize: '13px', fontWeight: 500 }
const btnSec: React.CSSProperties      = { padding: '7px 12px', borderRadius: '6px', border: '1px solid #d1d5db', background: '#fff', cursor: 'pointer', fontSize: '13px' }
const closeBtn: React.CSSProperties    = { background: 'none', border: 'none', cursor: 'pointer', fontSize: '18px', color: '#6b7280', lineHeight: 1 }
const errBox: React.CSSProperties      = { padding: '10px', borderRadius: '6px', background: '#fef2f2', border: '1px solid #fecaca', color: '#dc2626', fontSize: '13px', marginBottom: '8px' }
const okBox: React.CSSProperties       = { padding: '10px', borderRadius: '6px', background: '#f0fdf4', border: '1px solid #bbf7d0', color: '#16a34a', fontSize: '13px', marginBottom: '8px' }

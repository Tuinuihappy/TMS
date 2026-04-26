import { useRef, useState } from 'react'
import { ordersApi } from '../api/orders'
import type { ImportOrdersResult } from '../types/order'

interface Props {
  onClose: () => void
  onSuccess: () => void
}

const TEMPLATE_CSV = `CustomerId,Priority,PickupStreet,PickupSubDistrict,PickupDistrict,PickupProvince,PickupPostalCode,PickupLatitude,PickupLongitude,DropoffStreet,DropoffSubDistrict,DropoffDistrict,DropoffProvince,DropoffPostalCode,DropoffLatitude,DropoffLongitude,ItemDescription,WeightKg,VolumeCBM,Quantity
00000000-0000-0000-0000-000000000000,Normal,1 Main St,SubDistrict,District,Bangkok,10000,13.7563,100.5018,99 End Rd,SubDistrict2,District2,Bangkok,10100,13.7200,100.5200,Sample Item,50.0,0.5,2`

function downloadTemplate() {
  const blob = new Blob([TEMPLATE_CSV], { type: 'text/csv' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'orders_template.csv'
  a.click()
  URL.revokeObjectURL(url)
}

export function ImportOrders({ onClose, onSuccess }: Props) {
  const [file, setFile] = useState<File | null>(null)
  const [dragging, setDragging] = useState(false)
  const [loading, setLoading] = useState(false)
  const [result, setResult] = useState<ImportOrdersResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  const handleFile = (f: File) => {
    setFile(f)
    setResult(null)
    setError(null)
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    setDragging(false)
    const f = e.dataTransfer.files[0]
    if (f) handleFile(f)
  }

  const handleSubmit = async () => {
    if (!file) return
    setLoading(true)
    setError(null)
    try {
      const res = await ordersApi.import(file)
      setResult(res)
      if (res.failCount === 0) {
        setTimeout(onSuccess, 1200)
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Import failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={overlay}>
      <div style={modal}>
        <div style={modalHeader}>
          <h2 style={{ margin: 0, fontSize: '18px' }}>Import Orders (CSV / Excel)</h2>
          <button onClick={onClose} style={closeBtn}>✕</button>
        </div>

        <div style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
          {error && <div style={errorBox}>{error}</div>}

          {/* Drop zone */}
          {!result && (
            <div
              style={{
                ...dropZone,
                background: dragging ? '#eff6ff' : '#f9fafb',
                borderColor: dragging ? '#2563eb' : '#d1d5db',
              }}
              onDragOver={e => { e.preventDefault(); setDragging(true) }}
              onDragLeave={() => setDragging(false)}
              onDrop={handleDrop}
              onClick={() => inputRef.current?.click()}
            >
              <input
                ref={inputRef}
                type="file"
                accept=".csv,.xlsx"
                style={{ display: 'none' }}
                onChange={e => e.target.files?.[0] && handleFile(e.target.files[0])}
              />
              <div style={{ fontSize: '32px', marginBottom: '8px' }}>📂</div>
              {file ? (
                <div>
                  <div style={{ fontWeight: 600, fontSize: '14px' }}>{file.name}</div>
                  <div style={{ color: '#6b7280', fontSize: '12px', marginTop: '4px' }}>
                    {(file.size / 1024).toFixed(1)} KB
                  </div>
                </div>
              ) : (
                <div style={{ textAlign: 'center' }}>
                  <div style={{ fontWeight: 500, fontSize: '14px' }}>Drop CSV or Excel file here</div>
                  <div style={{ color: '#9ca3af', fontSize: '12px', marginTop: '4px' }}>or click to browse · max 10 MB</div>
                </div>
              )}
            </div>
          )}

          {/* Result */}
          {result && (
            <div style={{ border: '1px solid #e5e7eb', borderRadius: '8px', overflow: 'hidden' }}>
              <div style={{
                padding: '16px',
                background: result.failCount === 0 ? '#f0fdf4' : '#fef3c7',
                display: 'flex', gap: '24px',
              }}>
                <Stat label="Total Rows" value={result.totalRows} />
                <Stat label="Imported" value={result.successCount} color="#16a34a" />
                <Stat label="Failed" value={result.failCount} color={result.failCount > 0 ? '#dc2626' : undefined} />
              </div>
              {result.errors.length > 0 && (
                <div style={{ padding: '12px 16px', maxHeight: '200px', overflowY: 'auto' }}>
                  <div style={{ fontSize: '12px', fontWeight: 600, color: '#374151', marginBottom: '8px' }}>ERRORS</div>
                  {result.errors.map((e, i) => (
                    <div key={i} style={{ fontSize: '12px', color: '#dc2626', padding: '4px 0', borderBottom: '1px solid #fef2f2' }}>
                      Row {e.row}: {e.message}
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* Template download hint */}
          <div style={{ fontSize: '12px', color: '#6b7280' }}>
            Need the correct format?{' '}
            <button onClick={downloadTemplate} style={btnLink}>Download CSV template</button>
          </div>
        </div>

        <div style={modalFooter}>
          <button onClick={onClose} style={btnSecondary}>
            {result ? 'Close' : 'Cancel'}
          </button>
          {!result && (
            <button onClick={handleSubmit} disabled={!file || loading} style={btnPrimary}>
              {loading ? 'Uploading…' : 'Import'}
            </button>
          )}
          {result && result.failCount > 0 && (
            <button onClick={() => { setResult(null); setFile(null) }} style={btnSecondary}>
              Try Again
            </button>
          )}
        </div>
      </div>
    </div>
  )
}

function Stat({ label, value, color }: { label: string; value: number; color?: string }) {
  return (
    <div>
      <div style={{ fontSize: '20px', fontWeight: 700, color: color ?? '#111827' }}>{value}</div>
      <div style={{ fontSize: '11px', color: '#6b7280' }}>{label}</div>
    </div>
  )
}

// ── Styles ──────────────────────────────────────────────────────────────────

const overlay: React.CSSProperties = {
  position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)',
  display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 100,
}
const modal: React.CSSProperties = {
  background: '#fff', borderRadius: '12px', width: '100%', maxWidth: '500px',
  boxShadow: '0 20px 60px rgba(0,0,0,0.2)',
}
const modalHeader: React.CSSProperties = {
  display: 'flex', justifyContent: 'space-between', alignItems: 'center',
  padding: '20px 24px', borderBottom: '1px solid #e5e7eb',
}
const modalFooter: React.CSSProperties = {
  display: 'flex', justifyContent: 'flex-end', gap: '8px',
  padding: '16px 24px', borderTop: '1px solid #e5e7eb',
}
const dropZone: React.CSSProperties = {
  border: '2px dashed', borderRadius: '8px', padding: '40px 24px',
  display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
  cursor: 'pointer', transition: 'all 0.15s',
}
const btnPrimary: React.CSSProperties = {
  padding: '8px 20px', borderRadius: '6px', border: 'none',
  background: '#2563eb', color: '#fff', cursor: 'pointer', fontSize: '13px', fontWeight: 500,
}
const btnSecondary: React.CSSProperties = {
  padding: '8px 14px', borderRadius: '6px', border: '1px solid #d1d5db',
  background: '#fff', cursor: 'pointer', fontSize: '13px',
}
const btnLink: React.CSSProperties = {
  background: 'none', border: 'none', color: '#2563eb', cursor: 'pointer', fontSize: '12px', padding: 0,
}
const closeBtn: React.CSSProperties = {
  background: 'none', border: 'none', cursor: 'pointer', fontSize: '18px', color: '#6b7280', lineHeight: 1,
}
const errorBox: React.CSSProperties = {
  padding: '12px', borderRadius: '6px', background: '#fef2f2',
  border: '1px solid #fecaca', color: '#dc2626', fontSize: '13px',
}

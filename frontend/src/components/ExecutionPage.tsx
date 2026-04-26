import { useState } from 'react'
import { ShipmentList, DriverTodayView } from './execution/ShipmentList'
import { ShipmentDetail } from './execution/ShipmentDetail'
import type { ShipmentDto } from '../types/execution'

type Section = 'list' | 'driver'

export function ExecutionPage() {
  const [section, setSection]         = useState<Section>('list')
  const [selected, setSelected]       = useState<ShipmentDto | null>(null)
  const [refreshKey, setRefreshKey]   = useState(0)

  const refresh = () => setRefreshKey(k => k + 1)

  return (
    <div style={{ padding: '24px' }}>
      {/* Section tabs */}
      <div style={{ display: 'flex', gap: '0', marginBottom: '24px', borderBottom: '2px solid #e5e7eb' }}>
        {([
          ['list',   '📋 Shipments'],
          ['driver', '🚚 Driver View'],
        ] as [Section, string][]).map(([key, label]) => (
          <button
            key={key}
            onClick={() => setSection(key)}
            style={{
              padding: '10px 20px',
              background: 'none', border: 'none',
              borderBottom: `2px solid ${section === key ? '#2563eb' : 'transparent'}`,
              marginBottom: '-2px', cursor: 'pointer',
              fontSize: '14px',
              fontWeight: section === key ? 600 : 400,
              color: section === key ? '#2563eb' : '#6b7280',
            }}
          >
            {label}
          </button>
        ))}
      </div>

      {section === 'list'   && <ShipmentList     onSelect={setSelected} refreshKey={refreshKey} />}
      {section === 'driver' && <DriverTodayView  onSelect={setSelected} refreshKey={refreshKey} />}

      {selected && (
        <ShipmentDetail
          shipment={selected}
          onClose={() => setSelected(null)}
          onRefresh={() => { setSelected(null); refresh() }}
        />
      )}
    </div>
  )
}

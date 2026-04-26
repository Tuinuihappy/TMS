import { useState } from 'react'
import { RoutePlanList } from './planning/RoutePlanList'
import { RoutePlanDetail } from './planning/RoutePlanDetail'
import { AutoPlanForm } from './planning/AutoPlanForm'
import { TripList, DispatchBoard } from './planning/TripList'
import { TripDetail } from './planning/TripDetail'
import type { RoutePlanDto } from '../types/planning'
import type { TripDto } from '../types/planning'

type Section = 'routeplans' | 'dispatch' | 'trips'

export function PlanningPage() {
  const [section, setSection] = useState<Section>('dispatch')
  const [selectedPlan, setSelectedPlan] = useState<RoutePlanDto | null>(null)
  const [selectedTrip, setSelectedTrip] = useState<TripDto | null>(null)
  const [showAutoPlan, setShowAutoPlan] = useState(false)
  const [refreshKey, setRefreshKey] = useState(0)

  const refresh = () => setRefreshKey(k => k + 1)

  return (
    <div style={{ padding: '24px' }}>
      {/* Section tabs */}
      <div style={{ display: 'flex', gap: '0', marginBottom: '24px', borderBottom: '2px solid #e5e7eb' }}>
        {([
          ['dispatch', '📋 Dispatch Board'],
          ['routeplans', '🗺 Route Plans'],
          ['trips', '🚚 Trips'],
        ] as [Section, string][]).map(([key, label]) => (
          <button
            key={key}
            onClick={() => setSection(key)}
            style={{
              padding: '10px 20px',
              background: 'none',
              border: 'none',
              borderBottom: `2px solid ${section === key ? '#2563eb' : 'transparent'}`,
              marginBottom: '-2px',
              cursor: 'pointer',
              fontSize: '14px',
              fontWeight: section === key ? 600 : 400,
              color: section === key ? '#2563eb' : '#6b7280',
            }}
          >
            {label}
          </button>
        ))}
      </div>

      {/* Dispatch Board */}
      {section === 'dispatch' && (
        <DispatchBoard onSelect={setSelectedTrip} refreshKey={refreshKey} />
      )}

      {/* Route Plans */}
      {section === 'routeplans' && (
        <RoutePlanList
          onSelect={setSelectedPlan}
          onNewPlan={() => setShowAutoPlan(true)}
          refreshKey={refreshKey}
        />
      )}

      {/* Trips list */}
      {section === 'trips' && (
        <TripList onSelect={setSelectedTrip} refreshKey={refreshKey} />
      )}

      {/* Modals */}
      {selectedPlan && (
        <RoutePlanDetail
          plan={selectedPlan}
          onClose={() => setSelectedPlan(null)}
          onRefresh={() => { setSelectedPlan(null); refresh() }}
        />
      )}

      {selectedTrip && (
        <TripDetail
          trip={selectedTrip}
          onClose={() => setSelectedTrip(null)}
          onRefresh={() => { setSelectedTrip(null); refresh() }}
        />
      )}

      {showAutoPlan && (
        <AutoPlanForm
          onClose={() => setShowAutoPlan(false)}
          onSuccess={() => { setShowAutoPlan(false); setSection('routeplans'); refresh() }}
        />
      )}
    </div>
  )
}

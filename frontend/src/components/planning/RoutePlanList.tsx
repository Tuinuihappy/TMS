import { useCallback, useEffect, useState } from 'react'
import { routePlanApi } from '../../api/planning'
import type { RoutePlanDto } from '../../types/planning'

const STATUS_COLOR: Record<string, string> = {
  Draft: '#6b7280', Locked: '#2563eb', Executing: '#d97706', Completed: '#16a34a',
}

interface Props {
  onSelect: (plan: RoutePlanDto) => void
  onNewPlan: () => void
  refreshKey: number
}

export function RoutePlanList({ onSelect, onNewPlan, refreshKey }: Props) {
  const [plans, setPlans] = useState<RoutePlanDto[]>([])
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true); setError(null)
    try {
      const res = await routePlanApi.list(date)
      setPlans(res.items ?? [])
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load')
    } finally {
      setLoading(false)
    }
  }, [date, refreshKey]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => { load() }, [load])

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
        <h2 style={{ margin: 0, fontSize: '16px', fontWeight: 600 }}>Route Plans</h2>
        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
          <input type="date" value={date} onChange={e => setDate(e.target.value)} style={inputStyle} />
          <button onClick={load} style={btnSecondary}>Refresh</button>
          <button onClick={onNewPlan} style={btnPrimary}>+ Auto Plan</button>
        </div>
      </div>

      {error && <div style={errorBox}>{error}</div>}

      {loading ? (
        <div style={emptyState}>Loading…</div>
      ) : plans.length === 0 ? (
        <div style={emptyState}>No plans for {date}</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          {plans.map(plan => (
            <div key={plan.id} onClick={() => onSelect(plan)} style={planCard}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div>
                  <div style={{ fontWeight: 600, fontSize: '14px' }}>{plan.planNumber}</div>
                  <div style={{ color: '#6b7280', fontSize: '12px', marginTop: '2px' }}>
                    {plan.stops.length} stops · {plan.totalDistanceKm.toFixed(1)} km · ~{plan.estimatedTotalDurationMin} min
                  </div>
                </div>
                <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                  <span style={{ ...badge, background: STATUS_COLOR[plan.status] ?? '#6b7280' }}>
                    {plan.status}
                  </span>
                  <span style={{ fontSize: '12px', color: '#6b7280' }}>
                    {plan.capacityUtilizationPercent}% cap
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

const inputStyle: React.CSSProperties = { padding: '6px 10px', borderRadius: '6px', border: '1px solid #d1d5db', fontSize: '13px' }
const btnPrimary: React.CSSProperties = { padding: '7px 14px', borderRadius: '6px', border: 'none', background: '#2563eb', color: '#fff', cursor: 'pointer', fontSize: '13px', fontWeight: 500 }
const btnSecondary: React.CSSProperties = { padding: '7px 12px', borderRadius: '6px', border: '1px solid #d1d5db', background: '#fff', cursor: 'pointer', fontSize: '13px' }
const badge: React.CSSProperties = { display: 'inline-block', padding: '2px 8px', borderRadius: '12px', fontSize: '11px', fontWeight: 600, color: '#fff' }
const planCard: React.CSSProperties = { padding: '14px 16px', background: '#fff', borderRadius: '8px', border: '1px solid #e5e7eb', cursor: 'pointer', transition: 'border-color 0.15s' }
const emptyState: React.CSSProperties = { textAlign: 'center', padding: '40px', color: '#9ca3af', fontSize: '13px' }
const errorBox: React.CSSProperties = { padding: '10px', borderRadius: '6px', background: '#fef2f2', border: '1px solid #fecaca', color: '#dc2626', fontSize: '13px', marginBottom: '12px' }

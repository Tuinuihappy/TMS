import type {
  DispatchBoardDto,
  OptimizationStatusDto,
  PlanWithSplitRequest,
  RoutePlanDto,
  TripDto,
} from '../types/planning'
import type { PagedResult } from '../types/order'

const DEV_HEADERS: Record<string, string> = {
  'Content-Type': 'application/json',
  'X-TenantId': '00000000-0000-0000-0000-000000000002',
  'X-UserId': '00000000-0000-0000-0000-000000000001',
}

async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    headers: { ...DEV_HEADERS, ...init?.headers },
  })
  if (!res.ok) {
    const body = await res.json().catch(() => ({ detail: res.statusText }))
    throw new Error(body?.detail ?? body?.title ?? `HTTP ${res.status}`)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

// ── Route Plans ───────────────────────────────────────────────────────────────

export const routePlanApi = {
  list(date?: string): Promise<{ items: RoutePlanDto[] }> {
    const q = new URLSearchParams()
    if (date) q.set('date', date)
    return apiFetch(`/api/planning/plans?${q}`)
  },

  get(id: string): Promise<RoutePlanDto> {
    return apiFetch(`/api/planning/plans/${id}`)
  },

  reorderStops(planId: string, reorder: Array<{ stopId: string; newSequence: number }>): Promise<void> {
    return apiFetch(`/api/planning/plans/${planId}/stops`, {
      method: 'PUT',
      body: JSON.stringify({ reorder }),
    })
  },

  lock(planId: string): Promise<void> {
    return apiFetch(`/api/planning/plans/${planId}/lock`, { method: 'PUT' })
  },

  optimize(payload: {
    orders: Array<{
      orderId: string; orderStopId: string
      pickupLat: number; pickupLng: number
      dropoffLat: number; dropoffLng: number
      weightKg?: number; volumeCBM?: number
    }>
    tenantId: string
    plannedDate: string
    maxOrdersPerRoute?: number
    maxCapacityKg?: number
    depotLat?: number
    depotLng?: number
  }): Promise<{ optimizationRequestId: string; status: string }> {
    return apiFetch('/api/planning/optimize', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  getOptimizationStatus(id: string): Promise<OptimizationStatusDto> {
    return apiFetch(`/api/planning/optimize/${id}`)
  },

  planWithSplit(payload: PlanWithSplitRequest): Promise<unknown> {
    return apiFetch('/api/planning/plan-with-split', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },
}

// ── Trips ────────────────────────────────────────────────────────────────────

export const tripApi = {
  list(params: { page?: number; pageSize?: number; status?: string; date?: string } = {}): Promise<PagedResult<TripDto>> {
    const q = new URLSearchParams()
    if (params.page) q.set('page', String(params.page))
    if (params.pageSize) q.set('pageSize', String(params.pageSize))
    if (params.status) q.set('status', params.status)
    if (params.date) q.set('date', params.date)
    return apiFetch(`/api/trips?${q}`)
  },

  board(date?: string): Promise<DispatchBoardDto> {
    const q = new URLSearchParams()
    if (date) q.set('date', date)
    return apiFetch(`/api/trips/board?${q}`)
  },

  get(id: string): Promise<TripDto> {
    return apiFetch(`/api/trips/${id}`)
  },

  create(payload: {
    plannedDate: string
    tenantId: string
    totalWeight?: number
    totalVolumeCBM?: number
  }): Promise<{ id: string }> {
    return apiFetch('/api/trips', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  addStop(tripId: string, stop: {
    sequence: number; orderId: string; type: string
    addressName?: string; addressStreet?: string; addressProvince?: string
    lat?: number; lng?: number
    windowFrom?: string; windowTo?: string
  }): Promise<{ id: string }> {
    return apiFetch(`/api/trips/${tripId}/stops`, {
      method: 'POST',
      body: JSON.stringify(stop),
    })
  },

  assign(tripId: string, vehicleId: string, driverId: string): Promise<void> {
    return apiFetch(`/api/trips/${tripId}/assign`, {
      method: 'PUT',
      body: JSON.stringify({ vehicleId, driverId }),
    })
  },

  reassign(tripId: string, vehicleId: string, driverId: string): Promise<void> {
    return apiFetch(`/api/trips/${tripId}/reassign`, {
      method: 'PUT',
      body: JSON.stringify({ vehicleId, driverId }),
    })
  },

  dispatch(tripId: string): Promise<void> {
    return apiFetch(`/api/trips/${tripId}/dispatch`, { method: 'PUT' })
  },

  cancel(tripId: string, reason: string): Promise<void> {
    return apiFetch(`/api/trips/${tripId}/cancel`, {
      method: 'PUT',
      body: JSON.stringify({ reason }),
    })
  },

  complete(tripId: string): Promise<void> {
    return apiFetch(`/api/trips/${tripId}/complete`, { method: 'PUT' })
  },
}

// ── Resource lookups ─────────────────────────────────────────────────────────

export interface VehicleSummary { id: string; plateNumber: string; category: string }
export interface DriverSummary  { id: string; fullName: string; employeeCode: string }

export const resourceApi = {
  vehicles(): Promise<{ items: VehicleSummary[] }> {
    return apiFetch('/api/vehicles?pageSize=100')
  },
  drivers(): Promise<{ items: DriverSummary[] }> {
    return apiFetch('/api/drivers?pageSize=100')
  },
}

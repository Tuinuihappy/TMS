// ── Route Plans ───────────────────────────────────────────────────────────────

export type RoutePlanStatus = 'Draft' | 'Locked' | 'Executing' | 'Completed'

export interface RouteStopDto {
  id: string
  sequence: number
  orderId: string
  orderStopId: string
  stopType: 'Pickup' | 'Dropoff' | string
  lat: number
  lng: number
  estimatedArrivalTime?: string
  estimatedDepartureTime?: string
}

export interface RoutePlanDto {
  id: string
  planNumber: string
  status: RoutePlanStatus
  vehicleTypeId?: string
  plannedDate: string
  totalDistanceKm: number
  estimatedTotalDurationMin: number
  capacityUtilizationPercent: number
  createdAt: string
  stops: RouteStopDto[]
}

export interface OptimizationStatusDto {
  id: string
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed'
  requestedAt: string
  completedAt?: string
  error?: string
}

// ── Trips ────────────────────────────────────────────────────────────────────

export type TripStatus = 'Created' | 'Assigned' | 'Dispatched' | 'InTransit' | 'Completed' | 'Cancelled'

export interface TripStopDto {
  id: string
  sequence: number
  orderId: string
  type: string
  status: string
  addressName?: string
  addressProvince?: string
  windowFrom?: string
  windowTo?: string
  arrivalAt?: string
  departureAt?: string
}

export interface TripDto {
  id: string
  tripNumber: string
  status: TripStatus
  vehicleId?: string
  driverId?: string
  plannedDate: string
  totalWeight: number
  totalVolumeCBM: number
  totalDistanceKm?: number
  estimatedDurationMin?: number
  cancelReason?: string
  dispatchedAt?: string
  completedAt?: string
  createdAt: string
  stops: TripStopDto[]
}

export interface DispatchBoardDto {
  date: string
  trips: TripDto[]
  summary: { total: number; dispatched: number; pending: number }
}

// ── Plan-with-split request ──────────────────────────────────────────────────

export interface PlanWithSplitRequest {
  orderIds: string[]
  plannedDate: string           // DateOnly format: "2026-05-10"
  tenantId: string
  maxVehicleWeightKg: number
  maxVehicleVolumeCBM: number
  maxOrdersPerRoute: number
  vehicleTypeId?: string
  depotLat: number
  depotLng: number
  departureTime?: string
}

export interface PlanWithSplitResult {
  planIds: string[]
  totalPlans: number
  totalOrders: number
}

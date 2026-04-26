export type OrderStatus = 'Draft' | 'Confirmed' | 'InTransit' | 'Delivered' | 'Cancelled'
export type OrderPriority = 'Normal' | 'Express' | 'SameDay'

export interface AddressDto {
  name?: string
  street: string
  subDistrict: string
  district: string
  province: string
  postalCode: string
  latitude?: number
  longitude?: number
}

export interface TimeWindowDto {
  from: string
  to: string
}

export interface OrderItemDto {
  description: string
  weightKg: number
  volumeCBM: number
  quantity: number
  sku?: string
  isDangerousGoods?: boolean
}

export interface OrderStopDto {
  sequence?: number
  pickupAddress: AddressDto
  dropoffAddress: AddressDto
  items: OrderItemDto[]
  pickupWindow?: TimeWindowDto
  dropoffWindow?: TimeWindowDto
}

export interface OrderStopSummary {
  stopId: string
  sequence: number
  pickupSummary: string
  pickupProvince: string
  dropoffSummary: string
  dropoffProvince: string
  pickupWindowFrom?: string
  pickupWindowTo?: string
  dropoffWindowFrom?: string
  dropoffWindowTo?: string
}

export interface OrderDto {
  id: string
  orderNumber: string
  customerId: string
  status: OrderStatus
  priority: OrderPriority
  totalWeight: number
  totalVolumeCBM: number
  stopCount: number
  itemCount: number
  stops: OrderStopSummary[]
  notes?: string
  createdAt: string
  updatedAt?: string
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface CreateOrderResponse {
  id: string
  orderNumber: string
  status: string
  totalWeight: number
  totalVolumeCBM: number
  createdAt: string
}

export interface AmendOrderRequest {
  stopId?: string
  pickupAddress?: AddressDto
  dropoffAddress?: AddressDto
  pickupWindow?: TimeWindowDto
  dropoffWindow?: TimeWindowDto
  priority?: OrderPriority
  notes?: string
}

export interface ImportOrdersResult {
  totalRows: number
  successCount: number
  failCount: number
  errors: Array<{ row: number; message: string }>
}

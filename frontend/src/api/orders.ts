import type {
  AmendOrderRequest,
  CreateOrderResponse,
  ImportOrdersResult,
  OrderDto,
  OrderStopDto,
  OrderPriority,
  PagedResult,
} from '../types/order'

const DEV_HEADERS = {
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
    throw new ApiError(res.status, body?.detail ?? body?.title ?? 'Request failed', body)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public body?: unknown,
  ) {
    super(message)
  }
}

// ── Orders ─────────────────────────────────────────────────────────────────

export interface GetOrdersParams {
  page?: number
  pageSize?: number
  status?: string
  customerId?: string
}

export const ordersApi = {
  list(params: GetOrdersParams = {}): Promise<PagedResult<OrderDto>> {
    const q = new URLSearchParams()
    if (params.page) q.set('page', String(params.page))
    if (params.pageSize) q.set('pageSize', String(params.pageSize))
    if (params.status) q.set('status', params.status)
    if (params.customerId) q.set('customerId', params.customerId)
    return apiFetch(`/api/orders?${q}`)
  },

  get(id: string): Promise<OrderDto> {
    return apiFetch(`/api/orders/${id}`)
  },

  create(payload: {
    customerId: string
    orderNumber?: string
    stops: OrderStopDto[]
    priority?: OrderPriority
    notes?: string
  }): Promise<CreateOrderResponse> {
    return apiFetch('/api/orders', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  confirm(id: string): Promise<void> {
    return apiFetch(`/api/orders/${id}/confirm`, { method: 'PUT' })
  },

  amend(id: string, req: AmendOrderRequest): Promise<void> {
    return apiFetch(`/api/orders/${id}/amend`, {
      method: 'PUT',
      body: JSON.stringify(req),
    })
  },

  cancel(id: string, reason: string): Promise<void> {
    return apiFetch(`/api/orders/${id}/cancel`, {
      method: 'PUT',
      body: JSON.stringify({ reason }),
    })
  },

  import(file: File): Promise<ImportOrdersResult> {
    const form = new FormData()
    form.append('file', file)
    return apiFetch('/api/orders/import', {
      method: 'POST',
      headers: {
        'X-TenantId': DEV_HEADERS['X-TenantId'],
        'X-UserId': DEV_HEADERS['X-UserId'],
      },
      body: form,
    })
  },
}

import type { PagedResult } from '../types/order'
import type { ShipmentDto, PodDocumentDto } from '../types/execution'

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

// ── Shipments ─────────────────────────────────────────────────────────────────

export const shipmentApi = {
  list(params: {
    page?: number; pageSize?: number
    status?: string; tripId?: string
  } = {}): Promise<PagedResult<ShipmentDto>> {
    const q = new URLSearchParams()
    if (params.page)     q.set('page',     String(params.page))
    if (params.pageSize) q.set('pageSize', String(params.pageSize))
    if (params.status)   q.set('status',   params.status)
    if (params.tripId)   q.set('tripId',   params.tripId)
    return apiFetch(`/api/shipments?${q}`)
  },

  driverToday(driverId: string): Promise<{ items: ShipmentDto[] }> {
    return apiFetch(`/api/shipments/driver/today?driverId=${driverId}`)
  },

  get(id: string): Promise<ShipmentDto> {
    return apiFetch(`/api/shipments/${id}`)
  },

  pickup(id: string): Promise<void> {
    return apiFetch(`/api/shipments/${id}/pickup`, { method: 'PUT' })
  },

  arrive(id: string): Promise<void> {
    return apiFetch(`/api/shipments/${id}/arrive`, { method: 'PUT' })
  },

  deliver(id: string, payload: {
    items: Array<{ shipmentItemId: string; deliveredQty: number }>
    pod: { receiverName?: string; signatureUrl?: string; photoUrls?: string[]; latitude?: number; longitude?: number }
  }): Promise<void> {
    return apiFetch(`/api/shipments/${id}/deliver`, {
      method: 'PUT', body: JSON.stringify(payload),
    })
  },

  partialDeliver(id: string, payload: {
    items: Array<{ shipmentItemId: string; deliveredQty: number }>
    pod: { receiverName?: string; signatureUrl?: string; photoUrls?: string[]; latitude?: number; longitude?: number }
  }): Promise<void> {
    return apiFetch(`/api/shipments/${id}/partial-deliver`, {
      method: 'PUT', body: JSON.stringify(payload),
    })
  },

  reject(id: string, reason: string, reasonCode: string): Promise<void> {
    return apiFetch(`/api/shipments/${id}/reject`, {
      method: 'PUT', body: JSON.stringify({ reason, reasonCode }),
    })
  },

  exception(id: string, reason: string, reasonCode: string): Promise<void> {
    return apiFetch(`/api/shipments/${id}/exception`, {
      method: 'PUT', body: JSON.stringify({ reason, reasonCode }),
    })
  },

  approvePod(id: string, approvedBy: string): Promise<void> {
    return apiFetch(`/api/shipments/${id}/pod/approve`, {
      method: 'PUT', body: JSON.stringify({ approvedBy }),
    })
  },
}

// ── POD Documents ─────────────────────────────────────────────────────────────

export const podApi = {
  upload(shipmentId: string, file: File, verificationType: string, lat?: number, lng?: number): Promise<{ blobUrl: string }> {
    const form = new FormData()
    form.append('file', file)
    const q = new URLSearchParams({ verificationType })
    if (lat != null) q.set('lat', String(lat))
    if (lng != null) q.set('lng', String(lng))
    return fetch(`/api/execution/pod/${shipmentId}/attachments?${q}`, {
      method: 'POST',
      headers: {
        'X-TenantId': DEV_HEADERS['X-TenantId'],
        'X-UserId':   DEV_HEADERS['X-UserId'],
      },
      body: form,
    }).then(async r => {
      if (!r.ok) {
        const b = await r.json().catch(() => ({}))
        throw new Error(b?.detail ?? `HTTP ${r.status}`)
      }
      return r.json()
    })
  },

  submit(shipmentId: string, lat?: number, lng?: number): Promise<void> {
    return apiFetch(`/api/execution/pod/${shipmentId}/submit`, {
      method: 'PUT',
      body: JSON.stringify({ captureLatitude: lat, captureLongitude: lng }),
    })
  },

  getForEvaluation(shipmentId: string): Promise<PodDocumentDto> {
    return apiFetch(`/api/execution/pod/${shipmentId}/evaluate`)
  },

  evaluate(shipmentId: string, evaluatedBy: string, isApproved: boolean, rejectReason?: string): Promise<void> {
    return apiFetch(`/api/execution/pod/${shipmentId}/evaluate`, {
      method: 'POST',
      body: JSON.stringify({ evaluatedBy, isApproved, rejectReason }),
    })
  },

  generatePdf(shipmentId: string): Promise<{ pdfUrl: string }> {
    return apiFetch(`/api/execution/pod/${shipmentId}/generate-pdf`, { method: 'POST' })
  },
}

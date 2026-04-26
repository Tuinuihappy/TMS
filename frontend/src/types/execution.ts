export type ShipmentStatus =
  | 'Pending' | 'PickedUp' | 'InTransit' | 'Arrived'
  | 'Delivered' | 'PartialDelivered' | 'Rejected' | 'Exception'

export type PodApprovalStatus = 'Pending' | 'AutoApproved' | 'Approved' | 'Rejected'

export interface ShipmentItemDto {
  id: string
  sku?: string
  description: string
  expectedQty: number
  deliveredQty: number
  returnedQty: number
  status: string
}

export interface PodResponseDto {
  receiverName?: string
  signatureUrl?: string
  photoUrls: string[]
  capturedAt: string
  approvalStatus: PodApprovalStatus
}

export interface ShipmentDto {
  id: string
  shipmentNumber: string
  tripId: string
  orderId: string
  dropoffStopId: string
  status: ShipmentStatus
  addressName?: string
  addressStreet?: string
  addressProvince?: string
  exceptionReason?: string
  exceptionReasonCode?: string
  pickedUpAt?: string
  arrivedAt?: string
  deliveredAt?: string
  createdAt: string
  items: ShipmentItemDto[]
  pod?: PodResponseDto
}

// POD Document types
export type VerificationType = 'Signature' | 'BoxPhoto' | 'DocumentScan'
export type PodDocStatus = 'Draft' | 'Submitted' | 'AutoApproved' | 'Approved' | 'Rejected'

export interface VerificationItemDto {
  id: string
  type: VerificationType
  blobUrl: string
  lat?: number
  lng?: number
}

export interface PodDocumentDto {
  id: string
  shipmentId: string
  documentReference: string
  status: PodDocStatus
  capturedAt: string
  geoDistanceMeters?: number
  verifications: VerificationItemDto[]
}

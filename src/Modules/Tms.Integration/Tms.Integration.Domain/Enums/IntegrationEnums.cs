namespace Tms.Integration.Domain.Enums;

public enum SyncStatus
{
    Pending,      // รอ Worker หยิบไปประมวลผล
    Processing,   // กำลังแปลงข้อมูล
    Succeeded,    // สำเร็จ
    Failed,       // ล้มเหลว — รอ Retry
    DeadLetter    // เกิน Max Retry — รอ Admin ตรวจสอบ
}

public enum SyncDirection { Inbound, Outbound }

public enum OutboxStatus { Pending, Sent, Failed }

public enum HandoffStatus
{
    Pending,         // รอ AMR นำสินค้ามา
    AmrAtDock,       // AMR ถึง Dock
    Transferring,    // กำลังโอนถ่ายสินค้า
    Confirmed,       // รับครบ
    PartialHandoff,  // รับบางส่วน
    Failed
}

public enum DockStatus { Available, Occupied, Reserved }

public enum ErpExportType
{
    ArInvoice,      // AR Invoice → ERP
    ApCost,         // AP Cost → ERP
    Reconciliation  // รับ Payment Status จาก ERP
}

public enum ExportJobStatus
{
    Queued,
    Running,
    Completed,
    CompletedWithErrors,
    Failed
}

public enum RecordStatus { Pending, Sent, Accepted, Rejected, Failed }

public enum ReconciliationStatus { Pending, Matched, Unmatched }

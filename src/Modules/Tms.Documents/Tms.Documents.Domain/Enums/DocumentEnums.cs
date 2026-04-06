namespace Tms.Documents.Domain.Enums;

public enum DocumentCategory
{
    ProofOfDelivery,  // POD รูป + ลายเซ็น — Retention 5 ปี
    TripManifest,     // ใบงาน/Manifest — Retention 2 ปี
    VehicleDoc,       // ทะเบียน, ประกัน, พ.ร.บ. — Manual delete
    DriverLicense,    // ใบขับขี่ — Manual delete
    Invoice,          // PDF Invoice — Retention 7 ปี
    ImportFile,       // CSV/Excel Import — Retention 90 วัน
    Other
}

public enum DocumentAccessLevel
{
    Private,          // เจ้าของเท่านั้น
    TenantInternal,   // ทุกคนใน Tenant
    SpecificRoles     // กำหนดเอง per Role/User
}

public enum AccessPermission { Read, ReadWrite }

public enum UploadSessionStatus { Active, Completed, Expired }

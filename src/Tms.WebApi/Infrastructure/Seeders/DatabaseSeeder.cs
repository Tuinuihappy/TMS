using Microsoft.EntityFrameworkCore;
using Tms.Platform.Domain.Entities;
using Tms.Platform.Domain.Enums;
using Tms.Platform.Infrastructure.Persistence;

namespace Tms.WebApi.Infrastructure.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var resourcesDb = scope.ServiceProvider.GetRequiredService<Tms.Resources.Infrastructure.Persistence.ResourcesDbContext>();

        await SeedProvincesAsync(platformDb);
        await SeedRolesAndAdminAsync(platformDb);
        await SeedReasonCodesAsync(platformDb);
        await SeedVehicleTypesAsync(resourcesDb);
    }

    private static async Task SeedVehicleTypesAsync(Tms.Resources.Infrastructure.Persistence.ResourcesDbContext db)
    {
        if (await db.VehicleTypes.AnyAsync()) return;

        var types = new[]
        {
            Tms.Resources.Domain.Entities.VehicleType.Create("4-Wheel Pickup (Cab)", "Pickup", 1500m, 3.5m, Guid.Empty, "T2"),
            Tms.Resources.Domain.Entities.VehicleType.Create("4-Wheel Jumbo", "Truck", 2500m, 5.0m, Guid.Empty, "T2"),
            Tms.Resources.Domain.Entities.VehicleType.Create("6-Wheel Standard", "Truck", 5000m, 15.0m, Guid.Empty, "T2"),
            Tms.Resources.Domain.Entities.VehicleType.Create("10-Wheel Truck", "Heavy Truck", 15000m, 30.0m, Guid.Empty, "T3"),
            Tms.Resources.Domain.Entities.VehicleType.Create("18-Wheel Trailer", "Trailer", 28000m, 60.0m, Guid.Empty, "T3")
        };

        db.VehicleTypes.AddRange(types);
        await db.SaveChangesAsync();
    }

    private static async Task SeedProvincesAsync(PlatformDbContext db)
    {
        if (await db.Provinces.AnyAsync()) return;

        var provinces = new[]
        {
            Province.Create(1, "กรุงเทพมหานคร", "Bangkok", "กลาง"),
            Province.Create(2, "สมุทรปราการ", "Samut Prakan", "กลาง"),
            Province.Create(3, "นนทบุรี", "Nonthaburi", "กลาง"),
            Province.Create(4, "ปทุมธานี", "Pathum Thani", "กลาง"),
            Province.Create(5, "พระนครศรีอยุธยา", "Phra Nakhon Si Ayutthaya", "กลาง"),
            Province.Create(6, "อ่างทอง", "Ang Thong", "กลาง"),
            Province.Create(7, "ลพบุรี", "Lop Buri", "กลาง"),
            Province.Create(8, "สิงห์บุรี", "Sing Buri", "กลาง"),
            Province.Create(9, "ชัยนาท", "Chai Nat", "กลาง"),
            Province.Create(10, "สระบุรี", "Saraburi", "กลาง"),
            Province.Create(11, "ชลบุรี", "Chon Buri", "ตะวันออก"),
            Province.Create(12, "ระยอง", "Rayong", "ตะวันออก"),
            Province.Create(13, "จันทบุรี", "Chanthaburi", "ตะวันออก"),
            Province.Create(14, "ตราด", "Trat", "ตะวันออก"),
            Province.Create(15, "ฉะเชิงเทรา", "Chachoengsao", "ตะวันออก"),
            Province.Create(16, "ปราจีนบุรี", "Prachin Buri", "ตะวันออก"),
            Province.Create(17, "นครนายก", "Nakhon Nayok", "ตะวันออก"),
            Province.Create(18, "สระแก้ว", "Sa Kaeo", "ตะวันออก"),
            Province.Create(19, "นครราชสีมา", "Nakhon Ratchasima", "ตะวันออกเฉียงเหนือ"),
            Province.Create(20, "บุรีรัมย์", "Buri Ram", "ตะวันออกเฉียงเหนือ"),
            Province.Create(21, "สุรินทร์", "Surin", "ตะวันออกเฉียงเหนือ"),
            Province.Create(22, "ศรีสะเกษ", "Si Sa Ket", "ตะวันออกเฉียงเหนือ"),
            Province.Create(23, "อุบลราชธานี", "Ubon Ratchathani", "ตะวันออกเฉียงเหนือ"),
            Province.Create(24, "ยโสธร", "Yasothon", "ตะวันออกเฉียงเหนือ"),
            Province.Create(25, "ชัยภูมิ", "Chaiyaphum", "ตะวันออกเฉียงเหนือ"),
            Province.Create(26, "อำนาจเจริญ", "Amnat Charoen", "ตะวันออกเฉียงเหนือ"),
            Province.Create(27, "หนองบัวลำภู", "Nong Bua Lam Phu", "ตะวันออกเฉียงเหนือ"),
            Province.Create(28, "ขอนแก่น", "Khon Kaen", "ตะวันออกเฉียงเหนือ"),
            Province.Create(29, "อุดรธานี", "Udon Thani", "ตะวันออกเฉียงเหนือ"),
            Province.Create(30, "เลย", "Loei", "ตะวันออกเฉียงเหนือ"),
            Province.Create(31, "หนองคาย", "Nong Khai", "ตะวันออกเฉียงเหนือ"),
            Province.Create(32, "มหาสารคาม", "Maha Sarakham", "ตะวันออกเฉียงเหนือ"),
            Province.Create(33, "ร้อยเอ็ด", "Roi Et", "ตะวันออกเฉียงเหนือ"),
            Province.Create(34, "กาฬสินธุ์", "Kalasin", "ตะวันออกเฉียงเหนือ"),
            Province.Create(35, "สกลนคร", "Sakon Nakhon", "ตะวันออกเฉียงเหนือ"),
            Province.Create(36, "นครพนม", "Nakhon Phanom", "ตะวันออกเฉียงเหนือ"),
            Province.Create(37, "มุกดาหาร", "Mukdahan", "ตะวันออกเฉียงเหนือ"),
            Province.Create(38, "เชียงใหม่", "Chiang Mai", "เหนือ"),
            Province.Create(39, "ลำพูน", "Lamphun", "เหนือ"),
            Province.Create(40, "ลำปาง", "Lampang", "เหนือ"),
            Province.Create(41, "อุตรดิตถ์", "Uttaradit", "เหนือ"),
            Province.Create(42, "แพร่", "Phrae", "เหนือ"),
            Province.Create(43, "น่าน", "Nan", "เหนือ"),
            Province.Create(44, "พะเยา", "Phayao", "เหนือ"),
            Province.Create(45, "เชียงราย", "Chiang Rai", "เหนือ"),
            Province.Create(46, "แม่ฮ่องสอน", "Mae Hong Son", "เหนือ"),
            Province.Create(47, "นครสวรรค์", "Nakhon Sawan", "กลาง"),
            Province.Create(48, "อุทัยธานี", "Uthai Thani", "กลาง"),
            Province.Create(49, "กำแพงเพชร", "Kamphaeng Phet", "กลาง"),
            Province.Create(50, "ตาก", "Tak", "ตะวันตก"),
            Province.Create(51, "สุโขทัย", "Sukhothai", "กลาง"),
            Province.Create(52, "พิษณุโลก", "Phitsanulok", "กลาง"),
            Province.Create(53, "พิจิตร", "Phichit", "กลาง"),
            Province.Create(54, "เพชรบูรณ์", "Phetchabun", "กลาง"),
            Province.Create(55, "ราชบุรี", "Ratchaburi", "ตะวันตก"),
            Province.Create(56, "กาญจนบุรี", "Kanchanaburi", "ตะวันตก"),
            Province.Create(57, "สุพรรณบุรี", "Suphan Buri", "กลาง"),
            Province.Create(58, "นครปฐม", "Nakhon Pathom", "กลาง"),
            Province.Create(59, "สมุทรสาคร", "Samut Sakhon", "กลาง"),
            Province.Create(60, "สมุทรสงคราม", "Samut Songkhram", "กลาง"),
            Province.Create(61, "เพชรบุรี", "Phetchaburi", "ตะวันตก"),
            Province.Create(62, "ประจวบคีรีขันธ์", "Prachuap Khiri Khan", "ตะวันตก"),
            Province.Create(63, "นครศรีธรรมราช", "Nakhon Si Thammarat", "ใต้"),
            Province.Create(64, "กระบี่", "Krabi", "ใต้"),
            Province.Create(65, "พังงา", "Phang-nga", "ใต้"),
            Province.Create(66, "ภูเก็ต", "Phuket", "ใต้"),
            Province.Create(67, "สุราษฎร์ธานี", "Surat Thani", "ใต้"),
            Province.Create(68, "ระนอง", "Ranong", "ใต้"),
            Province.Create(69, "ชุมพร", "Chumphon", "ใต้"),
            Province.Create(70, "สงขลา", "Songkhla", "ใต้"),
            Province.Create(71, "สตูล", "Satun", "ใต้"),
            Province.Create(72, "ตรัง", "Trang", "ใต้"),
            Province.Create(73, "พัทลุง", "Phatthalung", "ใต้"),
            Province.Create(74, "ปัตตานี", "Pattani", "ใต้"),
            Province.Create(75, "ยะลา", "Yala", "ใต้"),
            Province.Create(76, "นราธิวาส", "Narathiwat", "ใต้"),
            Province.Create(77, "บึงกาฬ", "Bueng Kan", "ตะวันออกเฉียงเหนือ")
        };

        db.Provinces.AddRange(provinces);
        await db.SaveChangesAsync();
    }

    private static async Task SeedRolesAndAdminAsync(PlatformDbContext db)
    {
        var tenantId = Guid.Empty; // Default single-tenant setup
        
        // Ensure Admin Role exists
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "System Administrator");
        if (adminRole == null)
        {
            adminRole = Role.Create("System Administrator", tenantId, "Super user with full access.", true);
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync(); // save to get ID
        }

        // Add Dispatcher & Driver Roles
        if (!await db.Roles.AnyAsync(r => r.Name == "Dispatcher"))
        {
            var dispatcherRole = Role.Create("Dispatcher", tenantId, "Manage and assign trips/orders.");
            db.Roles.Add(dispatcherRole);
        }
        
        if (!await db.Roles.AnyAsync(r => r.Name == "Driver"))
        {
            var driverRole = Role.Create("Driver", tenantId, "Execute trips via mobile app.");
            db.Roles.Add(driverRole);
        }

        // Default Admin User
        var adminExtId = "admin|auth0";
        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == adminExtId);
        if (adminUser == null)
        {
            adminUser = User.Create(adminExtId, "admin", "System Administrator", "admin@tms.local", tenantId);
            adminUser.AssignRole(adminRole.Id);
            db.Users.Add(adminUser);
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedReasonCodesAsync(PlatformDbContext db)
    {
        var tenantId = Guid.Empty;
        if (await db.ReasonCodes.AnyAsync()) return;

        var reasons = new[]
        {
            // Delay Reasons -> Exception
            ReasonCode.Create("DLY_TRAFFIC", "Delayed due to heavy traffic", ReasonCategory.Exception, tenantId),
            ReasonCode.Create("DLY_WEATHER", "Delayed due to bad weather", ReasonCategory.Exception, tenantId),
            ReasonCode.Create("DLY_BREAKDOWN", "Vehicle breakdown", ReasonCategory.Exception, tenantId),
            
            // Exception / Failed Delivery
            ReasonCode.Create("EXC_CUST_ABSENT", "Customer not available", ReasonCategory.Exception, tenantId),
            ReasonCode.Create("EXC_WRONG_ADDR", "Incorrect address", ReasonCategory.Exception, tenantId),
            ReasonCode.Create("EXC_REJECTED", "Customer rejected goods", ReasonCategory.Reject, tenantId),
            ReasonCode.Create("EXC_DAMAGED", "Goods damaged in transit", ReasonCategory.Reject, tenantId),

            // Cancellation
            ReasonCode.Create("CAN_CUST_REQ", "Cancelled by customer", ReasonCategory.Cancel, tenantId),
            ReasonCode.Create("CAN_NO_STOCK", "Out of stock/inventory", ReasonCategory.Cancel, tenantId),
            ReasonCode.Create("CAN_NO_CAPACITY", "No vehicle capacity (Fleet)", ReasonCategory.Cancel, tenantId)
        };

        db.ReasonCodes.AddRange(reasons);
        await db.SaveChangesAsync();
    }
}

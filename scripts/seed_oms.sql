-- =============================================================
-- seed_oms.sql — Seed data สำหรับทดสอบ OMS Integration
-- รันอัตโนมัติหลัง EF Core Migration ผ่าน docker-entrypoint-initdb.d
-- =============================================================

-- รอให้ EF Migrations สร้าง Schema ก่อน (script นี้ run หลัง initdb)
-- NOTE: ถ้า migration ยังไม่ได้รัน ให้รัน migrate-all.ps1 ก่อน แล้วค่อย seed ผ่าน psql

-- ── 1. Seed OmsFieldMappings สำหรับ Provider "DEFAULT_OMS" ──────────────────
-- ต้องตรงกับ TmsField ที่ OmsAclMapper ใช้:
--   "customerId", "externalRef",
--   "pickupAddress.street", "pickupAddress.subDistrict", ...
--   "dropoffAddress.street", ...

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables 
               WHERE table_schema = 'itg' AND table_name = 'OmsFieldMappings') THEN

        -- ลบ mapping เดิมของ DEFAULT_OMS ก่อน (idempotent)
        DELETE FROM itg."OmsFieldMappings" WHERE "OmsProviderCode" = 'DEFAULT_OMS';

        -- Insert Field Mappings
        INSERT INTO itg."OmsFieldMappings"
            ("Id", "OmsProviderCode", "OmsField", "TmsField", "IsRequired", "TransformExpression", "UpdatedAt")
        VALUES
            -- Required fields
            (gen_random_uuid(), 'DEFAULT_OMS', 'customerId',         'customerId',              TRUE,  NULL, NOW()),
            (gen_random_uuid(), 'DEFAULT_OMS', 'externalRef',        'externalRef',             TRUE,  NULL, NOW()),

            -- Pickup Address
            (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.street',      'pickupAddress.street',      FALSE, NULL, NOW()),
            (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.subDistrict', 'pickupAddress.subDistrict', FALSE, NULL, NOW()),
            (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.district',    'pickupAddress.district',    FALSE, NULL, NOW()),
            (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.province',    'pickupAddress.province',    FALSE, NULL, NOW()),
            (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.postalCode',  'pickupAddress.postalCode',  FALSE, NULL, NOW()),

            -- Dropoff Address
            (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.street',      'dropoffAddress.street',      FALSE, NULL, NOW()),
            (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.subDistrict', 'dropoffAddress.subDistrict', FALSE, NULL, NOW()),
            (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.district',    'dropoffAddress.district',    FALSE, NULL, NOW()),
            (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.province',    'dropoffAddress.province',    FALSE, NULL, NOW()),
            (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.postalCode',  'dropoffAddress.postalCode',  FALSE, NULL, NOW());

        RAISE NOTICE 'OmsFieldMappings seeded for DEFAULT_OMS';
    ELSE
        RAISE NOTICE 'Table itg.OmsFieldMappings not found — run EF migrations first';
    END IF;
END $$;


-- ── 2. Seed Customer (ถ้า Schema ord มีอยู่) ─────────────────────────────────
-- customerId ที่ใช้ในการทดสอบ: 00000000-0000-0000-0000-000000000001
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables 
               WHERE table_schema = 'ord' AND table_name = 'Customers') THEN

        INSERT INTO ord."Customers"
            ("Id", "Name", "TaxId", "ContactEmail", "ContactPhone", "TenantId", "IsActive", "CreatedAt")
        VALUES
            ('00000000-0000-0000-0000-000000000001',
             'OMS Test Customer Co., Ltd.',
             '0105555000001',
             'test@oms-customer.com',
             '0812345678',
             '00000000-0000-0000-0000-000000000000',
             TRUE,
             NOW())
        ON CONFLICT ("Id") DO NOTHING;

        RAISE NOTICE 'Test Customer seeded (ID: 00000000-0000-0000-0000-000000000001)';
    ELSE
        RAISE NOTICE 'Table ord.Customers not found — run EF migrations first';
    END IF;
END $$;

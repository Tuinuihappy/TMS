DELETE FROM itg."OmsFieldMappings" WHERE "OmsProviderCode" = 'DEFAULT_OMS';

INSERT INTO itg."OmsFieldMappings" ("Id", "OmsProviderCode", "OmsField", "TmsField", "IsRequired", "TransformExpression", "UpdatedAt")
VALUES
    (gen_random_uuid(), 'DEFAULT_OMS', 'customerId',                 'customerId',                TRUE,  NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'externalRef',                'externalRef',               TRUE,  NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.street',       'pickupAddress.street',      FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.subDistrict',  'pickupAddress.subDistrict', FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.district',     'pickupAddress.district',    FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.province',     'pickupAddress.province',    FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'pickupAddress.postalCode',   'pickupAddress.postalCode',  FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.street',      'dropoffAddress.street',     FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.subDistrict', 'dropoffAddress.subDistrict',FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.district',    'dropoffAddress.district',   FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.province',    'dropoffAddress.province',   FALSE, NULL, NOW()),
    (gen_random_uuid(), 'DEFAULT_OMS', 'dropoffAddress.postalCode',  'dropoffAddress.postalCode', FALSE, NULL, NOW());

SELECT COUNT(*) AS mapping_count, "OmsProviderCode"
FROM itg."OmsFieldMappings"
GROUP BY "OmsProviderCode";

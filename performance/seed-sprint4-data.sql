-- Clean up existing test data to ensure clean slate
DELETE FROM "DeliveryStatusHistories";
DELETE FROM "DeliveryNotifications";
DELETE FROM "DeliveryArrangements";
DELETE FROM "DonorOffers";
DELETE FROM "OrgNeedRequests";
DELETE FROM "Requests";
DELETE FROM "Donations";

-- 1. Insert Seed Donation
INSERT INTO "Donations" (
    "Id", "DonorId", "DonorName", "DonorEmail", "FoodTitle", "Category",
    "TotalQuantity", "ClaimedQuantity", "RemainingQuantity", "Unit",
    "ExpiryTime", "CollectionMode", "Status", "Location", "Notes", "DietaryTags", "CreatedAt"
) VALUES (
    1,
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    'JMeter Test Bakery',
    'jmeter_tester@rescueplate.org',
    'Artisan Bread Loaves',
    'Bakery',
    100,
    10,
    90,
    'portions',
    NOW() + INTERVAL '7 days',
    'Organization Pickup',
    'Available',
    '450 5th Ave, New York, NY',
    'Freshly baked surplus loaves',
    'Vegetarian',
    NOW()
);

-- Reset sequence for Donations
SELECT setval('"Donations_Id_seq"', 1, true);

-- 2. Insert Seed Requests (Request 1 is ACCEPTED for Delivery tests, Request 2 is PENDING for bulk reject tests)
INSERT INTO "Requests" (
    "Id", "DonationId", "DonationTitle", "OrganizationId", "OrganizationName",
    "DonorId", "RequestedQuantity", "AcceptedQuantity", "Unit", "Status", "Notes", "CreatedAt"
) VALUES 
(
    1,
    1,
    'Artisan Bread Loaves',
    '001d4354-47d5-4878-ab63-dd7c880fc5d4',
    'Community Hope Kitchen',
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    10,
    10,
    'portions',
    'ACCEPTED',
    'Accepted for community outreach center',
    NOW()
),
(
    2,
    1,
    'Artisan Bread Loaves',
    '001d4354-47d5-4878-ab63-dd7c880fc5d4',
    'Community Hope Kitchen',
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    5,
    NULL,
    'portions',
    'PENDING',
    'Pending food claim request',
    NOW()
);

-- Reset sequence for Requests
SELECT setval('"FoodRequests_Id_seq"', 2, true);

-- 3. Insert Seed Delivery Arrangement
INSERT INTO "DeliveryArrangements" (
    "Id", "RequestId", "DonationId", "DonationTitle", "DonorId", "DonorName",
    "OrganizationId", "OrganizationName", "PickupAddress", "DeliveryAddress",
    "ContactName", "ContactPhone", "ScheduledCollectionTime", "Status",
    "ArrangedAt", "CreatedAt"
) VALUES (
    1,
    1,
    1,
    'Artisan Bread Loaves',
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    'JMeter Test Bakery',
    '001d4354-47d5-4878-ab63-dd7c880fc5d4',
    'Community Hope Kitchen',
    '450 5th Ave, Grand Horizon Bakery',
    '350 W 42nd St, Community Shelter',
    'Sarah Jenkins',
    '+15551234567',
    NOW() + INTERVAL '2 days',
    'CollectionArranged',
    NOW(),
    NOW()
);

SELECT setval('"DeliveryArrangements_Id_seq"', 1, true);

-- 4. Insert Seed Delivery Status History
INSERT INTO "DeliveryStatusHistories" (
    "Id", "DeliveryArrangementId", "PreviousStatus", "NewStatus",
    "ChangedByUserId", "ChangedByRole", "Notes", "Timestamp"
) VALUES (
    1,
    1,
    'Accepted',
    'CollectionArranged',
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    'DONOR',
    'Initial logistics arrangement established',
    NOW()
);

SELECT setval('"DeliveryStatusHistories_Id_seq"', 1, true);

-- 5. Insert Seed Organization Need Request
INSERT INTO "OrgNeedRequests" (
    "Id", "OrganizationId", "OrganizationName", "Title", "Category",
    "QuantityNeeded", "FulfilledQuantity", "RemainingNeeded", "Unit",
    "Description", "Location", "NeededByDate", "Status", "CreatedAt"
) VALUES (
    1,
    '001d4354-47d5-4878-ab63-dd7c880fc5d4',
    'Community Hope Kitchen',
    'Fresh Produce Urgently Needed',
    'Fresh Produce',
    50,
    10,
    40,
    'crates',
    'Need urgent supply of seasonal vegetables and fruits',
    '350 W 42nd St, New York, NY',
    NOW() + INTERVAL '5 days',
    'OPEN',
    NOW()
);

SELECT setval('"OrgNeedRequests_Id_seq"', 1, true);

-- 6. Insert Seed Donor Food Offer (submitted by JMeter tester donor)
INSERT INTO "DonorOffers" (
    "Id", "OrgFoodNeedRequestId", "OrgFoodNeedRequestTitle", "DonorId", "DonorName",
    "FoodType", "OfferedQuantity", "Unit", "Notes", "Status", "CreatedAt"
) VALUES (
    1,
    1,
    'Fresh Produce Urgently Needed',
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    'JMeter Test Bakery',
    'Organic Apples & Citrus',
    15,
    'crates',
    'Available for immediate pickup',
    'PENDING',
    NOW()
);

SELECT setval('"DonorOffers_Id_seq"', 1, true);

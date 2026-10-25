-- ==============================================================================
-- RescuePlate Multi-Service Performance & Load Testing Seed Data
-- Services: UserService (Port 5000), DonationService (Port 5001), RequestWorkflowService (Port 5002)
-- ==============================================================================

-- 1. Clean up existing operational records
DELETE FROM "DeliveryStatusHistories";
DELETE FROM "DeliveryNotifications";
DELETE FROM "DeliveryArrangements";
DELETE FROM "DonorOffers";
DELETE FROM "OrgNeedRequests";
DELETE FROM "Requests";
DELETE FROM "Donations";

-- Ensure clean state for test user accounts
DELETE FROM "DonorProfiles" WHERE "UserId" IN ('604d5297-2ffd-4a8c-ba6b-717d5c0648fc');
DELETE FROM "OrganizationProfiles" WHERE "UserId" IN ('001d4354-47d5-4878-ab63-dd7c880fc5d4');
DELETE FROM "Users" WHERE "Id" IN ('604d5297-2ffd-4a8c-ba6b-717d5c0648fc', '001d4354-47d5-4878-ab63-dd7c880fc5d4') OR "Email" IN ('jmeter_donor@rescueplate.org', 'jmeter_org@rescueplate.org', 'jmeter_tester@rescueplate.org');

-- 2. Seed Donor User (Email: jmeter_donor@rescueplate.org, Password: Password123!)
INSERT INTO "Users" (
    "Id", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt", "UpdatedAt"
) VALUES (
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    'jmeter_donor@rescueplate.org',
    '$2a$11$Xtpkdmxwbg76zZLGdbTdIuG0K7aSqf0JyZoAA5r4xbeZpF7RkfpM2',
    'DONOR',
    TRUE,
    NOW(),
    NOW()
);

INSERT INTO "DonorProfiles" (
    "Id", "UserId", "BusinessName", "ContactPerson", "DonorType", "Address", "Phone", "Bio", "CreatedAt", "UpdatedAt"
) VALUES (
    'b04d5297-2ffd-4a8c-ba6b-717d5c0648fa',
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    'Grand Horizon Bakery & Cafe',
    'Sarah Jenkins',
    'Bakery & Cafe',
    '450 5th Ave, New York, NY',
    '+1 (555) 234-5678',
    'Daily provider of artisan surplus bread and pastries for local charities.',
    NOW(),
    NOW()
);

-- 3. Seed Organization User (Email: jmeter_org@rescueplate.org, Password: Password123!)
INSERT INTO "Users" (
    "Id", "Email", "PasswordHash", "Role", "IsActive", "CreatedAt", "UpdatedAt"
) VALUES (
    '001d4354-47d5-4878-ab63-dd7c880fc5d4',
    'jmeter_org@rescueplate.org',
    '$2a$11$Xtpkdmxwbg76zZLGdbTdIuG0K7aSqf0JyZoAA5r4xbeZpF7RkfpM2',
    'ORGANIZATION',
    TRUE,
    NOW(),
    NOW()
);

INSERT INTO "OrganizationProfiles" (
    "Id", "UserId", "OrganizationName", "ContactPerson", "RegistrationNumber", "Address", "Phone", "AcceptedFoodCategories", "Description", "CreatedAt", "UpdatedAt"
) VALUES (
    'c01d4354-47d5-4878-ab63-dd7c880fc5fa',
    '001d4354-47d5-4878-ab63-dd7c880fc5d4',
    'Community Hope Shelter & Kitchen',
    'David Martinez',
    'ORG-NY-2024-8891',
    '350 W 42nd St, New York, NY',
    '+1 (555) 345-6789',
    'Bakery, Cooked Meals, Fresh Produce, Canned Goods',
    'Non-profit providing hot meals and grocery packages to families across Midtown.',
    NOW(),
    NOW()
);

-- 4. Seed Surplus Food Donations (Generous portions to avoid starvation under load)
INSERT INTO "Donations" (
    "Id", "DonorId", "DonorName", "DonorEmail", "FoodTitle", "Category",
    "TotalQuantity", "ClaimedQuantity", "RemainingQuantity", "Unit",
    "ExpiryTime", "CollectionMode", "Status", "Location", "Notes", "DietaryTags", "CreatedAt"
) VALUES 
(
    1,
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    'Grand Horizon Bakery & Cafe',
    'jmeter_donor@rescueplate.org',
    'Artisan Sourdough & Baguettes',
    'Bakery',
    10000,
    50,
    9950,
    'portions',
    NOW() + INTERVAL '7 days',
    'Organization Pickup',
    'Available',
    '450 5th Ave, New York, NY',
    'Freshly baked breakfast surplus loaves',
    'Vegetarian, Dairy-Free',
    NOW()
),
(
    2,
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    'Grand Horizon Bakery & Cafe',
    'jmeter_donor@rescueplate.org',
    'Warm Vegetable Soup & Dinner Rolls',
    'Cooked Meals',
    8000,
    100,
    7900,
    'portions',
    NOW() + INTERVAL '5 days',
    'Organization Pickup',
    'Available',
    '450 5th Ave, New York, NY',
    'Nutritious hot meal portions in insulated crates',
    'Halal, Vegetarian',
    NOW()
),
(
    3,
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    'Grand Horizon Bakery & Cafe',
    'jmeter_donor@rescueplate.org',
    'Fresh Farm Apples & Citrus Crates',
    'Fresh Produce',
    5000,
    0,
    5000,
    'crates',
    NOW() + INTERVAL '10 days',
    'Organization Pickup',
    'Available',
    '78 Farmers Way, Brooklyn, NY',
    'Seasonal organic produce boxes',
    'Vegan, Organic',
    NOW()
);

SELECT setval('"Donations_Id_seq"', 3, true);

-- 5. Seed Open Organization Food Need Requests
INSERT INTO "OrgNeedRequests" (
    "Id", "OrganizationId", "OrganizationName", "Title", "Category",
    "QuantityNeeded", "FulfilledQuantity", "RemainingNeeded", "Unit",
    "Description", "Location", "NeededByDate", "Status", "CreatedAt"
) VALUES 
(
    1,
    '001d4354-47d5-4878-ab63-dd7c880fc5d4',
    'Community Hope Shelter & Kitchen',
    'Urgent Soup & Bread for Evening Shelter',
    'Cooked Meals',
    5000,
    50,
    4950,
    'portions',
    'Emergency warm dinner for 150 shelter residents',
    '350 W 42nd St, New York, NY',
    NOW() + INTERVAL '7 days',
    'OPEN',
    NOW()
),
(
    2,
    '001d4354-47d5-4878-ab63-dd7c880fc5d4',
    'Community Hope Shelter & Kitchen',
    'Fresh Produce and Fruit Crates Needed',
    'Fresh Produce',
    2000,
    20,
    1980,
    'crates',
    'Weekly community family grocery pantry packages',
    '350 W 42nd St, New York, NY',
    NOW() + INTERVAL '10 days',
    'OPEN',
    NOW()
);

SELECT setval('"OrgNeedRequests_Id_seq"', 2, true);

-- 6. Seed Existing Portion Request & Donor Food Offer
INSERT INTO "DonorOffers" (
    "Id", "OrgFoodNeedRequestId", "OrgFoodNeedRequestTitle", "DonorId", "DonorName",
    "FoodType", "OfferedQuantity", "Unit", "Notes", "Status", "CreatedAt"
) VALUES (
    1,
    1,
    'Urgent Soup & Bread for Evening Shelter',
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    'Grand Horizon Bakery & Cafe',
    'Fresh Baked Dinner Rolls',
    50,
    'portions',
    'Available for pickup this afternoon',
    'PENDING',
    NOW()
);

SELECT setval('"DonorOffers_Id_seq"', 1, true);

INSERT INTO "Requests" (
    "Id", "DonationId", "DonationTitle", "OrganizationId", "OrganizationName",
    "DonorId", "RequestedQuantity", "AcceptedQuantity", "Unit", "Status", "Notes", "CreatedAt"
) VALUES (
    1,
    1,
    'Artisan Sourdough & Baguettes',
    '001d4354-47d5-4878-ab63-dd7c880fc5d4',
    'Community Hope Shelter & Kitchen',
    '604d5297-2ffd-4a8c-ba6b-717d5c0648fc',
    25,
    NULL,
    'portions',
    'PENDING',
    'Requested portions for evening food pantry service',
    NOW()
);

SELECT setval('"FoodRequests_Id_seq"', 1, true);

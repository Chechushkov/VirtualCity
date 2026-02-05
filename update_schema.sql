-- Update buildings table schema
ALTER TABLE buildings RENAME COLUMN latitude TO x;
ALTER TABLE buildings RENAME COLUMN longitude TO z;

-- Update test data with Web Mercator coordinates for Moscow
UPDATE buildings
SET x = 4187663.692806, z = 7509053.911078
WHERE id = '10000000-0000-0000-0000-000000000001';

UPDATE buildings
SET x = 4187648.123456, z = 7509200.654321
WHERE id = '10000000-0000-0000-0000-000000000002';

-- Note: For Ekaterinburg buildings seeded from JSON, the BuildingDataSeeder
-- will handle the conversion from Web Mercator coordinates correctly.
-- The seeder now stores X and Z coordinates directly without converting to lat/lon.

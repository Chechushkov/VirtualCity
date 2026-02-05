-- Add nodes column to buildings table to store polygon nodes as JSONB
-- This allows storing the actual building polygon geometry instead of just center point

-- Add nodes column as JSONB to store array of building nodes
ALTER TABLE buildings ADD COLUMN IF NOT EXISTS nodes JSONB;

-- Update comment for the table
COMMENT ON TABLE buildings IS 'Buildings with Web Mercator coordinates and polygon geometry';

-- Update comment for the nodes column
COMMENT ON COLUMN buildings.nodes IS 'Building polygon nodes as JSONB array of {"x": number, "z": number} objects';

-- Example of how nodes data would be structured in JSONB:
-- [
--   {"x": 6735190.233506037, "z": 7727614.219643679},
--   {"x": 6735200.233506037, "z": 7727614.219643679},
--   {"x": 6735200.233506037, "z": 7727624.219643679},
--   {"x": 6735190.233506037, "z": 7727624.219643679}
-- ]

-- Note: For Ekaterinburg data, X coordinates in database are positive (inverted from JSON)
-- BuildingDataSeeder inverts negative X coordinates from buildings.json to positive for storage

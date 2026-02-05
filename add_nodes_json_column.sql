-- Add nodes_json column to buildings table to store polygon nodes as JSON text
-- Remove width and depth columns if they exist

-- Remove width and depth columns if they exist
ALTER TABLE buildings DROP COLUMN IF EXISTS width;
ALTER TABLE buildings DROP COLUMN IF EXISTS depth;

-- Remove old nodes column if it exists
ALTER TABLE buildings DROP COLUMN IF EXISTS nodes;

-- Add nodes_json column as TEXT to store JSON array of building nodes
ALTER TABLE buildings ADD COLUMN IF NOT EXISTS nodes_json TEXT;

-- Update comment for the table
COMMENT ON TABLE buildings IS 'Buildings with Web Mercator coordinates and polygon geometry stored as JSON';

-- Update comment for the nodes_json column
COMMENT ON COLUMN buildings.nodes_json IS 'Building polygon nodes as JSON text array of [x, z] coordinate pairs';

-- Example of how nodes data would be structured in JSON:
-- [
--   [6735190.233506037, 7727614.219643679],
--   [6735200.233506037, 7727614.219643679],
--   [6735200.233506037, 7727624.219643679],
--   [6735190.233506037, 7727624.219643679]
-- ]

-- Note: For Ekaterinburg data, X coordinates in database are positive (inverted from JSON)
-- BuildingDataSeeder inverts negative X coordinates from buildings.json to positive for storage

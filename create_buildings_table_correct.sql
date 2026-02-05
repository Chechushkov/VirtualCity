-- Create buildings table with correct structure
-- This script creates or recreates the buildings table with the correct schema
-- Includes nodes_json column for storing building polygon geometry

-- Drop existing table if exists (WARNING: This will delete all data!)
DROP TABLE IF EXISTS buildings CASCADE;

-- Create buildings table with all required columns
CREATE TABLE buildings (
    id UUID PRIMARY KEY,
    x DOUBLE PRECISION NOT NULL,
    z DOUBLE PRECISION NOT NULL,
    address TEXT,
    height DOUBLE PRECISION,
    modelid UUID,
    rotation JSONB,
    nodes_json TEXT,

    -- Indexes
    CONSTRAINT IX_buildings_modelid UNIQUE (modelid)
);

-- Add foreign key constraint if models table exists
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'models') THEN
        ALTER TABLE buildings
        ADD CONSTRAINT FK_buildings_models_modelid
        FOREIGN KEY (modelid) REFERENCES models(id)
        ON DELETE SET NULL;
    END IF;
END $$;

-- Add comments
COMMENT ON TABLE buildings IS 'Buildings with Web Mercator coordinates and polygon geometry stored as JSON';
COMMENT ON COLUMN buildings.id IS 'Unique identifier for the building';
COMMENT ON COLUMN buildings.x IS 'Web Mercator X coordinate (meters) - center point';
COMMENT ON COLUMN buildings.z IS 'Web Mercator Z coordinate (meters) - center point';
COMMENT ON COLUMN buildings.address IS 'Building address from buildings.json';
COMMENT ON COLUMN buildings.height IS 'Building height in meters from buildings.json';
COMMENT ON COLUMN buildings.modelid IS 'Reference to custom model if exists';
COMMENT ON COLUMN buildings.rotation IS 'Rotation [x, y, z] if custom model, stored as JSONB array';
COMMENT ON COLUMN buildings.nodes_json IS 'Building polygon nodes as JSON text array of [x, z] coordinate pairs';

-- Create indexes for spatial queries (optional but recommended for performance)
CREATE INDEX IF NOT EXISTS idx_buildings_x ON buildings(x);
CREATE INDEX IF NOT EXISTS idx_buildings_z ON buildings(z);
CREATE INDEX IF NOT EXISTS idx_buildings_x_z ON buildings(x, z);

-- Example of how nodes data would be structured in JSON:
-- [
--   [6735190.233506037, 7727614.219643679],
--   [6735200.233506037, 7727614.219643679],
--   [6735200.233506037, 7727624.219643679],
--   [6735190.233506037, 7727624.219643679]
-- ]

-- Note: For Ekaterinburg data, X coordinates in database are positive (inverted from JSON)
-- BuildingDataSeeder inverts negative X coordinates from buildings.json to positive for storage
-- This is because Ekaterinburg has negative X coordinates in Web Mercator projection

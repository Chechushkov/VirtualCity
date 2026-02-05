-- Create buildings table with address and height columns
-- This script creates the buildings table if it doesn't exist

CREATE TABLE IF NOT EXISTS buildings (
    id UUID PRIMARY KEY,
    x DOUBLE PRECISION NOT NULL,
    z DOUBLE PRECISION NOT NULL,
    address TEXT,
    height DOUBLE PRECISION,
    modelid UUID,
    rotation JSONB,

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
COMMENT ON TABLE buildings IS 'Buildings with Web Mercator coordinates';
COMMENT ON COLUMN buildings.id IS 'Unique identifier for the building';
COMMENT ON COLUMN buildings.x IS 'Web Mercator X coordinate (meters)';
COMMENT ON COLUMN buildings.z IS 'Web Mercator Z coordinate (meters)';
COMMENT ON COLUMN buildings.address IS 'Building address from buildings.json';
COMMENT ON COLUMN buildings.height IS 'Building height in meters from buildings.json';
COMMENT ON COLUMN buildings.modelid IS 'Reference to custom model if exists';
COMMENT ON COLUMN buildings.rotation IS 'Rotation [x, y, z] if custom model';

-- Display table structure
-- \d buildings

-- Create model_polygons table for many-to-many relationship between models and polygons (buildings)
-- This replaces the polygons_json field in the models table

-- Drop the table if it exists (for development)
DROP TABLE IF EXISTS model_polygons;

-- Create the model_polygons table
CREATE TABLE model_polygons (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    model_id UUID NOT NULL,
    polygon_id UUID NOT NULL, -- This is the building ID (polygon identifier)
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Foreign key constraints
    CONSTRAINT fk_model_polygons_model FOREIGN KEY (model_id)
        REFERENCES models(id) ON DELETE CASCADE,
    CONSTRAINT fk_model_polygons_building FOREIGN KEY (polygon_id)
        REFERENCES buildings(id) ON DELETE CASCADE,

    -- Unique constraint to prevent duplicate relationships
    CONSTRAINT uq_model_polygon UNIQUE (model_id, polygon_id)
);

-- Create indexes for performance
CREATE INDEX idx_model_polygons_model_id ON model_polygons(model_id);
CREATE INDEX idx_model_polygons_polygon_id ON model_polygons(polygon_id);
CREATE INDEX idx_model_polygons_created_at ON model_polygons(created_at);

-- Comment on the table
COMMENT ON TABLE model_polygons IS 'Many-to-many relationship between 3D models and building polygons';

-- Comment on columns
COMMENT ON COLUMN model_polygons.id IS 'Primary key';
COMMENT ON COLUMN model_polygons.model_id IS 'Reference to the model';
COMMENT ON COLUMN model_polygons.polygon_id IS 'Reference to the building (polygon)';
COMMENT ON COLUMN model_polygons.created_at IS 'Timestamp when the relationship was created';

-- Optional: Create a view to simplify queries
CREATE OR REPLACE VIEW model_polygons_view AS
SELECT
    mp.id,
    mp.model_id,
    mp.polygon_id,
    mp.created_at,
    m.minioobjectname as model_name,
    b.address as building_address,
    b.x as building_x,
    b.z as building_z
FROM model_polygons mp
JOIN models m ON mp.model_id = m.id
JOIN buildings b ON mp.polygon_id = b.id;

-- Migration script to move data from polygons_json to model_polygons table
DO $$
DECLARE
    model_record RECORD;
    polygon_guid UUID;
    polygon_json TEXT;
    polygon_array TEXT[];
    polygon_item TEXT;
BEGIN
    -- Loop through all models that have polygons_json
    FOR model_record IN SELECT id, polygons_json FROM models WHERE polygons_json IS NOT NULL AND polygons_json != '' LOOP
        -- Parse the JSON array
        polygon_json := REPLACE(REPLACE(REPLACE(model_record.polygons_json, '[', ''), ']', ''), '"', '');
        polygon_array := STRING_TO_ARRAY(polygon_json, ',');

        -- Insert each polygon into the new table
        FOREACH polygon_item IN ARRAY polygon_array LOOP
            BEGIN
                polygon_guid := polygon_item::UUID;

                INSERT INTO model_polygons (model_id, polygon_id)
                VALUES (model_record.id, polygon_guid)
                ON CONFLICT (model_id, polygon_id) DO NOTHING;

            EXCEPTION WHEN OTHERS THEN
                RAISE NOTICE 'Failed to insert polygon % for model %: %', polygon_item, model_record.id, SQLERRM;
            END;
        END LOOP;
    END LOOP;

    RAISE NOTICE 'Migration completed: Moved polygons from models.polygons_json to model_polygons table';
END $$;

-- After migration, we can optionally drop the polygons_json column
-- ALTER TABLE models DROP COLUMN polygons_json;

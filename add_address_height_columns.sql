-- Add address and height columns to buildings table
-- These columns will store building address and height information from buildings.json

-- Add address column (nullable text field)
ALTER TABLE buildings ADD COLUMN IF NOT EXISTS address TEXT;

-- Add height column (nullable double precision field)
ALTER TABLE buildings ADD COLUMN IF NOT EXISTS height DOUBLE PRECISION;

-- Add comment to describe the new columns
COMMENT ON COLUMN buildings.address IS 'Building address from buildings.json';
COMMENT ON COLUMN buildings.height IS 'Building height in meters from buildings.json';

-- Display the updated table structure
-- \d buildings

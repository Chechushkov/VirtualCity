-- Add width and depth columns to buildings table
-- These columns will store the actual dimensions of buildings computed from polygon nodes

-- Add width column (nullable double precision field)
ALTER TABLE buildings ADD COLUMN IF NOT EXISTS width DOUBLE PRECISION;

-- Add depth column (nullable double precision field)
ALTER TABLE buildings ADD COLUMN IF NOT EXISTS depth DOUBLE PRECISION;

-- Add comment to describe the new columns
COMMENT ON COLUMN buildings.width IS 'Building width in meters computed from polygon nodes';
COMMENT ON COLUMN buildings.depth IS 'Building depth in meters computed from polygon nodes';

-- Remove the nodes column if it exists (we're not using it anymore)
ALTER TABLE buildings DROP COLUMN IF EXISTS nodes;

-- Display the updated table structure
-- \d buildings

-- Create users table for Excursion GPT API
-- This script creates the users table if it doesn't exist

CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY,
    name TEXT NOT NULL,
    login TEXT NOT NULL UNIQUE,
    passwordhash TEXT NOT NULL,
    phone TEXT NOT NULL,
    schoolname TEXT NOT NULL,
    role TEXT NOT NULL
);

-- Create index on login for faster lookups
CREATE INDEX IF NOT EXISTS IX_users_login ON users(login);

-- Insert default users if they don't exist
INSERT INTO users (id, name, login, passwordhash, phone, schoolname, role)
VALUES
    ('00000000-0000-0000-0000-000000000001', 'Admin User', 'admin', '$2a$11$Kc7z7b7b7b7b7b7b7b7b7u7b7b7b7b7b7b7b7b7b7b7b7b7b7b7b7b', '+1234567890', 'Admin Academy', 'Admin')
ON CONFLICT (id) DO NOTHING;

INSERT INTO users (id, name, login, passwordhash, phone, schoolname, role)
VALUES
    ('00000000-0000-0000-0000-000000000002', 'Creator User', 'creator', '$2a$11$Kc7z7b7b7b7b7b7b7b7b7u7b7b7b7b7b7b7b7b7b7b7b7b7b7b7b7b', '+0987654321', 'Art School', 'Creator')
ON CONFLICT (id) DO NOTHING;

-- Add comments
COMMENT ON TABLE users IS 'Users for Excursion GPT API';
COMMENT ON COLUMN users.id IS 'Unique identifier for the user';
COMMENT ON COLUMN users.name IS 'Full name of the user';
COMMENT ON COLUMN users.login IS 'Login username (must be unique)';
COMMENT ON COLUMN users.passwordhash IS 'BCrypt hashed password';
COMMENT ON COLUMN users.phone IS 'Phone number';
COMMENT ON COLUMN users.schoolname IS 'School or organization name';
COMMENT ON COLUMN users.role IS 'User role: Admin, Creator, or User';

-- Display table structure
-- \d users

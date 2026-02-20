CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE model_files (
    id uuid NOT NULL,
    minio_object_name text NOT NULL,
    original_file_name text NOT NULL,
    content_type text NOT NULL,
    file_size bigint NOT NULL,
    uploaded_at timestamp with time zone NOT NULL,
    CONSTRAINT "PK_model_files" PRIMARY KEY (id)
);

CREATE TABLE users (
    id uuid NOT NULL,
    name text NOT NULL,
    login text NOT NULL,
    passwordhash text NOT NULL,
    phone text NOT NULL,
    schoolname text NOT NULL,
    role text NOT NULL,
    CONSTRAINT "PK_users" PRIMARY KEY (id)
);

CREATE TABLE tracks (
    id uuid NOT NULL,
    name text NOT NULL,
    creatorid uuid NOT NULL,
    CONSTRAINT "PK_tracks" PRIMARY KEY (id),
    CONSTRAINT "FK_tracks_users_creatorid" FOREIGN KEY (creatorid) REFERENCES users (id) ON DELETE RESTRICT
);

CREATE TABLE points (
    id uuid NOT NULL,
    trackid uuid NOT NULL,
    name text NOT NULL,
    type text NOT NULL,
    position jsonb NOT NULL,
    rotation jsonb NOT NULL,
    CONSTRAINT "PK_points" PRIMARY KEY (id),
    CONSTRAINT "FK_points_tracks_trackid" FOREIGN KEY (trackid) REFERENCES tracks (id) ON DELETE CASCADE
);

CREATE TABLE buildings (
    id uuid NOT NULL,
    x double precision NOT NULL,
    z double precision NOT NULL,
    address text,
    height double precision,
    modelid uuid,
    rotation jsonb,
    nodes_json text,
    CONSTRAINT "PK_buildings" PRIMARY KEY (id)
);

CREATE TABLE models (
    id uuid NOT NULL,
    buildingid uuid NOT NULL,
    trackid uuid NOT NULL,
    minioobjectname text NOT NULL,
    position jsonb NOT NULL,
    rotation jsonb NOT NULL,
    scale double precision NOT NULL,
    CONSTRAINT "PK_models" PRIMARY KEY (id),
    CONSTRAINT "FK_models_buildings_buildingid" FOREIGN KEY (buildingid) REFERENCES buildings (id) ON DELETE CASCADE,
    CONSTRAINT "FK_models_tracks_trackid" FOREIGN KEY (trackid) REFERENCES tracks (id) ON DELETE CASCADE
);

CREATE TABLE model_polygons (
    id uuid NOT NULL,
    model_id uuid NOT NULL,
    polygon_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    CONSTRAINT "PK_model_polygons" PRIMARY KEY (id),
    CONSTRAINT "FK_model_polygons_buildings_polygon_id" FOREIGN KEY (polygon_id) REFERENCES buildings (id) ON DELETE CASCADE,
    CONSTRAINT "FK_model_polygons_models_model_id" FOREIGN KEY (model_id) REFERENCES models (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_buildings_modelid" ON buildings (modelid);

CREATE UNIQUE INDEX "IX_model_polygons_model_id_polygon_id" ON model_polygons (model_id, polygon_id);

CREATE INDEX "IX_model_polygons_polygon_id" ON model_polygons (polygon_id);

CREATE INDEX "IX_models_buildingid" ON models (buildingid);

CREATE INDEX "IX_models_trackid" ON models (trackid);

CREATE INDEX "IX_points_trackid" ON points (trackid);

CREATE INDEX "IX_tracks_creatorid" ON tracks (creatorid);

CREATE UNIQUE INDEX "IX_users_login" ON users (login);

ALTER TABLE buildings ADD CONSTRAINT "FK_buildings_models_modelid" FOREIGN KEY (modelid) REFERENCES models (id) ON DELETE SET NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260218105036_InitialCreate', '9.0.0');

COMMIT;


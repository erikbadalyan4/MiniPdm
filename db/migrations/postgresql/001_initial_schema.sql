-- Миграция 001: Создание базовых таблиц системы Mini-PDM (PostgreSQL)

CREATE TABLE IF NOT EXISTS pdm_object (
    id UUID PRIMARY KEY,
    object_type INT NOT NULL,
    designation VARCHAR(50) NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    current_version_id UUID NULL
);

CREATE TABLE IF NOT EXISTS object_version (
    id UUID PRIMARY KEY,
    object_id UUID NOT NULL REFERENCES pdm_object(id) ON DELETE CASCADE,
    version_no INT NOT NULL,
    state INT NOT NULL,
    material VARCHAR(255) NULL,
    mass_kg NUMERIC(12,4) NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    CONSTRAINT uq_object_version UNIQUE (object_id, version_no)
);

CREATE TABLE IF NOT EXISTS bom_link (
    id UUID PRIMARY KEY,
    parent_version_id UUID NOT NULL REFERENCES object_version(id) ON DELETE CASCADE,
    child_object_id UUID NOT NULL REFERENCES pdm_object(id) ON DELETE CASCADE,
    quantity INT NOT NULL CHECK (quantity > 0),
    CONSTRAINT uq_bom_link UNIQUE (parent_version_id, child_object_id)
);

CREATE TABLE IF NOT EXISTS import_log (
    id UUID PRIMARY KEY,
    started_at TIMESTAMP WITH TIME ZONE NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    severity INT NOT NULL,
    reason TEXT NULL
);

CREATE INDEX IF NOT EXISTS idx_pdm_object_name ON pdm_object(name);
CREATE INDEX IF NOT EXISTS idx_pdm_object_designation ON pdm_object(designation);
CREATE INDEX IF NOT EXISTS idx_bom_link_parent ON bom_link(parent_version_id);
CREATE INDEX IF NOT EXISTS idx_bom_link_child ON bom_link(child_object_id);


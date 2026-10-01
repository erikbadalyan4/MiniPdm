-- Миграция 001: Создание базовых таблиц системы Mini-PDM (MS SQL)

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='pdm_object' AND xtype='U')
CREATE TABLE pdm_object (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    object_type INT NOT NULL,
    designation NVARCHAR(50) NULL UNIQUE,
    name NVARCHAR(255) NOT NULL,
    current_version_id UNIQUEIDENTIFIER NULL
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='object_version' AND xtype='U')
CREATE TABLE object_version (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    object_id UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES pdm_object(id) ON DELETE CASCADE,
    version_no INT NOT NULL,
    state INT NOT NULL,
    material NVARCHAR(255) NULL,
    mass_kg DECIMAL(12,4) NULL,
    created_at DATETIMEOFFSET NOT NULL,
    CONSTRAINT uq_object_version UNIQUE (object_id, version_no)
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='bom_link' AND xtype='U')
CREATE TABLE bom_link (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    parent_version_id UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES object_version(id) ON DELETE CASCADE,
    child_object_id UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES pdm_object(id),
    quantity INT NOT NULL CHECK (quantity > 0),
    CONSTRAINT uq_bom_link UNIQUE (parent_version_id, child_object_id)
);

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='import_log' AND xtype='U')
CREATE TABLE import_log (
    id UNIQUEIDENTIFIER PRIMARY KEY,
    started_at DATETIMEOFFSET NOT NULL,
    file_name NVARCHAR(255) NOT NULL,
    severity INT NOT NULL,
    reason NVARCHAR(MAX) NULL
);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_pdm_object_name')
CREATE INDEX idx_pdm_object_name ON pdm_object(name);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='idx_pdm_object_designation')
CREATE INDEX idx_pdm_object_designation ON pdm_object(designation);


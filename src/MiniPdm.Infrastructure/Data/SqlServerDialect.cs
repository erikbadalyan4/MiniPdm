namespace MiniPdm.Infrastructure.Data;

public sealed class SqlServerDialect : ISqlDialect
{
    public DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public string GetInitSchemaSql() => """
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
        """;

    public string GetRecursiveBomCteSql() => """
        WITH bom_tree AS (
            SELECT 
                o.id AS ObjectId,
                o.object_type AS Type,
                o.designation AS Designation,
                o.name AS Name,
                o.current_version_id AS CurrentVersionId,
                v.version_no AS VersionNumber,
                v.state AS State,
                v.material AS Material,
                v.mass_kg AS MassKg,
                1 AS Quantity,
                0 AS Level,
                CAST(NULL AS UNIQUEIDENTIFIER) AS ParentObjectId,
                CAST(NULL AS UNIQUEIDENTIFIER) AS ParentVersionId,
                CAST('/' + CAST(o.id AS NVARCHAR(36)) + '/' AS NVARCHAR(MAX)) AS HierarchyPath
            FROM pdm_object o
            LEFT JOIN object_version v ON v.id = o.current_version_id
            WHERE o.id = @RootObjectId

            UNION ALL

            SELECT 
                child.id AS ObjectId,
                child.object_type AS Type,
                child.designation AS Designation,
                child.name AS Name,
                child.current_version_id AS CurrentVersionId,
                child_v.version_no AS VersionNumber,
                child_v.state AS State,
                child_v.material AS Material,
                child_v.mass_kg AS MassKg,
                bl.quantity AS Quantity,
                parent_tree.Level + 1 AS Level,
                parent_tree.ObjectId AS ParentObjectId,
                parent_tree.CurrentVersionId AS ParentVersionId,
                CAST(parent_tree.HierarchyPath + CAST(child.id AS NVARCHAR(36)) + '/' AS NVARCHAR(MAX)) AS HierarchyPath
            FROM bom_tree parent_tree
            JOIN bom_link bl ON bl.parent_version_id = parent_tree.CurrentVersionId
            JOIN pdm_object child ON child.id = bl.child_object_id
            LEFT JOIN object_version child_v ON child_v.id = child.current_version_id
            WHERE parent_tree.HierarchyPath NOT LIKE '%/' + CAST(child.id AS NVARCHAR(36)) + '/%'
        )
        SELECT * FROM bom_tree ORDER BY Level, Name;
        """;

    public string GetFirstLevelComponentsSql() => """
        SELECT 
            child.id AS ObjectId,
            child.object_type AS Type,
            child.designation AS Designation,
            child.name AS Name,
            child.current_version_id AS CurrentVersionId,
            child_v.version_no AS VersionNumber,
            child_v.state AS State,
            child_v.material AS Material,
            child_v.mass_kg AS MassKg,
            bl.quantity AS Quantity,
            1 AS Level,
            bl.parent_version_id AS ParentVersionId,
            CAST(NULL AS UNIQUEIDENTIFIER) AS ParentObjectId,
            '' AS HierarchyPath
        FROM bom_link bl
        JOIN pdm_object child ON child.id = bl.child_object_id
        LEFT JOIN object_version child_v ON child_v.id = child.current_version_id
        WHERE bl.parent_version_id = @VersionId
        ORDER BY child.object_type, child.designation, child.name;
        """;
}


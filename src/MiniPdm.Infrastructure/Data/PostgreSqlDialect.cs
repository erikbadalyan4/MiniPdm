namespace MiniPdm.Infrastructure.Data;

public sealed class PostgreSqlDialect : ISqlDialect
{
    public DatabaseProvider Provider => DatabaseProvider.PostgreSql;

    public string GetInitSchemaSql() => """
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
        """;

    public string GetRecursiveBomCteSql() => """
        WITH RECURSIVE bom_tree AS (
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
                CAST(NULL AS UUID) AS ParentObjectId,
                CAST(NULL AS UUID) AS ParentVersionId,
                CAST('/' || o.id::text || '/' AS TEXT) AS HierarchyPath
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
                parent_tree.HierarchyPath || child.id::text || '/' AS HierarchyPath
            FROM bom_tree parent_tree
            JOIN bom_link bl ON bl.parent_version_id = parent_tree.CurrentVersionId
            JOIN pdm_object child ON child.id = bl.child_object_id
            LEFT JOIN object_version child_v ON child_v.id = child.current_version_id
            WHERE parent_tree.HierarchyPath NOT LIKE '%/' || child.id::text || '/%'
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
            CAST(NULL AS UUID) AS ParentObjectId,
            '' AS HierarchyPath
        FROM bom_link bl
        JOIN pdm_object child ON child.id = bl.child_object_id
        LEFT JOIN object_version child_v ON child_v.id = child.current_version_id
        WHERE bl.parent_version_id = @VersionId
        ORDER BY child.object_type, child.designation, child.name;
        """;
}


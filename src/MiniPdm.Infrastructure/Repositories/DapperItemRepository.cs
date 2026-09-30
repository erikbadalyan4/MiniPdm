using System.Data;
using System.Data.Common;
using Dapper;
using MiniPdm.Application.Abstractions;
using MiniPdm.Domain.Entities;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.ValueObjects;
using MiniPdm.Infrastructure.Data;

namespace MiniPdm.Infrastructure.Repositories;

public sealed class DapperItemRepository : IItemRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDbContext? _dbContext;

    public DapperItemRepository(IDbConnectionFactory connectionFactory, IDbContext? dbContext = null)
    {
        _connectionFactory = connectionFactory;
        _dbContext = dbContext;
    }

    private (DbConnection Connection, DbTransaction? Transaction) GetConnection()
    {
        if (_dbContext != null && _dbContext.Transaction != null)
        {
            return (_dbContext.Connection, _dbContext.Transaction);
        }

        return (_connectionFactory.CreateConnection(), null);
    }

    public async Task<Item?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        const string sql = """
            SELECT id, object_type, designation, name, current_version_id
            FROM pdm_object
            WHERE id = @Id;
            """;

        var record = await conn.QuerySingleOrDefaultAsync<PdmObjectDto>(
            new CommandDefinition(sql, new { Id = id }, transaction: tx, cancellationToken: ct));

        if (record == null)
            return null;

        var item = MapToItem(record);
        var versions = await GetVersionsByObjectIdAsync(conn, tx, id, ct);
        item.LoadVersions(versions);
        return item;
    }

    public async Task<Item?> GetByDesignationAsync(Designation designation, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        const string sql = """
            SELECT id, object_type, designation, name, current_version_id
            FROM pdm_object
            WHERE designation = @Designation;
            """;

        var record = await conn.QuerySingleOrDefaultAsync<PdmObjectDto>(
            new CommandDefinition(sql, new { Designation = designation.Value }, transaction: tx, cancellationToken: ct));

        if (record == null)
            return null;

        var item = MapToItem(record);
        var versions = await GetVersionsByObjectIdAsync(conn, tx, item.Id, ct);
        item.LoadVersions(versions);
        return item;
    }

    public async Task<Item?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        const string sql = """
            SELECT id, object_type, designation, name, current_version_id
            FROM pdm_object
            WHERE name = @Name;
            """;

        var record = await conn.QuerySingleOrDefaultAsync<PdmObjectDto>(
            new CommandDefinition(sql, new { Name = name }, transaction: tx, cancellationToken: ct));

        if (record == null)
            return null;

        var item = MapToItem(record);
        var versions = await GetVersionsByObjectIdAsync(conn, tx, item.Id, ct);
        item.LoadVersions(versions);
        return item;
    }

    public async Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        const string sql = """
            SELECT id, object_type, designation, name, current_version_id
            FROM pdm_object
            ORDER BY object_type, designation, name;
            """;

        var records = await conn.QueryAsync<PdmObjectDto>(
            new CommandDefinition(sql, transaction: tx, cancellationToken: ct));

        var items = new List<Item>();
        foreach (var r in records)
        {
            var item = MapToItem(r);
            var versions = await GetVersionsByObjectIdAsync(conn, tx, item.Id, ct);
            item.LoadVersions(versions);
            items.Add(item);
        }

        return items;
    }

    public async Task<IReadOnlyList<Item>> SearchAsync(string query, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        const string sql = """
            SELECT id, object_type, designation, name, current_version_id
            FROM pdm_object
            WHERE designation LIKE @Pattern OR name LIKE @Pattern
            ORDER BY object_type, designation, name;
            """;

        var pattern = $"%{query}%";
        var records = await conn.QueryAsync<PdmObjectDto>(
            new CommandDefinition(sql, new { Pattern = pattern }, transaction: tx, cancellationToken: ct));

        var items = new List<Item>();
        foreach (var r in records)
        {
            var item = MapToItem(r);
            var versions = await GetVersionsByObjectIdAsync(conn, tx, item.Id, ct);
            item.LoadVersions(versions);
            items.Add(item);
        }

        return items;
    }

    public async Task AddAsync(Item item, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        const string sql = """
            INSERT INTO pdm_object (id, object_type, designation, name, current_version_id)
            VALUES (@Id, @Type, @Designation, @Name, @CurrentVersionId);
            """;

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = item.Id,
            Type = (int)item.Type,
            Designation = item.Designation?.Value,
            Name = item.Name,
            CurrentVersionId = item.CurrentVersionId
        }, transaction: tx, cancellationToken: ct));
    }

    public async Task UpdateAsync(Item item, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        const string sql = """
            UPDATE pdm_object
            SET designation = @Designation,
                name = @Name,
                current_version_id = @CurrentVersionId
            WHERE id = @Id;
            """;

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = item.Id,
            Designation = item.Designation?.Value,
            Name = item.Name,
            CurrentVersionId = item.CurrentVersionId
        }, transaction: tx, cancellationToken: ct));
    }

    public async Task SaveVersionAsync(ItemVersion version, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        const string checkSql = "SELECT COUNT(1) FROM object_version WHERE id = @Id;";
        var exists = await conn.ExecuteScalarAsync<int>(new CommandDefinition(checkSql, new { Id = version.Id }, transaction: tx, cancellationToken: ct));

        if (exists > 0)
        {
            const string updateSql = """
                UPDATE object_version
                SET state = @State,
                    material = @Material,
                    mass_kg = @MassKg
                WHERE id = @Id;
                """;

            await conn.ExecuteAsync(new CommandDefinition(updateSql, new
            {
                Id = version.Id,
                State = (int)version.State,
                Material = version.Material,
                MassKg = version.MassKg
            }, transaction: tx, cancellationToken: ct));
        }
        else
        {
            const string insertSql = """
                INSERT INTO object_version (id, object_id, version_no, state, material, mass_kg, created_at)
                VALUES (@Id, @ObjectId, @VersionNumber, @State, @Material, @MassKg, @CreatedAt);
                """;

            await conn.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                Id = version.Id,
                ObjectId = version.ObjectId,
                VersionNumber = version.VersionNumber,
                State = (int)version.State,
                Material = version.Material,
                MassKg = version.MassKg,
                CreatedAt = version.CreatedAt
            }, transaction: tx, cancellationToken: ct));
        }
    }

    public async Task SaveBomLinksAsync(Guid parentVersionId, IEnumerable<BomLink> links, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        const string deleteSql = "DELETE FROM bom_link WHERE parent_version_id = @ParentVersionId;";
        await conn.ExecuteAsync(new CommandDefinition(deleteSql, new { ParentVersionId = parentVersionId }, transaction: tx, cancellationToken: ct));

        const string insertSql = """
            INSERT INTO bom_link (id, parent_version_id, child_object_id, quantity)
            VALUES (@Id, @ParentVersionId, @ChildObjectId, @Quantity);
            """;

        foreach (var link in links)
        {
            await conn.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                Id = link.Id,
                ParentVersionId = link.ParentVersionId,
                ChildObjectId = link.ChildObjectId,
                Quantity = link.Quantity
            }, transaction: tx, cancellationToken: ct));
        }
    }

    public async Task<ItemVersion?> GetVersionByIdAsync(Guid versionId, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        const string sql = """
            SELECT id, object_id, version_no, state, material, mass_kg, created_at
            FROM object_version
            WHERE id = @Id;
            """;

        var record = await conn.QuerySingleOrDefaultAsync<ObjectVersionDto>(
            new CommandDefinition(sql, new { Id = versionId }, transaction: tx, cancellationToken: ct));

        if (record == null)
            return null;

        var version = MapToVersion(record);
        var links = await GetBomLinksByVersionIdAsync(versionId, ct);
        version.LoadBomLinks(links);
        return version;
    }

    public async Task<IReadOnlyList<BomLink>> GetBomLinksByVersionIdAsync(Guid versionId, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        const string sql = """
            SELECT id, parent_version_id, child_object_id, quantity
            FROM bom_link
            WHERE parent_version_id = @VersionId;
            """;

        var records = await conn.QueryAsync<BomLinkDto>(
            new CommandDefinition(sql, new { VersionId = versionId }, transaction: tx, cancellationToken: ct));

        return records.Select(r => new BomLink(r.id, r.parent_version_id, r.child_object_id, r.quantity)).ToList();
    }

    private static async Task<List<ItemVersion>> GetVersionsByObjectIdAsync(
        DbConnection conn,
        DbTransaction? tx,
        Guid objectId,
        CancellationToken ct)
    {
        const string sql = """
            SELECT id, object_id, version_no, state, material, mass_kg, created_at
            FROM object_version
            WHERE object_id = @ObjectId
            ORDER BY version_no;
            """;

        var records = await conn.QueryAsync<ObjectVersionDto>(
            new CommandDefinition(sql, new { ObjectId = objectId }, transaction: tx, cancellationToken: ct));

        return records.Select(MapToVersion).ToList();
    }

    private static Item MapToItem(PdmObjectDto dto)
    {
        var type = (ItemType)dto.object_type;
        var designation = !string.IsNullOrEmpty(dto.designation)
            ? Designation.Create(dto.designation)
            : (Designation?)null;

        return type switch
        {
            ItemType.Assembly => Item.CreateAssembly(dto.id, designation!.Value, dto.name),
            ItemType.Part => Item.CreatePart(dto.id, designation!.Value, dto.name),
            ItemType.StandardPart => Item.CreateStandardPart(dto.id, dto.name),
            _ => throw new InvalidOperationException()
        };
    }

    private static ItemVersion MapToVersion(ObjectVersionDto dto)
    {
        return new ItemVersion(
            dto.id,
            dto.object_id,
            dto.version_no,
            (VersionState)dto.state,
            dto.material,
            dto.mass_kg,
            dto.created_at);
    }

    private sealed class PdmObjectDto
    {
        public Guid id { get; set; }
        public int object_type { get; set; }
        public string? designation { get; set; }
        public string name { get; set; } = string.Empty;
        public Guid? current_version_id { get; set; }
    }

    private sealed class ObjectVersionDto
    {
        public Guid id { get; set; }
        public Guid object_id { get; set; }
        public int version_no { get; set; }
        public int state { get; set; }
        public string? material { get; set; }
        public decimal? mass_kg { get; set; }
        public DateTime created_at { get; set; }
    }

    private sealed class BomLinkDto
    {
        public Guid id { get; set; }
        public Guid parent_version_id { get; set; }
        public Guid child_object_id { get; set; }
        public int quantity { get; set; }
    }
}

using System.Data;
using System.Data.Common;
using Dapper;
using MiniPdm.Application.Abstractions;
using MiniPdm.Application.Models.Bom;
using MiniPdm.Domain.Enums;
using MiniPdm.Domain.ValueObjects;
using MiniPdm.Infrastructure.Data;

namespace MiniPdm.Infrastructure.Repositories;

public sealed class DapperBomQueryRepository : IBomQueryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDbContext? _dbContext;

    public DapperBomQueryRepository(IDbConnectionFactory connectionFactory, IDbContext? dbContext = null)
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

    public async Task<IReadOnlyList<BomHierarchyNode>> GetFullHierarchyAsync(Guid rootObjectId, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var sql = _connectionFactory.Dialect.GetRecursiveBomCteSql();
        var records = await conn.QueryAsync<BomHierarchyDto>(
            new CommandDefinition(sql, new { RootObjectId = rootObjectId }, transaction: tx, cancellationToken: ct));

        return records.Select(MapToNode).ToList();
    }

    public async Task<IReadOnlyList<BomHierarchyNode>> GetFirstLevelComponentsAsync(Guid assemblyVersionId, CancellationToken ct = default)
    {
        var (conn, tx) = GetConnection();
        await using var _ = tx == null ? conn : null;
        if (conn.State != ConnectionState.Open) await conn.OpenAsync(ct);

        var sql = _connectionFactory.Dialect.GetFirstLevelComponentsSql();
        var records = await conn.QueryAsync<BomHierarchyDto>(
            new CommandDefinition(sql, new { VersionId = assemblyVersionId }, transaction: tx, cancellationToken: ct));

        return records.Select(MapToNode).ToList();
    }

    private static BomHierarchyNode MapToNode(BomHierarchyDto dto)
    {
        var designation = !string.IsNullOrEmpty(dto.Designation)
            ? Designation.Create(dto.Designation)
            : (Designation?)null;

        return new BomHierarchyNode(
            dto.ObjectId,
            (ItemType)dto.Type,
            designation,
            dto.Name,
            dto.CurrentVersionId,
            dto.VersionNumber,
            dto.State.HasValue ? (VersionState)dto.State.Value : null,
            dto.Material,
            dto.MassKg,
            dto.Quantity,
            dto.Level,
            dto.ParentObjectId,
            dto.ParentVersionId,
            dto.HierarchyPath ?? string.Empty);
    }

    private sealed class BomHierarchyDto
    {
        public Guid ObjectId { get; set; }
        public int Type { get; set; }
        public string? Designation { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? CurrentVersionId { get; set; }
        public int? VersionNumber { get; set; }
        public int? State { get; set; }
        public string? Material { get; set; }
        public decimal? MassKg { get; set; }
        public int Quantity { get; set; }
        public int Level { get; set; }
        public Guid? ParentObjectId { get; set; }
        public Guid? ParentVersionId { get; set; }
        public string? HierarchyPath { get; set; }
    }
}

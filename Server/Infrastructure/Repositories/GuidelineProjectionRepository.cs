// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Domain.Models;
using AccessService.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AccessService.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for persisting the relational guideline projection.
/// Uses the GuidelineService ID (ServiceId) as the stable identity key for upserts:
/// - First upload for a ServiceId → fresh insert of the full projection.
/// - Subsequent uploads → granular in-place upsert: existing classifications/properties/property-sets
///   are updated, new ones added, and removed ones deleted via cascade.
/// </summary>
public class GuidelineProjectionRepository : IGuidelineProjectionRepository
{
    private readonly AccessRightDbContext _context;

    public GuidelineProjectionRepository(AccessRightDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string serviceId, string etag, CancellationToken cancellationToken = default)
    {
        return await _context.GuidelineVersions
            .AnyAsync(g => g.ServiceId == serviceId && g.Etag == etag, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GuidelineUpsertResult> UpsertAsync(GuidelineVersion newVersion, CancellationToken cancellationToken = default)
    {
        var lockKey = ComputeAdvisoryLockId(newVersion.ServiceId ?? newVersion.ObjectName);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0})", lockKey);

            // Load the existing version (if any) together with all child collections
            var existing = await FindExistingAsync(newVersion, cancellationToken);

            // Idempotency: same identity + same etag = already fully processed
            if (existing != null && existing.Etag == newVersion.Etag)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new GuidelineUpsertResult([], []);
            }

            GuidelineUpsertResult result;
            if (existing == null)
            {
                await _context.GuidelineVersions.AddAsync(newVersion, cancellationToken);
                result = new GuidelineUpsertResult([], []);
            }
            else
            {
                UpdateVersionScalars(existing, newVersion);
                var (removedClassIds, removedPropIds) = ApplyClassifications(existing, newVersion.Classifications);
                ApplyPropertySets(existing, newVersion.PropertySets);
                ApplyProperties(existing, newVersion.Properties);
                result = new GuidelineUpsertResult(removedClassIds, removedPropIds);
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Concurrent consumer already processed the same version — treat as success
            await transaction.RollbackAsync(cancellationToken);
            return new GuidelineUpsertResult([], []);
        }
    }

    /// <inheritdoc/>
    public async Task<List<string>> GetClassificationIdsByServiceIdAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        return await _context.GuidelineVersions
            .Where(v => v.ServiceId == serviceId)
            .SelectMany(v => v.Classifications)
            .Select(c => c.ClassificationId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteByServiceIdAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        await _context.GuidelineVersions
            .Where(g => g.ServiceId == serviceId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> GetActiveVersionIdsAsync(CancellationToken cancellationToken)
    {
        // COALESCE(service_id, object_name) handles both new records (keyed by service_id)
        // and legacy records that pre-date the service_id column.
        return await _context.Database
            .SqlQueryRaw<Guid>(
                "SELECT DISTINCT ON (COALESCE(service_id, object_name)) id AS \"Value\" " +
                "FROM guideline_version " +
                "ORDER BY COALESCE(service_id, object_name), processed_at DESC")
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<GuidelineClassification>> GetClassificationsAsync(IEnumerable<Guid> versionIds, CancellationToken cancellationToken = default)
    {
        var ids = versionIds.ToList();
        return await _context.GuidelineClassifications
            .AsNoTracking()
            .Where(c => ids.Contains(c.GuidelineVersionId))
            .Select(c => new GuidelineClassification
            {
                Id = c.Id,
                GuidelineVersionId = c.GuidelineVersionId,
                ClassificationId = c.ClassificationId,
                Name = c.Name,
                Identifier = c.Identifier,
                Code = c.Code,
                PropertyCount = c.ClassificationProperties.Count(),
                GuidelineVersion = new GuidelineVersion { Name = c.GuidelineVersion.Name }
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<GuidelineClassification>> GetClassificationsWithPropertiesAsync(Guid versionId, IEnumerable<string> classificationIds, CancellationToken cancellationToken = default)
    {
        var ids = classificationIds.ToList();
        return await _context.GuidelineClassifications
            .Include(c => c.ClassificationProperties)
            .AsNoTracking()
            .Where(c => c.GuidelineVersionId == versionId
                     && (ids.Contains(c.ClassificationId) || (c.Identifier != null && ids.Contains(c.Identifier))))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GuidelineClassification?> GetClassificationByIdentifierAsync(IEnumerable<Guid> versionIds, string identifier, CancellationToken cancellationToken = default)
    {
        var ids = versionIds.ToList();
        return await _context.GuidelineClassifications
            .Include(c => c.ClassificationProperties)
            .AsNoTracking()
            .Where(c => ids.Contains(c.GuidelineVersionId)
                     && (c.Identifier == identifier || c.ClassificationId == identifier))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, GuidelineProperty>> GetPropertiesByIdsAsync(Guid versionId, IEnumerable<string> propertyIds, CancellationToken cancellationToken = default)
    {
        var ids = propertyIds.ToList();
        return await _context.GuidelineProperties
            .AsNoTracking()
            .Where(p => p.GuidelineVersionId == versionId && ids.Contains(p.PropertyId))
            .ToDictionaryAsync(p => p.PropertyId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, GuidelinePropertySet>> GetPropertySetsByIdsAsync(Guid versionId, IEnumerable<string> propertySetIds, CancellationToken cancellationToken = default)
    {
        var ids = propertySetIds.ToList();
        return await _context.GuidelinePropertySets
            .AsNoTracking()
            .Where(ps => ps.GuidelineVersionId == versionId && ids.Contains(ps.PropertySetId))
            .ToDictionaryAsync(ps => ps.PropertySetId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GuidelineVersion?> GetVersionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GuidelineVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Guid?> GetActiveVersionIdByGuidelineAsync(string guidelineId, CancellationToken cancellationToken = default)
    {
        return await _context.GuidelineVersions
            .AsNoTracking()
            .Where(v => v.GuidelineId == guidelineId || v.Identifier == guidelineId)
            .OrderByDescending(v => v.ProcessedAt)
            .Select(v => (Guid?)v.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<GuidelineVersion>> GetActiveGuidelineVersionsAsync(CancellationToken cancellationToken = default)
    {
        var activeIds = await GetActiveVersionIdsAsync(cancellationToken);
        return await _context.GuidelineVersions
            .AsNoTracking()
            .Where(v => activeIds.Contains(v.Id))
            .Select(v => new GuidelineVersion
            {
                Id = v.Id,
                ServiceId = v.ServiceId,
                GuidelineId = v.GuidelineId,
                Name = v.Name,
                Identifier = v.Identifier,
                Version = v.Version,
                ObjectName = v.ObjectName,
                BucketName = v.BucketName,
                Etag = v.Etag,
                CorrelationId = v.CorrelationId,
                EventTimestamp = v.EventTimestamp,
                ProcessedAt = v.ProcessedAt
            })
            .ToListAsync(cancellationToken);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<GuidelineVersion?> FindExistingAsync(GuidelineVersion newVersion, CancellationToken ct)
    {
        // AsSplitQuery avoids the cartesian explosion that a single JOIN across
        // Classifications × ClassificationProperties × PropertySets × Properties would produce.
        if (!string.IsNullOrEmpty(newVersion.ServiceId))
        {
            return await _context.GuidelineVersions
                .Include(v => v.Classifications).ThenInclude(c => c.ClassificationProperties)
                .Include(v => v.PropertySets)
                .Include(v => v.Properties)
                .AsSplitQuery()
                .FirstOrDefaultAsync(v => v.ServiceId == newVersion.ServiceId, ct);
        }

        // Fallback for legacy records that have no ServiceId
        return await _context.GuidelineVersions
            .Include(v => v.Classifications).ThenInclude(c => c.ClassificationProperties)
            .Include(v => v.PropertySets)
            .Include(v => v.Properties)
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.ObjectName == newVersion.ObjectName, ct);
    }

    private static void UpdateVersionScalars(GuidelineVersion existing, GuidelineVersion src)
    {
        existing.ServiceId = src.ServiceId;
        existing.GuidelineId = src.GuidelineId;
        existing.Name = src.Name;
        existing.Identifier = src.Identifier;
        existing.Description = src.Description;
        existing.Version = src.Version;
        existing.ObjectName = src.ObjectName;
        existing.BucketName = src.BucketName;
        existing.Etag = src.Etag;
        existing.CorrelationId = src.CorrelationId;
        existing.EventTimestamp = src.EventTimestamp;
        existing.ProcessedAt = src.ProcessedAt;
        existing.MappingsJson = src.MappingsJson;
        existing.ComplexDataJson = src.ComplexDataJson;
        existing.DomainJson = src.DomainJson;
    }

    private (List<string> removedClassIds, List<string> removedPropIds) ApplyClassifications(
        GuidelineVersion existing, ICollection<GuidelineClassification> incoming)
    {
        var existingByKey = existing.Classifications.ToDictionary(c => c.ClassificationId);
        var incomingKeys = new HashSet<string>(incoming.Select(c => c.ClassificationId));
        var removedClassIds = new List<string>();
        var removedPropIds = new List<string>();

        // Remove classifications no longer in the guideline; their access rights become orphaned
        foreach (var cls in existing.Classifications.Where(c => !incomingKeys.Contains(c.ClassificationId)).ToList())
        {
            _context.GuidelineClassifications.Remove(cls);
            removedClassIds.Add(cls.ClassificationId);
        }

        foreach (var newCls in incoming)
        {
            if (existingByKey.TryGetValue(newCls.ClassificationId, out var existingCls))
            {
                existingCls.Name = newCls.Name;
                existingCls.Identifier = newCls.Identifier;
                existingCls.Code = newCls.Code;
                existingCls.Description = newCls.Description;
                existingCls.Status = newCls.Status;
                existingCls.RelationsJson = newCls.RelationsJson;

                var removed = ApplyClassificationProperties(existingCls, newCls.ClassificationProperties);
                removedPropIds.AddRange(removed);
            }
            else
            {
                newCls.GuidelineVersionId = existing.Id;
                _context.GuidelineClassifications.Add(newCls);
            }
        }

        return (removedClassIds, removedPropIds);
    }

    private List<string> ApplyClassificationProperties(
        GuidelineClassification existingCls, ICollection<GuidelineClassificationProperty> incoming)
    {
        var existingByKey = existingCls.ClassificationProperties.ToDictionary(cp => cp.ClassificationPropertyId);
        var incomingKeys = new HashSet<string>(incoming.Select(cp => cp.ClassificationPropertyId));
        var removedPropIds = new List<string>();

        foreach (var cp in existingCls.ClassificationProperties.Where(cp => !incomingKeys.Contains(cp.ClassificationPropertyId)).ToList())
        {
            _context.GuidelineClassificationProperties.Remove(cp);
            removedPropIds.Add(cp.ClassificationPropertyId);
        }

        foreach (var newCp in incoming)
        {
            if (existingByKey.TryGetValue(newCp.ClassificationPropertyId, out var existingCp))
            {
                existingCp.PropertyId = newCp.PropertyId;
                existingCp.PropertySetId = newCp.PropertySetId;
                existingCp.IsRequired = newCp.IsRequired;
                existingCp.SortNumber = newCp.SortNumber;
                existingCp.IsReadonly = newCp.IsReadonly;
                existingCp.DefaultValue = newCp.DefaultValue;
                existingCp.Reference = newCp.Reference;
                existingCp.AssignmentJson = newCp.AssignmentJson;
            }
            else
            {
                newCp.GuidelineClassificationId = existingCls.Id;
                _context.GuidelineClassificationProperties.Add(newCp);
            }
        }

        return removedPropIds;
    }

    private void ApplyPropertySets(GuidelineVersion existing, ICollection<GuidelinePropertySet> incoming)
    {
        var existingByKey = existing.PropertySets.ToDictionary(ps => ps.PropertySetId);
        var incomingKeys = new HashSet<string>(incoming.Select(ps => ps.PropertySetId));

        foreach (var ps in existing.PropertySets.Where(ps => !incomingKeys.Contains(ps.PropertySetId)).ToList())
            _context.GuidelinePropertySets.Remove(ps);

        foreach (var newPs in incoming)
        {
            if (existingByKey.TryGetValue(newPs.PropertySetId, out var existingPs))
            {
                existingPs.Name = newPs.Name;
                existingPs.Identifier = newPs.Identifier;
                existingPs.Description = newPs.Description;
                existingPs.Status = newPs.Status;
            }
            else
            {
                newPs.GuidelineVersionId = existing.Id;
                _context.GuidelinePropertySets.Add(newPs);
            }
        }
    }

    private void ApplyProperties(GuidelineVersion existing, ICollection<GuidelineProperty> incoming)
    {
        var existingByKey = existing.Properties.ToDictionary(p => p.PropertyId);
        var incomingKeys = new HashSet<string>(incoming.Select(p => p.PropertyId));

        foreach (var p in existing.Properties.Where(p => !incomingKeys.Contains(p.PropertyId)).ToList())
            _context.GuidelineProperties.Remove(p);

        foreach (var newP in incoming)
        {
            if (existingByKey.TryGetValue(newP.PropertyId, out var existingP))
            {
                existingP.Name = newP.Name;
                existingP.Identifier = newP.Identifier;
                existingP.Description = newP.Description;
                existingP.StorageType = newP.StorageType;
                existingP.Code = newP.Code;
                existingP.UnitType = newP.UnitType;
                existingP.UnitAbbreviation = newP.UnitAbbreviation;
                existingP.Status = newP.Status;
                existingP.PropertyType = newP.PropertyType;
                existingP.ExtraJson = newP.ExtraJson;
            }
            else
            {
                newP.GuidelineVersionId = existing.Id;
                _context.GuidelineProperties.Add(newP);
            }
        }
    }

    private static long ComputeAdvisoryLockId(string key)
    {
        return BitConverter.ToInt64(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(key)).AsSpan(0, 8));
    }
}

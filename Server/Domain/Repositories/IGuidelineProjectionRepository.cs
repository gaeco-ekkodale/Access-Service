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

namespace AccessService.Domain.Repositories;

/// <summary>
/// Defines repository operations for the relational guideline projection.
/// </summary>
public interface IGuidelineProjectionRepository
{
    /// <summary>
    /// Checks whether a guideline version with the given GuidelineService ID and ETag already exists,
    /// indicating it has already been processed and no further action is needed.
    /// </summary>
    Task<bool> ExistsAsync(string serviceId, string etag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a full guideline version with all related entities (classifications, properties, etc.)
    /// in a single transaction. If a previous version for the same GuidelineService ID exists it is
    /// updated in-place (granular upsert). Returns the domain keys that were removed during the upsert
    /// so callers can clean up orphaned access rights.
    /// </summary>
    Task<GuidelineUpsertResult> UpsertAsync(GuidelineVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all ClassificationId strings for the guideline version identified by the given
    /// GuidelineService ID. Used before deletion to collect which access rights to remove.
    /// Returns an empty list when no version exists for the service ID.
    /// </summary>
    Task<List<string>> GetClassificationIdsByServiceIdAsync(string serviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the IDs of the latest (most recently processed) guideline version per distinct guideline.
    /// Uses service_id as the primary key; falls back to object_name for legacy records without a service_id.
    /// </summary>
    Task<List<Guid>> GetActiveVersionIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all classifications across the given guideline versions.
    /// </summary>
    Task<List<GuidelineClassification>> GetClassificationsAsync(IEnumerable<Guid> versionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns classifications (with their ClassificationProperties) for the given version,
    /// filtered to classifications whose ClassificationId or Identifier is in the given set.
    /// </summary>
    Task<List<GuidelineClassification>> GetClassificationsWithPropertiesAsync(Guid versionId, IEnumerable<string> classificationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single classification (with its ClassificationProperties) by identifier, searching across the given versions.
    /// </summary>
    Task<GuidelineClassification?> GetClassificationByIdentifierAsync(IEnumerable<Guid> versionIds, string identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns guideline properties for the given version, filtered to the given property IDs.
    /// </summary>
    Task<Dictionary<string, GuidelineProperty>> GetPropertiesByIdsAsync(Guid versionId, IEnumerable<string> propertyIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns guideline property sets for the given version, filtered to the given property set IDs.
    /// </summary>
    Task<Dictionary<string, GuidelinePropertySet>> GetPropertySetsByIdsAsync(Guid versionId, IEnumerable<string> propertySetIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a guideline version by its ID, or null if not found.
    /// </summary>
    Task<GuidelineVersion?> GetVersionByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the ID of the most recently processed guideline version matching the given guideline identifier
    /// (matched against GuidelineId or Identifier). Returns null if no match is found.
    /// </summary>
    Task<Guid?> GetActiveVersionIdByGuidelineAsync(string guidelineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the latest active guideline version per distinct ObjectName, with basic metadata only (no navigation properties).
    /// </summary>
    Task<List<GuidelineVersion>> GetActiveGuidelineVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the guideline version (and all child data via cascade) for the given GuidelineService ID.
    /// No-op if no version exists for the given service ID.
    /// </summary>
    Task DeleteByServiceIdAsync(string serviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes <paramref name="action"/> inside a single database transaction that is committed on success
    /// and rolled back on failure. Both the projection repository and any repository sharing the same
    /// DbContext (e.g. AccessRightsRepository) participate in the transaction automatically.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}

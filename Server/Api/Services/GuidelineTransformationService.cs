// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.Events;
using AccessService.Api.Serialization;
using AccessService.Domain.Models;
using AccessService.Domain.Repositories;
using Guideline.Model.Model;
using GuidelineModelIO;
using Minio;
using Minio.DataModel.Args;

namespace AccessService.Api.Services;

/// <summary>
/// Processes guideline upload events by loading the guideline from MinIO,
/// transforming it into a relational projection (GuidelineVersion → Classifications → Properties etc.),
/// and persisting it idempotently.
/// Downloads to a temp file so large guideline files never have to be held in memory as a string.
/// </summary>
public class GuidelineTransformationService : IGuidelineTransformationService
{
    private readonly IMinioClient _minioClient;
    private readonly IGuidelineProjectionRepository _repository;
    private readonly IAccessRightsRepository _accessRightsRepository;
    private readonly IUseCaseGuidelineService _useCaseGuidelineService;
    private readonly ILogger<GuidelineTransformationService> _logger;

    /// <summary>
    /// Reading the guideline file is delegated to the Guideline.Model package, which owns the on-disk
    /// schema. Hand-rolled serializer settings cannot keep up with it: since SchemaVersion 2.0 the file
    /// no longer carries a type discriminator for classifications, which is why the previous
    /// Newtonsoft-based reader failed with "Could not create an instance of type IClassification".
    /// The reader handles both the 2.0 format and older files that still carry <c>$type</c> everywhere.
    /// </summary>
    private static readonly GuidelineReaderWriter GuidelineReader = new();

    public GuidelineTransformationService(
        IMinioClient minioClient,
        IGuidelineProjectionRepository repository,
        IAccessRightsRepository accessRightsRepository,
        IUseCaseGuidelineService useCaseGuidelineService,
        ILogger<GuidelineTransformationService> logger)
    {
        _minioClient = minioClient;
        _repository = repository;
        _accessRightsRepository = accessRightsRepository;
        _useCaseGuidelineService = useCaseGuidelineService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ProcessAsync(UploadedGuideline uploadedGuideline, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing guideline upload event: Name={Name}, ObjectKey={ObjectKey}, Etag={Etag}, CorrelationId={CorrelationId}",
            uploadedGuideline.Name, uploadedGuideline.ObjectKey, uploadedGuideline.Etag, uploadedGuideline.CorrelationId);

        // Early-exit if this exact version is already persisted (same GuidelineService ID + same Etag).
        // The full idempotency guard is also inside UpsertAsync (advisory lock), but this avoids
        // an unnecessary MinIO download.
        if (!string.IsNullOrEmpty(uploadedGuideline.Id)
            && await _repository.ExistsAsync(uploadedGuideline.Id, uploadedGuideline.Etag, cancellationToken))
        {
            _logger.LogInformation(
                "Guideline {ServiceId} with Etag={Etag} already processed. Skipping.",
                uploadedGuideline.Id, uploadedGuideline.Etag);
            return;
        }

        var tempFilePath = Path.GetTempFileName();
        try
        {
            // Step 1: Stream from MinIO to temp file
            await DownloadToTempFileAsync(uploadedGuideline.BucketName, uploadedGuideline.ObjectKey, tempFilePath, cancellationToken);

            // Step 2: Deserialize from the temp file
            Guideline.Model.Model.Guideline guideline;
            try
            {
                guideline = DeserializeGuideline(tempFilePath, uploadedGuideline.ObjectKey);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex,
                    "I/O error reading temp file for ObjectName={ObjectName}. File may be corrupted or inaccessible.",
                    uploadedGuideline.Name);
                throw;
            }

            // Step 3: Transform into relational model
            var version = TransformToRelationalModel(guideline, uploadedGuideline);

            // Step 4: Persist idempotently (replaces previous version for same GuidelineService ID)
            var upsertResult = await _repository.UpsertAsync(version, cancellationToken);

            _logger.LogInformation(
                "Successfully persisted guideline projection: Name={Name}, ObjectKey={ObjectKey}, Etag={Etag}, " +
                "Classifications={ClassCount}, Properties={PropCount}, ClassificationProperties={CpCount}",
                uploadedGuideline.Name, uploadedGuideline.ObjectKey, uploadedGuideline.Etag,
                version.Classifications.Count, version.Properties.Count,
                version.Classifications.Sum(c => c.ClassificationProperties.Count));

            // Step 5: Delete access rights that reference removed classifications or properties
            if (upsertResult.RemovedClassificationIds.Count > 0 || upsertResult.RemovedClassificationPropertyIds.Count > 0)
            {
                _logger.LogInformation(
                    "Cleaning up orphaned access rights: {RemovedClassCount} removed classifications, " +
                    "{RemovedPropCount} removed classification properties.",
                    upsertResult.RemovedClassificationIds.Count, upsertResult.RemovedClassificationPropertyIds.Count);
                try
                {
                    await _accessRightsRepository.DeleteOrphanedAccessRightsAsync(
                        upsertResult.RemovedClassificationIds,
                        upsertResult.RemovedClassificationPropertyIds,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to delete orphaned access rights after guideline upsert. ObjectKey={ObjectKey}",
                        uploadedGuideline.ObjectKey);
                }
            }

            // Trigger UseCase-Guideline generation for all use cases
            try
            {
                await _useCaseGuidelineService.GenerateForAllUseCasesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate UseCase-Guidelines after guideline processing. " +
                    "ObjectName={ObjectName}, Etag={Etag}", uploadedGuideline.Name, uploadedGuideline.Etag);
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(DeletedGuideline deletedGuideline, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing guideline delete event: Id={Id}, ObjectKey={ObjectKey}",
            deletedGuideline.Id, deletedGuideline.ObjectKey);

        // Wrap the read + both deletes in a single transaction so a crash between steps
        // cannot leave access rights orphaned without a retryable path to clean them up.
        // Order: capture IDs → delete access rights → delete projection (cascade).
        // If the transaction rolls back the entire attempt is safe to retry.
        await _repository.ExecuteInTransactionAsync(async ct =>
        {
            var classificationIds = await _repository.GetClassificationIdsByServiceIdAsync(
                deletedGuideline.Id, ct);

            if (classificationIds.Count > 0)
            {
                _logger.LogInformation(
                    "Deleting {Count} orphaned access rights for guideline Id={Id}.",
                    classificationIds.Count, deletedGuideline.Id);

                await _accessRightsRepository.DeleteOrphanedAccessRightsAsync(
                    classificationIds, [], ct);
            }

            await _repository.DeleteByServiceIdAsync(deletedGuideline.Id, ct);
        }, cancellationToken);

        _logger.LogInformation(
            "Deleted guideline projection for ObjectKey={ObjectKey}. Regenerating UseCase-Guidelines.",
            deletedGuideline.ObjectKey);

        try
        {
            await _useCaseGuidelineService.GenerateForAllUseCasesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to regenerate UseCase-Guidelines after guideline deletion. ObjectKey={ObjectKey}",
                deletedGuideline.ObjectKey);
        }
    }

    private async Task DownloadToTempFileAsync(string bucketName, string objectName, string tempFilePath, CancellationToken cancellationToken)
    {
        bool bucketExists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName), cancellationToken);

        if (!bucketExists)
        {
            throw new FileNotFoundException($"Bucket '{bucketName}' does not exist.");
        }

        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithFile(tempFilePath), cancellationToken);

        _logger.LogDebug("Downloaded guideline {ObjectName} from {BucketName} ({Size} bytes)",
            objectName, bucketName, new FileInfo(tempFilePath).Length);
    }

    /// <summary>
    /// Reads the guideline from the downloaded temp file via the Guideline.Model reader,
    /// which resolves the concrete model types for the schema the file was written in.
    /// </summary>
    private static Guideline.Model.Model.Guideline DeserializeGuideline(string tempFilePath, string objectKey)
    {
        var guideline = GuidelineReader.GuidelineRead(tempFilePath);

        return guideline as Guideline.Model.Model.Guideline
               ?? throw new InvalidOperationException(
                   $"Guideline '{objectKey}' deserialized to '{guideline?.GetType().Name ?? "null"}' " +
                   $"instead of {nameof(Guideline.Model.Model.Guideline)}.");
    }

    /// <summary>
    /// Transforms the deserialized Guideline.Model into the relational domain model.
    /// Business-relevant fields become proper columns; everything else is serialized as compact JSON blobs.
    /// </summary>
    private GuidelineVersion TransformToRelationalModel(Guideline.Model.Model.Guideline guideline, UploadedGuideline evt)
    {
        var version = new GuidelineVersion
        {
            Id = Guid.NewGuid(),
            ServiceId = string.IsNullOrEmpty(evt.Id) ? null : evt.Id,
            GuidelineId = guideline.Identifier ?? throw new InvalidOperationException(
                $"Guideline Identifier is null or empty for ObjectKey='{evt.ObjectKey}'."),
            Name = evt.Name,
            Identifier = guideline.Identifier,
            Description = guideline.Description,
            Version = guideline.Version,
            ObjectName = evt.ObjectKey,
            BucketName = evt.BucketName,
            Etag = evt.Etag,
            CorrelationId = evt.CorrelationId,
            EventTimestamp = evt.Timestamp,
            ProcessedAt = DateTimeOffset.UtcNow,
            MappingsJson = GuidelineJson.SerializeCompact(guideline.Mappings),
            ComplexDataJson = GuidelineJson.SerializeCompact(guideline.ComplexData),
            DomainJson = SerializeDomainMeta(guideline.Domain)
        };

        // Transform domain-level property definitions (deduplicate by PropertyId, keep last occurrence)
        if (guideline.Domain?.Properties != null)
        {
            var deduplicatedProperties = DeduplicateByKey(
                guideline.Domain.Properties, p => p.Identifier!, "Property", evt.Name);
            foreach (var prop in deduplicatedProperties)
            {
                version.Properties.Add(TransformProperty(prop, version.Id));
            }
        }

        // Transform domain-level property sets (deduplicate by PropertySetId, keep last occurrence)
        if (guideline.Domain?.PropertySets != null)
        {
            var deduplicatedPropertySets = DeduplicateByKey(
                guideline.Domain.PropertySets, ps => ps.Identifier!, "PropertySet", evt.Name);
            foreach (var ps in deduplicatedPropertySets)
            {
                version.PropertySets.Add(new GuidelinePropertySet
                {
                    Id = Guid.NewGuid(),
                    GuidelineVersionId = version.Id,
                    PropertySetId = ps.Identifier!,
                    Name = ps.Name ?? string.Empty,
                    Identifier = ps.Identifier,
                    Description = ps.Description,
                    Status = ps.Status.ToString()
                });
            }
        }

         // Transform classifications with their classification properties (deduplicate by ClassificationId, keep last occurrence)
         if (guideline.Domain?.Classifications != null)
         {
             var deduplicatedClassifications = DeduplicateByKey(
                  guideline.Domain.Classifications, cls => cls.Identifier!, "Classification", evt.Name);
             foreach (var cls in deduplicatedClassifications)
             {
                 version.Classifications.Add(TransformClassification(cls, version.Id, evt.Name));
             }
         }

        return version;
    }

    /// <summary>
    /// Validates that all items have unique, non-null/empty keys.
    /// Throws if any key is null/empty or if duplicate keys are found.
    /// </summary>
    private IReadOnlyList<T> DeduplicateByKey<T>(
        IEnumerable<T> items, Func<T, string> keySelector, string entityType, string objectName)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var list = new List<T>();

        foreach (var item in items)
        {
            var key = keySelector(item);
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException(
                    $"{entityType} has a null or empty key in guideline '{objectName}'.");
            }

            if (!seen.Add(key))
            {
                throw new InvalidOperationException(
                    $"Duplicate {entityType} with key '{key}' found in guideline '{objectName}'.");
            }

            list.Add(item);
        }

        return list;
    }

    private GuidelineClassification TransformClassification(IClassification cls, Guid versionId, string objectName)
    {
        var gc = new GuidelineClassification
        {
            Id = Guid.NewGuid(),
            GuidelineVersionId = versionId,
            ClassificationId = cls.Identifier ?? throw new InvalidOperationException(
                $"Classification has a null or empty Identifier in guideline '{objectName}'."),
            Name = cls.Name ?? string.Empty,
            Identifier = cls.Identifier,
            Code = cls.Code,
            Description = cls.Description,
            Status = cls.Status.ToString(),
            RelationsJson = SerializeRelations(cls)
        };

        if (cls.ClassificationProperties != null)
        {
            // Silently deduplicate by Identifier (the DB unique key), keeping the first occurrence.
            // Structural validation (null/empty IDs, true duplicates) is the GuidelineService's responsibility.
            // The AccessService only adapts the data to fit its relational DB constraints.
            var seenIdentifiers = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cp in cls.ClassificationProperties)
            {
                if (!string.IsNullOrEmpty(cp.Identifier) && seenIdentifiers.Add(cp.Identifier))
                {
                    gc.ClassificationProperties.Add(TransformClassificationProperty(cp, gc.Id));
                }
                else
                {
                    _logger.LogWarning(
                        "Skipping ClassificationProperty with duplicate or empty Identifier '{Identifier}' in Classification '{ClassificationId}' of guideline '{ObjectName}'.",
                        cp.Identifier, cls.Identifier, objectName);
                }
            }
        }

        return gc;
    }

    private static GuidelineClassificationProperty TransformClassificationProperty(IClassificationProperty cp, Guid classificationId)
    {
        var propertyId = cp.PropertyAssignment?.Property?.Identifier ?? string.Empty;

        return new GuidelineClassificationProperty
        {
            Id = Guid.NewGuid(),
            GuidelineClassificationId = classificationId,
            ClassificationPropertyId = cp.Identifier,
            PropertyId = propertyId,
            PropertySetId = cp.PropertySet?.Identifier,
            IsRequired = cp.IsRequired,
            SortNumber = cp.SortNumber,
            IsReadonly = cp.IsReadonly,
            DefaultValue = cp.DefaultValue,
            Reference = cp.Reference,
            AssignmentJson = SerializeAssignment(cp.PropertyAssignment)
        };
    }

    private static GuidelineProperty TransformProperty(IProperty prop, Guid versionId)
    {
        string? extraJson = null;
        string? propertyType = null;

        switch (prop)
        {
            case PropertySuperEnum pse:
                propertyType = nameof(PropertySuperEnum);
                extraJson = GuidelineJson.SerializeCompact(new
                {
                    pse.Level,
                    Item = pse.Item
                });
                break;
            case PropertyEnum pe:
                propertyType = nameof(PropertyEnum);
                extraJson = GuidelineJson.SerializeCompact(pe.Enums);
                break;
            case PropertySimple ps:
                propertyType = nameof(PropertySimple);
                if (ps.Min != null || ps.Max != null)
                {
                    extraJson = GuidelineJson.SerializeCompact(new
                    {
                        ps.Min,
                        ps.MinIsInclusive,
                        ps.Max,
                        ps.MaxIsInclusive
                    });
                }
                break;
            case PropertyTree pt:
                propertyType = nameof(PropertyTree);
                extraJson = GuidelineJson.SerializeCompact(pt.Item);
                break;
            default:
                propertyType = prop.GetType().Name;
                break;
        }

        return new GuidelineProperty
        {
            Id = Guid.NewGuid(),
            GuidelineVersionId = versionId,
            PropertyId = prop.Identifier ?? throw new InvalidOperationException(
                "Property has a null or empty Identifier."),
            Name = prop.Name ?? string.Empty,
            Identifier = prop.Identifier,
            Description = prop.Description,
            StorageType = prop.StorageType.ToString(),
            Code = prop.Code,
            UnitType = prop.UnitType,
            UnitAbbreviation = prop.UnitAbbreviation,
            Status = prop.Status.ToString(),
            PropertyType = propertyType,
            ExtraJson = extraJson
        };
    }

    /// <summary>
    /// Serializes parent/children classification relations to a compact JSON string.
    /// Only stores IDs to avoid circular references and keep the blob small.
    /// </summary>
    private static string? SerializeRelations(IClassification cls)
    {
        var parentId = cls.Parent?.Item?.Identifier;
        var childIds = cls.Children?.Select(c => c.Item?.Identifier).Where(id => id != null).ToList();

        if (parentId == null && (childIds == null || childIds.Count == 0))
            return null;

        return GuidelineJson.SerializeCompact(new
        {
            ParentId = parentId,
            ChildIds = childIds
        });
    }

    /// <summary>
    /// Serializes domain-level metadata (ID, Name, Identifier, etc.) to JSON.
    /// The domain's collections (Classifications, Properties, PropertySets) are stored relationally, not here.
    /// </summary>
    private static string? SerializeDomainMeta(IDomain? domain)
    {
        if (domain == null)
            return null;
        return GuidelineJson.SerializeCompact(new
        {
            domain.ID,
            domain.Name,
            domain.Identifier,
            domain.Description,
            Status = domain.Status.ToString(),
            domain.Version
        });
    }

    /// <summary>
    /// Serializes the PropertyAssignment details to JSON, excluding the Property reference
    /// (which is already stored relationally via PropertyId).
    /// </summary>
    private static string? SerializeAssignment(IPropertyAssignment? assignment)
    {
        if (assignment == null)
            return null;

        return assignment switch
        {
            PropertyEnumAssignment pea => GuidelineJson.SerializeCompact(new
            {
                Type = nameof(PropertyEnumAssignment),
                pea.FreeTextEnabled,
                SelectedEnum = pea.SelectedEnum != null ? new
                {
                    pea.SelectedEnum.ID,
                    pea.SelectedEnum.Name
                } : null
            }),
            PropertySimpleAssignment psa => GuidelineJson.SerializeCompact(new
            {
                Type = nameof(PropertySimpleAssignment),
                psa.Min,
                psa.MinIsInclusive,
                psa.Max,
                psa.MaxIsInclusive
            }),
            PropertySuperEnumAssignment psea => GuidelineJson.SerializeCompact(new
            {
                Type = nameof(PropertySuperEnumAssignment),
                ParentId = psea.Parent?.ID
            }),
            _ => GuidelineJson.SerializeCompact(new { Type = assignment.GetType().Name, assignment.ID })
        };
    }

}

// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.DTOs;
using AccessService.Domain.Models;
using AccessService.Domain.Models.Enums;
using AccessService.Domain.Repositories;
using Guideline.Model.Enums;

namespace AccessService.Api.Services;

public class ClassificationsService : IClassificationsService
{
    private readonly IGuidelineProjectionRepository _repository;
    private readonly ILogger<ClassificationsService> _logger;

    public ClassificationsService(
        IGuidelineProjectionRepository repository,
        ILogger<ClassificationsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ClassificationsListSet?> GetClassificationsAsync(CancellationToken cancellationToken = default)
    {
        var activeVersionIds = await _repository.GetActiveVersionIdsAsync(cancellationToken);
        if (activeVersionIds.Count == 0)
            return null;

        var classifications = await _repository.GetClassificationsAsync(activeVersionIds, cancellationToken);

        var classificationList = classifications
            .Select(c => new ClassificationList
            {
                Id = c.Identifier ?? c.ClassificationId,
                Name = c.Name,
                Code = c.Code,
                GuidelineName = c.GuidelineVersion?.Name,
                PropertyCount = c.PropertyCount > 0 ? c.PropertyCount : c.ClassificationProperties.Count
            })
            .ToList();

        return new ClassificationsListSet { Classifications = classificationList };
    }

    public async Task<Classification?> GetClassificationAsync(string id, CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveClassificationWithPropertiesAsync(id, cancellationToken);
        if (resolved is null)
            return null;

        var (gc, properties, propertySets) = resolved.Value;

        var mappedPropertySets = gc.ClassificationProperties
            .GroupBy(cp => cp.PropertySetId ?? string.Empty)
            .Select(g =>
            {
                propertySets.TryGetValue(g.Key, out var ps);
                return new PropertySet
                {
                    Id = ps?.PropertySetId ?? g.Key,
                    Name = ps?.Name ?? string.Empty,
                    Properties = g
                        .Where(cp => properties.ContainsKey(cp.PropertyId))
                        .Select(cp => MapProperty(properties[cp.PropertyId]))
                        .ToList(),
                    Right = PropertySetRight.Write
                };
            })
            .ToList();

        return new Classification
        {
            Id = gc.Identifier ?? gc.ClassificationId,
            Name = gc.Name,
            Right = ClassificationRight.Write,
            PropertySets = mappedPropertySets
        };
    }

    public async Task<List<ClassificationPropertyDTO>> GetPropertiesByClassificationIdAsync(string classificationId, CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveClassificationWithPropertiesAsync(classificationId, cancellationToken);
        if (resolved is null)
            return [];

        var (gc, properties, propertySets) = resolved.Value;

        return gc.ClassificationProperties
            .Where(cp => properties.ContainsKey(cp.PropertyId))
            .Select(cp =>
            {
                var prop = properties[cp.PropertyId];
                propertySets.TryGetValue(cp.PropertySetId ?? string.Empty, out var ps);
                return new ClassificationPropertyDTO
                {
                    Id = prop.Identifier ?? prop.PropertyId,
                    Name = prop.Name,
                    StorageType = ParseStorageType(prop),
                    PropertySetName = ps?.Name ?? string.Empty,
                    PropertySetId = ps?.PropertySetId ?? string.Empty
                };
            })
            .ToList();
    }

    private async Task<(GuidelineClassification Classification, Dictionary<string, GuidelineProperty> Properties, Dictionary<string, GuidelinePropertySet> PropertySets)?> ResolveClassificationWithPropertiesAsync(
        string id, CancellationToken cancellationToken)
    {
        var decodedId = Uri.UnescapeDataString(id);

        var activeVersionIds = await _repository.GetActiveVersionIdsAsync(cancellationToken);
        if (activeVersionIds.Count == 0)
            return null;

        var gc = await _repository.GetClassificationByIdentifierAsync(activeVersionIds, decodedId, cancellationToken);
        if (gc == null)
            return null;

        var propertyIds = gc.ClassificationProperties.Select(cp => cp.PropertyId).Distinct();
        var propertySetIds = gc.ClassificationProperties
            .Select(cp => cp.PropertySetId)
            .Where(psId => psId != null)
            .Distinct()!;

        var properties = await _repository.GetPropertiesByIdsAsync(
            gc.GuidelineVersionId,
            propertyIds,
            cancellationToken);
        var propertySets = await _repository.GetPropertySetsByIdsAsync(
            gc.GuidelineVersionId,
            propertySetIds!,
            cancellationToken);

        return (gc, properties, propertySets);
    }

    public async Task<List<GuidelineDTO>> GetGuidelinesAsync(CancellationToken cancellationToken = default)
    {
        var versions = await _repository.GetActiveGuidelineVersionsAsync(cancellationToken);
        return versions
            .Select(v => new GuidelineDTO
            {
                Id = v.GuidelineId,
                Name = v.Name,
                Identifier = v.Identifier,
                Version = v.Version
            })
            .ToList();
    }

    public async Task<ClassificationsListSet?> GetClassificationsByGuidelineAsync(string guidelineId, CancellationToken cancellationToken = default)
    {
        var versionId = await _repository.GetActiveVersionIdByGuidelineAsync(guidelineId, cancellationToken);
        if (versionId == null)
            return null;

        var version = await _repository.GetVersionByIdAsync(versionId.Value, cancellationToken);
        var classifications = await _repository.GetClassificationsAsync([versionId.Value], cancellationToken);

        var classificationList = classifications
            .Select(c => new ClassificationList
            {
                Id = c.Identifier ?? c.ClassificationId,
                Name = c.Name,
                Code = c.Code,
                GuidelineName = version?.Name,
                PropertyCount = c.PropertyCount > 0 ? c.PropertyCount : c.ClassificationProperties.Count
            })
            .ToList();

        return new ClassificationsListSet { Classifications = classificationList };
    }

    public async Task<ClassificationDetailDTO?> GetClassificationDetailByGuidelineAsync(string guidelineId, string classificationId, CancellationToken cancellationToken = default)
    {
        var versionId = await _repository.GetActiveVersionIdByGuidelineAsync(guidelineId, cancellationToken);
        if (versionId == null)
            return null;

        var decodedClassificationId = Uri.UnescapeDataString(classificationId);
        var gc = await _repository.GetClassificationByIdentifierAsync([versionId.Value], decodedClassificationId, cancellationToken);
        if (gc == null)
            return null;

        var propertyIds = gc.ClassificationProperties.Select(cp => cp.PropertyId).Distinct();
        var propertySetIds = gc.ClassificationProperties
            .Select(cp => cp.PropertySetId)
            .Where(psId => psId != null)
            .Distinct()!;

        var properties = await _repository.GetPropertiesByIdsAsync(gc.GuidelineVersionId, propertyIds, cancellationToken);
        var propertySets = await _repository.GetPropertySetsByIdsAsync(gc.GuidelineVersionId, propertySetIds!, cancellationToken);

        var propertySetsWithProps = gc.ClassificationProperties
            .Where(cp => !string.IsNullOrEmpty(cp.PropertySetId) && propertySets.ContainsKey(cp.PropertySetId!))
            .GroupBy(cp => cp.PropertySetId!)
            .Select(g =>
            {
                propertySets.TryGetValue(g.Key, out var ps);
                return new PropertySetDetailDTO
                {
                    Id = ps?.PropertySetId ?? g.Key,
                    Name = ps?.Name ?? string.Empty,
                    Properties = g
                        .Where(cp => properties.ContainsKey(cp.PropertyId))
                        .Select(cp => MapPropertyDetail(properties[cp.PropertyId]))
                        .ToList()
                };
            })
            .ToList();

        var standaloneProperties = gc.ClassificationProperties
            .Where(cp => string.IsNullOrEmpty(cp.PropertySetId) && properties.ContainsKey(cp.PropertyId))
            .Select(cp => MapPropertyDetail(properties[cp.PropertyId]))
            .ToList();

        return new ClassificationDetailDTO
        {
            Id = gc.Identifier ?? gc.ClassificationId,
            Name = gc.Name,
            PropertySets = propertySetsWithProps,
            StandaloneProperties = standaloneProperties
        };
    }

    private PropertyDetailDTO MapPropertyDetail(GuidelineProperty prop)
    {
        return new PropertyDetailDTO
        {
            Id = prop.Identifier ?? prop.PropertyId,
            Name = prop.Name,
            StorageType = ParseStorageType(prop)
        };
    }

    private Property MapProperty(GuidelineProperty prop)
    {
        return new Property
        {
            Id = prop.Identifier ?? prop.PropertyId,
            Name = prop.Name,
            Value = "",
            StorageType = ParseStorageType(prop),
            Right = PropertyRight.Write
        };
    }

    private StorageType ParseStorageType(GuidelineProperty prop)
    {
        if (!Enum.TryParse<StorageType>(prop.StorageType, out var storageType))
        {
            _logger.LogWarning("Unknown StorageType '{StorageType}' for property {PropertyId}. Defaulting to {Default}.",
                prop.StorageType, prop.PropertyId, default(StorageType));
            storageType = default;
        }
        return storageType;
    }
}

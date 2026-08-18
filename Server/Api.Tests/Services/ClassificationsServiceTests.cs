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
using AccessService.Api.Services;
using AccessService.Domain.Models;
using AccessService.Domain.Repositories;
using Guideline.Model.Enums;
using Microsoft.Extensions.Logging;

namespace AccessService.Api.Tests.Services;

public class ClassificationsServiceTests
{
    private readonly IGuidelineProjectionRepository _repository;
    private readonly ILogger<ClassificationsService> _logger;
    private readonly ClassificationsService _sut;

    public ClassificationsServiceTests()
    {
        _repository = Substitute.For<IGuidelineProjectionRepository>();
        _logger = Substitute.For<ILogger<ClassificationsService>>();
        _sut = new ClassificationsService(_repository, _logger);
    }

    #region GetClassificationsAsync

    [Fact]
    public async Task GetClassificationsAsync_ReturnsNull_WhenNoActiveVersions()
    {
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var result = await _sut.GetClassificationsAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetClassificationsAsync_ReturnsClassifications_FromSingleGuideline()
    {
        var versionId = Guid.NewGuid();
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId });

        _repository.GetClassificationsAsync(
                Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(versionId)),
                Arg.Any<CancellationToken>())
            .Returns(new List<GuidelineClassification>
            {
                new() { ClassificationId = "cls-1", Name = "Wall", Identifier = "IfcWall" },
                new() { ClassificationId = "cls-2", Name = "Door", Identifier = "IfcDoor" }
            });

        var result = await _sut.GetClassificationsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result!.Classifications.Count);
        Assert.Equal("IfcWall", result.Classifications[0].Id);
        Assert.Equal("Wall", result.Classifications[0].Name);
        Assert.Equal("IfcDoor", result.Classifications[1].Id);
    }

    [Fact]
    public async Task GetClassificationsAsync_AggregatesClassifications_FromMultipleGuidelines()
    {
        var versionId1 = Guid.NewGuid();
        var versionId2 = Guid.NewGuid();
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId1, versionId2 });

        _repository.GetClassificationsAsync(
                Arg.Is<IEnumerable<Guid>>(ids => ids.Count() == 2),
                Arg.Any<CancellationToken>())
            .Returns(new List<GuidelineClassification>
            {
                new() { GuidelineVersionId = versionId1, ClassificationId = "cls-1", Name = "Wall", Identifier = "IfcWall" },
                new() { GuidelineVersionId = versionId2, ClassificationId = "cls-2", Name = "Door", Identifier = "IfcDoor" }
            });

        var result = await _sut.GetClassificationsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result!.Classifications.Count);
    }

    [Fact]
    public async Task GetClassificationsAsync_UsesClassificationId_WhenIdentifierIsNull()
    {
        var versionId = Guid.NewGuid();
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId });

        _repository.GetClassificationsAsync(
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<GuidelineClassification>
            {
                new() { ClassificationId = "cls-1", Name = "Wall", Identifier = null }
            });

        var result = await _sut.GetClassificationsAsync();

        Assert.Equal("cls-1", result!.Classifications[0].Id);
    }

    [Fact]
    public async Task GetClassificationsAsync_IncludesPropertyCount()
    {
        var versionId = Guid.NewGuid();
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId });

        _repository.GetClassificationsAsync(
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<GuidelineClassification>
            {
                new()
                {
                    ClassificationId = "cls-1", Name = "Wall", Identifier = "IfcWall",
                    ClassificationProperties = new List<GuidelineClassificationProperty>
                    {
                        new() { PropertyId = "p1", PropertySetId = "ps1" },
                        new() { PropertyId = "p2", PropertySetId = "ps1" },
                        new() { PropertyId = "p3", PropertySetId = "ps2" }
                    }
                },
                new()
                {
                    ClassificationId = "cls-2", Name = "Door", Identifier = "IfcDoor",
                    ClassificationProperties = new List<GuidelineClassificationProperty>()
                }
            });

        var result = await _sut.GetClassificationsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result!.Classifications.Count);
        Assert.Equal(3, result.Classifications[0].PropertyCount);
        Assert.Equal(0, result.Classifications[1].PropertyCount);
    }

    #endregion

    #region GetClassificationAsync

    [Fact]
    public async Task GetClassificationAsync_ReturnsNull_WhenNoActiveVersions()
    {
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var result = await _sut.GetClassificationAsync("some-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetClassificationAsync_ReturnsNull_WhenClassificationNotFound()
    {
        var versionId = Guid.NewGuid();
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId });

        _repository.GetClassificationByIdentifierAsync(
                Arg.Any<IEnumerable<Guid>>(), "missing-id", Arg.Any<CancellationToken>())
            .Returns((GuidelineClassification?)null);

        var result = await _sut.GetClassificationAsync("missing-id");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetClassificationAsync_GroupsPropertiesByPropertySet()
    {
        var versionId = Guid.NewGuid();
        var classificationId = Guid.NewGuid();
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId });

        var gc = new GuidelineClassification
        {
            Id = classificationId,
            GuidelineVersionId = versionId,
            ClassificationId = "cls-1",
            Name = "Wall",
            Identifier = "IfcWall",
            ClassificationProperties = new List<GuidelineClassificationProperty>
            {
                new() { PropertyId = "prop-1", PropertySetId = "ps-1" },
                new() { PropertyId = "prop-2", PropertySetId = "ps-1" },
                new() { PropertyId = "prop-3", PropertySetId = "ps-2" }
            }
        };

        _repository.GetClassificationByIdentifierAsync(
                Arg.Any<IEnumerable<Guid>>(), "IfcWall", Arg.Any<CancellationToken>())
            .Returns(gc);

        _repository.GetPropertiesByIdsAsync(versionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelineProperty>
            {
                ["prop-1"] = new() { PropertyId = "prop-1", Name = "Height", Identifier = "height", StorageType = "IfcReal" },
                ["prop-2"] = new() { PropertyId = "prop-2", Name = "Width", Identifier = "width", StorageType = "IfcReal" },
                ["prop-3"] = new() { PropertyId = "prop-3", Name = "Material", Identifier = "material", StorageType = "IfcLabel" }
            });

        _repository.GetPropertySetsByIdsAsync(versionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelinePropertySet>
            {
                ["ps-1"] = new() { PropertySetId = "ps-1", Name = "Dimensions" },
                ["ps-2"] = new() { PropertySetId = "ps-2", Name = "Materials" }
            });

        var result = await _sut.GetClassificationAsync("IfcWall");

        Assert.NotNull(result);
        Assert.Equal("IfcWall", result!.Id);
        Assert.Equal("Wall", result.Name);
        Assert.Equal(2, result.PropertySets.Count);

        var dimensions = result.PropertySets.First(ps => ps.Name == "Dimensions");
        Assert.Equal(2, dimensions.Properties.Count);

        var materials = result.PropertySets.First(ps => ps.Name == "Materials");
        Assert.Equal(1, materials.Properties.Count);
    }

    [Fact]
    public async Task GetClassificationAsync_QueriesPropertiesFromCorrectVersion()
    {
        var versionId1 = Guid.NewGuid();
        var versionId2 = Guid.NewGuid();
        var classificationId = Guid.NewGuid();

        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId1, versionId2 });

        // Classification belongs to versionId2
        var gc = new GuidelineClassification
        {
            Id = classificationId,
            GuidelineVersionId = versionId2,
            ClassificationId = "cls-1",
            Name = "Wall",
            Identifier = "IfcWall",
            ClassificationProperties = new List<GuidelineClassificationProperty>
            {
                new() { PropertyId = "prop-1", PropertySetId = null }
            }
        };

        _repository.GetClassificationByIdentifierAsync(
                Arg.Any<IEnumerable<Guid>>(), "IfcWall", Arg.Any<CancellationToken>())
            .Returns(gc);

        _repository.GetPropertiesByIdsAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelineProperty>
            {
                ["prop-1"] = new() { PropertyId = "prop-1", Name = "Height", StorageType = "IfcReal" }
            });

        _repository.GetPropertySetsByIdsAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelinePropertySet>());

        await _sut.GetClassificationAsync("IfcWall");

        // Verify property queries use the classification's own version, not just any version
        await _repository.Received(1).GetPropertiesByIdsAsync(
            versionId2, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetClassificationAsync_DecodesUrlEncodedId()
    {
        var versionId = Guid.NewGuid();
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId });

        _repository.GetClassificationByIdentifierAsync(
                Arg.Any<IEnumerable<Guid>>(), "IfcWall Type", Arg.Any<CancellationToken>())
            .Returns((GuidelineClassification?)null);

        await _sut.GetClassificationAsync("IfcWall%20Type");

        await _repository.Received(1).GetClassificationByIdentifierAsync(
            Arg.Any<IEnumerable<Guid>>(), "IfcWall Type", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetClassificationAsync_HandlesUnknownStorageType_WithWarning()
    {
        var versionId = Guid.NewGuid();
        var classificationId = Guid.NewGuid();
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId });

        var gc = new GuidelineClassification
        {
            Id = classificationId,
            GuidelineVersionId = versionId,
            ClassificationId = "cls-1",
            Name = "Wall",
            Identifier = "IfcWall",
            ClassificationProperties = new List<GuidelineClassificationProperty>
            {
                new() { PropertyId = "prop-1", PropertySetId = null }
            }
        };

        _repository.GetClassificationByIdentifierAsync(
                Arg.Any<IEnumerable<Guid>>(), "IfcWall", Arg.Any<CancellationToken>())
            .Returns(gc);

        _repository.GetPropertiesByIdsAsync(versionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelineProperty>
            {
                ["prop-1"] = new() { PropertyId = "prop-1", Name = "Height", Identifier = "height", StorageType = "UnknownType" }
            });

        _repository.GetPropertySetsByIdsAsync(versionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelinePropertySet>());

        var result = await _sut.GetClassificationAsync("IfcWall");

        Assert.NotNull(result);
        Assert.Equal(1, result!.PropertySets.Count);
        var prop = result.PropertySets[0].Properties[0];
        Assert.Equal(default(StorageType), prop.StorageType);

        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("UnknownType")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region GetPropertiesByClassificationIdAsync

    [Fact]
    public async Task GetPropertiesByClassificationIdAsync_ReturnsEmpty_WhenNoActiveVersions()
    {
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        var result = await _sut.GetPropertiesByClassificationIdAsync("some-id");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertiesByClassificationIdAsync_ReturnsEmpty_WhenClassificationNotFound()
    {
        var versionId = Guid.NewGuid();
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId });

        _repository.GetClassificationByIdentifierAsync(
                Arg.Any<IEnumerable<Guid>>(), "missing", Arg.Any<CancellationToken>())
            .Returns((GuidelineClassification?)null);

        var result = await _sut.GetPropertiesByClassificationIdAsync("missing");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPropertiesByClassificationIdAsync_ReturnsProperties_WithPropertySetInfo()
    {
        var versionId = Guid.NewGuid();
        var classificationId = Guid.NewGuid();
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId });

        var gc = new GuidelineClassification
        {
            Id = classificationId,
            GuidelineVersionId = versionId,
            ClassificationId = "cls-1",
            Name = "Wall",
            Identifier = "IfcWall",
            ClassificationProperties = new List<GuidelineClassificationProperty>
            {
                new() { PropertyId = "prop-1", PropertySetId = "ps-1" },
                new() { PropertyId = "prop-2", PropertySetId = null }
            }
        };

        _repository.GetClassificationByIdentifierAsync(
                Arg.Any<IEnumerable<Guid>>(), "IfcWall", Arg.Any<CancellationToken>())
            .Returns(gc);

        _repository.GetPropertiesByIdsAsync(versionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelineProperty>
            {
                ["prop-1"] = new() { PropertyId = "prop-1", Name = "Height", Identifier = "height", StorageType = "IfcReal" },
                ["prop-2"] = new() { PropertyId = "prop-2", Name = "Width", Identifier = "width", StorageType = "IfcLabel" }
            });

        _repository.GetPropertySetsByIdsAsync(versionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelinePropertySet>
            {
                ["ps-1"] = new() { PropertySetId = "ps-1", Name = "Dimensions" }
            });

        var result = await _sut.GetPropertiesByClassificationIdAsync("IfcWall");

        Assert.Equal(2, result.Count);

        var heightProp = result.First(p => p.Name == "Height");
        Assert.Equal("height", heightProp.Id);
        Assert.Equal("Dimensions", heightProp.PropertySetName);
        Assert.Equal("ps-1", heightProp.PropertySetId);

        var widthProp = result.First(p => p.Name == "Width");
        Assert.Empty(widthProp.PropertySetName);
    }

    [Fact]
    public async Task GetPropertiesByClassificationIdAsync_SkipsProperties_NotFoundInRepository()
    {
        var versionId = Guid.NewGuid();
        var classificationId = Guid.NewGuid();
        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId });

        var gc = new GuidelineClassification
        {
            Id = classificationId,
            GuidelineVersionId = versionId,
            ClassificationId = "cls-1",
            Name = "Wall",
            Identifier = "IfcWall",
            ClassificationProperties = new List<GuidelineClassificationProperty>
            {
                new() { PropertyId = "prop-1", PropertySetId = null },
                new() { PropertyId = "prop-missing", PropertySetId = null }
            }
        };

        _repository.GetClassificationByIdentifierAsync(
                Arg.Any<IEnumerable<Guid>>(), "IfcWall", Arg.Any<CancellationToken>())
            .Returns(gc);

        _repository.GetPropertiesByIdsAsync(versionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelineProperty>
            {
                ["prop-1"] = new() { PropertyId = "prop-1", Name = "Height", StorageType = "IfcReal" }
            });

        _repository.GetPropertySetsByIdsAsync(versionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelinePropertySet>());

        var result = await _sut.GetPropertiesByClassificationIdAsync("IfcWall");

        Assert.Equal(1, result.Count);
        Assert.Equal("Height", result[0].Name);
    }

    [Fact]
    public async Task GetPropertiesByClassificationIdAsync_AwaitsPropertyLookup_BeforePropertySetLookup()
    {
        var versionId = Guid.NewGuid();
        var classificationId = Guid.NewGuid();
        var propertiesStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowPropertiesToComplete =
            new TaskCompletionSource<Dictionary<string, GuidelineProperty>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        var propertiesCompleted = false;
        var propertySetsStartedBeforePropertiesCompleted = false;

        _repository.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { versionId });

        var gc = new GuidelineClassification
        {
            Id = classificationId,
            GuidelineVersionId = versionId,
            ClassificationId = "cls-1",
            Name = "Wall",
            Identifier = "IfcWall",
            ClassificationProperties = new List<GuidelineClassificationProperty>
            {
                new() { PropertyId = "prop-1", PropertySetId = "ps-1" }
            }
        };

        _repository.GetClassificationByIdentifierAsync(
                Arg.Any<IEnumerable<Guid>>(), "IfcWall", Arg.Any<CancellationToken>())
            .Returns(gc);

        _repository.GetPropertiesByIdsAsync(versionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                propertiesStarted.SetResult();
                var properties = await allowPropertiesToComplete.Task;
                propertiesCompleted = true;
                return properties;
            });

        _repository.GetPropertySetsByIdsAsync(versionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                propertySetsStartedBeforePropertiesCompleted = !propertiesCompleted;
                return Task.FromResult(new Dictionary<string, GuidelinePropertySet>
                {
                    ["ps-1"] = new() { PropertySetId = "ps-1", Name = "Dimensions" }
                });
            });

        var resultTask = _sut.GetPropertiesByClassificationIdAsync("IfcWall");

        await propertiesStarted.Task;
        Assert.False(propertySetsStartedBeforePropertiesCompleted);

        allowPropertiesToComplete.SetResult(new Dictionary<string, GuidelineProperty>
        {
            ["prop-1"] = new() { PropertyId = "prop-1", Name = "Height", StorageType = "IfcReal" }
        });

        var result = await resultTask;

        Assert.False(propertySetsStartedBeforePropertiesCompleted);
        Assert.Equal(1, result.Count);
    }

    #endregion
}

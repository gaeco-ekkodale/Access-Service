// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.Options;
using AccessService.Api.Services;
using AccessService.Api.Tests.TestData;
using AccessService.Domain.Models;
using AccessService.Domain.Models.Enums;
using AccessService.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using NSubstitute.ExceptionExtensions;

namespace AccessService.Api.Tests.Services;

public class UseCaseGuidelineServiceTests
{
    private readonly IMinioClient _minioClient;
    private readonly IGuidelineProjectionRepository _guidelineRepo;
    private readonly IAccessRightsRepository _accessRightsRepo;
    private readonly IOutboxRepository _outboxRepo;
    private readonly ILogger<UseCaseGuidelineService> _logger;
    private readonly UseCaseGuidelineService _sut;

    private static readonly Guid UseCaseId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid VersionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid UserGroupId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public UseCaseGuidelineServiceTests()
    {
        _minioClient = Substitute.For<IMinioClient>();
        _guidelineRepo = Substitute.For<IGuidelineProjectionRepository>();
        _accessRightsRepo = Substitute.For<IAccessRightsRepository>();
        _outboxRepo = Substitute.For<IOutboxRepository>();
        _logger = Substitute.For<ILogger<UseCaseGuidelineService>>();

        var kafkaOptions = Microsoft.Extensions.Options.Options.Create(new KafkaOptions
        {
            Address = "localhost:9092",
            ConsumerGroup = "test-group",
            Topics = new KafkaTopicsOptions
            {
                AccessRights = "access-rights",
                UserGroups = "user-groups",
                Guidelines = "guidelines",
                UseCaseGuidelines = "usecase-guidelines"
            }
        });

        var useCaseGuidelineOptions = Microsoft.Extensions.Options.Options.Create(new UseCaseGuidelineOptions
        {
            BucketName = "usecase-guideline"
        });

        _sut = new UseCaseGuidelineService(
            _minioClient,
            _guidelineRepo,
            _accessRightsRepo,
            _outboxRepo,
            kafkaOptions,
            useCaseGuidelineOptions,
            _logger);
    }

    #region Test Helpers

    private void SetupMinioUpload(string etag = "upload-etag")
    {
        _minioClient.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _minioClient.PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var type = typeof(Minio.DataModel.Response.PutObjectResponse);
                var instance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
                var etagProp = type.GetProperty("Etag") ?? type.BaseType?.GetProperty("Etag");
                etagProp?.SetValue(instance, etag);
                return (Minio.DataModel.Response.PutObjectResponse)instance;
            });
    }

    private void SetupStandardScenario(
        List<AccessRight>? accessRights = null,
        GuidelineVersion? version = null,
        List<GuidelineClassification>? classifications = null,
        Dictionary<string, GuidelineProperty>? properties = null,
        Dictionary<string, GuidelinePropertySet>? propertySets = null)
    {
        accessRights ??= [new AccessRightBuilder()
            .WithClassificationId("cls-1")
            .WithClassificationPropertyId("prop-1")
            .WithUseCaseId(UseCaseId)
            .WithUserGroupId(UserGroupId)
            .Build()];

        version ??= new GuidelineVersionBuilder().WithId(VersionId).Build();

        var cp = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-1")
            .WithPropertyId("prop-1")
            .Build();

        classifications ??= [new GuidelineClassificationBuilder()
            .WithVersionId(VersionId)
            .WithClassificationId("cls-1")
            .WithClassificationProperty(cp)
            .Build()];

        properties ??= new Dictionary<string, GuidelineProperty>
        {
            ["prop-1"] = new GuidelinePropertyBuilder()
                .WithVersionId(VersionId)
                .WithPropertyId("prop-1")
                .Build()
        };
        propertySets ??= new Dictionary<string, GuidelinePropertySet>();

        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(UseCaseId.ToString(), UserGroupId.ToString())
            .Returns(accessRights.AsEnumerable());
        _guidelineRepo.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns([VersionId]);
        _guidelineRepo.GetVersionByIdAsync(VersionId, Arg.Any<CancellationToken>())
            .Returns(version);
        _guidelineRepo.GetClassificationsWithPropertiesAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(classifications);
        _guidelineRepo.GetPropertiesByIdsAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(properties);
        _guidelineRepo.GetPropertySetsByIdsAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(propertySets);
        SetupMinioUpload();
    }

    private void SetupStandardScenarioByUserGroup(
        List<AccessRight>? accessRights = null,
        GuidelineVersion? version = null,
        List<GuidelineClassification>? classifications = null,
        Dictionary<string, GuidelineProperty>? properties = null,
        Dictionary<string, GuidelinePropertySet>? propertySets = null)
    {
        accessRights ??= [new AccessRightBuilder()
            .WithClassificationId("cls-1")
            .WithClassificationPropertyId("prop-1")
            .WithUseCaseId(UseCaseId)
            .WithUserGroupId(UserGroupId)
            .Build()];

        version ??= new GuidelineVersionBuilder().WithId(VersionId).Build();

        var cp = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-1")
            .WithPropertyId("prop-1")
            .Build();

        classifications ??= [new GuidelineClassificationBuilder()
            .WithVersionId(VersionId)
            .WithClassificationId("cls-1")
            .WithClassificationProperty(cp)
            .Build()];

        properties ??= new Dictionary<string, GuidelineProperty>
        {
            ["prop-1"] = new GuidelinePropertyBuilder()
                .WithVersionId(VersionId)
                .WithPropertyId("prop-1")
                .Build()
        };
        propertySets ??= new Dictionary<string, GuidelinePropertySet>();

        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(UseCaseId.ToString(), UserGroupId.ToString())
            .Returns(accessRights.AsEnumerable());
        _guidelineRepo.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns([VersionId]);
        _guidelineRepo.GetVersionByIdAsync(VersionId, Arg.Any<CancellationToken>())
            .Returns(version);
        _guidelineRepo.GetClassificationsWithPropertiesAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(classifications);
        _guidelineRepo.GetPropertiesByIdsAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(properties);
        _guidelineRepo.GetPropertySetsByIdsAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(propertySets);
        SetupMinioUpload();
    }

    #endregion

    #region GenerateForUseCaseAsync - No AccessRights

    [Fact]
    public async Task GenerateForUseCaseAsync_NoAccessRights_SkipsGeneration()
    {
        // Arrange
        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(UseCaseId.ToString(), UserGroupId.ToString())
            .Returns(Enumerable.Empty<AccessRight>());

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _guidelineRepo.DidNotReceiveWithAnyArgs()
            .GetActiveVersionIdsAsync(default);
    }

    #endregion

    #region GenerateForUseCaseAsync - No Active Versions

    [Fact]
    public async Task GenerateForUseCaseAsync_NoActiveVersions_SkipsGeneration()
    {
        // Arrange
        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(UseCaseId.ToString(), UserGroupId.ToString())
            .Returns(new[] { new AccessRightBuilder().WithUseCaseId(UseCaseId).WithUserGroupId(UserGroupId).Build() }.AsEnumerable());
        _guidelineRepo.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _guidelineRepo.DidNotReceiveWithAnyArgs()
            .GetVersionByIdAsync(default, default);
    }

    #endregion

    #region GenerateForUseCaseAsync - Version Not Found

    [Fact]
    public async Task GenerateForUseCaseAsync_VersionNotFound_SkipsVersion()
    {
        // Arrange
        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(UseCaseId.ToString(), UserGroupId.ToString())
            .Returns(new[] { new AccessRightBuilder().WithUseCaseId(UseCaseId).WithUserGroupId(UserGroupId).Build() }.AsEnumerable());
        _guidelineRepo.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns([VersionId]);
        _guidelineRepo.GetVersionByIdAsync(VersionId, Arg.Any<CancellationToken>())
            .Returns((GuidelineVersion?)null);

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _guidelineRepo.DidNotReceiveWithAnyArgs()
            .GetClassificationsWithPropertiesAsync(default, default!, default);
    }

    #endregion

    #region GenerateForUseCaseAsync - No Matching Classifications

    [Fact]
    public async Task GenerateForUseCaseAsync_NoMatchingClassifications_SkipsUpload()
    {
        // Arrange
        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(UseCaseId.ToString(), UserGroupId.ToString())
            .Returns(new[] { new AccessRightBuilder().WithUseCaseId(UseCaseId).WithUserGroupId(UserGroupId).Build() }.AsEnumerable());
        _guidelineRepo.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns([VersionId]);
        _guidelineRepo.GetVersionByIdAsync(VersionId, Arg.Any<CancellationToken>())
            .Returns(new GuidelineVersionBuilder().WithId(VersionId).Build());
        _guidelineRepo.GetClassificationsWithPropertiesAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<GuidelineClassification>());

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.DidNotReceiveWithAnyArgs()
            .PutObjectAsync(default!, default);
    }

    #endregion

    #region GenerateForUseCaseAsync - Classifications With No Matching CPs After Filtering

    [Fact]
    public async Task GenerateForUseCaseAsync_NoMatchingClassificationProperties_SkipsUpload()
    {
        // Arrange
        var accessRights = new[] { new AccessRightBuilder()
            .WithUseCaseId(UseCaseId).WithUserGroupId(UserGroupId)
            .WithClassificationPropertyId("cp-999").Build() };
        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(UseCaseId.ToString(), UserGroupId.ToString())
            .Returns(accessRights.AsEnumerable());
        _guidelineRepo.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns([VersionId]);
        _guidelineRepo.GetVersionByIdAsync(VersionId, Arg.Any<CancellationToken>())
            .Returns(new GuidelineVersionBuilder().WithId(VersionId).Build());

        // Classification exists but has a CP with a different ID than the access right references
        var cp = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-other").Build();
        var classification = new GuidelineClassificationBuilder()
            .WithVersionId(VersionId).WithClassificationProperty(cp).Build();
        _guidelineRepo.GetClassificationsWithPropertiesAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<GuidelineClassification> { classification });

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.DidNotReceiveWithAnyArgs()
            .PutObjectAsync(default!, default);
    }

    #endregion

    #region GenerateForUseCaseAsync - Successful Generation

    [Fact]
    public async Task GenerateForUseCaseAsync_Success_UploadsToMinIO()
    {
        // Arrange
        SetupStandardScenarioByUserGroup();

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.Received(1)
            .PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_Success_ChecksBucketExists()
    {
        // Arrange
        SetupStandardScenarioByUserGroup();

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.Received(1)
            .BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_BucketDoesNotExist_CreatesBucket()
    {
        // Arrange
        SetupStandardScenarioByUserGroup();
        _minioClient.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.Received(1)
            .MakeBucketAsync(Arg.Any<MakeBucketArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_BucketExists_DoesNotCreateBucket()
    {
        // Arrange
        SetupStandardScenarioByUserGroup();

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.DidNotReceiveWithAnyArgs()
            .MakeBucketAsync(default!, default);
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_Success_PublishesOutboxEvent()
    {
        // Arrange
        SetupStandardScenarioByUserGroup();

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        _outboxRepo.Received(1).Add(
            Arg.Any<object>(),
            "usecase-guidelines",
            UseCaseId.ToString());
        await _outboxRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_Success_LoadsCorrectClassifications()
    {
        // Arrange
        var ar1 = new AccessRightBuilder()
            .WithUseCaseId(UseCaseId).WithUserGroupId(UserGroupId)
            .WithClassificationId("cls-A").WithClassificationPropertyId("cp-A1").Build();
        var ar2 = new AccessRightBuilder()
            .WithUseCaseId(UseCaseId).WithUserGroupId(UserGroupId)
            .WithClassificationId("cls-B").WithClassificationPropertyId("cp-B1").Build();
        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(UseCaseId.ToString(), UserGroupId.ToString())
            .Returns(new[] { ar1, ar2 }.AsEnumerable());
        _guidelineRepo.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns([VersionId]);
        _guidelineRepo.GetVersionByIdAsync(VersionId, Arg.Any<CancellationToken>())
            .Returns(new GuidelineVersionBuilder().WithId(VersionId).Build());

        var cpA = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-A1").WithPropertyId("prop-A").Build();
        var cpB = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-B1").WithPropertyId("prop-B").Build();
        var clsA = new GuidelineClassificationBuilder()
            .WithVersionId(VersionId).WithClassificationId("cls-A").WithClassificationProperty(cpA).Build();
        var clsB = new GuidelineClassificationBuilder()
            .WithVersionId(VersionId).WithClassificationId("cls-B").WithClassificationProperty(cpB).Build();
        _guidelineRepo.GetClassificationsWithPropertiesAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<GuidelineClassification> { clsA, clsB });

        var properties = new Dictionary<string, GuidelineProperty>
        {
            ["prop-A"] = new GuidelinePropertyBuilder().WithVersionId(VersionId).WithPropertyId("prop-A").Build(),
            ["prop-B"] = new GuidelinePropertyBuilder().WithVersionId(VersionId).WithPropertyId("prop-B").Build()
        };
        _guidelineRepo.GetPropertiesByIdsAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(properties);
        _guidelineRepo.GetPropertySetsByIdsAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelinePropertySet>());
        SetupMinioUpload();

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert - classifications were requested with the correct IDs
        await _guidelineRepo.Received(1).GetClassificationsWithPropertiesAsync(
            VersionId,
            Arg.Is<IEnumerable<string>>(ids => ids.Contains("cls-A") && ids.Contains("cls-B")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GenerateForUseCaseAsync - Property Types

    [Fact]
    public async Task GenerateForUseCaseAsync_WithPropertyEnum_ReconstructsCorrectly()
    {
        // Arrange
        var enumProp = new GuidelinePropertyBuilder()
            .WithVersionId(VersionId).WithPropertyId("prop-enum").WithPropertyType("PropertyEnum")
            .WithExtraJson("[{\"ID\":\"e1\",\"Name\":\"Option1\",\"Identifier\":\"opt1\",\"Code\":\"O1\",\"Description\":\"Opt 1\"}]")
            .Build();

        SetupStandardScenarioByUserGroup(properties: new Dictionary<string, GuidelineProperty>
        {
            ["prop-1"] = enumProp
        });

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert - if it got here without exception, reconstruction succeeded
        await _minioClient.Received(1)
            .PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_WithPropertySimple_ReconstructsCorrectly()
    {
        // Arrange
        var simpleProp = new GuidelinePropertyBuilder()
            .WithVersionId(VersionId).WithPropertyId("prop-simple").WithPropertyType("PropertySimple").WithStorageType("Integer")
            .WithExtraJson("{\"Min\":\"0\",\"MinIsInclusive\":true,\"Max\":\"100\",\"MaxIsInclusive\":true}")
            .Build();

        SetupStandardScenarioByUserGroup(properties: new Dictionary<string, GuidelineProperty>
        {
            ["prop-1"] = simpleProp
        });

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.Received(1)
            .PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_WithPropertySuperEnum_ReconstructsCorrectly()
    {
        // Arrange
        var superEnumProp = new GuidelinePropertyBuilder()
            .WithVersionId(VersionId).WithPropertyId("prop-se").WithPropertyType("PropertySuperEnum")
            .WithExtraJson("{\"Level\":2,\"Item\":null}")
            .Build();

        SetupStandardScenarioByUserGroup(properties: new Dictionary<string, GuidelineProperty>
        {
            ["prop-1"] = superEnumProp
        });

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.Received(1)
            .PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_WithPropertyTree_ReconstructsCorrectly()
    {
        // Arrange
        var treeProp = new GuidelinePropertyBuilder()
            .WithVersionId(VersionId).WithPropertyId("prop-tree").WithPropertyType("PropertyTree")
            .WithExtraJson(null)
            .Build();

        SetupStandardScenarioByUserGroup(properties: new Dictionary<string, GuidelineProperty>
        {
            ["prop-1"] = treeProp
        });

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.Received(1)
            .PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GenerateForUseCaseAsync - Assignment Types

    [Fact]
    public async Task GenerateForUseCaseAsync_WithPropertyEnumAssignment_ReconstructsCorrectly()
    {
        // Arrange
        var cp = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-1").WithPropertyId("prop-1")
            .WithAssignmentJson("{\"Type\":\"PropertyEnumAssignment\",\"FreeTextEnabled\":true,\"SelectedEnum\":null}")
            .Build();
        var cls = new GuidelineClassificationBuilder()
            .WithVersionId(VersionId).WithClassificationId("cls-1").WithClassificationProperty(cp).Build();

        SetupStandardScenarioByUserGroup(classifications: [cls]);

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.Received(1)
            .PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_WithPropertySimpleAssignment_ReconstructsCorrectly()
    {
        // Arrange
        var cp = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-1").WithPropertyId("prop-1")
            .WithAssignmentJson("{\"Type\":\"PropertySimpleAssignment\",\"Min\":\"10\",\"MinIsInclusive\":true,\"Max\":\"50\",\"MaxIsInclusive\":false}")
            .Build();
        var cls = new GuidelineClassificationBuilder()
            .WithVersionId(VersionId).WithClassificationId("cls-1").WithClassificationProperty(cp).Build();

        SetupStandardScenarioByUserGroup(classifications: [cls]);

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.Received(1)
            .PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_WithPropertySuperEnumAssignment_ReconstructsCorrectly()
    {
        // Arrange
        var cp = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-1").WithPropertyId("prop-1")
            .WithAssignmentJson("{\"Type\":\"PropertySuperEnumAssignment\"}")
            .Build();
        var cls = new GuidelineClassificationBuilder()
            .WithVersionId(VersionId).WithClassificationId("cls-1").WithClassificationProperty(cp).Build();

        SetupStandardScenarioByUserGroup(classifications: [cls]);

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.Received(1)
            .PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_WithNullAssignmentJson_UsesDefaultAssignment()
    {
        // Arrange
        var cp = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-1").WithPropertyId("prop-1")
            .WithAssignmentJson(null)
            .Build();
        var cls = new GuidelineClassificationBuilder()
            .WithVersionId(VersionId).WithClassificationId("cls-1").WithClassificationProperty(cp).Build();

        SetupStandardScenarioByUserGroup(classifications: [cls]);

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _minioClient.Received(1)
            .PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GenerateForUseCaseAsync - PropertySets

    [Fact]
    public async Task GenerateForUseCaseAsync_WithPropertySet_LoadsPropertySets()
    {
        // Arrange
        var cp = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-1").WithPropertyId("prop-1").WithPropertySetId("ps-1").Build();
        var cls = new GuidelineClassificationBuilder()
            .WithVersionId(VersionId).WithClassificationId("cls-1").WithClassificationProperty(cp).Build();
        var propertySets = new Dictionary<string, GuidelinePropertySet>
        {
            ["ps-1"] = new GuidelinePropertySetBuilder().WithVersionId(VersionId).WithPropertySetId("ps-1").Build()
        };

        SetupStandardScenarioByUserGroup(classifications: [cls], propertySets: propertySets);

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _guidelineRepo.Received(1).GetPropertySetsByIdsAsync(
            VersionId,
            Arg.Is<IEnumerable<string>>(ids => ids.Contains("ps-1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateForUseCaseAsync_WithoutPropertySets_DoesNotLoadPropertySets()
    {
        // Arrange
        SetupStandardScenarioByUserGroup();

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert
        await _guidelineRepo.DidNotReceiveWithAnyArgs()
            .GetPropertySetsByIdsAsync(default, default!, default);
    }

    #endregion

    #region GenerateForUseCaseAsync - Multiple Versions

    [Fact]
    public async Task GenerateForUserGroupAsync_MultipleVersions_MergesIntoOneUpload()
    {
        // Arrange
        var versionId2 = Guid.NewGuid();
        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(UseCaseId.ToString(), UserGroupId.ToString())
            .Returns(new[] { new AccessRightBuilder()
                .WithUseCaseId(UseCaseId).WithUserGroupId(UserGroupId)
                .WithClassificationId("cls-1").WithClassificationPropertyId("prop-1").Build() }.AsEnumerable());
        _guidelineRepo.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { VersionId, versionId2 });

        var version1 = new GuidelineVersionBuilder().WithId(VersionId).Build();
        var version2 = new GuidelineVersionBuilder().WithId(versionId2).Build();
        _guidelineRepo.GetVersionByIdAsync(VersionId, Arg.Any<CancellationToken>())
            .Returns(version1);
        _guidelineRepo.GetVersionByIdAsync(versionId2, Arg.Any<CancellationToken>())
            .Returns(version2);

        var cp = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-1").WithPropertyId("prop-1").Build();
        var cls = new GuidelineClassificationBuilder()
            .WithVersionId(VersionId).WithClassificationId("cls-1").WithClassificationProperty(cp).Build();
        _guidelineRepo.GetClassificationsWithPropertiesAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<GuidelineClassification> { cls });
        _guidelineRepo.GetPropertiesByIdsAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelineProperty> { ["prop-1"] = new GuidelinePropertyBuilder().WithVersionId(VersionId).WithPropertyId("prop-1").Build() });
        SetupMinioUpload();

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert - both versions are merged into a single upload
        await _minioClient.Received(1)
            .PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GenerateForUseCaseAsync - DomainJson

    [Fact]
    public async Task GenerateForUseCaseAsync_WithNullDomainJson_DefaultsToActiveStatus()
    {
        // Arrange
        var version = new GuidelineVersionBuilder().WithId(VersionId).WithDomainJson(null).Build();
        SetupStandardScenarioByUserGroup(version: version);

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert - no exception means it handled null DomainJson gracefully
        await _minioClient.Received(1)
            .PutObjectAsync(Arg.Any<PutObjectArgs>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region GenerateForAllUseCasesAsync

    [Fact]
    public async Task GenerateForAllUseCasesAsync_CallsGenerateForEachPair()
    {
        // Arrange
        var useCaseId2 = Guid.NewGuid();
        var userGroupId2 = Guid.NewGuid();
        _accessRightsRepo.GetDistinctUseCaseUserGroupPairsAsync()
            .Returns(new List<(Guid, Guid)> { (UseCaseId, UserGroupId), (useCaseId2, userGroupId2) });

        // Both pairs have access rights
        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new[] { new AccessRightBuilder().WithUseCaseId(UseCaseId).WithUserGroupId(UserGroupId).Build() }.AsEnumerable());
        _guidelineRepo.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns([VersionId]);
        _guidelineRepo.GetVersionByIdAsync(VersionId, Arg.Any<CancellationToken>())
            .Returns(new GuidelineVersionBuilder().WithId(VersionId).Build());

        var cp = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-1").WithPropertyId("prop-1").Build();
        _guidelineRepo.GetClassificationsWithPropertiesAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<GuidelineClassification> { new GuidelineClassificationBuilder()
                .WithVersionId(VersionId).WithClassificationId("cls-1").WithClassificationProperty(cp).Build() });
        _guidelineRepo.GetPropertiesByIdsAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelineProperty> { ["prop-1"] = new GuidelinePropertyBuilder().WithVersionId(VersionId).WithPropertyId("prop-1").Build() });
        SetupMinioUpload();

        // Act
        await _sut.GenerateForAllUseCasesAsync();

        // Assert
        await _accessRightsRepo.Received(2).GetAccessRightsByUseCaseUserGroupAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GenerateForAllUseCasesAsync_NoPairs_CompletesWithoutError()
    {
        // Arrange
        _accessRightsRepo.GetDistinctUseCaseUserGroupPairsAsync()
            .Returns(new List<(Guid, Guid)>());

        // Act
        await _sut.GenerateForAllUseCasesAsync();

        // Assert
        await _accessRightsRepo.DidNotReceiveWithAnyArgs()
            .GetAccessRightsByUseCaseUserGroupAsync(default!, default!);
    }

    [Fact]
    public async Task GenerateForAllUseCasesAsync_OnePairFails_ContinuesWithNext()
    {
        // Arrange
        var useCaseId2 = Guid.NewGuid();
        var userGroupId2 = Guid.NewGuid();
        _accessRightsRepo.GetDistinctUseCaseUserGroupPairsAsync()
            .Returns(new List<(Guid, Guid)> { (UseCaseId, UserGroupId), (useCaseId2, userGroupId2) });

        // First pair throws, second returns empty (skips generation)
        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(UseCaseId.ToString(), UserGroupId.ToString())
            .Throws(new InvalidOperationException("Test failure"));
        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(useCaseId2.ToString(), userGroupId2.ToString())
            .Returns(Enumerable.Empty<AccessRight>());

        // Act
        await _sut.GenerateForAllUseCasesAsync();

        // Assert - second pair was still attempted
        await _accessRightsRepo.Received(1).GetAccessRightsByUseCaseUserGroupAsync(useCaseId2.ToString(), userGroupId2.ToString());
    }

    #endregion

    #region GenerateForUseCaseAsync - Filtering

    [Fact]
    public async Task GenerateForUseCaseAsync_FiltersClassificationPropertiesToOnlyAccessRightReferenced()
    {
        // Arrange - access right references prop-1 but classification has cp-1 (prop-1) and cp-2 (prop-2)
        var accessRights = new[] { new AccessRightBuilder()
            .WithUseCaseId(UseCaseId).WithUserGroupId(UserGroupId)
            .WithClassificationPropertyId("prop-1").Build() };
        _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(UseCaseId.ToString(), UserGroupId.ToString())
            .Returns(accessRights.AsEnumerable());
        _guidelineRepo.GetActiveVersionIdsAsync(Arg.Any<CancellationToken>())
            .Returns([VersionId]);
        _guidelineRepo.GetVersionByIdAsync(VersionId, Arg.Any<CancellationToken>())
            .Returns(new GuidelineVersionBuilder().WithId(VersionId).Build());

        var cp1 = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-1").WithPropertyId("prop-1").Build();
        var cp2 = new GuidelineClassificationPropertyBuilder()
            .WithClassificationPropertyId("cp-2").WithPropertyId("prop-2").Build();
        var cls = new GuidelineClassificationBuilder()
            .WithVersionId(VersionId).WithClassificationProperties(cp1, cp2).Build();
        _guidelineRepo.GetClassificationsWithPropertiesAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<GuidelineClassification> { cls });

        _guidelineRepo.GetPropertiesByIdsAsync(VersionId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, GuidelineProperty>
            {
                ["prop-1"] = new GuidelinePropertyBuilder().WithVersionId(VersionId).WithPropertyId("prop-1").Build()
            });
        SetupMinioUpload();

        // Act
        await _sut.GenerateForUserGroupAsync(UseCaseId, UserGroupId);

        // Assert - only prop-1 should be requested since cp-2 was filtered out
        await _guidelineRepo.Received(1).GetPropertiesByIdsAsync(
            VersionId,
            Arg.Is<IEnumerable<string>>(ids => ids.Contains("prop-1") && !ids.Contains("prop-2")),
            Arg.Any<CancellationToken>());
    }

    #endregion
}

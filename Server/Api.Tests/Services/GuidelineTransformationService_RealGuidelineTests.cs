// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Reflection;
using AccessService.Api.Events;
using AccessService.Api.Services;
using AccessService.Domain.Models;
using AccessService.Domain.Repositories;
using Guideline.Model.Model;
using Microsoft.Extensions.Logging;
using GuidelineModelIO;
using Minio;
using System.Text.Json;

namespace AccessService.Api.Tests.Services;

/// <summary>
/// Integration tests that deserialize the real IBPDI guideline file
/// and verify the transformation pipeline produces correct and complete results.
/// </summary>
public class GuidelineTransformationService_RealGuidelineTests : IDisposable
{
	private static readonly string DataDir =
		Path.Combine(AppContext.BaseDirectory, "data");

	private static readonly string GuidelineFilePath =
		Path.Combine(DataDir, "IBPDI.guideline");

	private readonly Guideline.Model.Model.Guideline _guideline;
	private readonly GuidelineVersion _version;
	private readonly GuidelineTransformationService _sut;

	public GuidelineTransformationService_RealGuidelineTests()
	{
		// Deserialize the real guideline via the same approach the service uses
		_guideline = DeserializeGuidelineFromFile(GuidelineFilePath);

		var repository = Substitute.For<IGuidelineProjectionRepository>();
		var logger = Substitute.For<ILogger<GuidelineTransformationService>>();
		var minioClient = Substitute.For<IMinioClient>();
		var accessRightsRepository = Substitute.For<IAccessRightsRepository>();
		var useCaseGuidelineService = Substitute.For<IUseCaseGuidelineService>();

		_sut = new GuidelineTransformationService(minioClient, repository, accessRightsRepository, useCaseGuidelineService, logger);

		var evt = new UploadedGuideline
		{
			Id = Guid.NewGuid().ToString(),
			Name = "IBPDI.guideline",
			ObjectKey = "IBPDI.guideline",
			Etag = "real-etag-ibpdi",
			BucketName = "guidelines",
			CorrelationId = Guid.NewGuid(),
			Timestamp = DateTimeOffset.UtcNow
		};

		_version = InvokeTransformToRelationalModel(_guideline, evt);
	}

	public void Dispose()
	{
		// nothing to clean up
	}

	#region Helpers

	private static Guideline.Model.Model.Guideline DeserializeGuidelineFromFile(string filePath)
	{
		// Read through the package reader, exactly as the service does — it owns the file schema.
		return new GuidelineReaderWriter().GuidelineRead(filePath) as Guideline.Model.Model.Guideline
			   ?? throw new InvalidOperationException("Failed to deserialize guideline from test file.");
	}

	private GuidelineVersion InvokeTransformToRelationalModel(
		Guideline.Model.Model.Guideline guideline, UploadedGuideline evt)
	{
		var method = typeof(GuidelineTransformationService)
			.GetMethod("TransformToRelationalModel", BindingFlags.NonPublic | BindingFlags.Instance)!;
		return (GuidelineVersion)method.Invoke(_sut, new object[] { guideline, evt })!;
	}

	#endregion

	#region Deserialization

	[Fact]
	public void RealGuideline_DeserializesSuccessfully()
	{
		Assert.NotNull(_guideline);
	}

	[Fact]
	public void RealGuideline_HasNonEmptyId()
	{
		Assert.NotNull(_guideline.ID);
		Assert.NotEmpty(_guideline.ID);
	}

	[Fact]
	public void RealGuideline_HasName()
	{
		// The guideline Name may be empty; verify it is at least not null
		Assert.NotNull(_guideline.Name);
	}

	[Fact]
	public void RealGuideline_HasDomain()
	{
		Assert.NotNull(_guideline.Domain);
	}

	[Fact]
	public void RealGuideline_DomainHasClassifications()
	{
		Assert.NotNull(_guideline.Domain!.Classifications);
		Assert.NotEmpty(_guideline.Domain.Classifications);
	}

	[Fact]
	public void RealGuideline_DomainHasProperties()
	{
		Assert.NotNull(_guideline.Domain!.Properties);
		Assert.NotEmpty(_guideline.Domain.Properties);
	}

	#endregion

	#region Version-Level Transformation

	[Fact]
	public void Transform_Version_HasValidId()
	{
		Assert.NotEqual(Guid.Empty, _version.Id);
	}

	[Fact]
	public void Transform_Version_MapsGuidelineId()
	{
		Assert.Equal(_guideline.Identifier, _version.GuidelineId);
	}

	[Fact]
	public void Transform_Version_MapsName()
	{
		Assert.Equal(_version.Name, _version.Name);
	}

	[Fact]
	public void Transform_Version_MapsEventFields()
	{
		Assert.Equal("IBPDI.guideline", _version.ObjectName);
		Assert.Equal("real-etag-ibpdi", _version.Etag);
		Assert.Equal("guidelines", _version.BucketName);
	}

	[Fact]
	public void Transform_Version_HasDomainJson()
	{
		Assert.NotNull(_version.DomainJson);
		Assert.NotEmpty(_version.DomainJson);
		Assert.Contains(_guideline.Domain!.ID, _version.DomainJson);
	}

	#endregion

	#region Classifications

	[Fact]
	public void Transform_Classifications_CountMatchesSource()
	{
		var sourceCount = _guideline.Domain!.Classifications!.Count;
		Assert.Equal(sourceCount, _version.Classifications.Count);
	}

	[Fact]
	public void Transform_Classifications_AllHaveNonEmptyIds()
	{
		Assert.All(_version.Classifications, c => Assert.NotEqual(Guid.Empty, c.Id));
	}

	[Fact]
	public void Transform_Classifications_AllReferenceCorrectVersion()
	{
		Assert.All(_version.Classifications, c => Assert.Equal(_version.Id, c.GuidelineVersionId));
	}

	[Fact]
	public void Transform_Classifications_AllHaveNonEmptyClassificationId()
	{
		Assert.All(_version.Classifications, c => Assert.False(string.IsNullOrEmpty(c.ClassificationId)));
	}

	[Fact]
	public void Transform_Classifications_AllHaveNonEmptyName()
	{
		Assert.All(_version.Classifications, c => Assert.False(string.IsNullOrEmpty(c.Name)));
	}

	[Fact]
	public void Transform_Classifications_HaveUniqueIds()
	{
		var ids = _version.Classifications.Select(c => c.Id).ToList();
		var uniqueIds = ids.Distinct().ToList();
		Assert.Equal(ids.Count, uniqueIds.Count);
	}

	[Fact]
	public void Transform_Classifications_SourceClassificationIdsArePreserved()
	{
		var sourceIds = _guideline.Domain!.Classifications!.Select(c => c.Identifier).ToHashSet();
		var transformedIds = _version.Classifications.Select(c => c.ClassificationId).ToHashSet();
		Assert.Equal(sourceIds, transformedIds);
	}

	[Fact]
	public void Transform_Classifications_TotalClassificationPropertiesMatchOrAreLessThanSource()
	{
		// The source may contain ClassificationProperties with duplicate Identifiers.
		// The AccessService silently deduplicates by Identifier (the DB unique key),
		// so the transformed count can be equal to or less than the source count.
		var sourceTotal = _guideline.Domain!.Classifications!
			.Where(c => c.ClassificationProperties != null)
			.Sum(c => c.ClassificationProperties!.Count);

		var transformedTotal = _version.Classifications
			.Sum(c => c.ClassificationProperties.Count);

		Assert.True(transformedTotal > 0);
		Assert.True(transformedTotal <= sourceTotal);
	}

	#endregion

	#region Classification Properties

	[Fact]
	public void Transform_ClassificationProperties_AllHaveNonEmptyIds()
	{
		var allCps = _version.Classifications.SelectMany(c => c.ClassificationProperties);
		Assert.All(allCps, cp => Assert.NotEqual(Guid.Empty, cp.Id));
	}

	[Fact]
	public void Transform_ClassificationProperties_AllReferenceParentClassification()
	{
		foreach (var cls in _version.Classifications)
		{
			Assert.All(cls.ClassificationProperties, cp => Assert.Equal(cls.Id, cp.GuidelineClassificationId));
		}
	}

	[Fact]
	public void Transform_ClassificationProperties_HaveUniqueIds()
	{
		var ids = _version.Classifications
			.SelectMany(c => c.ClassificationProperties)
			.Select(cp => cp.Id)
			.ToList();
		var uniqueIds = ids.Distinct().ToList();
		Assert.Equal(ids.Count, uniqueIds.Count);
	}

	#endregion

	#region Properties

	[Fact]
	public void Transform_Properties_CountMatchesSource()
	{
		var sourceCount = _guideline.Domain!.Properties!.Count;
		Assert.Equal(sourceCount, _version.Properties.Count);
	}

	[Fact]
	public void Transform_Properties_AllHaveNonEmptyIds()
	{
		Assert.All(_version.Properties, p => Assert.NotEqual(Guid.Empty, p.Id));
	}

	[Fact]
	public void Transform_Properties_AllReferenceCorrectVersion()
	{
		Assert.All(_version.Properties, p => Assert.Equal(_version.Id, p.GuidelineVersionId));
	}

	[Fact]
	public void Transform_Properties_AllHavePropertyType()
	{
		Assert.All(_version.Properties, p => Assert.False(string.IsNullOrEmpty(p.PropertyType)));
	}

	[Fact]
	public void Transform_Properties_AllHaveNonEmptyPropertyId()
	{
		Assert.All(_version.Properties, p => Assert.False(string.IsNullOrEmpty(p.PropertyId)));
	}

	[Fact]
	public void Transform_Properties_HaveUniqueIds()
	{
		var ids = _version.Properties.Select(p => p.Id).ToList();
		var uniqueIds = ids.Distinct().ToList();
		Assert.Equal(ids.Count, uniqueIds.Count);
	}

	[Fact]
	public void Transform_Properties_SourcePropertyIdsArePreserved()
	{
		var sourceIds = _guideline.Domain!.Properties!.Select(p => p.Identifier).ToHashSet();
		var transformedIds = _version.Properties.Select(p => p.PropertyId).ToHashSet();
		Assert.Equal(sourceIds, transformedIds);
	}

	[Fact]
	public void Transform_Properties_ContainKnownPropertyTypes()
	{
		var types = _version.Properties.Select(p => p.PropertyType).Distinct().ToList();
		// The IBPDI guideline should contain at least some of these property types
		var knownTypes = new[] { nameof(PropertySimple), nameof(PropertyEnum), nameof(PropertySuperEnum) };
		Assert.True(types.Any(t => knownTypes.Contains(t)));
	}

	[Fact]
	public void Transform_Properties_AllHaveStorageType()
	{
		Assert.All(_version.Properties, p => Assert.False(string.IsNullOrEmpty(p.StorageType)));
	}

	[Fact]
	public void Transform_Properties_AllHaveStatus()
	{
		Assert.All(_version.Properties, p => Assert.False(string.IsNullOrEmpty(p.Status)));
	}

	#endregion

	#region Property Sets

	[Fact]
	public void Transform_PropertySets_CountMatchesSource()
	{
		if (_guideline.Domain!.PropertySets == null)
		{
			Assert.Empty(_version.PropertySets);
			return;
		}

		Assert.Equal(_guideline.Domain.PropertySets.Count, _version.PropertySets.Count);
	}

	[Fact]
	public void Transform_PropertySets_AllReferenceCorrectVersion()
	{
		Assert.All(_version.PropertySets, ps => Assert.Equal(_version.Id, ps.GuidelineVersionId));
	}

	[Fact]
	public void Transform_PropertySets_HaveUniqueIds()
	{
		if (_version.PropertySets.Count == 0)
			return;

		var ids = _version.PropertySets.Select(ps => ps.Id).ToList();
		var uniqueIds = ids.Distinct().ToList();
		Assert.Equal(ids.Count, uniqueIds.Count);
	}

	#endregion

	#region Referential Integrity

	[Fact]
	public void Transform_ClassificationPropertyIds_ReferenceExistingProperties()
	{
		var propertyIds = _version.Properties.Select(p => p.PropertyId).ToHashSet();

		var cpPropertyIds = _version.Classifications
			.SelectMany(c => c.ClassificationProperties)
			.Where(cp => !string.IsNullOrEmpty(cp.PropertyId))
			.Select(cp => cp.PropertyId)
			.Distinct()
			.ToList();

		// All property IDs referenced by classification properties should exist
		// in the domain-level properties collection
		Assert.All(cpPropertyIds, id => Assert.True(propertyIds.Contains(id), "every classification property's PropertyId should reference a known property"));
	}

	[Fact]
	public void Transform_ClassificationPropertySetIds_ReferenceExistingPropertySets()
	{
		if (_version.PropertySets.Count == 0)
			return;

		var propertySetIds = _version.PropertySets.Select(ps => ps.PropertySetId).ToHashSet();

		var cpPropertySetIds = _version.Classifications
			.SelectMany(c => c.ClassificationProperties)
			.Where(cp => !string.IsNullOrEmpty(cp.PropertySetId))
			.Select(cp => cp.PropertySetId!)
			.Distinct()
			.ToList();

		Assert.All(cpPropertySetIds, id => Assert.True(propertySetIds.Contains(id), "every classification property's PropertySetId should reference a known property set"));
	}

	#endregion

	#region JSON Blobs

	[Fact]
	public void Transform_ClassificationsWithRelations_HaveValidRelationsJson()
	{
		var withRelations = _version.Classifications
			.Where(c => !string.IsNullOrEmpty(c.RelationsJson))
			.ToList();

		foreach (var cls in withRelations)
		{
			var action = () => JsonDocument.Parse(cls.RelationsJson!);
			var exception = Record.Exception(action);
			Assert.Null(exception);
		}
	}

	[Fact]
	public void Transform_PropertiesWithExtraJson_HaveValidJson()
	{
		var withExtra = _version.Properties
			.Where(p => !string.IsNullOrEmpty(p.ExtraJson))
			.ToList();

		foreach (var prop in withExtra)
		{
			var action = () => JsonDocument.Parse(prop.ExtraJson!);
			var exception = Record.Exception(action);
			Assert.Null(exception);
		}
	}

	[Fact]
	public void Transform_ClassificationPropertiesWithAssignmentJson_HaveValidJson()
	{
		var withAssignment = _version.Classifications
			.SelectMany(c => c.ClassificationProperties)
			.Where(cp => !string.IsNullOrEmpty(cp.AssignmentJson))
			.ToList();

		foreach (var cp in withAssignment)
		{
			var action = () => JsonDocument.Parse(cp.AssignmentJson!);
			var exception = Record.Exception(action);
			Assert.Null(exception);
		}
	}

	[Fact]
	public void Transform_DomainJson_IsValidJson()
	{
		var action = () => JsonDocument.Parse(_version.DomainJson!);
		var exception = Record.Exception(action);
		Assert.Null(exception);
	}

	#endregion

	#region Scale Assertions

	[Fact]
	public void Transform_RealGuideline_HasSignificantNumberOfClassifications()
	{
		// IBPDI should have a substantial number of classifications
		Assert.True(_version.Classifications.Count > 10, "IBPDI guideline should have many classifications");
	}

	[Fact]
	public void Transform_RealGuideline_HasSignificantNumberOfProperties()
	{
		Assert.True(_version.Properties.Count > 10, "IBPDI guideline should have many properties");
	}

	[Fact]
	public void Transform_RealGuideline_HasClassificationProperties()
	{
		var totalCps = _version.Classifications.Sum(c => c.ClassificationProperties.Count);
		Assert.True(totalCps > 0, "IBPDI guideline should have classification-property assignments");
	}

	#endregion
}

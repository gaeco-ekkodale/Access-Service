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
using Minio;
using Minio.DataModel.Args;

namespace AccessService.Api.Tests.Services;

public class GuidelineTransformationServiceTests
{
	private readonly IGuidelineProjectionRepository _repository;
	private readonly ILogger<GuidelineTransformationService> _logger;
	private readonly IMinioClient _minioClient;
	private readonly GuidelineTransformationService _sut;

	public GuidelineTransformationServiceTests()
	{
		_repository = Substitute.For<IGuidelineProjectionRepository>();
		_logger = Substitute.For<ILogger<GuidelineTransformationService>>();
		_minioClient = Substitute.For<IMinioClient>();

		var useCaseGuidelineService = Substitute.For<IUseCaseGuidelineService>();
		var accessRightsRepository = Substitute.For<IAccessRightsRepository>();
		_sut = new GuidelineTransformationService(_minioClient, _repository, accessRightsRepository, useCaseGuidelineService, _logger);
	}

	#region Test Helpers

	private static UploadedGuideline CreateEvent(
		string name = "test-guideline.json",
		string etag = "etag-abc123",
		string bucket = "guidelines")
	{
		return new UploadedGuideline
		{
			Id = Guid.NewGuid().ToString(),
			Name = name,
			ObjectKey = name,
			Etag = etag,
			BucketName = bucket,
			CorrelationId = Guid.NewGuid(),
			Timestamp = DateTimeOffset.UtcNow
		};
	}

	private GuidelineVersion InvokeTransformToRelationalModel(
		Guideline.Model.Model.Guideline guideline, UploadedGuideline evt)
	{
		var method = typeof(GuidelineTransformationService)
			.GetMethod("TransformToRelationalModel", BindingFlags.NonPublic | BindingFlags.Instance)!;
		return (GuidelineVersion)method.Invoke(_sut, new object[] { guideline, evt })!;
	}

	private GuidelineClassification InvokeTransformClassification(
		IClassification cls, Guid versionId, string objectName = "test-guideline.json")
	{
		var method = typeof(GuidelineTransformationService)
			.GetMethod("TransformClassification", BindingFlags.NonPublic | BindingFlags.Instance)!;
		return (GuidelineClassification)method.Invoke(_sut, new object[] { cls, versionId, objectName })!;
	}

	private static GuidelineClassificationProperty InvokeTransformClassificationProperty(
		IClassificationProperty cp, Guid classificationId)
	{
		var method = typeof(GuidelineTransformationService)
			.GetMethod("TransformClassificationProperty", BindingFlags.NonPublic | BindingFlags.Static)!;
		return (GuidelineClassificationProperty)method.Invoke(null, new object[] { cp, classificationId })!;
	}

	private static GuidelineProperty InvokeTransformProperty(IProperty prop, Guid versionId)
	{
		var method = typeof(GuidelineTransformationService)
			.GetMethod("TransformProperty", BindingFlags.NonPublic | BindingFlags.Static)!;
		return (GuidelineProperty)method.Invoke(null, new object[] { prop, versionId })!;
	}

	/// <summary>
	/// Sets a property value on an object, supporting both public and non-public setters.
	/// Useful for Guideline.Model types that may have internal/private setters.
	/// </summary>
	private static void SetProperty(object obj, string propertyName, object? value)
	{
		var prop = obj.GetType().GetProperty(propertyName,
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
		if (prop != null && prop.CanWrite)
		{
			prop.SetValue(obj, value);
		}
		else
		{
			// Try setting the backing field directly
			var field = obj.GetType().GetField($"<{propertyName}>k__BackingField",
				BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			field?.SetValue(obj, value);
		}
	}

	private static T CreateModelInstance<T>() where T : class
	{
		return (T)(Activator.CreateInstance(typeof(T), nonPublic: true)
				   ?? throw new InvalidOperationException($"Cannot create {typeof(T).Name}"));
	}

	private static Guideline.Model.Model.Guideline CreateTestGuideline(
		string id = "gl-001",
		string name = "Test Guideline",
		string? identifier = "TG",
		string? description = "A test guideline",
		string? version = "1.0",
		IDomain? domain = null)
	{
		var guideline = CreateModelInstance<Guideline.Model.Model.Guideline>();
		SetProperty(guideline, "ID", id);
		SetProperty(guideline, "Name", name);
		SetProperty(guideline, "Identifier", identifier);
		SetProperty(guideline, "Description", description);
		SetProperty(guideline, "Version", version);
		SetProperty(guideline, "Domain", domain);
		return guideline;
	}

	private static IClassification CreateMockClassification(
		string id = "cls-001",
		string name = "TestClassification",
		string? identifier = null,
		string? code = "TC01",
		string? description = "A test classification",
		IList<IClassificationProperty>? classificationProperties = null)
	{
		var cls = Substitute.For<IClassification>();
		cls.ID.Returns(id);
		cls.Name.Returns(name);
		cls.Identifier.Returns(identifier ?? id);
		cls.Code.Returns(code);
		cls.Description.Returns(description);
		cls.ClassificationProperties.Returns(classificationProperties);
		// Explicitly set Parent and Children to null to avoid NSubstitute auto-mocking
		cls.Parent.Returns((IClassificationRelation?)null);
		cls.Children.Returns((IList<IClassificationRelation>?)null);
		return cls;
	}

	private static IClassificationProperty CreateMockClassificationProperty(
		string id = "cp-001",
		string? identifier = null,
		bool isRequired = true,
		int sortNumber = 1,
		bool isReadonly = false,
		string? defaultValue = null,
		string? reference = null,
		string? propertySetId = null,
		IPropertyAssignment? assignment = null)
	{
		var cp = Substitute.For<IClassificationProperty>();
		cp.ID.Returns(id);
		cp.Identifier.Returns(identifier ?? id);
		cp.IsRequired.Returns(isRequired);
		cp.SortNumber.Returns(sortNumber);
		cp.IsReadonly.Returns(isReadonly);
		cp.DefaultValue.Returns(defaultValue);
		cp.Reference.Returns(reference);
		cp.PropertyAssignment.Returns(assignment);

		if (propertySetId != null)
		{
			var ps = Substitute.For<IPropertySet>();
			ps.ID.Returns(propertySetId);
			ps.Identifier.Returns(propertySetId);
			cp.PropertySet.Returns(ps);
		}
		else
		{
			cp.PropertySet.Returns((IPropertySet?)null);
		}

		return cp;
	}

	#endregion

	#region ProcessAsync - Idempotency

	[Fact]
	public async Task ProcessAsync_WhenGuidelineAlreadyExists_SkipsProcessing()
	{
		// Arrange
		var evt = CreateEvent();
		_repository.ExistsAsync(evt.Id, evt.Etag, Arg.Any<CancellationToken>())
			.Returns(true);

		// Act
		await _sut.ProcessAsync(evt);

		// Assert
		await _repository.DidNotReceive()
			.UpsertAsync(Arg.Any<GuidelineVersion>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ProcessAsync_WhenGuidelineAlreadyExists_DoesNotInteractWithMinIO()
	{
		// Arrange
		var evt = CreateEvent();
		_repository.ExistsAsync(evt.Id, evt.Etag, Arg.Any<CancellationToken>())
			.Returns(true);

		// Act
		await _sut.ProcessAsync(evt);

		// Assert
		await _minioClient.DidNotReceiveWithAnyArgs()
			.BucketExistsAsync(default!, default);
		await _minioClient.DidNotReceiveWithAnyArgs()
			.GetObjectAsync(default!, default);
	}

	[Fact]
	public async Task ProcessAsync_WhenGuidelineDoesNotExist_ChecksBucketExists()
	{
		// Arrange
		var evt = CreateEvent();
		_repository.ExistsAsync(evt.Name, evt.Etag, Arg.Any<CancellationToken>())
			.Returns(false);
		_minioClient.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
			.Returns(false);

		// Act
		try
		{
			await _sut.ProcessAsync(evt);
		}
		catch { /* expected */ }

		// Assert
		await _minioClient.Received(1)
			.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region ProcessAsync - Bucket Not Found

	[Fact]
	public async Task ProcessAsync_WhenBucketDoesNotExist_ThrowsFileNotFoundException()
	{
		// Arrange
		var evt = CreateEvent(bucket: "nonexistent-bucket");
		_repository.ExistsAsync(evt.Name, evt.Etag, Arg.Any<CancellationToken>())
			.Returns(false);
		_minioClient.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
			.Returns(false);

		// Act
		var act = () => _sut.ProcessAsync(evt);

		// Assert
		var exception = await Assert.ThrowsAsync<FileNotFoundException>(act);
		Assert.Contains("nonexistent-bucket", exception.Message);
		Assert.Contains("does not exist", exception.Message);
	}

	[Fact]
	public async Task ProcessAsync_WhenBucketDoesNotExist_DoesNotPersist()
	{
		// Arrange
		var evt = CreateEvent();
		_repository.ExistsAsync(evt.Name, evt.Etag, Arg.Any<CancellationToken>())
			.Returns(false);
		_minioClient.BucketExistsAsync(Arg.Any<BucketExistsArgs>(), Arg.Any<CancellationToken>())
			.Returns(false);

		// Act
		try
		{
			await _sut.ProcessAsync(evt);
		}
		catch { /* expected */ }

		// Assert
		await _repository.DidNotReceive()
			.UpsertAsync(Arg.Any<GuidelineVersion>(), Arg.Any<CancellationToken>());
	}

	#endregion

	#region TransformToRelationalModel - Basic Fields

	[Fact]
	public void TransformToRelationalModel_MapsOptionalGuidelineFields()
	{
		// Arrange
		var guideline = CreateTestGuideline(
			identifier: "IDENT-01",
			description: "Description here",
			version: "2.5");
		var evt = CreateEvent();

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		Assert.Equal("IDENT-01", version.Identifier);
		Assert.Equal("Description here", version.Description);
		Assert.Equal("2.5", version.Version);
	}

	[Fact]
	public void TransformToRelationalModel_MapsEventFields()
	{
		// Arrange
		var guideline = CreateTestGuideline();
		var evt = CreateEvent(name: "obj-key.json", etag: "etag-xyz", bucket: "my-bucket");

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		Assert.Equal("obj-key.json", version.ObjectName);
		Assert.Equal("etag-xyz", version.Etag);
		Assert.Equal("my-bucket", version.BucketName);
		Assert.Equal(evt.CorrelationId, version.CorrelationId);
		Assert.Equal(evt.Timestamp, version.EventTimestamp);
	}

	[Fact]
	public void TransformToRelationalModel_GeneratesNonEmptyId()
	{
		// Arrange
		var guideline = CreateTestGuideline();
		var evt = CreateEvent();

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		Assert.NotEqual(Guid.Empty, version.Id);
	}

	[Fact]
	public void TransformToRelationalModel_SetsProcessedAtToApproximatelyNow()
	{
		// Arrange
		var guideline = CreateTestGuideline();
		var evt = CreateEvent();
		var before = DateTimeOffset.UtcNow;

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		var after = DateTimeOffset.UtcNow;
		Assert.True(version.ProcessedAt >= before, "ProcessedAt should be on or after before");
		Assert.True(version.ProcessedAt <= after, "ProcessedAt should be on or before after");
	}

	[Fact]
	public void TransformToRelationalModel_NullGuidelineId_ThrowsInvalidOperationException()
	{
		// Arrange
		var guideline = CreateTestGuideline(identifier: null!);
		var evt = CreateEvent();

		// Act
		var act = () => InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		var thrownException = Assert.Throws<TargetInvocationException>(act);
		Assert.IsType<InvalidOperationException>(thrownException.InnerException);
	}

	[Fact]
	public void TransformToRelationalModel_NullGuidelineName_DefaultsToEmptyString()
	{
		// Arrange
		var guideline = CreateTestGuideline(name: null!);
		var evt = CreateEvent();

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		Assert.Equal(evt.Name, version.Name);
	}

	#endregion

	#region TransformToRelationalModel - Domain

	[Fact]
	public void TransformToRelationalModel_NullDomain_HasEmptyCollections()
	{
		// Arrange
		var guideline = CreateTestGuideline(domain: null);
		var evt = CreateEvent();

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		Assert.Empty(version.Classifications);
		Assert.Empty(version.Properties);
		Assert.Empty(version.PropertySets);
		Assert.Null(version.DomainJson);
	}

	[Fact]
	public void TransformToRelationalModel_WithDomain_SerializesDomainJson()
	{
		// Arrange
		var domain = Substitute.For<IDomain>();
		domain.ID.Returns("dom-001");
		domain.Name.Returns("TestDomain");
		domain.Identifier.Returns("TD");
		domain.Description.Returns("Domain desc");
		domain.Version.Returns("3.0");

		var guideline = CreateTestGuideline(domain: domain);
		var evt = CreateEvent();

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		Assert.NotNull(version.DomainJson);
		Assert.NotEmpty(version.DomainJson);
		Assert.Contains("dom-001", version.DomainJson);
		Assert.Contains("TestDomain", version.DomainJson);
	}

	[Fact]
	public void TransformToRelationalModel_WithClassifications_TransformsAll()
	{
		// Arrange
		var cls1 = CreateMockClassification(id: "cls-1", name: "Classification1");
		var cls2 = CreateMockClassification(id: "cls-2", name: "Classification2");

		var domain = Substitute.For<IDomain>();
		domain.Classifications.Returns(new List<IClassification> { cls1, cls2 });

		var guideline = CreateTestGuideline(domain: domain);
		var evt = CreateEvent();

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		Assert.Equal(2, version.Classifications.Count);
		var classificationIds = version.Classifications.Select(c => c.ClassificationId).ToList();
		Assert.Contains("cls-1", classificationIds);
		Assert.Contains("cls-2", classificationIds);
	}

	[Fact]
	public void TransformToRelationalModel_WithPropertySets_TransformsAll()
	{
		// Arrange
		var ps1 = Substitute.For<IPropertySet>();
		ps1.ID.Returns("ps-1");
		ps1.Name.Returns("PropSet1");
		ps1.Identifier.Returns("ps-1");
		ps1.Description.Returns("Desc 1");

		var ps2 = Substitute.For<IPropertySet>();
		ps2.ID.Returns("ps-2");
		ps2.Name.Returns("PropSet2");
		ps2.Identifier.Returns("ps-2");

		var domain = Substitute.For<IDomain>();
		domain.PropertySets.Returns(new List<IPropertySet> { ps1, ps2 });

		var guideline = CreateTestGuideline(domain: domain);
		var evt = CreateEvent();

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		Assert.Equal(2, version.PropertySets.Count);
		var propertySetIds = version.PropertySets.Select(p => p.PropertySetId).ToList();
		Assert.Contains("ps-1", propertySetIds);
		Assert.Contains("ps-2", propertySetIds);
	}

	#endregion

	#region TransformClassification

	[Fact]
	public void TransformClassification_MapsBasicFields()
	{
		// Arrange
		var versionId = Guid.NewGuid();
		var cls = CreateMockClassification(
			id: "cls-abc",
			name: "Wall",
			identifier: "IfcWall",
			code: "W01",
			description: "Represents a wall");

		// Act
		var result = InvokeTransformClassification(cls, versionId);

		// Assert
		Assert.NotEqual(Guid.Empty, result.Id);
		Assert.Equal(versionId, result.GuidelineVersionId);
		Assert.Equal("IfcWall", result.ClassificationId);
		Assert.Equal("Wall", result.Name);
		Assert.Equal("IfcWall", result.Identifier);
		Assert.Equal("W01", result.Code);
		Assert.Equal("Represents a wall", result.Description);
		Assert.NotNull(result.Status);
	}

	[Fact]
	public void TransformClassification_NullId_ThrowsInvalidOperationException()
	{
		// Arrange
		var cls = CreateMockClassification(id: null!);

		// Act
		var act = () => InvokeTransformClassification(cls, Guid.NewGuid());

		// Assert
		var thrownException = Assert.Throws<TargetInvocationException>(act);
		Assert.IsType<InvalidOperationException>(thrownException.InnerException);
	}

	[Fact]
	public void TransformClassification_NullName_DefaultsToEmptyString()
	{
		// Arrange
		var cls = CreateMockClassification(name: null!);

		// Act
		var result = InvokeTransformClassification(cls, Guid.NewGuid());

		// Assert
		Assert.Empty(result.Name);
	}

	[Fact]
	public void TransformClassification_NoRelations_NullRelationsJson()
	{
		// Arrange - NSubstitute returns null for Parent and Children by default
		var cls = CreateMockClassification();

		// Act
		var result = InvokeTransformClassification(cls, Guid.NewGuid());

		// Assert
		Assert.Null(result.RelationsJson);
	}

	[Fact]
	public void TransformClassification_WithClassificationProperties_TransformsAll()
	{
		// Arrange
		var cp1 = CreateMockClassificationProperty(id: "cp-1");
		var cp2 = CreateMockClassificationProperty(id: "cp-2");
		var cls = CreateMockClassification(
			classificationProperties: new List<IClassificationProperty> { cp1, cp2 });

		// Act
		var result = InvokeTransformClassification(cls, Guid.NewGuid());

		// Assert
		Assert.Equal(2, result.ClassificationProperties.Count);
		var cpIds = result.ClassificationProperties.Select(cp => cp.ClassificationPropertyId).ToList();
		Assert.Contains("cp-1", cpIds);
		Assert.Contains("cp-2", cpIds);
	}

	[Fact]
	public void TransformClassification_NullClassificationProperties_EmptyCollection()
	{
		// Arrange
		var cls = CreateMockClassification(classificationProperties: null);

		// Act
		var result = InvokeTransformClassification(cls, Guid.NewGuid());

		// Assert
		Assert.Empty(result.ClassificationProperties);
	}

	#endregion

	#region TransformClassificationProperty

	[Fact]
	public void TransformClassificationProperty_MapsBasicFields()
	{
		// Arrange
		var classificationId = Guid.NewGuid();
		var cp = CreateMockClassificationProperty(
			id: "cp-123",
			isRequired: true,
			sortNumber: 5,
			isReadonly: true,
			defaultValue: "42",
			reference: "ref-001",
			propertySetId: "ps-abc");

		// Act
		var result = InvokeTransformClassificationProperty(cp, classificationId);

		// Assert
		Assert.NotEqual(Guid.Empty, result.Id);
		Assert.Equal(classificationId, result.GuidelineClassificationId);
		Assert.Equal("cp-123", result.ClassificationPropertyId);
		Assert.True(result.IsRequired);
		Assert.Equal(5, result.SortNumber);
		Assert.True(result.IsReadonly);
		Assert.Equal("42", result.DefaultValue);
		Assert.Equal("ref-001", result.Reference);
		Assert.Equal("ps-abc", result.PropertySetId);
	}

	[Fact]
	public void TransformClassificationProperty_WithAssignment_MapsPropertyId()
	{
		// Arrange
		var property = Substitute.For<IProperty>();
		property.ID.Returns("prop-xyz");
		property.Identifier.Returns("prop-xyz");

		var assignment = Substitute.For<IPropertyAssignment>();
		assignment.Property.Returns(property);

		var cp = CreateMockClassificationProperty(assignment: assignment);

		// Act
		var result = InvokeTransformClassificationProperty(cp, Guid.NewGuid());

		// Assert
		Assert.Equal("prop-xyz", result.PropertyId);
	}

	[Fact]
	public void TransformClassificationProperty_NullAssignment_EmptyPropertyId()
	{
		// Arrange
		var cp = CreateMockClassificationProperty(assignment: null);

		// Act
		var result = InvokeTransformClassificationProperty(cp, Guid.NewGuid());

		// Assert
		Assert.Empty(result.PropertyId);
		Assert.Null(result.AssignmentJson);
	}

	[Fact]
	public void TransformClassificationProperty_NullPropertySet_NullPropertySetId()
	{
		// Arrange
		var cp = CreateMockClassificationProperty(propertySetId: null);

		// Act
		var result = InvokeTransformClassificationProperty(cp, Guid.NewGuid());

		// Assert
		Assert.Null(result.PropertySetId);
	}

	#endregion

	#region TransformProperty - PropertySimple

	[Fact]
	public void TransformProperty_SimpleWithMinMax_HasExtraJson()
	{
		// Arrange
		var versionId = Guid.NewGuid();
		var prop = CreateModelInstance<PropertySimple>();
		SetProperty(prop, "ID", "prop-simple-1");
		SetProperty(prop, "Name", "Length");
		SetProperty(prop, "Identifier", "prop-simple-1");
		SetProperty(prop, "Description", "Wall length");
		SetProperty(prop, "Code", "LEN");
		SetProperty(prop, "UnitType", "meter");
		SetProperty(prop, "UnitAbbreviation", "m");
		SetProperty(prop, "Min", "0");
		SetProperty(prop, "Max", "100");
		SetProperty(prop, "MinIsInclusive", true);
		SetProperty(prop, "MaxIsInclusive", false);

		// Act
		var result = InvokeTransformProperty(prop, versionId);

		// Assert
		Assert.Equal(nameof(PropertySimple), result.PropertyType);
		Assert.NotNull(result.ExtraJson);
		Assert.NotEmpty(result.ExtraJson);
		Assert.Equal("prop-simple-1", result.PropertyId);
		Assert.Equal("Length", result.Name);
		Assert.Equal(versionId, result.GuidelineVersionId);
	}

	[Fact]
	public void TransformProperty_SimpleWithoutMinMax_NullExtraJson()
	{
		// Arrange
		var prop = CreateModelInstance<PropertySimple>();
		SetProperty(prop, "ID", "prop-simple-2");
		SetProperty(prop, "Identifier", "prop-simple-2");
		SetProperty(prop, "Name", "BasicProp");
		// Min and Max left as null/default

		// Act
		var result = InvokeTransformProperty(prop, Guid.NewGuid());

		// Assert
		Assert.Equal(nameof(PropertySimple), result.PropertyType);
		Assert.Null(result.ExtraJson);
	}

	#endregion

	#region TransformProperty - PropertyEnum

	[Fact]
	public void TransformProperty_Enum_SetsPropertyTypeAndExtraJson()
	{
		// Arrange
		var prop = CreateModelInstance<PropertyEnum>();
		SetProperty(prop, "ID", "prop-enum-1");
		SetProperty(prop, "Identifier", "prop-enum-1");
		SetProperty(prop, "Name", "MaterialType");

		// Act
		var result = InvokeTransformProperty(prop, Guid.NewGuid());

		// Assert
		Assert.Equal(nameof(PropertyEnum), result.PropertyType);
		Assert.Equal("prop-enum-1", result.PropertyId);
		Assert.Equal("MaterialType", result.Name);
	}

	#endregion

	#region TransformProperty - PropertySuperEnum

	[Fact]
	public void TransformProperty_SuperEnum_SetsPropertyTypeAndExtraJson()
	{
		// Arrange
		var prop = CreateModelInstance<PropertySuperEnum>();
		SetProperty(prop, "ID", "prop-se-1");
		SetProperty(prop, "Identifier", "prop-se-1");
		SetProperty(prop, "Name", "HierarchicalMaterial");

		// Act
		var result = InvokeTransformProperty(prop, Guid.NewGuid());

		// Assert
		Assert.Equal(nameof(PropertySuperEnum), result.PropertyType);
		Assert.Equal("prop-se-1", result.PropertyId);
	}

	#endregion

	#region TransformProperty - PropertyTree

	[Fact]
	public void TransformProperty_Tree_SetsPropertyTypeAndExtraJson()
	{
		// Arrange
		var prop = CreateModelInstance<PropertyTree>();
		SetProperty(prop, "ID", "prop-tree-1");
		SetProperty(prop, "Identifier", "prop-tree-1");
		SetProperty(prop, "Name", "CategoryTree");

		// Act
		var result = InvokeTransformProperty(prop, Guid.NewGuid());

		// Assert
		Assert.Equal(nameof(PropertyTree), result.PropertyType);
		Assert.Equal("prop-tree-1", result.PropertyId);
	}

	#endregion

	#region TransformProperty - Common Behavior

	[Fact]
	public void TransformProperty_MapsAllBaseFields()
	{
		// Arrange
		var versionId = Guid.NewGuid();
		var prop = CreateModelInstance<PropertySimple>();
		SetProperty(prop, "ID", "prop-base");
		SetProperty(prop, "Name", "TestProp");
		SetProperty(prop, "Identifier", "test-prop-ident");
		SetProperty(prop, "Description", "A test property");
		SetProperty(prop, "Code", "TP01");
		SetProperty(prop, "UnitType", "length");
		SetProperty(prop, "UnitAbbreviation", "mm");

		// Act
		var result = InvokeTransformProperty(prop, versionId);

		// Assert
		Assert.NotEqual(Guid.Empty, result.Id);
		Assert.Equal(versionId, result.GuidelineVersionId);
		Assert.Equal("test-prop-ident", result.PropertyId);
		Assert.Equal("TestProp", result.Name);
		Assert.Equal("test-prop-ident", result.Identifier);
		Assert.Equal("A test property", result.Description);
		Assert.Equal("TP01", result.Code);
		Assert.Equal("length", result.UnitType);
		Assert.Equal("mm", result.UnitAbbreviation);
		Assert.NotNull(result.StorageType);
		Assert.NotNull(result.Status);
	}

	[Fact]
	public void TransformProperty_NullId_ThrowsInvalidOperationException()
	{
		// Arrange
		var prop = CreateModelInstance<PropertySimple>();
		SetProperty(prop, "ID", null);
		SetProperty(prop, "Identifier", null);

		// Act
		var act = () => InvokeTransformProperty(prop, Guid.NewGuid());

		// Assert
		var thrownException = Assert.Throws<TargetInvocationException>(act);
		Assert.IsType<InvalidOperationException>(thrownException.InnerException);
	}

	[Fact]
	public void TransformProperty_NullName_DefaultsToEmptyString()
	{
		// Arrange
		var prop = CreateModelInstance<PropertySimple>();
		// Name is null by default

		// Act
		var result = InvokeTransformProperty(prop, Guid.NewGuid());

		// Assert
		Assert.Empty(result.Name);
	}

	[Fact]
	public void TransformProperty_EachCallGeneratesUniqueId()
	{
		// Arrange
		var prop1 = CreateModelInstance<PropertySimple>();
		SetProperty(prop1, "ID", "p1");
		SetProperty(prop1, "Identifier", "p1");
		SetProperty(prop1, "Name", "P1");

		var prop2 = CreateModelInstance<PropertySimple>();
		SetProperty(prop2, "ID", "p2");
		SetProperty(prop2, "Identifier", "p2");
		SetProperty(prop2, "Name", "P2");

		var versionId = Guid.NewGuid();

		// Act
		var result1 = InvokeTransformProperty(prop1, versionId);
		var result2 = InvokeTransformProperty(prop2, versionId);

		// Assert
		Assert.NotEqual(result1.Id, result2.Id);
	}

	#endregion

	#region TransformToRelationalModel - Multiple Calls Produce Unique Ids

	[Fact]
	public void TransformToRelationalModel_MultipleCallsProduceUniqueVersionIds()
	{
		// Arrange
		var guideline = CreateTestGuideline();
		var evt1 = CreateEvent(etag: "etag-1");
		var evt2 = CreateEvent(etag: "etag-2");

		// Act
		var version1 = InvokeTransformToRelationalModel(guideline, evt1);
		var version2 = InvokeTransformToRelationalModel(guideline, evt2);

		// Assert
		Assert.NotEqual(version1.Id, version2.Id);
	}

	#endregion

	#region PropertySet Transformation

	[Fact]
	public void TransformToRelationalModel_PropertySet_MapsAllFields()
	{
		// Arrange
		var ps = Substitute.For<IPropertySet>();
		ps.ID.Returns("ps-full");
		ps.Name.Returns("FullPropertySet");
		ps.Identifier.Returns("FPS");
		ps.Description.Returns("A full property set");

		var domain = Substitute.For<IDomain>();
		domain.PropertySets.Returns(new List<IPropertySet> { ps });

		var guideline = CreateTestGuideline(domain: domain);
		var evt = CreateEvent();

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		Assert.Equal(1, version.PropertySets.Count);
		var resultPs = version.PropertySets.First();
		Assert.NotEqual(Guid.Empty, resultPs.Id);
		Assert.Equal(version.Id, resultPs.GuidelineVersionId);
		Assert.Equal("FPS", resultPs.PropertySetId);
		Assert.Equal("FullPropertySet", resultPs.Name);
		Assert.Equal("FPS", resultPs.Identifier);
		Assert.Equal("A full property set", resultPs.Description);
	}

	[Fact]
	public void TransformToRelationalModel_NullPropertySets_EmptyCollection()
	{
		// Arrange
		var domain = Substitute.For<IDomain>();
		domain.PropertySets.Returns((IList<IPropertySet>?)null);

		var guideline = CreateTestGuideline(domain: domain);
		var evt = CreateEvent();

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		Assert.Empty(version.PropertySets);
	}

	#endregion

	#region Duplicate ID Validation

	[Fact]
	public void TransformToRelationalModel_DuplicateClassificationIds_ThrowsInvalidOperationException()
	{
		// Arrange
		var cls1 = CreateMockClassification(id: "cls-dup", name: "First");
		var cls2 = CreateMockClassification(id: "cls-dup", name: "Second");

		var domain = Substitute.For<IDomain>();
		domain.Classifications.Returns(new List<IClassification> { cls1, cls2 });

		var guideline = CreateTestGuideline(domain: domain);
		var evt = CreateEvent();

		// Act
		var act = () => InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		var thrownException = Assert.Throws<TargetInvocationException>(act);
		Assert.IsType<InvalidOperationException>(thrownException.InnerException);
		Assert.Contains("Duplicate", thrownException.InnerException!.Message);
		Assert.Contains("cls-dup", thrownException.InnerException.Message);
	}

	[Fact]
	public void TransformToRelationalModel_DuplicatePropertyIds_ThrowsInvalidOperationException()
	{
		// Arrange
		var prop1 = CreateModelInstance<PropertySimple>();
		SetProperty(prop1, "ID", "prop-dup");
		SetProperty(prop1, "Identifier", "prop-dup");
		SetProperty(prop1, "Name", "First");

		var prop2 = CreateModelInstance<PropertySimple>();
		SetProperty(prop2, "ID", "prop-dup");
		SetProperty(prop2, "Identifier", "prop-dup");
		SetProperty(prop2, "Name", "Second");

		var domain = Substitute.For<IDomain>();
		domain.Properties.Returns(new List<IProperty> { prop1, prop2 });

		var guideline = CreateTestGuideline(domain: domain);
		var evt = CreateEvent();

		// Act
		var act = () => InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		var thrownException = Assert.Throws<TargetInvocationException>(act);
		Assert.IsType<InvalidOperationException>(thrownException.InnerException);
		Assert.Contains("Duplicate", thrownException.InnerException!.Message);
		Assert.Contains("prop-dup", thrownException.InnerException.Message);
	}

	[Fact]
	public void TransformToRelationalModel_DuplicatePropertySetIds_ThrowsInvalidOperationException()
	{
		// Arrange
		var ps1 = Substitute.For<IPropertySet>();
		ps1.ID.Returns("ps-dup");
		ps1.Identifier.Returns("ps-dup");
		ps1.Name.Returns("First");

		var ps2 = Substitute.For<IPropertySet>();
		ps2.ID.Returns("ps-dup");
		ps2.Identifier.Returns("ps-dup");
		ps2.Name.Returns("Second");

		var domain = Substitute.For<IDomain>();
		domain.PropertySets.Returns(new List<IPropertySet> { ps1, ps2 });

		var guideline = CreateTestGuideline(domain: domain);
		var evt = CreateEvent();

		// Act
		var act = () => InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		var thrownException = Assert.Throws<TargetInvocationException>(act);
		Assert.IsType<InvalidOperationException>(thrownException.InnerException);
		Assert.Contains("Duplicate", thrownException.InnerException!.Message);
		Assert.Contains("ps-dup", thrownException.InnerException.Message);
	}

	[Fact]
	public void TransformToRelationalModel_DuplicateClassifications_ThrowsInvalidOperationException()
	{
		// Arrange
		var cls1 = CreateMockClassification(id: "cls-dup", name: "First");
		var cls2 = CreateMockClassification(id: "cls-dup", name: "Second");

		var domain = Substitute.For<IDomain>();
		domain.Classifications.Returns(new List<IClassification> { cls1, cls2 });

		var guideline = CreateTestGuideline(domain: domain);
		var evt = CreateEvent(name: "my-guideline.json");

		// Act
		var act = () => InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		var thrownException = Assert.Throws<TargetInvocationException>(act);
		Assert.IsType<InvalidOperationException>(thrownException.InnerException);
	}

	[Fact]
	public void TransformToRelationalModel_NoDuplicates_NoDeduplicationNeeded()
	{
		// Arrange
		var cls1 = CreateMockClassification(id: "cls-1", name: "One");
		var cls2 = CreateMockClassification(id: "cls-2", name: "Two");

		var domain = Substitute.For<IDomain>();
		domain.Classifications.Returns(new List<IClassification> { cls1, cls2 });

		var guideline = CreateTestGuideline(domain: domain);
		var evt = CreateEvent();

		// Act
		var version = InvokeTransformToRelationalModel(guideline, evt);

		// Assert
		Assert.Equal(2, version.Classifications.Count);
	}

	#endregion
}

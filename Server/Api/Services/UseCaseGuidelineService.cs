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
using AccessService.Api.Serialization;
using AccessService.Domain.Models;
using AccessService.Domain.Repositories;
using AccessService.Events.UseCaseGuidelines;
using Guideline.Model.Enums;
using GuidelineModelIO;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using System.Text;
using System.Text.Json;

using GuidelineModel = Guideline.Model.Model;

namespace AccessService.Api.Services;

/// <summary>
/// Generates UseCase-specific Guidelines from the relational guideline projection stored in the database,
/// filtered by AccessRights for the given UseCase. Uploads the result to S3 and publishes a success event.
/// The generated UseCase-Guideline follows the Guideline.Model format exactly.
/// </summary>
public class UseCaseGuidelineService : IUseCaseGuidelineService
{
	private readonly IMinioClient _minioClient;
	private readonly IGuidelineProjectionRepository _guidelineRepo;
	private readonly IAccessRightsRepository _accessRightsRepo;
	private readonly IOutboxRepository _outboxRepo;
	private readonly ILogger<UseCaseGuidelineService> _logger;
	private readonly KafkaTopicsOptions _kafkaTopics;
	private readonly UseCaseGuidelineOptions _useCaseGuidelineOptions;

	public UseCaseGuidelineService(
		IMinioClient minioClient,
		IGuidelineProjectionRepository guidelineRepo,
		IAccessRightsRepository accessRightsRepo,
		IOutboxRepository outboxRepo,
		IOptions<KafkaOptions> kafkaOptions,
		IOptions<UseCaseGuidelineOptions> useCaseGuidelineOptions,
		ILogger<UseCaseGuidelineService> logger)
	{
		_minioClient = minioClient;
		_guidelineRepo = guidelineRepo;
		_accessRightsRepo = accessRightsRepo;
		_outboxRepo = outboxRepo;
		_logger = logger;
		_kafkaTopics = kafkaOptions.Value.Topics;
		_useCaseGuidelineOptions = useCaseGuidelineOptions.Value;
	}

	/// <inheritdoc/>
	public async Task GenerateForUserGroupAsync(Guid useCaseId, Guid userGroupId, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Generating UseCase-Guideline for UseCaseId={UseCaseId}, UserGroupId={UserGroupId}", useCaseId, userGroupId);

		var accessRights = (await _accessRightsRepo.GetAccessRightsByUseCaseUserGroupAsync(useCaseId.ToString(), userGroupId.ToString())).ToList();
		if (accessRights.Count == 0)
		{
			_logger.LogInformation("No access rights found for UseCaseId={UseCaseId}, UserGroupId={UserGroupId}. Skipping generation.", useCaseId, userGroupId);
			return;
		}

		var activeVersionIds = await _guidelineRepo.GetActiveVersionIdsAsync(cancellationToken);
		if (activeVersionIds.Count == 0)
		{
			_logger.LogWarning("No active guideline versions found. Skipping UseCase-Guideline generation for UseCaseId={UseCaseId}.", useCaseId);
			return;
		}

		var partialModels = new List<GuidelineModel.Guideline>();
		foreach (var versionId in activeVersionIds)
		{
			var partial = await BuildFilteredModelForVersionAsync(useCaseId, versionId, accessRights, cancellationToken);
			if (partial != null)
				partialModels.Add(partial);
		}

		if (partialModels.Count == 0)
		{
			_logger.LogInformation(
				"No matching classifications across any guideline version for UseCaseId={UseCaseId}, UserGroupId={UserGroupId}. Skipping upload.",
				useCaseId, userGroupId);
			return;
		}

		var merged = MergeGuidelineModels(partialModels);

		var guidelineReaderWriter = new GuidelineReaderWriter();
		var json = guidelineReaderWriter.GetGuidelineAsString(merged);

		var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
		var objectName = $"{useCaseId}/{userGroupId}-{timestamp}.guideline";
		var etag = await UploadGuidelineJsonAsync(_useCaseGuidelineOptions.BucketName, objectName, json, cancellationToken);

		var correlationId = Guid.NewGuid();
		var evt = new UploadedUseCaseGuideline
		{
			UseCaseId = useCaseId,
			UserGroupId = userGroupId,
			Name = objectName,
			Etag = etag,
			BucketName = _useCaseGuidelineOptions.BucketName,
			CorrelationId = correlationId,
			Timestamp = DateTimeOffset.UtcNow
		};

		_outboxRepo.Add(evt, _kafkaTopics.UseCaseGuidelines, useCaseId.ToString());
		await _outboxRepo.SaveChangesAsync(cancellationToken);

		_logger.LogInformation(
			"Successfully generated and uploaded UseCase-Guideline: UseCaseId={UseCaseId}, UserGroupId={UserGroupId}, ObjectName={ObjectName}, Etag={Etag}, CorrelationId={CorrelationId}",
			useCaseId, userGroupId, objectName, etag, correlationId);
	}

	/// <inheritdoc/>
	public async Task GenerateForAllUseCasesAsync(CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Generating UseCase-Guidelines for all (useCaseId, userGroupId) pairs with access rights.");

		var pairs = await _accessRightsRepo.GetDistinctUseCaseUserGroupPairsAsync();

		_logger.LogInformation("Found {Count} distinct (useCaseId, userGroupId) pairs with access rights.", pairs.Count);

		foreach (var (useCaseId, userGroupId) in pairs)
		{
			try
			{
				await GenerateForUserGroupAsync(useCaseId, userGroupId, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to generate UseCase-Guideline for UseCaseId={UseCaseId}, UserGroupId={UserGroupId}. Continuing with next.", useCaseId, userGroupId);
			}
		}
	}

	private async Task<GuidelineModel.Guideline?> BuildFilteredModelForVersionAsync(Guid useCaseId, Guid versionId, List<AccessRight> accessRights, CancellationToken ct)
	{
		var version = await _guidelineRepo.GetVersionByIdAsync(versionId, ct);
		if (version is null)
		{
			_logger.LogWarning("GuidelineVersion {VersionId} not found. Skipping.", versionId);
			return null;
		}

		_logger.LogInformation(
			"Building filtered model from DB projection: ObjectName={ObjectName}, Etag={Etag}, UseCaseId={UseCaseId}",
			version.ObjectName, version.Etag, useCaseId);

		var arClassIds = accessRights.Select(ar => ar.GuidelineClassificationId).Distinct().ToList();
		var arCpIds = new HashSet<string>(accessRights.Select(ar => ar.GuidlineClassificationPropertyId));

		var classifications = await _guidelineRepo.GetClassificationsWithPropertiesAsync(versionId, arClassIds, ct);
		if (classifications.Count == 0)
		{
			_logger.LogInformation(
				"No matching classifications in DB for UseCaseId={UseCaseId}, VersionId={VersionId}. Skipping.",
				useCaseId, versionId);
			return null;
		}

		var neededPropertyIds = new HashSet<string>();
		var neededPropertySetIds = new HashSet<string>();
		foreach (var cls in classifications)
		{
			var filteredCps = cls.ClassificationProperties
				.Where(cp => arCpIds.Contains(cp.PropertyId))
				.ToList();

			cls.ClassificationProperties.Clear();
			foreach (var cp in filteredCps)
			{
				cls.ClassificationProperties.Add(cp);
				neededPropertyIds.Add(cp.PropertyId);
				if (cp.PropertySetId != null)
					neededPropertySetIds.Add(cp.PropertySetId);
			}
		}

		classifications = classifications.Where(c => c.ClassificationProperties.Count > 0).ToList();
		if (classifications.Count == 0)
		{
			_logger.LogInformation(
				"No classification properties matched after filtering for UseCaseId={UseCaseId}, VersionId={VersionId}. Skipping.",
				useCaseId, versionId);
			return null;
		}

		var properties = await _guidelineRepo.GetPropertiesByIdsAsync(versionId, neededPropertyIds, ct);
		var propertySets = neededPropertySetIds.Count > 0
			? await _guidelineRepo.GetPropertySetsByIdsAsync(versionId, neededPropertySetIds, ct)
			: new Dictionary<string, GuidelinePropertySet>();

		return BuildGuidelineModel(version, classifications, properties, propertySets);
	}

	private static GuidelineModel.Guideline MergeGuidelineModels(IReadOnlyList<GuidelineModel.Guideline> parts)
	{
		if (parts.Count == 1)
			return parts[0];

		var allClassifications = parts
			.SelectMany(g => g.Domain?.Classifications ?? Enumerable.Empty<GuidelineModel.IClassification>())
			.ToList();

		var mergedProperties = new Dictionary<string, GuidelineModel.IProperty>();
		foreach (var prop in parts.SelectMany(g => g.Domain?.Properties ?? Enumerable.Empty<GuidelineModel.IProperty>()))
		{
			if (prop.Identifier != null)
				mergedProperties[prop.Identifier] = prop;
		}

		var mergedPropertySets = new Dictionary<string, GuidelineModel.IPropertySet>();
		foreach (var ps in parts.SelectMany(g => g.Domain?.PropertySets ?? Enumerable.Empty<GuidelineModel.IPropertySet>()))
		{
			if (ps.Identifier != null)
				mergedPropertySets[ps.Identifier] = ps;
		}

		return new GuidelineModel.Guideline
		{
			Domain = new GuidelineModel.Domain
			{
				Classifications = allClassifications,
				Properties = mergedProperties.Values.Cast<GuidelineModel.IProperty>().ToList(),
				PropertySets = mergedPropertySets.Values.Cast<GuidelineModel.IPropertySet>().ToList()
			}
		};
	}

	/// <summary>
	/// Reconstructs a Guideline.Model.Guideline from the relational DB projection data.
	/// </summary>
	private static GuidelineModel.Guideline BuildGuidelineModel(
		GuidelineVersion version,
		List<GuidelineClassification> classifications,
		Dictionary<string, GuidelineProperty> properties,
		Dictionary<string, GuidelinePropertySet> propertySets)
	{
		var domainMeta = DeserializeDomainMeta(version.DomainJson);

		var modelProperties = properties.Values
			.Select(ReconstructProperty)
			.Cast<GuidelineModel.IProperty>()
			.ToList();

		var modelPropertySets = propertySets.Values
			.Select(ReconstructPropertySet)
			.Cast<GuidelineModel.IPropertySet>()
			.ToList();

		var modelClassifications = classifications
			.Select(c => ReconstructClassification(c, properties, propertySets))
			.Cast<GuidelineModel.IClassification>()
			.ToList();

		return new GuidelineModel.Guideline
		{
			Identifier = version.GuidelineId,
			Name = version.Name,
			Description = version.Description,
			Version = version.Version,
			Status = ParseStatus(domainMeta?.Status),
			Mappings = DeserializeJson<List<GuidelineModel.IMapping>>(version.MappingsJson),
			ComplexData = DeserializeJson<GuidelineModel.ComplexData>(version.ComplexDataJson),
			Domain = new GuidelineModel.Domain
			{
				ID = domainMeta?.ID,
				Name = domainMeta?.Name,
				Identifier = domainMeta?.Identifier,
				Description = domainMeta?.Description,
				Version = domainMeta?.Version,
				Status = ParseStatus(domainMeta?.Status),
				Classifications = modelClassifications,
				Properties = modelProperties,
				PropertySets = modelPropertySets
			}
		};
	}

	private static GuidelineModel.Classification ReconstructClassification(
		GuidelineClassification gc,
		Dictionary<string, GuidelineProperty> properties,
		Dictionary<string, GuidelinePropertySet> propertySets)
	{
		var cps = gc.ClassificationProperties
			.Select(cp => ReconstructClassificationProperty(cp, properties, propertySets))
			.Cast<GuidelineModel.IClassificationProperty>()
			.ToList();

		return new GuidelineModel.Classification
		{
			Identifier = gc.ClassificationId,
			Name = gc.Name,
			Code = gc.Code,
			Description = gc.Description,
			Status = ParseStatus(gc.Status),
			ClassificationProperties = cps,
			Parent = null,
			Children = null
		};
	}

	private static GuidelineModel.ClassificationProperty ReconstructClassificationProperty(
		GuidelineClassificationProperty cp,
		Dictionary<string, GuidelineProperty> properties,
		Dictionary<string, GuidelinePropertySet> propertySets)
	{
		GuidelineModel.IPropertyAssignment? assignment = null;
		GuidelineModel.IProperty? prop = null;

		if (properties.TryGetValue(cp.PropertyId, out var gp))
		{
			prop = ReconstructProperty(gp);
		}

		// Reconstruct assignment from AssignmentJson
		if (cp.AssignmentJson != null)
		{
			assignment = ReconstructAssignment(cp.AssignmentJson, prop);
		}
		else if (prop != null)
		{
			// Default assignment referencing the property
			assignment = new GuidelineModel.PropertyAssignment { Property = prop };
		}

		GuidelineModel.IPropertySet? propertySet = null;
		if (cp.PropertySetId != null && propertySets.TryGetValue(cp.PropertySetId, out var gps))
		{
			propertySet = ReconstructPropertySet(gps);
		}

		return new GuidelineModel.ClassificationProperty
		{
			Identifier = cp.ClassificationPropertyId,
			IsRequired = cp.IsRequired,
			SortNumber = cp.SortNumber,
			IsReadonly = cp.IsReadonly,
			DefaultValue = cp.DefaultValue,
			Reference = cp.Reference,
			PropertyAssignment = assignment,
			PropertySet = propertySet
		};
	}

	private static GuidelineModel.IProperty ReconstructProperty(GuidelineProperty gp)
	{
		var storageType = Enum.TryParse<StorageType>(gp.StorageType, out var st) ? st : StorageType.String;
		var status = ParseStatus(gp.Status);

		switch (gp.PropertyType)
		{
			case nameof(GuidelineModel.PropertyEnum):
				{
					var pe = new GuidelineModel.PropertyEnum
					{
						Identifier = gp.PropertyId,
						Name = gp.Name,
						Description = gp.Description,
						StorageType = storageType,
						Code = gp.Code,
						UnitType = gp.UnitType,
						UnitAbbreviation = gp.UnitAbbreviation,
						Status = status,
						Enums = DeserializeJson<List<GuidelineModel.PropertyEnumItem>>(gp.ExtraJson)
					};
					return pe;
				}
			case nameof(GuidelineModel.PropertySimple):
				{
					var extra = GuidelineJson.Deserialize<RangeExtra>(gp.ExtraJson);
					var ps = new GuidelineModel.PropertySimple
					{
						Identifier = gp.PropertyId,
						Name = gp.Name,
						Description = gp.Description,
						StorageType = storageType,
						Code = gp.Code,
						UnitType = gp.UnitType,
						UnitAbbreviation = gp.UnitAbbreviation,
						Status = status,
						Min = extra?.Min,
						MinIsInclusive = extra?.MinIsInclusive ?? false,
						Max = extra?.Max,
						MaxIsInclusive = extra?.MaxIsInclusive ?? false
					};
					return ps;
				}
			case nameof(GuidelineModel.PropertySuperEnum):
				{
					var extra = GuidelineJson.Deserialize<SuperEnumExtra>(gp.ExtraJson);
					var pse = new GuidelineModel.PropertySuperEnum
					{
						Identifier = gp.PropertyId,
						Name = gp.Name,
						Description = gp.Description,
						StorageType = storageType,
						Code = gp.Code,
						UnitType = gp.UnitType,
						UnitAbbreviation = gp.UnitAbbreviation,
						Status = status,
						Level = extra?.Level ?? 0,
						Item = extra?.Item
					};
					return pse;
				}
			case nameof(GuidelineModel.PropertyTree):
				{
					var item = DeserializeJson<GuidelineModel.ComplexDataItem>(gp.ExtraJson);
					var pt = new GuidelineModel.PropertyTree
					{
						Identifier = gp.PropertyId,
						Name = gp.Name,
						Description = gp.Description,
						StorageType = storageType,
						Code = gp.Code,
						UnitType = gp.UnitType,
						UnitAbbreviation = gp.UnitAbbreviation,
						Status = status,
						Item = item
					};
					return pt;
				}
			default:
				{
					return new GuidelineModel.PropertySimple
					{
						Identifier = gp.PropertyId,
						Name = gp.Name,
						Description = gp.Description,
						StorageType = storageType,
						Code = gp.Code,
						UnitType = gp.UnitType,
						UnitAbbreviation = gp.UnitAbbreviation,
						Status = status
					};
				}
		}
	}

	private static GuidelineModel.PropertySet ReconstructPropertySet(GuidelinePropertySet gps)
	{
		return new GuidelineModel.PropertySet
		{
			Identifier = gps.PropertySetId,
			Name = gps.Name,
			Description = gps.Description,
			Status = ParseStatus(gps.Status)
		};
	}

	private static GuidelineModel.IPropertyAssignment? ReconstructAssignment(string assignmentJson, GuidelineModel.IProperty? prop)
	{
		// AssignmentJson is a flat blob written by the transformation with an explicit "Type"
		// discriminator, so it is read as a document rather than deserialized into a model type.
		using var document = JsonDocument.Parse(assignmentJson);
		var root = document.RootElement;
		if (root.ValueKind != JsonValueKind.Object)
			return null;

		var type = root.TryGetProperty("Type", out var typeElement) ? typeElement.GetString() : null;

		return type switch
		{
			nameof(GuidelineModel.PropertyEnumAssignment) => new GuidelineModel.PropertyEnumAssignment
			{
				Property = prop,
				FreeTextEnabled = GetBoolean(root, "FreeTextEnabled"),
				SelectedEnum = root.TryGetProperty("SelectedEnum", out var se) && se.ValueKind == JsonValueKind.Object
					? se.Deserialize<GuidelineModel.PropertyEnumItem>(GuidelineJson.Options)
					: null
			},
			nameof(GuidelineModel.PropertySimpleAssignment) => new GuidelineModel.PropertySimpleAssignment
			{
				Property = prop,
				Min = GetString(root, "Min"),
				MinIsInclusive = GetBoolean(root, "MinIsInclusive"),
				Max = GetString(root, "Max"),
				MaxIsInclusive = GetBoolean(root, "MaxIsInclusive")
			},
			nameof(GuidelineModel.PropertySuperEnumAssignment) => new GuidelineModel.PropertySuperEnumAssignment
			{
				Property = prop
			},
			_ => new GuidelineModel.PropertyAssignment { Property = prop }
		};
	}

	private static string? GetString(JsonElement element, string propertyName)
	{
		return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
			? value.GetString()
			: null;
	}

	private static bool GetBoolean(JsonElement element, string propertyName)
	{
		return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.True;
	}

	private static Status ParseStatus(string? statusStr)
	{
		return Enum.TryParse<Status>(statusStr, out var status) ? status : Status.Active;
	}

	private static T? DeserializeJson<T>(string? json) where T : class
	{
		return GuidelineJson.Deserialize<T>(json);
	}

	private static DomainMeta? DeserializeDomainMeta(string? json)
	{
		return GuidelineJson.Deserialize<DomainMeta>(json);
	}

	private async Task<string> UploadGuidelineJsonAsync(string bucketName, string objectName, string json, CancellationToken ct)
	{
		bool exists = await _minioClient.BucketExistsAsync(
			new BucketExistsArgs().WithBucket(bucketName), ct);
		if (!exists)
		{
			await _minioClient.MakeBucketAsync(
				new MakeBucketArgs().WithBucket(bucketName), ct);
		}

		var bytes = Encoding.UTF8.GetBytes(json);
		using var ms = new MemoryStream(bytes);

		var response = await _minioClient.PutObjectAsync(new PutObjectArgs()
			.WithBucket(bucketName)
			.WithObject(objectName)
			.WithStreamData(ms)
			.WithObjectSize(bytes.Length)
			.WithContentType("application/json"), ct);

		return response.Etag;
	}

	/// <summary>
	/// Internal DTO for the ExtraJson blob of a <see cref="GuidelineModel.PropertySimple"/>.
	/// Plain settable properties, no constructor — reference preservation does not support
	/// parameterized constructors.
	/// </summary>
	private sealed class RangeExtra
	{
		public string? Min
		{
			get; set;
		}
		public bool MinIsInclusive
		{
			get; set;
		}
		public string? Max
		{
			get; set;
		}
		public bool MaxIsInclusive
		{
			get; set;
		}
	}

	/// <summary>
	/// Internal DTO for the ExtraJson blob of a <see cref="GuidelineModel.PropertySuperEnum"/>.
	/// </summary>
	private sealed class SuperEnumExtra
	{
		public int Level
		{
			get; set;
		}
		public GuidelineModel.ComplexDataItem? Item
		{
			get; set;
		}
	}

	/// <summary>
	/// Internal DTO for deserializing the DomainJson metadata blob.
	/// </summary>
	private sealed class DomainMeta
	{
		public string? ID
		{
			get; set;
		}
		public string? Name
		{
			get; set;
		}
		public string? Identifier
		{
			get; set;
		}
		public string? Description
		{
			get; set;
		}
		public string? Status
		{
			get; set;
		}
		public string? Version
		{
			get; set;
		}
	}
}

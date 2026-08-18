// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessService.Domain.Models;

/// <summary>
/// Represents a persisted guideline version. Acts as the root aggregate for the relational projection.
/// Business-relevant fields are relational columns; mappings and complex data are stored as JSON blobs.
/// </summary>
[Table("guideline_version")]
public class GuidelineVersion
{
    [Key]
    [Column("id")]
    public Guid Id
    {
        get; set;
    }

    /// <summary>
    /// Stable external identity assigned by the GuidelineService (its database ID).
    /// Used as the upsert key so re-uploads of the same guideline update the existing projection
    /// rather than creating a new row.
    /// </summary>
    [MaxLength(500)]
    [Column("service_id")]
    public string? ServiceId { get; set; }

    /// <summary>Original ID from the guideline model.</summary>
    [Required]
    [MaxLength(500)]
    [Column("guideline_id")]
    public string GuidelineId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("identifier")]
    public string? Identifier
    {
        get; set;
    }

    [MaxLength(2000)]
    [Column("description")]
    public string? Description
    {
        get; set;
    }

    [MaxLength(100)]
    [Column("version")]
    public string? Version
    {
        get; set;
    }

    /// <summary>Object name (key) in MinIO storage.</summary>
    [Required]
    [MaxLength(500)]
    [Column("object_name")]
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>Storage bucket name.</summary>
    [Required]
    [MaxLength(200)]
    [Column("bucket_name")]
    public string BucketName { get; set; } = string.Empty;

    /// <summary>ETag (version identifier) of the uploaded object for idempotency.</summary>
    [Required]
    [MaxLength(200)]
    [Column("etag")]
    public string Etag { get; set; } = string.Empty;

    /// <summary>Correlation ID for end-to-end tracing.</summary>
    [Required]
    [Column("correlation_id")]
    public Guid CorrelationId
    {
        get; set;
    }

    /// <summary>UTC timestamp when the upload completed.</summary>
    [Required]
    [Column("event_timestamp")]
    public DateTimeOffset EventTimestamp
    {
        get; set;
    }

    /// <summary>UTC timestamp when this projection was processed.</summary>
    [Required]
    [Column("processed_at")]
    public DateTimeOffset ProcessedAt
    {
        get; set;
    }

    /// <summary>Mappings JSON blob — not needed for business logic but needed to reconstruct the full guideline.</summary>
    [Column("mappings_json")]
    public string? MappingsJson
    {
        get; set;
    }

    /// <summary>ComplexData JSON blob — not needed for business logic but needed to reconstruct the full guideline.</summary>
    [Column("complex_data_json")]
    public string? ComplexDataJson
    {
        get; set;
    }

    /// <summary>Domain-level metadata JSON (ID, Name, Identifier, Description, etc.).</summary>
    [Column("domain_json")]
    public string? DomainJson
    {
        get; set;
    }

    // Navigation properties
    public ICollection<GuidelineClassification> Classifications { get; set; } = new List<GuidelineClassification>();
    public ICollection<GuidelinePropertySet> PropertySets { get; set; } = new List<GuidelinePropertySet>();
    public ICollection<GuidelineProperty> Properties { get; set; } = new List<GuidelineProperty>();
}

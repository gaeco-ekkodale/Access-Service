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
/// Represents a classification within a guideline version.
/// Business fields are proper columns; parent/children relations are stored as JSON.
/// </summary>
[Table("guideline_classification")]
public class GuidelineClassification
{
    [Key]
    [Column("id")]
    public Guid Id
    {
        get; set;
    }

    [Required]
    [Column("guideline_version_id")]
    public Guid GuidelineVersionId
    {
        get; set;
    }

    /// <summary>Original classification ID from the guideline model.</summary>
    [Required]
    [MaxLength(500)]
    [Column("classification_id")]
    public string ClassificationId { get; set; } = string.Empty;

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

    [MaxLength(200)]
    [Column("code")]
    public string? Code
    {
        get; set;
    }

    [MaxLength(2000)]
    [Column("description")]
    public string? Description
    {
        get; set;
    }

    [MaxLength(50)]
    [Column("status")]
    public string? Status
    {
        get; set;
    }

    [NotMapped]
    public int PropertyCount
    {
        get; set;
    }

    /// <summary>Parent/children relation JSON — not needed for business logic, preserved for reconstruction.</summary>
    [Column("relations_json")]
    public string? RelationsJson
    {
        get; set;
    }

    // Navigation properties
    [ForeignKey(nameof(GuidelineVersionId))]
    public GuidelineVersion GuidelineVersion { get; set; } = null!;

    public ICollection<GuidelineClassificationProperty> ClassificationProperties { get; set; } = new List<GuidelineClassificationProperty>();
}

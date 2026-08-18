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
/// Join entity representing a classification's property assignment.
/// Business-relevant fields (IsRequired, SortNumber, IsReadonly, DefaultValue) are proper columns.
/// Assignment-specific data (PropertyAssignment details) is stored as JSON.
/// </summary>
[Table("guideline_classification_property")]
public class GuidelineClassificationProperty
{
    [Key]
    [Column("id")]
    public Guid Id
    {
        get; set;
    }

    [Required]
    [Column("guideline_classification_id")]
    public Guid GuidelineClassificationId
    {
        get; set;
    }

    /// <summary>Original classification property ID from the guideline model.</summary>
    [Required]
    [MaxLength(500)]
    [Column("classification_property_id")]
    public string ClassificationPropertyId { get; set; } = string.Empty;

    /// <summary>References the property definition by its original guideline ID.</summary>
    [Required]
    [MaxLength(500)]
    [Column("property_id")]
    public string PropertyId { get; set; } = string.Empty;

    /// <summary>References the property set by its original guideline ID (nullable).</summary>
    [MaxLength(500)]
    [Column("property_set_id")]
    public string? PropertySetId
    {
        get; set;
    }

    [Required]
    [Column("is_required")]
    public bool IsRequired
    {
        get; set;
    }

    [Required]
    [Column("sort_number")]
    public int SortNumber
    {
        get; set;
    }

    [Required]
    [Column("is_readonly")]
    public bool IsReadonly
    {
        get; set;
    }

    [MaxLength(1000)]
    [Column("default_value")]
    public string? DefaultValue
    {
        get; set;
    }

    [MaxLength(500)]
    [Column("reference")]
    public string? Reference
    {
        get; set;
    }

    /// <summary>PropertyAssignment-specific data as JSON (e.g. enum selection, ranges overrides).</summary>
    [Column("assignment_json")]
    public string? AssignmentJson
    {
        get; set;
    }

    // Navigation
    [ForeignKey(nameof(GuidelineClassificationId))]
    public GuidelineClassification GuidelineClassification { get; set; } = null!;
}

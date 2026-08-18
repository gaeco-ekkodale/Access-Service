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
/// Represents a property definition within a guideline version.
/// Business-relevant fields (StorageType, Code, UnitType) are proper columns.
/// Additional property details (enum values, ranges, tree data) are stored as JSON.
/// </summary>
[Table("guideline_property")]
public class GuidelineProperty
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

    /// <summary>Original property ID from the guideline model.</summary>
    [Required]
    [MaxLength(500)]
    [Column("property_id")]
    public string PropertyId { get; set; } = string.Empty;

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

    [MaxLength(50)]
    [Column("storage_type")]
    public string StorageType { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("code")]
    public string? Code
    {
        get; set;
    }

    [MaxLength(200)]
    [Column("unit_type")]
    public string? UnitType
    {
        get; set;
    }

    [MaxLength(50)]
    [Column("unit_abbreviation")]
    public string? UnitAbbreviation
    {
        get; set;
    }

    [MaxLength(50)]
    [Column("status")]
    public string? Status
    {
        get; set;
    }

    /// <summary>
    /// Discriminator for the property type (PropertySimple, PropertyEnum, PropertyTree, PropertySuperEnum).
    /// </summary>
    [MaxLength(100)]
    [Column("property_type")]
    public string? PropertyType
    {
        get; set;
    }

    /// <summary>Type-specific extra data (enum items, ranges, tree structure) as JSON.</summary>
    [Column("extra_json")]
    public string? ExtraJson
    {
        get; set;
    }

    // Navigation
    [ForeignKey(nameof(GuidelineVersionId))]
    public GuidelineVersion GuidelineVersion { get; set; } = null!;
}

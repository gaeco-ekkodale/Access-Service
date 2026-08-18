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
/// Represents a property set within a guideline version.
/// </summary>
[Table("guideline_property_set")]
public class GuidelinePropertySet
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

    /// <summary>Original property set ID from the guideline model.</summary>
    [Required]
    [MaxLength(500)]
    [Column("property_set_id")]
    public string PropertySetId { get; set; } = string.Empty;

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
    [Column("status")]
    public string? Status
    {
        get; set;
    }

    // Navigation
    [ForeignKey(nameof(GuidelineVersionId))]
    public GuidelineVersion GuidelineVersion { get; set; } = null!;
}

// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Domain.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessService.Domain.Models;

/// <summary>
/// Represents the database table definition for an Access Right.
/// </summary>
[Table("accessright")]
public class AccessRight
{
    /// <summary>
    /// Gets or sets the ID of the AccessRight.
    /// </summary>
    [Required]
    [MaxLength(40)]
    [Column("id")]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the guideline-classification-property.
    /// </summary>
    [Required]
    [MaxLength(150)]
    [Column("name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the ID of the GuidelineClassification.
    /// </summary>
    [Required]
    [MaxLength(300)]
    [Column("guideline_classification_id")]
    public string GuidelineClassificationId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the Usergroup the right belongs to.
    /// </summary>
    [Required]
    [MaxLength(40)]
    [Column("usergroup_id")]
    public Guid UserGroupId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the Use Case the Access right belongs to.
    /// </summary>
    [Required]
    [MaxLength(40)]
    [Column("usecase_id")]
    public Guid UseCaseId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the Guideline Classification Property.
    /// </summary>
    [Required]
    [MaxLength(300)]
    [Column("guidline_classification_property_id")]
    public string GuidlineClassificationPropertyId { get; set; }

    /// <summary>
    /// Gets or sets the right of the access right.
    /// </summary>
    [Required]
    [MaxLength(40)]
    [Column("right")]
    public PropertyRight Right { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessRight"/> class.
    /// </summary>
    /// <param name="id">The ID of the access right.</param>
    /// <param name="name">The name of the guideline-classification-property.</param>
    /// <param name="guidelineClassificationId">The ID of the GuidelineClassification.</param>
    /// <param name="userGroupId">The ID of the Usergroup the right belongs to.</param>
    /// <param name="useCaseId">The ID of the Use Case the Access right belongs to.</param>
    /// <param name="guidlineClassificationPropertyId">The ID of the Guideline Classification Property.</param>
    /// <param name="right">The property right.</param>
    public AccessRight(string id, string name, string guidelineClassificationId, Guid userGroupId, Guid useCaseId, string guidlineClassificationPropertyId, PropertyRight right)
    {
        Id = id;
        Name = name;
        GuidelineClassificationId = guidelineClassificationId;
        UserGroupId = userGroupId;
        UseCaseId = useCaseId;
        GuidlineClassificationPropertyId = guidlineClassificationPropertyId;
        Right = right;
    }
}

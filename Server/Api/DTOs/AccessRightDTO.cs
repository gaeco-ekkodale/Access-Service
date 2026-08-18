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

namespace AccessService.Api.DTOs;

/// <summary>
/// Represents the Data Transfer Object (DTO) for an access right.
/// </summary>
public class AccessRightDTO
{
    /// <summary>
    /// Gets or sets the ID of the access right.
    /// </summary>
    [Required]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the access right.
    /// </summary>
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the ID of the guideline classification.
    /// </summary>
    [Required]
    public string GuidelineClassificationId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user group to which the right belongs.
    /// </summary>
    [Required]
    public Guid UserGroupId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the use case to which the access right belongs.
    /// </summary>
    [Required]
    public Guid UseCaseId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the guideline classification property.
    /// </summary>
    [Required]
    public string GuidlineClassificationPropertyId { get; set; }

    /// <summary>
    /// Gets or sets the specific property right.
    /// </summary>
    [Required]
    public PropertyRight Right { get; set; }
}

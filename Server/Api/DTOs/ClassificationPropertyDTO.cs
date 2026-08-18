// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Guideline.Model.Enums;

namespace AccessService.Api.DTOs;

/// <summary>
/// Represents the Data Transfer Object (DTO) for a property of a classification instance item.
/// </summary>
public class ClassificationPropertyDTO
{
    /// <summary>
    /// Gets or sets the ID of the property.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the storage type of the property.
    /// </summary>
    public StorageType StorageType { get; set; }

    /// <summary>
    /// Gets or sets the name of the property set.
    /// </summary>
    public string PropertySetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the property set.
    /// </summary>
    public string PropertySetId { get; set; } = string.Empty;
}

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
using Guideline.Model.Enums;

namespace AccessService.Domain.Models;

/// <summary>
/// Represents a Property definition.
/// </summary>
public class Property
{
    /// <summary>
    /// The id of the property.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the property.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The value of the property.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// The storage type of the property.
    public StorageType StorageType { get; set; }

    /// <summary>
    /// The access right of the property.
    /// </summary>
    public PropertyRight Right { get; set; } = PropertyRight.None;

}

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

namespace AccessService.Domain.Models;

/// <summary>
/// Represents a Property Set definition.
/// </summary>
public class PropertySet
{
    /// <summary>
    /// The unique identifier of the property set.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the property set.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A list of classification properties associated with this property set.
    /// </summary>
    public List<Property> Properties { get; set; } = new List<Property>();

    /// <summary>
    /// The access right of the property set.
    /// </summary>
    public PropertySetRight Right { get; set; } = PropertySetRight.None;
}

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
/// Represents a Classification definition.
/// </summary>
public class Classification
{
    /// <summary>
    /// The unique identifier of the classification.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the classification.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The right of the classification.
    /// </summary>
    public ClassificationRight Right { get; set; } = ClassificationRight.None;

    /// <summary>
    /// A list of property sets associated with this classification.
    /// </summary>
    public List<PropertySet> PropertySets { get; set; } = new List<PropertySet>();
}

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

namespace AccessService.Domain.Models;
/// <summary>
/// Represents a classification inside of an list.
/// </summary>
public class ClassificationList
{
    /// <summary>
    /// The Id of the classification.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the classification.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The short code of the classification (e.g. "Ifc4x3:BUILDING").
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// The name of the guideline this classification belongs to.
    /// </summary>
    public string? GuidelineName { get; set; }

    /// <summary>
    /// The number of properties belonging to this classification.
    /// </summary>
    public int PropertyCount
    {
        get; set;
    }
}
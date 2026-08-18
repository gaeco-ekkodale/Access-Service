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

namespace AccessService.Events.AccessRights;

/// <summary>
/// A message to notify that a access right has been created.
/// </summary>
public class CreatedAccessRight
{
    /// <summary>
    /// Gets or sets the unique identifier for the new access right.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the access right.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the guideline classification.
    /// </summary>
    public string GuidelineClassificationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the user group.
    /// </summary>
    public Guid UserGroupId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the identifier of the use case.
    /// </summary>
    public Guid UseCaseId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets or sets the identifier of the guideline classification property.
    /// </summary>
    public string GuidlineClassificationPropertyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the property right.
    /// </summary>
    public PropertyRight PropertyRight { get; set; } = PropertyRight.None;
}
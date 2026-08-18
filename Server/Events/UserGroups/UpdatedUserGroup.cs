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

namespace AccessService.Events.UserGroups;

/// <summary>
/// Represents the event for an updated user group.
/// </summary>
public class UpdatedUserGroup
{
    /// <summary>
    /// Gets or sets the ID of the updated user group.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the new name of the user group.
    /// </summary>
    [Required]
    public string Name { get; set; }
}
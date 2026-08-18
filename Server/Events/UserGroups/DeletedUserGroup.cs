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
/// Represents the event for a deleted user group.
/// </summary>
public class DeletedUserGroup
{
    /// <summary>
    /// Gets or sets the ID of the deleted user group.
    /// </summary>
    [Required]
    public Guid Id { get; set; }
}
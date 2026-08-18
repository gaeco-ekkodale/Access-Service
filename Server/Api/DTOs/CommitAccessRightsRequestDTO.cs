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

namespace AccessService.Api.DTOs;

/// <summary>
/// Represents a request that commits the final access-right state for one use case and user group context.
/// </summary>
public class CommitAccessRightsRequestDTO
{
    /// <summary>
    /// Gets or sets the access rights that should exist after the commit.
    /// </summary>
    [Required]
    public List<CommitAccessRightDTO> AccessRights { get; set; } = [];
}

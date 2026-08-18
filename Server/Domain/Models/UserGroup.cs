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
using System.ComponentModel.DataAnnotations.Schema;

namespace AccessService.Domain.Models;

/// <summary>
/// Represents a user group definition.
/// </summary>
[Table("usergroup")]
public class UserGroup
{
    /// <summary>
    /// Gets or sets the unique identifier for the user group.
    /// </summary>
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the user group.
    /// </summary>
    [Required]
    [MaxLength(150)]
    [Column("name")]
    public string Name { get; set; }
}
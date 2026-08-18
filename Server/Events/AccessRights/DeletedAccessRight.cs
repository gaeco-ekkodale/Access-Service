// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AccessService.Events.AccessRights;

/// <summary>
/// A message to notify that a access right has been deleted.
/// </summary>
public class DeletedAccessRight
{
    /// <summary>
    /// Gets or sets the unique identifier for the access right which got deleted.
    /// </summary>
    public string Id { get; set; } = string.Empty;
}
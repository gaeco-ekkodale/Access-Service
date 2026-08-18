// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AccessService.Api.Events;

/// <summary>
/// Represents an event published by the GuidelineService when a guideline has been deleted,
/// so that downstream services can remove any derived data associated with it.
/// </summary>
public class DeletedGuideline
{
    /// <summary>Gets or sets the unique identifier of the deleted guideline.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the storage bucket where the file was located.</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>Gets or sets the object key (file path) of the deleted file within the storage bucket.</summary>
    public string ObjectKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the deletion occurred.</summary>
    public DateTimeOffset Timestamp { get; set; }
}

// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AccessService.Events.UseCaseGuidelines;

/// <summary>
/// Event published after a UseCase-Guideline has been successfully generated and uploaded to S3.
/// </summary>
public class UploadedUseCaseGuideline
{
    /// <summary>
    /// Gets or sets the ID of the use case this guideline was generated for.
    /// </summary>
    public Guid UseCaseId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the ID of the user group this guideline was generated for.
    /// </summary>
    public Guid UserGroupId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the object name (key) of the uploaded UseCase-Guideline in storage.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ETag of the uploaded object (version identifier).
    /// </summary>
    public string Etag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the storage bucket the UseCase-Guideline was uploaded to.
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation ID for end-to-end tracing.
    /// </summary>
    public Guid CorrelationId
    {
        get; set;
    }

    /// <summary>
    /// Gets or sets the UTC timestamp when the upload completed successfully.
    /// </summary>
    public DateTimeOffset Timestamp
    {
        get; set;
    }
}

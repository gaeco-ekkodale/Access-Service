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
/// Represents an event that is published when a guideline has been successfully uploaded.
/// </summary>
public class UploadedGuideline
{
    /// <summary>Gets or sets the unique identifier of the guideline.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the user-friendly display name of the guideline.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the object key (storage path) used to retrieve the file from MinIO.</summary>
    public string ObjectKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ETag of the uploaded object (version identifier).
    /// </summary>
    public string Etag { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the storage bucket the guideline was uploaded to.
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the correlation ID for end-to-end tracing of this upload.
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

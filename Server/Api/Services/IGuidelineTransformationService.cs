// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AccessService.Api.Events;
using AccessService.Domain.Models;

namespace AccessService.Api.Services;

/// <summary>
/// Defines the contract for processing guideline upload events:
/// loading the guideline from storage, transforming it, and persisting the projection.
/// </summary>
public interface IGuidelineTransformationService
{
    /// <summary>
    /// Processes an uploaded guideline event by loading the file from storage,
    /// transforming it into a relational projection, and persisting it idempotently.
    /// </summary>
    Task ProcessAsync(UploadedGuideline uploadedGuideline, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a deleted guideline event by removing the relational projection
    /// and triggering regeneration of all UseCase-Guidelines.
    /// </summary>
    Task DeleteAsync(DeletedGuideline deletedGuideline, CancellationToken cancellationToken = default);
}

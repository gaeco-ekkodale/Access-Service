// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AccessService.Api.Services;

/// <summary>
/// Generates UseCase-specific Guidelines by filtering the original Guideline
/// based on AccessRights, uploading the result to S3, and publishing a success event.
/// </summary>
public interface IUseCaseGuidelineService
{
    /// <summary>
    /// Generates and uploads a merged UseCase-Guideline for a specific use case and user group.
    /// All active guideline versions that contain matching access rights are merged into one file.
    /// </summary>
    /// <param name="useCaseId">The ID of the use case.</param>
    /// <param name="userGroupId">The ID of the user group.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task GenerateForUserGroupAsync(Guid useCaseId, Guid userGroupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates and uploads UseCase-Guidelines for all (useCaseId, userGroupId) pairs that have access rights.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task GenerateForAllUseCasesAsync(CancellationToken cancellationToken = default);
}

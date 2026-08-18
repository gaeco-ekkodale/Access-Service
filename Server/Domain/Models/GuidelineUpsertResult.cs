// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace AccessService.Domain.Models;

/// <summary>
/// The outcome of a guideline upsert. Carries the domain string keys that were removed
/// during the upsert so callers can clean up orphaned access rights.
/// </summary>
/// <param name="RemovedClassificationIds">
/// ClassificationId strings for classifications that no longer exist in the guideline.
/// All access rights referencing these classification IDs should be deleted.
/// </param>
/// <param name="RemovedClassificationPropertyIds">
/// ClassificationPropertyId strings for properties removed from classifications that still exist.
/// Access rights referencing these property IDs (but whose parent classification still exists)
/// should be deleted.
/// </param>
public record GuidelineUpsertResult(
    IReadOnlyList<string> RemovedClassificationIds,
    IReadOnlyList<string> RemovedClassificationPropertyIds);
